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

﻿using Algenta.Colectica.Model.Repository;
using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Addins.Editors.Models
{
    public class NotesModel : ICatalogRecordNavigator, IUserRights, IFileModel
    {
        public CatalogRecord CatalogRecord { get; set; }

        public bool IsLocked { get; set; }

        public string OperationStatus { get; set; }

        public ManagedFile File { get; set; }

        public Collection<UserCommentModel> Comments { get; set; }

        public bool IsUserCurator { get; set;}

        public bool IsUserApprover { get; set; }

        public Guid CatalogRecordId
        {
            get { return this.CatalogRecord.Id; }
        }

        public bool CanAssignCurators
        {
            get { return false; }
        }

        public Guid Id { get; set; }

        public int TaskCount { get; set; }

        public NotesModel()
        {
            Comments = new Collection<UserCommentModel>();
        }

    }

    public class UserCommentModel
    {
        public DateTime Timestamp { get; set; }

        public string Text { get; set; }

        public string UserName { get; set; }

        public ManagedFile File { get; set; }

        public string VariableAgency { get; internal set; }
        public Guid? VariableId { get; internal set; }
        public string VariableName { get; set; }
    }

}
