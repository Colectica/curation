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

﻿using Algenta.Colectica.Model;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colectica.Curation.Addins.Editors.Utility;

namespace Colectica.Curation.Common.Mappers
{
    public class ManagedFileToOtherMaterialMapper
    {

        public void Map(ManagedFile file, OtherMaterial material)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            if (material == null)
            {
                throw new ArgumentNullException("material");
            }

            material.Identifier = file.Id;
            material.SetUserId("FileNumber", file.Number?.ToString());
            material.DublinCoreMetadata.Title.Current = file.Title;
            material.DublinCoreMetadata.AlternateTitle.Current = file.PublicName;

            Uri uri = null;
            bool gotUri = Uri.TryCreate(file.PersistentLink, UriKind.RelativeOrAbsolute, out uri);
            if (gotUri)
            {
                material.UrlReference = uri;
            }

            material.SetUserId("PersistentLinkDate", file.PersistentLinkDate?.ToString());
            material.SetUserId("Version", file.Version.ToString());
            material.SetUserId("Type", file.Type);
            material.SetUserId("FormatName", file.FormatName);
            material.SetUserId("FormatId", file.FormatId);
            material.SetUserId("Size", file.Size.ToString());
            material.SetUserId("CreationDate", file.CreationDate?.ToString());
            material.SetUserId("KindOfData", file.KindOfData);
            material.DublinCoreMetadata.Source.Current = file.Source;
            material.SetUserId("SourceInformation", file.SourceInformation);

            material.DublinCoreMetadata.Rights.Current = file.Rights;

            material.SetUserId("IsPublicAccess", file.IsPublicAccess.ToString());
            material.SetUserId("UploadedDate", file.UploadedDate?.ToString());
            material.SetUserId("ExternalDatabase", file.ExternalDatabase);
            material.SetUserId("Software", file.Software);
            material.SetUserId("SoftwareVersion", file.SoftwareVersion);
            material.SetUserId("Hardware", file.Hardware);
            material.SetUserId("Checksum", file.Checksum);
            material.SetUserId("ChecksumMethod", file.ChecksumMethod);
            material.SetUserId("ChecksumDate", file.ChecksumDate?.ToString());
            material.SetUserId("VirusCheckOutcome", file.VirusCheckOutcome);
            material.SetUserId("VirusCheckMethod", file.VirusCheckMethod);
            material.SetUserId("VirusCheckDate", file.VirusCheckDate?.ToString());
            material.SetUserId("AcceptedDate", file.AcceptedDate?.ToString());
            material.SetUserId("CertifiedDate", file.CertifiedDate?.ToString());

        }
    }
}
