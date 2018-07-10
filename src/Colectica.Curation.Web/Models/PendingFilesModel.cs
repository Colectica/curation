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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colectica.Curation.Web.Models
{
    public class PendingFilesModel
    {
        public List<CatalogRecordPendingFilesModel> CatalogRecords { get; set; }

        public string RejectReason { get; set; }
        
        public string RejectMessage { get; set; }

        public PendingFilesModel()
        {
            CatalogRecords = new List<CatalogRecordPendingFilesModel>();
        }
    }

    public class CatalogRecordPendingFilesModel
    {
        public Guid Id { get; set; }

        public string CatalogRecordTitle { get; set; }

        public bool IsLocked { get; set; }

        public List<PendingFile> Files { get; protected set; }

        public CatalogRecordPendingFilesModel()
        {
            Files = new List<PendingFile>();
        }
    }

    public class PendingFile
    {

        public string UploadedDate { get; set; }

        public string OwnerUserName { get; set; }

        public string Size { get; set; }

        public string Name { get; set; }

        public string Title { get; set; }

        public Guid Id { get; set; }

        public bool IsLocked { get; set; }

        public string OperationStatus { get; set; }

        public bool IsChecked { get; set; }

    }
}
