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

﻿using Colectica.Curation.Addins.Editors.Utility;
using Colectica.Curation.Web.Utility;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Repository;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Algenta.Colectica.Model;
using System.Xml.Linq;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Ddi.Serialization;
using Algenta.Colectica.Model.Utility;
using Colectica.Curation.ViewModel.Utility;
using Colectica.Curation.Common.Mappers;
using Colectica.Curation.DdiAddins.Utility;
using log4net;

namespace Colectica.Curation.Addins.Editors.Mappers
{
    public class CurationToDdiMapper
    {
        public static void UpdateRepositoryItemFromModel(CatalogRecord record)
        {
            var client = RepositoryHelper.GetClient();

            var logger = LogManager.GetLogger("Curation");

            long version = 0;
            try
            {
                version = client.GetLatestVersionNumber(record.Id, record.Organization.AgencyID);
            }
            catch
            {
                // StudyUnit does not exist yet. That is okay, because we are making one from
                // scratch anyway.
                logger.Debug("StudyUnit does not yet exist in the repository");
            }

            var study = new StudyUnit()
            {
                Identifier = record.Id,
                AgencyId = record.Organization.AgencyID,
                Version = version + 1
            };

            var studyMapper = new CatalogRecordToStudyUnitMapper();
            studyMapper.Map(record, study);

            // Add each file as either a PhysicalInstance or an OtherMaterial.
            foreach (var file in record.Files.Where(x => x.Status != FileStatus.Removed))
            {
                if (file.IsStatisticalDataFile())
                {
                    try
                    {
                        var pi = client.GetLatestItem(file.Id, record.Organization.AgencyID)
                            as PhysicalInstance;
                        if (pi != null)
                        {
                            var mapper = new ManagedFileToPhysicalInstanceMapper();
                            mapper.Map(file, pi);

                            pi.Version++;

                            // Make sure the file is added to the StudyUnit.
                            if (!study.PhysicalInstances.Any(x => x.Identifier == pi.Identifier))
                            {
                                study.PhysicalInstances.Add(pi);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Could not attach PhysicalInstance to StudyUnit", ex);
                    }
                }
                else
                {
                    var material = study.OtherMaterials.Where(x => x.DublinCoreMetadata.Title.Current == file.PublicName)
                        .FirstOrDefault();

                    // If this material is not already on the study, add it.
                    if (material == null)
                    {
                        material = new OtherMaterial();
                        study.OtherMaterials.Add(material);
                    }

                    // Perform the mapping.
                    var mapper = new ManagedFileToOtherMaterialMapper();
                    mapper.Map(file, material);
                    material.Version++;
                }
            }

            // Register the updated item with the repository.
            var commitOptions = new CommitOptions();

            var dirty = new DirtyItemGatherer();
            study.Accept(dirty);

            foreach (var dirtyItem in dirty.DirtyItems)
            {
                client.RegisterItem(dirtyItem, commitOptions);
            }
        }



        public static void SaveCatalogRecordXml(CatalogRecord record, string ddiFilePath)
        {
            var study = GetStudyUnit(record);

            var client = RepositoryHelper.GetClient();
            var populator = new SetPopulator(client);
            study.Accept(populator);

            var serializer = new Ddi33Serializer();
            serializer.UseConciseBoundedDescription = true;
            serializer.SerializeFragments(ddiFilePath, study);
        }

        static StudyUnit GetStudyUnit(CatalogRecord record)
        {
            var client = RepositoryHelper.GetClient();

            var study = client.GetLatestItem(record.Id, record.Organization.AgencyID, Algenta.Colectica.Model.Repository.ChildReferenceProcessing.PopulateLatest)
                as StudyUnit;

            return study;
        }

    }
}
