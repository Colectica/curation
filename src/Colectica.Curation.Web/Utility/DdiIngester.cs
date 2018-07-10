// Copyright 2014 - 2018 Colectica.
// 
// This file is part of the Colectica Curation Tools.
// 
// The Colectica Curation Tools are free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// The Colectica Curation Tools are distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for
// more details.
// 
// You should have received a copy of the GNU Affero General Public License along
// with Colectica Curation Tools. If not, see <https://www.gnu.org/licenses/>.

﻿using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Ddi.Serialization;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Web;
using Colectica.Curation.Addins.Tasks;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using System.IO;
using Colectica.Curation.Operations;
using Algenta.Colectica.Core.Utility;
using System.Data.SqlClient;
using Algenta.Colectica.Model.Utility;
using Colectica.Curation.Common.ViewModels;

namespace Colectica.Curation.Web.Utility
{
    public class DdiIngester
    {
        public static Guid ImportEventType = new Guid("566382F6-46E4-4435-814A-D54DAD4DCC37");

        public void IngestIntoRepository(Group group)
        {
            // Register every DDI item in the repository.
            var client = new LocalRepositoryClient();
            var options = new CommitOptions();

            var gatherer = new DirtyItemGatherer(true);
            group.Accept(gatherer);

            client.RegisterItems(gatherer.DirtyItems, options);
        }

