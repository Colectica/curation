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

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Colectica.Curation.DdiAddins.Utility;

namespace Colectica.Curation.Web.Utility
{
    public static class DataverseConfigLoader
    {
        public static void LoadConfiguration()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(basePath, "dataverse.json");

            if (!File.Exists(configPath))
            {
                return;
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("dataverse.json", optional: true, reloadOnChange: false)
                .Build();

            var dataverseSection = configuration.GetSection("Dataverse");

            DataverseSettings.DataverseUrl = dataverseSection["Url"] ?? "";
            DataverseSettings.DataverseName = dataverseSection["DataverseName"] ?? "";
            DataverseSettings.ApiToken = dataverseSection["ApiToken"] ?? "";
            DataverseSettings.PublishedDataDirectory = dataverseSection["PublishedDataDirectory"] ?? "";
            DataverseSettings.DebugDirectory = dataverseSection["DebugDirectory"] ?? "";
        }
    }
}
