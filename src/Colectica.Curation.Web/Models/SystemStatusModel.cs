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

﻿using Colectica.Curation.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class SystemStatusModel
    {
        public List<SystemComponent> Components { get; set; }

        [ImportMany]
        public List<IApplyMetadataUpdatesAction> ApplyMetadataUpdatesActions { get; set; }

        [ImportMany]
        public List<ICreateCatalogRecordAction> CreateCatalogRecordActions { get; set; }

        [ImportMany]
        public List<ICreatePersistentIdentifiersAction> CreatePersistentIdentifiersActions { get; set; }

        [ImportMany]
        public List<ICreatePreservationFormatsAction> CreatePreservationFormatsActions { get; set; }

        [ImportMany]
        public List<IFileAction> FileActions { get; set; }

        [ImportMany]
        public List<IFinalizeMetadataAction> FinalizeMetadataActions { get; set; }

        [ImportMany]
        public List<IPublishAction> PublishActions { get; set; }

        [ImportMany]
        public List<ISubmitForCurationAction> SubmitForCurationActions { get; set; }

        [ImportMany]
        public List<IScriptedTask> TaskAddins { get; set; }

        [ImportMany]
        public List<ICatalogRecordEditor> CatalogRecordEditors { get; set;}

        [ImportMany]
        public List<IManagedFileEditor> ManagedFileEditors { get; set; }

        public SystemStatusModel()
        {
            Components = new List<SystemComponent>();
        }
    }

    public class SystemComponent
    {
        public string Name { get; set; }

        public bool IsWorking { get; set; }

        public string Message { get; set; }
    }

}
