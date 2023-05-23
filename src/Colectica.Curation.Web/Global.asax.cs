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
using Colectica.Curation.Web.Controllers;
using Colectica.Curation.Web.Utility;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Colectica.Curation.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var logger = LogManager.GetLogger("MvcApplication");
            logger.Info("Entering Application_Start()");
            logger.Info("Version: " + RevisionInfo.FullVersionString);

            string addinsPath = System.IO.Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, 
                "CurationAddins");

            MvcHandler.DisableMvcResponseHeader = true;
            HostingEnvironment.RegisterVirtualPathProvider(new EmbeddedViewPathProvider());

#if ISPRO
            MefConfig.RegisterMef(addinsPath, typeof(MvcApplication).Assembly, typeof(Colectica.Curation.DdiAddins.DdiAddinManifest).Assembly);
#else
            MefConfig.RegisterMef(addinsPath, typeof(MvcApplication).Assembly, typeof(Colectica.Curation.BaseAddins.BaseAddinManifest).Assembly);
#endif

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            ControllerBuilder.Current.SetControllerFactory(new CurationControllerFactory());
        }

        protected void Application_PreSendRequestHeaders()
        {
            //Try really hard to remove these headers
            Response.Headers.Remove("Server");
            Response.Headers.Remove("X-Powered-By");
            Response.Headers.Remove("X-AspNet-Version");
            Response.Headers.Remove("X-AspNetMvc-Version");
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var logger = LogManager.GetLogger("Application_Error");
            logger.Debug("Entering Application_Error()");

            Exception lastError = Server.GetLastError();
            logger.Error("Unhandled error", lastError);

            Server.ClearError();

            int statusCode = 0;

            if (lastError.GetType() == typeof(HttpException))
            {
                statusCode = ((HttpException)lastError).GetHttpCode();
            }
            else
            {
                // Not an HTTP related error so this is a problem in our code, set status to
                // 500 (internal server error)
                statusCode = 500;
            }

            HttpContextWrapper contextWrapper = new HttpContextWrapper(this.Context);

            RouteData routeData = new RouteData();
            routeData.Values.Add("controller", "Error");
            routeData.Values.Add("action", "Index");
            routeData.Values.Add("statusCode", statusCode);
            routeData.Values.Add("exception", lastError);
            routeData.Values.Add("isAjaxRequest", contextWrapper.Request.IsAjaxRequest());

            IController controller = new ErrorController();

            RequestContext requestContext = new RequestContext(contextWrapper, routeData);

            controller.Execute(requestContext);
            Response.End();

            logger.Debug("Leaving Application_Error()");
        }
    }
}
