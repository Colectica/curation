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

﻿using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Colectica.Curation.Web.Utility
{
    /// <summary>
    /// Represents the abstract base class for composable part catalogs, which collect
    /// and return System.ComponentModel.Composition.Primitives.ComposablePartDefinition
    /// objects. If one of the assemblies in the specified directory fails to load
    /// subsequent files will continue to be loaded.
    /// </summary>
    public class SafeDirectoryCatalog : ComposablePartCatalog
    {
        private readonly AggregateCatalog _catalog;

        ILog logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeDirectoryCatalog"/> class.
        /// </summary>
        /// <param name="directory">The directory from which Addins will be loaded.</param>
        public SafeDirectoryCatalog(string directory)
        {
            logger = LogManager.GetLogger("Curation");

            var files = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories);

            _catalog = new AggregateCatalog();

            foreach (var file in files)
            {
                string fileNameOnly = Path.GetFileName(file);

                try
                {
                    var asmCat = new AssemblyCatalog(file);

                    //Force MEF to load the plugin and figure out if there are any exports
                    // good assemblies will not throw the RTLE exception and can be added to the catalog
                    if (asmCat.Parts.ToList().Count > 0)
                        _catalog.Catalogs.Add(asmCat);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    logger.Warn("Could not load addin " + fileNameOnly + ". " + ex.Message);
                }
                catch (BadImageFormatException ex)
                {
                    logger.Warn("Could not load addin " + fileNameOnly + ". " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Gets the part definitions that are contained in the catalog.
        /// </summary>
        /// <returns>The <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartDefinition" /> 
        /// contained in the 
        /// <see cref="T:System.ComponentModel.Composition.Primitives.ComposablePartCatalog" />.
        /// </returns>
        public override IQueryable<ComposablePartDefinition> Parts
        {
            get { return _catalog.Parts; }
        }
    }
}
