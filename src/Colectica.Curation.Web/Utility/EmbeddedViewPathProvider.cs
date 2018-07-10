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

﻿using Colectica.Curation.Operations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;

namespace Colectica.Curation.Web.Utility
{
    public class EmbeddedViewPathProvider : VirtualPathProvider
    {
        private bool ResourceFileExists(string virtualPath)
        {
            foreach (var assembly in MefConfig.AddinManager.Assemblies)
            {
                var resourcename = EmbeddedVirtualFile.GetResourceName(virtualPath, assembly);
                var result = resourcename != null && 
                    assembly.GetManifestResourceNames().Contains(resourcename);

                if (result)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool FileExists(string virtualPath)
        {
            return base.FileExists(virtualPath) || 
                ResourceFileExists(virtualPath);
        }


        public override VirtualFile GetFile(string virtualPath)
        {

            if (!base.FileExists(virtualPath))
            {
                return new EmbeddedVirtualFile(virtualPath);
            }
            else
            {
                return base.GetFile(virtualPath);
            }
        }
    }

    public class EmbeddedVirtualFile : VirtualFile
    {
        public EmbeddedVirtualFile(string virtualPath)
            : base(virtualPath)
        {
        }

        internal static string GetResourceName(string virtualPath, Assembly assembly)
        {
            if (!virtualPath.Contains("/Views/"))
            {
                return null;
            }

            string prefix = assembly.GetName().Name + ".Views.";

            var resourcename = virtualPath
                .Substring(virtualPath.IndexOf("Views/"))
                .Replace("Views/", prefix)
                .Replace("/", ".");

            return resourcename;
        }


        public override Stream Open()
        {
            foreach (var assembly in MefConfig.AddinManager.Assemblies)
            {
                var resourcename = GetResourceName(this.VirtualPath, assembly);
                var stream = assembly.GetManifestResourceStream(resourcename);

                if (stream != null)
                {
                    return stream;
                }
            }

            throw new InvalidOperationException("The requested view could not be found");
        }

    }
}
