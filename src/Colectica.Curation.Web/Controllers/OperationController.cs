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
using Colectica.Curation.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace Colectica.Curation.Web.Controllers
{
    public class OperationController : CurationControllerBase
    {
        [Route("admin/operations")]
        public ActionResult All()
        {
            return GetOperationsList();
        }

        [Route("admin/operations/incomplete")]
        public ActionResult Incomplete()
        {
            return GetOperationsList(x => x.Where(o => o.Status == OperationStatus.Queued || o.Status == OperationStatus.Working));
        }


        [Route("admin/operations/errors")]
        public ActionResult Errors()
        {
            return GetOperationsList(x => x.Where(o => o.Status == OperationStatus.Error));
        }

        ActionResult GetOperationsList(Func<IQueryable<Operation>, IQueryable<Operation>> filter = null)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                if (!thisUser.IsAdministrator)
                {
                    throw new HttpException(403, "Forbidden");
                }

                var operations = db.Operations
                    .AsQueryable()
                    .Include(x => x.CatalogRecord)
                    .Include(x => x.User);

                if (filter != null)
                {
                    operations = filter(operations);
                }
                operations = operations.OrderBy(x => x.QueuedOn);

                var models = operations
                    .Select(x => new OperationModel()
                    {
                        Id = x.Id,
                        CatalogRecordId = x.CatalogRecord == null ?  Guid.Empty : x.CatalogRecord.Id,
                        CatalogRecordName = x.CatalogRecord == null ? string.Empty : x.CatalogRecord.Title,
                        RequestingUserName = x.User.UserName,
                        Status = x.Status,
                        Name = x.OperationName,
                        QueuedOn = x.QueuedOn,
                        StartedOn = x.StartedOn,
                        CompletedOn = x.CompletedOn
                    });
                
                return View("Index", models.ToList());
            }
        }
    }
}
