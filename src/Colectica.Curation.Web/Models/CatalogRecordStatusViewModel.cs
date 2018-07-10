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
    public class CatalogRecordStatusViewModel : ICatalogRecordNavigator
    {
        public Data.CatalogRecord CatalogRecord { get; set; }
     
        public List<PipelineStageViewModel> Stages { get; set; }

        public CatalogRecordStatusViewModel()
        {
            Stages = new List<PipelineStageViewModel>();
        }


        public bool IsUserApprover { get; set; }

        public bool IsUserCurator { get; set; }

        public Guid CatalogRecordId
        {
            get { return CatalogRecord.Id; }
        }

        public bool CanAssignCurators
        {
            get { return false; }
        }

        public int CuratorCount { get; set; }

        public int TaskCount { get; set; }
    }

    public class PipelineStageViewModel
    {
        public string Name { get; set; }

        public bool IsComplete { get; set; }

        public List<PipelineStepViewModel> Steps { get; set; }

        public PipelineStageViewModel()
        {
            Steps = new List<PipelineStepViewModel>();
        }
    }

    public class PipelineStepViewModel
    {
        public string Name { get; set; }

        public int CompletedUnits { get; set; }

        public int TotalUnits { get; set; }

        public bool IsComplete { get; set; }

        public string CompletedByUser { get; set; }

        public DateTime CompletedDate { get; set; }

        public string Message { get; set; }

        public List<ManagedFile> Files { get; set; }

        public PipelineStepViewModel()
        {
            Files = new List<ManagedFile>();
        }
    }
}
