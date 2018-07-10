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

﻿using Colectica.Curation.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Colectica.Curation.Web
{
    public class CurationControllerFactory : DefaultControllerFactory
    {
        public override IController CreateController(System.Web.Routing.RequestContext requestContext, string controllerName)
        {
            // Determine whether the site is configured.
            bool isConfigured = true;

            string path = requestContext.HttpContext.Server.MapPath(SetupController.IsConfiguredMarkerPath);
            if (!System.IO.File.Exists(path))
            {
                isConfigured = false;
            }

            if (WebConfigurationManager.ConnectionStrings.Count == 0)
            {
                isConfigured = false;
            }

            var connectionStringObj = WebConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (connectionStringObj == null)
            {
                isConfigured = false;
            }

            // If the site is not configured, direct to the first run setup page.
            if (!isConfigured)
            {
                requestContext.RouteData.Values["action"] = "FirstRun";
                requestContext.RouteData.Values["controller"] = "Setup";
                return base.CreateController(requestContext, "Setup");
            }

            return base.CreateController(requestContext, controllerName);
        }
    }
}