        public void IngestIntoCurator(Group group, HttpRequestBase request, string userName)
        {
            MefConfig.RegisterMef(string.Empty);

            // Load and populate all the StudyUnits in the group.
            using (var db = new ApplicationDbContext())
            {
                var organization = OrganizationHelper.GetOrganizationByHost(request, db);
                string storagePath = SettingsHelper.GetProcessingDirectory(organization, db);
                var user = db.Users.Where(x => x.UserName == userName)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();


                // For every StudyUnit
                int i = 1;
                foreach (StudyUnit study in group.StudyUnits)
                {
                    Console.WriteLine("Study " + i.ToString());

                    // Create a CatalogRecord and add it to the database.
                    var record = new CatalogRecord();
                    record.Id = study.Identifier;
                    record.Organization = organization;

                    // See if we can find a user with the contributor name.
                    // Otherwise just use the current user.
                    string contributorName = study.DublinCoreMetadata.Contributor.Best;
                    string[] parts = contributorName.Split(new char[] { ' ' });
                    if (parts.Length == 2)
                    {
                        string first = parts[0];
                        string last = parts[1];

                        var contribUser = db.Users.FirstOrDefault(x => x.FirstName == first &&
                            x.LastName == last);
                        if (contribUser != null)
                        {
                            record.CreatedBy = contribUser;
                        }
                    }

                    if (record.CreatedBy == null)
                    {
                        record.CreatedBy = user;
                    }

                    record.CreatedDate = DateTime.UtcNow;
                    record.Version = 1;
                    record.LastUpdatedDate = DateTime.UtcNow;
                    record.Status = CatalogRecordStatus.New;
                    record.DepositAgreement = organization.DepositAgreement;

                    var log = new Event()
                    {
                        EventType = ImportEventType,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = record,
                        Title = "Import"
                    };

                    record.Title = FixString(study.DublinCoreMetadata.Title.Current);
                    if (string.IsNullOrWhiteSpace(record.Title))
                    {
                        record.Title = "Missing title";
                    }

                    Directory.CreateDirectory(Path.Combine(storagePath, record.Id.ToString()));

                    record.Keywords = study.Coverage.TopicalCoverage.Subjects.Select(x => x.Value).FirstOrDefault();
                    record.AccessStatement = FixString(study.DublinCoreMetadata.Rights.Current);
                    record.AuthorsText = study.DublinCoreMetadata.Creator.Current;
                    record.ResearchDesign = study.DataCollections.First().Methodology.Methodology.First().Description.Best;
                    record.Location = study.Coverage.SpatialCoverage.Description.Current;
                    record.InclusionExclusionCriteria = study.DataCollections.First().Methodology.SamplingProcedure.First().Description.Best;
                    record.UnitOfObservation = study.AnalysisUnit.Value;
                    record.DataSource = study.DublinCoreMetadata.Source.Current;
                    record.ArchiveDate = study.DublinCoreMetadata.Date;
                    record.Number = study.DublinCoreMetadata.Identifiers.First().Identifier;

                    if (study.Coverage.TemporalCoverage.Dates.Count != 0)
                    {
                        var date = study.Coverage.TemporalCoverage.Dates[0];

                        var dateModel = new DateModel();
                        dateModel.dateType = "Date";
                        dateModel.date = date.Date.DateOnly.Value.ToString("yyyy-MM-dd");
                        dateModel.isRange = false;
                        record.FieldDates = Newtonsoft.Json.JsonConvert.SerializeObject(dateModel);
                    }

                    record.OwnerText = study.DublinCoreMetadata.Publisher.Current;

                    List<string> kindofdata = new List<string>();
                    foreach(var data in study.KindsOfData)
                    {
                        kindofdata.Add(data.Value);
                    }

                    record.DataType = string.Join(",",kindofdata.ToArray());
                    //record.IspsID = study.DublinCoreMetadata.Identifiers;
                    //record.DataSourceInformation = 
                    record.DataSourceInformation = GetUserAttribute(study, "DataSourceInformation");
                    record.SampleSize = GetUserAttribute(study, "SampleSize");
                    record.RandomizationProcedure = GetUserAttribute(study, "RandomizationProcedure");
                    record.Treatment = GetUserAttribute(study, "Treatment");
                    record.TreatmentAdministration = GetUserAttribute(study, "TreatmentAdministration");
                    record.OutcomeMeasures = GetUserAttribute(study, "OutcomeMeasures");
                    //record.OwnerContact = 
                    record.RelatedDatabase = GetUserAttribute(study, "RelatedDatabase");
                    //record.PrincipalInvestigator = enumerator.Current.Value.Value;
                    //record.AreaOfStudy = GetUserAttribute(study, "AreaOfStudy");
                    //record.Discipline= 
                    //record.FeatureImage = 
                    //record.FeatureText = 
                    record.LocationDetails = GetUserAttribute(study, "LocationDetails");
                    //record.DrupalNodeID = 
                    record.RelatedProjects= GetUserAttribute(study, "RelatedProjects");
                    record.RelatedPublications= GetUserAttribute(study, "RelatedPublications");

                    db.CatalogRecords.Add(record);

                    CreateTaskStatuses(record, user, db);

                    // For every PhysicalInstance in the StudyUnit
                    foreach (var pi in study.PhysicalInstances)
                    {
                        // Create a ManagedFile in the curation system.
                        var file = new ManagedFile();
                        file.Id = pi.Identifier;
                        file.CatalogRecord = record;
                        file.Number = GetUserAttribute(pi, "DataFileNumber");
                        file.Version = 1;
                        //file.Number = ++record.LastFileNumber;
                        file.CreationDate = DateTime.UtcNow;
                        file.Status = FileStatus.Accepted;
                        file.UploadedDate = DateTime.UtcNow;
                        file.AcceptedDate = DateTime.UtcNow;
                        
                        // Map the Handle to the PersisentLink
                        file.PersistentLink = pi.FileIdentifications
                            .Select(x => x.Uri.ToString())
                            .Where(x => x.Contains("hdl.handle.net"))
                            .FirstOrDefault();

                        file.Name = pi.DublinCoreMetadata.Title.Best;
                        if (string.IsNullOrWhiteSpace(file.Name))
                        {
                            file.Name = "Unnamed file";
                        }

                        file.Title = pi.Description.Best;
                        if (string.IsNullOrWhiteSpace(file.Title))
                        {
                            file.Title = "Untitled";
                        }
                        file.PublicName = file.Title;
                        file.FormatName = pi.Format;
                        file.IsPublicAccess = pi.FileIdentifications
                            .Select(x => x.IsPublic)
                            .FirstOrDefault();

                        file.Owner = user;
                        var temp = GetSize(pi);
                        //file.Size = Convert.ToInt64(GetUserAttribute(pi, "FileSize").Split('.')[0]);
                        file.Size = temp;
                        file.Type = "Data";

                        file.Software = MetadataHelpers.GetSoftware(file.Name);
                        
                        db.Files.Add(file);

                        TaskHelpers.AddProcessingTasksForFile(file, record, db);

                        log.RelatedManagedFiles.Add(file);

                        // TODO Ingest all the files into the processing space.
                        // For now, write a dummy file.
                        //string dummyPath = Path.Combine(storagePath, record.Id.ToString(), file.Name);
                        //File.WriteAllText(dummyPath, "This is a placeholder file without real content.");
                    }

                    // For every OtherMaterial in the StudyUnit
                    foreach (var material in study.OtherMaterials)
                    {
                        // Create a ManagedFile in the curation system.
                        var file = new ManagedFile();
                        file.CatalogRecord = record;
                        file.Id = Guid.NewGuid();
                        file.Status = FileStatus.Accepted;
                        file.UploadedDate = DateTime.UtcNow; // TODO get the historical date

                        if (material.UrlReference != null)
                        {
                            file.PersistentLink = material.UrlReference.ToString();
                        }

                        file.Name = material.DublinCoreMetadata.Title.Best;
                        if (string.IsNullOrWhiteSpace(file.Name))
                        {
                            file.Name = "Unnamed file";
                        }

                        file.Title = material.DublinCoreMetadata.Description.Best;
                        if (string.IsNullOrWhiteSpace(file.Title))
                        {
                            file.Title = "Unnamed";
                        }

                        file.Owner = user;
                        //file.Size = GetSize(pi);
                        file.Type = "Other";

                        file.Number = material.DublinCoreMetadata.Identifiers.Select(x => x.Identifier).FirstOrDefault();
                        file.FormatName = material.MimeType;
                        file.IsPublicAccess = material.DublinCoreMetadata.Rights.Best != "Not Public";

                        long size = 0;
                        string sizeStr = material.UserIds
                            .Where(x => x.Type == "Size")
                            .Select(x => x.Identifier)
                            .FirstOrDefault();
                        if (long.TryParse(sizeStr, out size))
                        {
                            file.Size = size;
                        }
                        
                        db.Files.Add(file);

                        TaskHelpers.AddProcessingTasksForFile(file, record, db);

                        // TODO Ingest all the files into the processing space.
                        // For now, write a dummy file.
                        //string dummyPath = Path.Combine(storagePath, record.Id.ToString(), file.Name);
                        //File.WriteAllText(dummyPath, "This is a placeholder file without real content.");
                    }

                    db.Events.Add(log);

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    i++;
                }
            }
        }

