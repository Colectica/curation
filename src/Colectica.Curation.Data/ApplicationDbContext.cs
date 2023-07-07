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

﻿using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public virtual IDbSet<Organization> Organizations { get; set; }

        public virtual IDbSet<CatalogRecord> CatalogRecords { get; set; }

        public virtual IDbSet<ManagedFile> Files { get; set; }

        public virtual IDbSet<TaskStatus> TaskStatuses { get; set; }

        public virtual IDbSet<Setting> Settings { get; set; }

        public virtual IDbSet<Operation> Operations { get; set; }

        public virtual IDbSet<Event> Events { get; set; }

        public virtual IDbSet<Permission> Permissions { get; set; }

        public virtual IDbSet<Note> Notes { get; set; }

        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {

        }

        public ApplicationDbContext(string nameOrConnectionString)
        : base(nameOrConnectionString, throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // For information about customizing many-to-many mappings, see:
            //   http://www.ojdevelops.com/2014/01/multiple-many-to-many-associations.html

            modelBuilder.Entity<CatalogRecord>()
                .HasMany(x => x.Authors)
                .WithMany(x => x.AuthorFor)
                .Map(x =>
                {
                    x.ToTable("Authors");
                    x.MapLeftKey("CatalogRecordId");
                    x.MapRightKey("ApplicationUserId");
                });

            modelBuilder.Entity<CatalogRecord>()
                .HasMany(x => x.Curators)
                .WithMany(x => x.CuratorFor)
                .Map(x =>
                {
                    x.ToTable("Curators");
                    x.MapLeftKey("CatalogRecordId");
                    x.MapRightKey("ApplicationUserId");
                });

            modelBuilder.Entity<CatalogRecord>()
                .HasMany(x => x.Approvers)
                .WithMany(x => x.ApproverFor)
                .Map(x =>
                {
                    x.ToTable("Approvers");
                    x.MapLeftKey("CatalogRecordId");
                    x.MapRightKey("ApplicationUserId");
                });
        }
    }
}
