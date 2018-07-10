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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Colectica.Curation.Operations;
using Colectica.Curation.Data;
using System.Linq;
using Colectica.Curation.Common.ViewModels;
using System.Data.Entity;
using System.ServiceModel;
using YaleIsps.HandleService.YalePersistentLinkingService;

namespace Colectica.Curation.Test
{
    [TestClass]
    public class HandleServiceTest
    {
        [TestMethod]
        public void RequestHandleTest()
        {
            var binding = CreateBasicHttpBinding();
            var address = new EndpointAddress("http://linktest.odai.yale.edu/ypls-ws/PersistentLinking");
            var client = new PersistentLinkingClient(binding, address);

            string[] ids = new string[]
            {
                "http://example.org/test1", "test2", "asdf asdf", "test2", "cb340d03-67a3-47c1-bec5-f25df1d6cd87"
            };

            var result = client.createBatch(ids, "10079.1/ISPS", "10079.1/ISPS", "isps");


            Console.WriteLine("test");
        }

        [TestMethod]
        public void ResolveHandleTest()
        {
            var binding = CreateBasicHttpBinding();
            var address = new EndpointAddress("http://linktest.odai.yale.edu/ypls-ws/PersistentLinking");
            var client = new PersistentLinkingClient(binding, address);

            string[] ids = new string[]
            {
                "10079.1.test", "test2", "test3"
            };

            var result = client.resolveBatch(ids, "TODO", "TODO");

            Console.WriteLine("test");
        }

        BasicHttpBinding CreateBasicHttpBinding()
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

            return binding;
        }
    }
}
