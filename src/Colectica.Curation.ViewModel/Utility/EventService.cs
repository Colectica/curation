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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Common.Utility
{
    public static class EventService
    {
        public static void LogEvent(CatalogRecord record, ApplicationUser user, ApplicationDbContext db, Guid eventType, string title, string details = null)
        {
            var log = new Event()
            {
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                User = user,
                RelatedCatalogRecord = record,
                Title = title,
                Details = details
            };
            db.Events.Add(log);
        }
    }
}
