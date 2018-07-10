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
using System.Data;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Areas.Ddi.Utility
{
    public class DataValueFrequencyGatherer
    {
        IDataReader reader;
        
        public DataValueFrequencyGatherer(IDataReader reader)
        {
            this.reader = reader;
        }

        /// <summary>
        /// Gets all unique values along with their frequency.
        /// </summary>
        /// <returns>A dictionary where the key is the value and the value is 
        /// the number of times that value appears in the data.</returns>
        public Dictionary<string, int> CountValueFrequencies(int columnIdx)
        {
            var result = new Dictionary<string, int>();

            while (reader.Read())
            {
                if (reader.IsDBNull(columnIdx))
                {
                    // TODO Handle system missing.
                }
                else
                {
                    string value = string.Empty;
                    if (reader.GetFieldType(columnIdx) == typeof(string))
                    {
                        value = reader.GetString(columnIdx);
                    }
                    else
                    {
                        value = reader.GetDouble(columnIdx).ToString();
                    }

                    if (!result.ContainsKey(value))
                    {
                        result.Add(value, 0);
                    }

                    result[value]++;
                }
                
            }

            return result;
        }
    }
}
