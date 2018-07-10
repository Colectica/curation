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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Data
{
    public class Operation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Guid LockId { get; set; }
        public OperationStatus Status { get; set; }

        public string ApplicationName { get; set; }
        public string QueueName { get; set; }
        public string OperationName { get; set; }
        public Guid OperationType { get; set; }
        public string Data { get; set; }
        public DateTime QueuedOn { get; set; }
        public DateTime? StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }

        public Guid CatalogRecordContext { get; set; }
        [ForeignKey("CatalogRecordContext")]
        public virtual CatalogRecord CatalogRecord { get; set;}

        public string RequestingUserId { get; set;}

        [ForeignKey("RequestingUserId")]
        public virtual ApplicationUser User { get; set; }

    }

    public enum OperationStatus
    {
        Queued = 0,
        Working = 1,
        Completed = 2,
        Error = 3
    }

}
