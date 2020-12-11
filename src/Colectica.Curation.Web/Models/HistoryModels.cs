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

﻿using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class CatalogRecordHistoryModel : ICatalogRecordNavigator
    {
        public CatalogRecord CatalogRecord { get; set; }

        public List<HistoryEventModel> Events { get; protected set; }

        public bool IsUserCurator { get; set; }

        public bool IsUserApprover { get; set; }

        public bool CanAssignCurators { get; set; }

        public int TaskCount { get; set; }

        public Guid CatalogRecordId
        {
            get { return this.CatalogRecord.Id; }
        }

        public CatalogRecordHistoryModel()
        {
            Events = new List<HistoryEventModel>();
        }

    }

    public class ManagedFileHistoryModel : IUserRights, IFileModel
    {
        public ManagedFile File { get; set; }

        public List<HistoryEventModel> Events { get; protected set; }

        public List<RevisionModel> Revisions { get; protected set; }

        public bool IsUserCurator { get; set; }
        public bool IsUserApprover { get; set; }

        public Guid Id
        {
            get { return File.Id; }
        }
            

        public ManagedFileHistoryModel()
        {
            Events = new List<HistoryEventModel>();
            Revisions = new List<RevisionModel>();
        }
    }

    public class HistoryEventModel
    {
        public DateTime Timestamp { get; set; }
        public Guid EventType { get; set; }
        public string UserName { get; set; }
        public string Title { get; set; }
        public string Details { get; set; }


        public Guid CatalogRecordId { get; set; }
        public string CatalogRecordTitle { get; set; }
        public string CatalogRecordNumber { get; set; }
        public List<RelatedFileModel> RelatedFiles { get; protected set; }

        public HistoryEventModel()
        {
            RelatedFiles = new List<RelatedFileModel>();
        }

        public static HistoryEventModel FromEvent(Event log, ApplicationUser user)
        {
            var model = new HistoryEventModel()
            {
                EventType = log.EventType,
                Timestamp = log.Timestamp,
                Title = log.Title,
                Details = log.Details
            };

            if (user != null)
            {
                model.UserName = user.UserName;
            }

            if (log.RelatedCatalogRecord != null)
            {
                model.CatalogRecordId = log.RelatedCatalogRecord.Id;
                model.CatalogRecordTitle = log.RelatedCatalogRecord.Title;
                model.CatalogRecordNumber = log.RelatedCatalogRecord.Number;
            }

            foreach (var managedFile in log.RelatedManagedFiles)
            {
                var fileModel = new RelatedFileModel();
                fileModel.FileId = managedFile.Id;
                fileModel.FileName = managedFile.Name;
                fileModel.FileNumber = managedFile.Number;
                model.RelatedFiles.Add(fileModel);
            }

            return model;
        }


    }

    public class RevisionModel : RelatedFileModel
    {
        public DateTime Timestamp { get; set; }
        public string CommitterId { get; set; }
        public string CommitterName { get; set; }
        public string CommitterEmail { get; set; }
        public string Message { get; set; }


        public static RevisionModel FromCommit(LibGit2Sharp.Commit commit, LibGit2Sharp.TreeEntry entry, ManagedFile file)
        {
            var model = new RevisionModel()
            {
                CommitterName = commit.Committer.Name,
                //CommitterEmail = commit.Committer.Email,
                Timestamp = commit.Committer.When.DateTime,
                Message = commit.Message,
                FileId = file.Id,
                FileName = entry.Name,
                Commit = commit.Sha,
                Sha = entry.Target.Sha,
                FileSize = ((LibGit2Sharp.Blob)entry.Target).Size
            };

            return model;
        }
    }

    public class RelatedFileModel
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string Commit { get; set; }
        public string Sha { get; set; }
        public string FileNumber { get; set; }
    }
}
