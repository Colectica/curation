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
using System.Threading.Tasks;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;

namespace Colectica.Curation.Web.Utility
{
    public class ManagedFileChangeDetector
    {
        public static string GetChangeSummary(ManagedFile file, FileViewModel model)
        {
            var builder = new StringBuilder();

            CheckProperty("Public Name", file.PublicName, model.PublicName, builder);
            CheckProperty("Type", file.Type, model.Type, builder);
            CheckProperty("Data Type", file.KindOfData, model.KindOfData, builder);
            CheckProperty("Software", file.Software, model.Software, builder);
            CheckProperty("Software Version", file.SoftwareVersion, model.SoftwareVersion, builder);
            CheckProperty("Hardware", file.Hardware, model.Hardware, builder);
            CheckProperty("Source", file.Source, model.Source, builder);
            CheckProperty("Source Information", file.SourceInformation, model.SourceInformation, builder);
            CheckProperty("Rights", file.Rights, model.Rights, builder);
            CheckProperty("Public Access", file.IsPublicAccess ? "Yes" : "No", model.IsPublicAccess.ToString(), builder);

            return builder.ToString();
        }

        static void CheckProperty(string propertyName, string oldValue, string newValue, StringBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(oldValue) &&
                string.IsNullOrWhiteSpace(newValue))
            {
                return;
            }

            if (oldValue != newValue)
            {
                string oldStr = string.IsNullOrWhiteSpace(oldValue) ? "blank" : oldValue;
                string newStr = string.IsNullOrWhiteSpace(newValue) ? "blank" : newValue;
                builder.AppendLine(string.Format("{0} changed from {1} to {2}", propertyName, oldStr, newStr));
            }
        }
    }
}
