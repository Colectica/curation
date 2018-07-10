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
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Colectica.Curation.Operations
{
    public class MefConfig
    {
        public static CompositionContainer Container { get; set; }

        public static AddinManager AddinManager { get; set; }

        public static void RegisterMef(string addinsPath, params Assembly[] addinAssemblies)
        {
            AddinManager = new AddinManager();

            var aggregateCatalog = new AggregateCatalog();

            foreach (var assembly in addinAssemblies)
            {
                var catalog = new AssemblyCatalog(assembly);
                AddinManager.Assemblies.Add(assembly);
                aggregateCatalog.Catalogs.Add(catalog);
            }

            Container = new CompositionContainer(aggregateCatalog);

            Container.SatisfyImportsOnce(AddinManager);
        }
    }

    public class AddinManager
    {
        public List<Assembly> Assemblies { get; protected set; }

        [ImportMany]
        public List<IScriptedTask> AllTasks { get; set; }

        [ImportMany]
        public List<ICatalogRecordEditor> AllCatalogEditors { get; set; }

        [ImportMany]
        public List<IManagedFileEditor> AllFileEditors { get; set; }

        [ImportMany]
        public List<ICreateCatalogRecordAction> AllCatalogRecordInitializers { get; set; }


        public AddinManager()
        {
            Assemblies = new List<Assembly>();
        }
    }

}