        public void CreateTaskStatuses(CatalogRecord record, ApplicationUser user, ApplicationDbContext db)
        {
            int i = 1;

            Console.WriteLine("Creating tasks for " + i.ToString());

            // Create the basic curation steps for the record.
            db.TaskStatuses.Add(GetTaskStatus(db, user, record, 100, "Created", "Collection", true));
            db.TaskStatuses.Add(GetTaskStatus(db, user, record, 200, "Accepted", "Collection", true));
            db.TaskStatuses.Add(GetTaskStatus(db, user, record, 10000, "Request Publication", "Publish", false));
            db.TaskStatuses.Add(GetTaskStatus(db, user, record, 11000, "Publication Approved", "Publish", false));
            db.TaskStatuses.Add(GetTaskStatus(db, user, record, 12000, "Published", "Publish", false));
            db.TaskStatuses.Add(GetTaskStatus(db, user, record, 13000, "Archived", "Archive", false));

            i++;
        }

        string FixString(string str)
        {
            if (str == null)
            {
                return "";
            }

            str = str
                .Replace("amp;amp;amp;amp;", string.Empty)
                .Replace("&amp;", "&");
            return Algenta.Colectica.Core.Utility.HttpUtility.HtmlDecode(str);
        }

        TaskStatus GetTaskStatus(ApplicationDbContext db, ApplicationUser user, CatalogRecord catalogRecord, int weight, string name, string stageName, bool isComplete)
        {
            var status = new TaskStatus()
            {
                Id = Guid.NewGuid(),
                CatalogRecord = catalogRecord,
                Name = name,
                StageName = stageName,
                Weight = weight,
                IsComplete = isComplete
            };

            if (isComplete)
            {
                status.CompletedDate = DateTime.UtcNow;
                status.CompletedBy = user;
            }

            return status;
        }

        public long GetSize(IVersionable item)
        {
            long x = 0;
            long.TryParse(GetUserAttribute(item, "FileSize"), out x);
            return x;
        }

        public static string GetUserAttribute(IVersionable item, string key)
        {
            var attr = item.UserAttributes.Where(x => x.Key != null && x.Key.Value == key).FirstOrDefault();
            if (attr == null)
            {
                return string.Empty;
            }

            if (attr.Value == null)
            {
                return string.Empty;
            }

            return attr.Value.Value;
        }
    }
}
