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

namespace Colectica.Curation.Common.Utility
{
    public static class EventTypes
    {
        public static Guid CreateUser = new Guid("6538758B-FD16-4CA4-B291-F83F2E802AF8");
        public static Guid Upload = new Guid("566382F6-46E4-4435-814A-D54DAD4DCC37");
        public static Guid AcceptFile = new Guid("1E44FF4F-E0AB-4359-91A1-2D12B1AF229F");
        public static Guid RejectFile = new Guid("3A326EF5-7C0C-456F-BC44-1F657E974200");
        public static Guid AcceptTask = new Guid("E6AC5A43-67C6-4EED-9737-D8457EEC4067");
        public static Guid RejectTask = new Guid("E732E8CF-C4D2-477F-A9D9-914403117863");
        public static Guid CreateCatalogRecord = new Guid("D1FC4F78-3A56-4548-B62A-3981D45E6746");
        public static Guid SubmitForCuration = new Guid("3A3B251D-436D-45C1-A823-EABE00CB3527");
        public static Guid EditCatalogRecord = new Guid("F38860D1-D38B-4052-9928-72147727F7CF");
        public static Guid AddNote = new Guid("C85AD21D-3F8F-4F0C-9B90-A3A12DF3CDBA");
        public static Guid EditManagedFile = new Guid("A4E81B37-4458-4572-B463-748C0F0050FA");
        public static Guid GitCommit = new Guid("FF4983BC-335F-450C-88A8-392EA811BB94");
        public static Guid AddOrganizationalPermission = new Guid("547DDE74-508C-4FF1-B2DC-91B52EF76DE2");
        public static Guid AddCatalogRecordPermission = new Guid("4CC57BF7-3229-4B3F-8C37-D1F759806572");
        public static Guid RequestPublication = new Guid("527C4A58-0901-4BA7-8B47-E5D05DE5E420");
        public static Guid ApprovePublication = new Guid("09853948-3C6B-43CD-A2D7-36F9AD66D89E");
        public static Guid RejectPublication = new Guid("EBAF3F07-D53A-40EB-A073-7749C98E5F93");
        public static Guid Publish = new Guid("05913E64-E1C8-4888-9992-676A9FD311FA");
        public static Guid FinalizeCatalogRecordFailed = new Guid("21A7C8A2-5BF8-4706-AA1C-626F24F6666E");
        public static Guid CreatePreservationFormat = new Guid("67BACA1B-E514-4D98-AC55-4A8E496E15AF");
        public static Guid CreatePersistentIdentifier = new Guid("2104E181-0A92-41A4-A1C3-944D1E1C9A23");
        public static Guid GenerateChecksums = new Guid("AC5BF761-45C2-4B3F-A6C8-E42B229ADD5F");
        public static Guid GenerateMetadata = new Guid("39D5CD3A-BEA7-4981-9712-AB38AF384F3E");
        public static Guid CreateArchivePackage = new Guid("52E68847-7FEE-4F19-B1B0-C75D9CC40F47");
        public static Guid RemoveFile = new Guid("1B9554ED-E07A-4513-8E98-B59BA0DD6A07");
        public static Guid ApplyMetadataUpdates = new Guid("CE1D9641-1778-4747-9162-57A7A55BAFC1");
    }
}
