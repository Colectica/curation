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

﻿using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Web.Utility
{
    public static class Extensions
    {
        public static bool IsStatisticalDataFile(this ManagedFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                return false;
            }

            string lower = file.Name.ToLower();

            if (lower.EndsWith(".dta") ||
                lower.EndsWith(".sav") ||
                lower.EndsWith(".csv") ||
                lower.EndsWith(".rdata"))
            {
                return true;
            }

            return false;
        }
        public static bool IsStataDataFile(this ManagedFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                return false;
            }

            string lower = file.Name.ToLower();

            if (lower.EndsWith(".dta"))
            {
                return true;
            }

            return false;
        }


        public static bool IsStatisticalCommandFile(this ManagedFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                return false;
            }

            string lower = file.Name.ToLower();
            if (lower.EndsWith(".do") ||
                lower.EndsWith(".r") ||
                lower.EndsWith(".sps"))
            {
                return true;
            }

            return false;
        }

        public static bool IsTextFile(this ManagedFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                return false;
            }

            string lower = file.Name.ToLower();

            var textFileExtensions = new[]
            {
                ".txt",
                ".do",
                ".sps",
                ".r",
                ".xml"
            };

            foreach (string str in textFileExtensions)
            {
                if (lower.EndsWith(str))
                {
                    return true;
                }
            }

            return false;
        }

        public static string MakeSafeFileName(this string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath);
            string fileName = Path.GetFileName(fullPath);

            // Make sure the file name is safe.
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '-');
            }

            return Path.Combine(directory, fileName);
        }
    }
}
