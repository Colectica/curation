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
using Algenta.Colectica.Model.Ddi;
using Colectica.Curation.ViewModel.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Addins.Editors.Utility
{
    public static class VersionableExensions
    {
        // TODO Use a prefix for the key for the UserAttributes? Or a full-blown CV?

        /// <summary>
        /// Gets the value of the specified UserAttribute.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string GetUserAttribute(this IVersionable item, string key)
        {
            var attr = item.UserAttributes.Where(x => x.Key != null && x.Key.Value == key).FirstOrDefault();
            if (attr == null)
            {
                return string.Empty;
            }

            if (attr.Value == null)
            {
                return string.Empty;
            }

            return attr.Value.Value;
        }

        /// <summary>
        /// Sets the value of the specified UserAttribute. If the UserAttribute already exists,
        /// the value is updated. If the UserAttribute does not exist, one is created.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetUserAttribute(this IVersionable item, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            // If the UserAttribute already exists, update it. Otherwise, create a new one.
            var attr = item.UserAttributes.Where(x => x.Key != null && x.Key.Value == key).FirstOrDefault();
            if (attr == null)
            {
                attr = new UserAttribute();
                attr.Key = key;
                item.UserAttributes.Add(attr);
            }

            attr.Value = value;
        }

        public static void SetUserAttribute(this IVersionable item, string key, DateTime? date)
        {
            if (date.HasValue)
            {
                item.SetUserAttribute(key, date.Value.ToString());
            }
        }

        /// <summary>
        /// Sets the value of the specified user identifier. If the user identifier already exists,
        /// the value is updated. If the user identifier does not exist, one is created.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="value">The value.</param>
        public static void SetUserId(this IIdentifiable item, string type, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            // If the UserID already exists, update it. Otherwise, create a new one.
            var userID = item.UserIds.Where(x => x.Type == type).FirstOrDefault();
            if (userID == null)
            {
                userID = new UserId();
                userID.Type = type;
                item.UserIds.Add(userID);
            }

            userID.Identifier = value;
        }

        public static string GetMethodology(this StudyUnit study)
        {
            var methodology = GetMethodologyItem(study);
            if (methodology == null)
            {
                return string.Empty;
            }

            if (methodology.Methodology.Count == 0)
            {
                return string.Empty;
            }

            return methodology.Methodology[0].Description.Current;
        }

        public static void SetMethodology(this StudyUnit study, string content)
        {
            var methodology = EnsureMethodologyItemExists(study);

            // Make sure there is a (DataCollection)Methodology item in the Methodology.
            MethodologyItem item = null;
            if (methodology.Methodology.Count == 0)
            {
                item = new MethodologyItem() { AgencyId = study.AgencyId };
                methodology.Methodology.Add(item);
            }
            else
            {
                item = methodology.Methodology[0];
            }

            item.Description.Current = content;
        }

        public static string GetSamplingProcedure(this StudyUnit study)
        {
            var methodology = GetMethodologyItem(study);
            if (methodology == null)
            {
                return string.Empty;
            }

            if (methodology.SamplingProcedure.Count == 0)
            {
                return string.Empty;
            }

            return methodology.SamplingProcedure[0].Description.Current;
        }

        public static void SetSamplingProcedure(this StudyUnit study, string content)
        {
            var methodology = EnsureMethodologyItemExists(study);

            // Make sure there is a (DataCollection)Methodology item in the Methodology.
            MethodologyItem item = null;
            if (methodology.SamplingProcedure.Count == 0)
            {
                item = new MethodologyItem() { AgencyId = study.AgencyId };
                methodology.SamplingProcedure.Add(item);
            }
            else
            {
                item = methodology.SamplingProcedure[0];
            }

            item.Description.Current = content;
        }

        public static void SetModeOfDataCollection(this StudyUnit study, string content)
        {
            var collectionEvent = EnsureCollectionEventExists(study);

            var modeOfCollection = new DataCollectionItem();
            modeOfCollection.Description.Current = content;
            collectionEvent.ModesOfCollection.Add(modeOfCollection);
        }

        public static string GetModeOfDataCollection(this StudyUnit study)
        {
            var dc = study.DataCollections.FirstOrDefault();
            if (dc == null)
            {
                return string.Empty;
            }

            var collectionEvent = dc.CollectionEvents.FirstOrDefault();
            if (collectionEvent == null)
            {
                return string.Empty;
            }

            if (collectionEvent.ModesOfCollection.Count == 0)
            {
                return string.Empty;
            }

            return collectionEvent.ModesOfCollection[0].Description.Current;
        }

        public static string GetDataCollectionDate(this StudyUnit study)
        {
            var dc = study.DataCollections.FirstOrDefault();
            if (dc == null)
            {
                return string.Empty;
            }

            var collectionEvent = dc.CollectionEvents.FirstOrDefault();
            if (collectionEvent == null)
            {
                return string.Empty;
            }

            return GetStringForDates(collectionEvent.DataCollectionDate);
        }

        public static void SetDataCollectionDate(this StudyUnit study, string dtJson)
        {
            var collectionEvent = EnsureCollectionEventExists(study);

            var dateSpec = FormMappingHelper.GetDateFromJson(dtJson);
            collectionEvent.DataCollectionDate = dateSpec;
        }

        static CollectionEvent EnsureCollectionEventExists(StudyUnit study)
        {
            var dc = EnsureDataCollectionExists(study);

            var collectionEvent = dc.CollectionEvents.FirstOrDefault();
            if (collectionEvent == null)
            {
                collectionEvent = new CollectionEvent() { AgencyId = study.AgencyId };
                dc.CollectionEvents.Add(collectionEvent);
            }

            return collectionEvent;
        }

        static DataCollection EnsureDataCollectionExists(StudyUnit study)
        {
            DataCollection dc = null;
            if (study.DataCollections.Count == 0)
            {
                dc = new DataCollection() { AgencyId = study.AgencyId };
                dc.Label.Copy(study.DublinCoreMetadata.Title);
                study.DataCollections.Add(dc);
            }
            else
            {
                dc = study.DataCollections[0];
            }

            return dc;
        }

        public static string GetStringForDates(this ObservableCollection<DateSpecification> datesList)
        {
            // TODO

            return string.Empty;
        }

        public static void SetDatesFromString(this ObservableCollection<DateSpecification> dateList, string dateStr)
        {
            // TODO
        }

        public static string GetStringForDates(DateSpecification dateSpec)
        {
            // TODO

            return string.Empty;
        }

        public static void SetDateFromString(DateSpecification dateSpec, string dateStr)
        {
            // TODO
        }

        static DataCollectionMethodology GetMethodologyItem(StudyUnit study)
        {
            if (study.DataCollections.Count == 0)
            {
                return null;
            }

            var dc = study.DataCollections[0];

            if (dc.Methodology == null)
            {
                return null;
            }

            return dc.Methodology;
        }

        static DataCollectionMethodology EnsureMethodologyItemExists(StudyUnit study)
        {
            // Make sure there is a DataCollection.
            var dc = EnsureDataCollectionExists(study);

            // Make sure there is a Methodology. Why on earth is it versionable?
            DataCollectionMethodology methodology = null;
            if (dc.Methodology == null)
            {
                methodology = new DataCollectionMethodology() { AgencyId = study.AgencyId };
                dc.Methodology = methodology;
            }
            else
            {
                methodology = dc.Methodology;
            }

            return methodology;
        }

    }
}
