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

﻿using Colectica.Curation.Data;
using Colectica.Curation.Operations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Colectica.Curation.Web.Utility
{
    public static class Extensions
    {

        /// <summary>
        /// Obtains a lock on the catalog record to be updated, and enqueues an operation to be performed
        /// </summary>
        /// <param name="record">Catalog Record on which this operation will be performed</param>
        /// <param name="operation">The operation to be performed</param>
        /// <returns>true if the record was locked</returns>
        public static bool Enqueue(this ApplicationDbContext db, Guid catalogRecordId, string userId, IOperation operation)
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead }))
            {
                int updated = 0;
                var catalogRecord = db.CatalogRecords.FirstOrDefault(x => x.Id == catalogRecordId && x.OperationLockId.HasValue == false);
                if (catalogRecord != null)
                {
                    Guid lockId = Guid.NewGuid();
                    catalogRecord.OperationLockId = lockId;
                    catalogRecord.OperationStatus = operation.Name;

                    updated = db.SaveChanges();

                    if (updated != 0)
                    {                        
                        Operation queued = new Operation()
                        {
                            CatalogRecordContext = catalogRecordId,
                            Data = JsonConvert.SerializeObject(operation),
                            LockId = lockId,
                            OperationName = operation.Name,
                            OperationType = operation.OperationType,
                            QueuedOn = DateTime.UtcNow,
                            RequestingUserId = userId,
                            Status = OperationStatus.Queued
                        };
                        db.Operations.Add(queued);                    
                        db.SaveChanges();
                    }
                }
                scope.Complete();
                return updated != 0;
            }
        }

        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static IEnumerable<Exception> FlattenHierarchy(this Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }

            var innerException = ex;
            do
            {
                yield return innerException;
                innerException = innerException.InnerException;
            }
            while (innerException != null);
        }
    }
}
