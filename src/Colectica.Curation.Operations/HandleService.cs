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

﻿using Algenta.Colectica.Model.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using YaleIsps.HandleService.YalePersistentLinkingService;

namespace Colectica.Curation.Operations
{
    public class HandleService
    {
        public static bool RequestHandles(string[] ids, string serverName, string groupName, string userName, string password)
        {
            bool isSuccessful = true;

            var binding = CreateBasicHttpBinding();
            var address = new EndpointAddress(serverName);
            var client = new PersistentLinkingClient(binding, address);

            try
            {
                Logger.Instance.Log.Debug("Requesting Handles for " + ids.Length + " items");

                var map = client.createBatch(ids, groupName, userName, password);

                // Handle any failures.
                if (map.failMap.Length > 0)
                {
                    isSuccessful = false;
                    foreach (var item in map.failMap)
                    {
                        string msg = string.Format("{0}: {1}", item.key, item.value);
                        this.messages.Add(msg);
                        Logger.Instance.Log.Warn(msg);
                    }
                }

                //// Assign the Handles to the CatalogRecord and files.
                foreach (var item in map.successMap)
                {
                    string id = item.key;
                    string handle = item.value;

                    if (record.Id.ToString() == id)
                    {
                        record.PersistentId = handle;
                    }
                    else
                    {
                        var file = record.Files.Where(x => x.Id.ToString() == id).FirstOrDefault();
                        if (file != null)
                        {
                            file.PersistentLink = handle;
                            file.PersistentLinkDate = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.messages.Add("Could not connect to handle server");
                Logger.Instance.Log.Warn("Could not connect to handle server", ex);
                return false;
            }

            return isSuccessful;
        }

        static BasicHttpBinding CreateBasicHttpBinding()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.AllowCookies = false;
            binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
            binding.OpenTimeout = new TimeSpan(0, 1, 0);
            binding.SendTimeout = new TimeSpan(0, 1, 0);

            //buffer size
            binding.MaxBufferSize = 65536;
            binding.MaxBufferPoolSize = 534288;
            binding.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;

            //quotas
            binding.ReaderQuotas.MaxDepth = 32;
            binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;

            return binding;
        }
    }
}
