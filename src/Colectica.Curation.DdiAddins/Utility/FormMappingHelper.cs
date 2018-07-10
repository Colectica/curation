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

﻿using Algenta.Colectica.Model.Ddi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Colectica.Curation.ViewModel.Utility
{
    public static class FormMappingHelper
    {
        public static void UpdateIfPresent(FormCollection form, string key, Action<string> update)
        {
            if (!form.AllKeys.Contains("name"))
            {
                return;
            }

            var name = form["name"];

            if (name == key)
            {
                string value = string.Empty;
                if (form.AllKeys.Contains("value[]"))
                {
                    value = form["value[]"];
                    update(value);
                }
                else if (form.AllKeys.Contains("value"))
                {
                    value = form["value"];
                    update(value);
                }
            }
        }

        public static DateTime? ToDateTime(this string str)
        {
            DateTime dt = DateTime.UtcNow;
            if (DateTime.TryParse(str, out dt))
            {
                return dt;
            }

            return null;
        }

        public static bool ToBool(this string str)
        {
            bool result = false;
            if (bool.TryParse(str, out result))
            {
                return result;
            }

            return false;
        }

        public static long ToLong(this string str)
        {
            long result = 0;
            if (long.TryParse(str, out result))
            {
                return result;
            }

            return 0;
        }

        public static DateSpecification GetDateFromJson(string dtJson)
        {
            try
            {
                var dateModel = JsonConvert.DeserializeObject<DateJsonModel>(dtJson);

                var dateSpec = new DateSpecification();
                dateSpec.IsRange = dateModel.isRange;

                if (!dateSpec.IsRange)
                {
                    FillDate(dateSpec.Date, dateModel.dateType, dateModel.date);
                }
                else
                {
                    FillDate(dateSpec.DateRange.StartDate, dateModel.dateType, dateModel.date);
                    FillDate(dateSpec.DateRange.EndDate, dateModel.dateType, dateModel.endDate);
                }

                return dateSpec;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void FillDate(Date date, string type, string dateStr)
        {
                if (type == "Date")
                {
                    date.Precision = DatePrecisionType.Date;

                    DateTime dt = new DateTime();
                    if (DateTime.TryParse(dateStr, out dt))
                    {
                        date.DateOnly = dt;
                    }
                }
                else if (type == "Year")
                {
                    date.Precision = DatePrecisionType.Year;

                    int year = 0;
                    if (int.TryParse(dateStr, out year))
                    {
                        date.Year = year;
                    }
                }
                else if (type == "Year/Month")
                {
                    date.Precision = DatePrecisionType.YearMonth;

                    string[] parts = dateStr.Split(new char[] { '-' });
                    if (parts.Length == 2)
                    {
                        int year = 0;
                        int month = 0;

                        bool yes1 = int.TryParse(parts[0], out year);
                        bool yes2 = int.TryParse(parts[1], out month);

                        if (yes1 && yes2)
                        {
                            date.SetYearMonth(year, month);
                        }
                    }
                }
        }
    }

    public class DateJsonModel
    {
        public string dateType { get; set; }
        public string date { get; set; }
        public bool isRange { get; set; }
        public string endDate { get; set; }
    }
}
