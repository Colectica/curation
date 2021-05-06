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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class FileStatusModel : IUserRights, IFileModel
    {
        public Guid Id
        {
            get { return File.Id; }
        }

        public ManagedFile File { get; set; }

        public string FileName { get; set; }

        public bool IsUserCurator { get; set; }
        public bool IsUserApprover { get; set; }
        public bool IsUserAdmin { get; set; }

        List<FileTaskModel> tasks = new List<FileTaskModel>();
        public List<FileTaskModel> Tasks
        {
            get { return tasks; }
        }
    }

    public class FileTaskModel
    {
        public string TaskType { get; set; }

        public string Controller { get; set; }

        public string CompletedDate { get; set; }

        public string CuratorName { get; set; }

        public string CuratorId { get; set; }

        public string Notes { get; set; }

        public bool IsComplete { get; set; }
    }
}
