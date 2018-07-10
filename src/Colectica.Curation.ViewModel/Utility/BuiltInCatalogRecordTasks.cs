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
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Utility
{
    public static class BuiltInCatalogRecordTasks
    {
        public static Guid CreateCatalogRecordTaskId = new Guid("F1FDDF16-4EFC-4A40-A306-17AE25083699");
        public static Guid AcceptCatalogRecordTaskId = new Guid("79ECED4C-754C-443C-A735-6B218C564FBB");
        public static Guid RequestCatalogRecordPublicationTaskId = new Guid("AAA32F4F-3A5B-439F-BD2C-897BDFA2257B");
        public static Guid ApproveCatalogRecordPublicationTaskId = new Guid("0BF3858C-7D38-4863-9AD9-99CF03808D51");
        public static Guid PublishCatalogRecordTaskId = new Guid("184BD7B9-C84C-4C99-9B53-3BA03199636A");
        public static Guid ArchiveCatalogRecordTaskId = new Guid("C5649721-430F-4E19-8F4F-200D81C44440");
    }
}
