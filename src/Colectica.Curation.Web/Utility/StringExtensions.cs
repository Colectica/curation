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
using System.Web;

namespace Colectica.Curation.Web.Utility
{
    public static class StringExtensions
    {
        public static string BytesToSize(this long? bytes)
        {
            if (bytes.HasValue)
            {
                return BytesToSize(bytes.Value);
            }

            return string.Empty;
        }

        public static string BytesToSize(this long bytes)
        {
            long kilobyte = 1024;
            long megabyte = kilobyte * 1024;
            long gigabyte = megabyte * 1024;
            long terabyte = gigabyte * 1024;

            if ((bytes >= 0) && (bytes < kilobyte))
            {
                return bytes.ToString() + " B";

            }
            else if ((bytes >= kilobyte) && (bytes < megabyte))
            {
                return Math.Round( (decimal)(bytes / kilobyte), 2) + " KB";

            }
            else if ((bytes >= megabyte) && (bytes < gigabyte))
            {
                return Math.Round((decimal)(bytes / megabyte), 2) + " MB";

            }
            else if ((bytes >= gigabyte) && (bytes < terabyte))
            {
                return Math.Round((decimal)(bytes / gigabyte), 2) + " GB";

            }
            else if (bytes >= terabyte)
            {
                return Math.Round((decimal)(bytes / terabyte), 2) + " TB";

            }
            else
            {
                return bytes + " B";
            }

        }

    }
}
