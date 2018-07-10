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

﻿using Algenta.Colectica.Model.Ddi;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colectica.Curation.ViewModel.Utility;
using Colectica.Curation.Addins.Editors.Utility;

namespace Colectica.Curation.Common.Mappers
{
    public class ManagedFileToPhysicalInstanceMapper
    {
        public void Map(ManagedFile file, PhysicalInstance pi)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            if (pi == null)
            {
                throw new ArgumentNullException("physicalInstance");
            }


            // Map properties from the ManagedFile to DDI.
            pi.SetUserId("FileNumber", file.Number?.ToString());
            pi.DublinCoreMetadata.Title.Current = file.Title;
            pi.DublinCoreMetadata.AlternateTitle.Current = file.PublicName;

            var fileId = pi.FileIdentifications.FirstOrDefault();
            if (fileId == null)
            {
                fileId = new DataFileIdentification();
                pi.FileIdentifications.Add(fileId);
            }

            Uri uri;
            bool gotUri = Uri.TryCreate(file.PersistentLink, UriKind.RelativeOrAbsolute, out uri);
            if (gotUri)
            {
                fileId.Uri = uri;
            }

            pi.SetUserAttribute("PersistentLinkDate", file.PersistentLinkDate?.ToString());
            pi.SetUserAttribute("FileType", file.Type);
            pi.SetUserAttribute("FormatName", file.FormatName);
            pi.SetUserAttribute("FormatId", file.FormatId);
            pi.SetUserAttribute("Size", file.Size.ToString());

            pi.SetUserAttribute("CreationDate", file.CreationDate);
            pi.SetUserAttribute("KindOfData", file.KindOfData);
            pi.DublinCoreMetadata.Source.Current = file.Source;
            pi.SetUserAttribute("SourceInformation", file.SourceInformation);
            pi.DublinCoreMetadata.Rights.Current = file.Rights;
            fileId.IsPublic = file.IsPublicAccess;
            pi.SetUserAttribute("UploadedDate", file.UploadedDate);
            pi.SetUserAttribute("ExternalDatabase", file.ExternalDatabase);
            pi.SetUserAttribute("Software", file.Software);
            pi.SetUserAttribute("SoftwareVersion", file.SoftwareVersion);
            pi.SetUserAttribute("Hardware", file.Hardware);


            var fingerprint = pi.Fingerprints.FirstOrDefault();
            if (fingerprint == null)
            {
                fingerprint = new Fingerprint();
                pi.Fingerprints.Add(fingerprint);
            }
            fingerprint.FingerprintValue = file.Checksum;
            fingerprint.AlgorithmSpecification = file.ChecksumMethod;
            pi.SetUserAttribute("ChecksumDate", file.ChecksumDate);

            pi.SetUserAttribute("VirusCheckOutcome", file.VirusCheckOutcome);
            pi.SetUserAttribute("VirusCheckMethod", file.VirusCheckMethod);
            pi.SetUserAttribute("VirusCheckDate", file.VirusCheckDate);
            pi.SetUserAttribute("AcceptedDate", file.AcceptedDate);
            pi.SetUserAttribute("CertifiedDate", file.CertifiedDate);
        }
    }
}
