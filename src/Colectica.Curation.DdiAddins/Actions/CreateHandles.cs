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

﻿using Colectica.Curation.Common.Utility;
using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(ICreatePersistentIdentifiersAction))]
    public class CreateHandles : ICreatePersistentIdentifiersAction
    {
        ILog logger;

        public string Name { get { return "Create Handles"; } }

        public CreateHandles()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public CreatePersisitentIdentifiersResult CreatePersistentIdentifiers(CatalogRecord record, ApplicationUser user, ApplicationDbContext db)
        {
            var result = new CreatePersisitentIdentifiersResult();
            result.Successful = true;

            if (string.IsNullOrWhiteSpace(record.Number))
            {
                logger.Info("Skipping Handle request for catalog record without a number: " + record.Title);
                result.Skipped = true;
                return result;
            }

            var org = record.Organization;
            bool isDev = org.HandleServerEndpoint.Contains("linktest");

            if (string.IsNullOrWhiteSpace(org.HandleServerEndpoint))
            {
                result.Skipped = true;
                return result;
            }

            // Determine which items need handles and make a list of their IDs.

            // Determine the Drupal URL to point to, based on whether this is a test environment or not.
            string drupalHostName = "isps.yale.edu";
            if (isDev)
            {
                drupalHostName = "dev.isps.yale.edu";
            }

            // Handle for the CatalogRecord.
            var handleRequests = new List<HandleRequestInformation>();
            if (string.IsNullOrWhiteSpace(record.PersistentId))
            {
                var req = new HandleRequestInformation
                {
                    Id = record.Id,
                    Url = $"https://{drupalHostName}/research/data/{record.Number.ToLower()}"
                };
                handleRequests.Add(req);
            }

            // Handle for each ManagedFile.
            foreach (var file in record.Files)
            {
                // Don't request handles for 
                //   * Removed files
                //   * Files that already have a persistent identifier
                //   * Non-public access files.
                if (file.Status == FileStatus.Removed ||
                    !string.IsNullOrWhiteSpace(file.PersistentLink) ||
                    !file.IsPublicAccess)
                {
                    continue;
                }

                var req = new HandleRequestInformation
                {
                    Id = file.Id,
                    Url = $"http://{record.Organization.Hostname}/File/Download/{file.Id}"
                };
                handleRequests.Add(req);
            }

            // Handle for the (to-be-created) DDI file.
            result.DdiFileId = Guid.NewGuid();
            handleRequests.Add(new HandleRequestInformation
            {
                Id = result.DdiFileId,
                Url = $"http://{record.Organization.Hostname}/File/Download/{result.DdiFileId}"
            });
            string[] valuesToRequest = handleRequests.Select(x => x.Url).ToArray();

            // Request the Handles.
            try
            {
                logger.Debug("Requesting Handles for " + handleRequests.Count + " items");
                logger.Debug("Handle request is for " + string.Join(", ", valuesToRequest));

                var binding = CreateBasicHttpBinding();
                var address = new EndpointAddress(org.HandleServerEndpoint);
                

                // Try the new service contract, first.
                List<KeyValuePair<string, string>> failMap = null;
                List<KeyValuePair<string, string>> successMap = null;
                try
                {
                    var client = new YaleIsps.HandleService.YalePersistentLinkingService3.PersistentLinkingClient(binding, address);
                    var map = client.createBatch(valuesToRequest, org.HandleGroupName, org.HandleUserName, org.HandlePassword);
                    failMap = map.failMap.Select(x => new KeyValuePair<string, string>(x.key, x.value)).ToList();
                    successMap = map.successMap.Select(x => new KeyValuePair<string, string>(x.key, x.value)).ToList();
                    logger.Debug("First Handle request succeeded");
                }
                catch (Exception ex)
                {
                    // If this failed, try the old service contract.
                    logger.Warn("First Handle request failed. Trying the other service contract.", ex);
                    try
                    {
                        var client = new YaleIsps.HandleService.YalePersistentLinkingService.PersistentLinkingClient(binding, address);
                        var map = client.createBatch(valuesToRequest, org.HandleGroupName, org.HandleUserName, org.HandlePassword);
                        failMap = map.failMap.Select(x => new KeyValuePair<string, string>(x.key, x.value)).ToList();
                        successMap = map.successMap.Select(x => new KeyValuePair<string, string>(x.key, x.value)).ToList();
                        logger.Debug("Second Handle request succeeded");
                    }
                    catch (Exception ex2)
                    {
                        logger.Warn("Second Handle request failed. Trying the other service contract.", ex2);
                    }
                }



                // Handle any failures.
                if (failMap.Count > 0)
                {
                    result.Successful = false;
                    foreach (KeyValuePair<string, string> item in failMap)
                    {
                        string msg = $"{item.Key}: {item.Value}";
                        result.Messages.Add(msg);
                        logger.Warn(msg);
                    }
                }

                // Assign the Handles to the CatalogRecord and files.
                foreach (KeyValuePair<string, string> item in successMap)
                {
                    string handle = "http://hdl.handle.net/" + item.Key;
                    string url = item.Value;

                    logger.Debug("Assigning Handle for " + handle + ", " + url);

                    Guid idForHandle = handleRequests
                        .Where(x => x.Url == url)
                        .Select(x => x.Id)
                        .FirstOrDefault();

                    // Is this Handle for CatalogRecord?
                    if (record.Id == idForHandle)
                    {
                        record.PersistentId = handle;
                        logger.Debug("Assigning handle to record");
                    }
                    else if (result.DdiFileId == idForHandle)
                    {
                        // How about for the DDI file?
                        result.DdiFileHandle = handle;
                        logger.Debug("Assigning handle to DDI file");
                    }
                    else
                    {
                        // Find which file this Handle is for.
                        var file = record.Files.Where(x => x.Id == idForHandle).FirstOrDefault();
                        if (file != null)
                        {
                            file.PersistentLink = handle;
                            file.PersistentLinkDate = DateTime.UtcNow;
                            logger.Debug("Assigning handle to file: " + file.Name);
                        }
                        else
                        {
                            logger.Warn($"Could not find object to assign Handle to: {handle} -> {url}");
                        }
                    }
                }

                EventService.LogEvent(record, user, db, EventTypes.CreatePersistentIdentifier, "Created Persistent Identifiers");
            }
            catch (Exception ex)
            {
                result.Successful = false;
                result.Messages.Add("Could not connect to handle server");
                logger.Warn("Could not connect to handle server", ex);
            }

            return result;
        }

        BasicHttpBinding CreateBasicHttpBinding()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.AllowCookies = false;
            binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
            binding.OpenTimeout = new TimeSpan(0, 1, 0);
            binding.SendTimeout = new TimeSpan(0, 1, 0);
            binding.Security.Mode = BasicHttpSecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;
            binding.Security.Transport.Realm = "";
            binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.Certificate;

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

    public class HandleRequestInformation
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
    }
}
