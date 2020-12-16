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
using Colectica.Curation.Operations;
using Colectica.Curation.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Web;
using System.ComponentModel.Composition;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using Colectica.Curation.DdiAddins.Utility;

#if ISPRO
using Algenta.Colectica.Repository;
using Algenta.Colectica.Model.Repository;
#endif

namespace Colectica.Curation.Web.Utility
{
    public class SystemStatusChecker
    {
        public SystemStatusModel GetSystemStatus()
        {
            var model = new SystemStatusModel();
            MefConfig.Container.SatisfyImportsOnce(model);

            // Database
            var dbStatus = new SystemComponent() { Name = "Colectica Curation Database" };
            model.Components.Add(dbStatus);

            try
            {
                using (var db = ApplicationDbContext.Create())
                {
                    var testUser = db.Users.FirstOrDefault();
                    dbStatus.IsWorking = true;
                }
            }
            catch (Exception ex)
            {
                dbStatus.IsWorking = false;
                dbStatus.Message = string.Join(". ", ex.FlattenHierarchy().Select(x => x.Message));
            }

            // Curation service
            var serviceStatus = new SystemComponent() { Name = "Colectica Curation Service" };
            model.Components.Add(serviceStatus);

            try
            {
                var curatorService = new ServiceController("Colectica Curation Service");
                serviceStatus.IsWorking = curatorService.Status == ServiceControllerStatus.Running;
                serviceStatus.Message = curatorService.Status.ToString();
            }
            catch (Exception ex)
            {
                serviceStatus.IsWorking = false;
                serviceStatus.Message = ex.Message;
            }

#if ISPRO
            // Repository
            var repoStatus = new SystemComponent() { Name = "Colectica Repository" };
            model.Components.Add(repoStatus);

            try
            {
                var connectionInfo = new RepositoryConnectionInfo()
                {
                    Url = "localhost",
                    AuthenticationMethod = RepositoryAuthenticationMethod.Windows,
                    TransportMethod = RepositoryTransportMethod.NetTcp
                };
                var client = RepositoryHelper.GetClient();
                var testInfo = client.GetRepositoryInfo();
                repoStatus.IsWorking = true;
            }
            catch (Exception ex)
            {
                repoStatus.IsWorking = false;
                repoStatus.Message = ex.Message;
            }
#endif

            // Clam Antivirus
            var clamStatus = new SystemComponent() { Name = "Clam Antivirus" };
            model.Components.Add(clamStatus);

            try
            {
                var clamService = new ServiceController("ClamD");
                clamStatus.IsWorking = clamService.Status == ServiceControllerStatus.Running;
                clamStatus.Message = clamService.Status.ToString();
            }
            catch (Exception ex)
            {
                clamStatus.IsWorking = false;
                clamStatus.Message = ex.Message;
            }

            // Stata
            //var stataStatus = new SystemComponent() { Name = "Stata" };
            //model.Components.Add(stataStatus);

            // R
            var rStatus = new SystemComponent() { Name = "R" };
            model.Components.Add(rStatus);

            try
            {
                //string rMessage = string.Empty;
                //var rImporter = new RDataImporter();
                //rStatus.IsWorking = rImporter.AreSystemRequirementsSatisfied(out rMessage);
            }
            catch (Exception ex)
            {
                rStatus.IsWorking = false;
                rStatus.Message = ex.Message;
            }

            return model;
        }
    }
}
