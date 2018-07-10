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

﻿using AutoMapper;
using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using Microsoft.Owin;
using Owin;
using System;
using System.Data.Entity;
using System.IO;

#if ISPRO
using Spss.Data;
#endif

[assembly: OwinStartupAttribute(typeof(Colectica.Curation.Web.Startup))]
namespace Colectica.Curation.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            using (var db = ApplicationDbContext.Create())
            {
                db.Database.CreateIfNotExists();
            }

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ApplicationDbContext, Colectica.Curation.Data.Migrations.Configuration>());

#if ISPRO
            // Set SPSS path.
            string spssPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
            SpssRaw.Instance.Initialize(spssPath);
#endif


            Mapper.CreateMap<CatalogRecordGeneralViewModel, CatalogRecord>()
                .ForMember(x => x.Organization, opt => opt.Ignore())
                .ForMember(x => x.Authors, opt => opt.Ignore())
                .ForMember(x => x.DepositAgreement, opt => opt.Ignore());

            Mapper.CreateMap<CatalogRecordMethodsViewModel, CatalogRecord>()
                .ForMember(x => x.Authors, opt => opt.Ignore())
                .ForMember(x => x.FieldDates, opt => opt.Ignore())
                .ForMember(x => x.StudyTimePeriod, opt => opt.Ignore());


            // Skip read-only properties.
            Mapper.CreateMap<FileViewModel, ManagedFile>()
                .ForMember(x => x.Name, opt => opt.Ignore())
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.PersistentLink, opt => opt.Ignore())
                .ForMember(x => x.PersistentLinkDate, opt => opt.Ignore())
                .ForMember(x => x.Version, opt => opt.Ignore())
                .ForMember(x => x.FormatName, opt => opt.Ignore())
                .ForMember(x => x.FormatId, opt => opt.Ignore())
                .ForMember(x => x.Size, opt => opt.Ignore())
                .ForMember(x => x.Checksum, opt => opt.Ignore())
                .ForMember(x => x.ChecksumMethod, opt => opt.Ignore())
                .ForMember(x => x.ChecksumDate, opt => opt.Ignore())
                .ForMember(x => x.VirusCheckOutcome, opt => opt.Ignore())
                .ForMember(x => x.VirusCheckMethod, opt => opt.Ignore())
                .ForMember(x => x.VirusCheckDate, opt => opt.Ignore())
                .ForMember(x => x.Owner, opt => opt.Ignore())
                .ForMember(x => x.CreationDate, opt => opt.Ignore())
                .ForMember(x => x.UploadedDate, opt => opt.Ignore())
                .ForMember(x => x.Status, opt => opt.Ignore())
                .ForMember(x => x.AcceptedDate, opt => opt.Ignore());


            Mapper.CreateMap<ManagedFile, FileViewModel>()
                .ForMember(x => x.IsReadOnly, opt => opt.Ignore());
            
            Mapper.CreateMap<CreatePlaceholderUserModel, ApplicationUser>();
            Mapper.CreateMap<ApplicationUser, UserSearchResultModel>();

            Mapper.CreateMap<UserDetailsModel, ApplicationUser>()
                .ForMember(x => x.UserName, opt => opt.Ignore())
                .ForMember(x => x.Organizations, opt => opt.Ignore())
                .ForMember(x => x.Id, opt => opt.Ignore());

            Mapper.CreateMap<ApplicationUser, UserDetailsModel>();

            Mapper.CreateMap<UserEmailPreferencesModel, ApplicationUser>()
                .ForMember(x => x.Id, opt => opt.Ignore());
            Mapper.CreateMap<ApplicationUser, UserEmailPreferencesModel>();


            Mapper.CreateMap<string, DateTime?>().ConvertUsing(new Colectica.Curation.Common.Mappers.NullableDateTimeTypeConverter());
            Mapper.CreateMap<string, DateTime>().ConvertUsing(new Colectica.Curation.Common.Mappers.DateTimeTypeConverter());
            Mapper.CreateMap<string, bool>().ConvertUsing(new Colectica.Curation.Common.Mappers.YesNoBooleanMapper());
        }
    }
}
