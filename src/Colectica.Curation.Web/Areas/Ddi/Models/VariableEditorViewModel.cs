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

﻿using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Colectica.Curation.Addins.Editors.Models
{
    public class VariableEditorViewModel : IUserRights, IFileModel
    {
        public ManagedFile File { get; set; }

        public List<VariableModel> Variables { get; set; }

        public string VariablesJson { get; set; }

        public bool IsLocked { get; set; }

        public bool IsUserCurator { get; set; }
        public bool IsUserApprover { get; set; }

        public string OperationStatus { get; set; }

        public Guid Id
        {
            get { return File.Id; }
        }

        public StringBuilder Message { get; } = new StringBuilder();

        public VariableEditorViewModel(ManagedFile file)
        {
            File = file;

            Variables = new List<VariableModel>();
        }
    }

    public class VariableModel
    {
        public string Id { get; set; }
        public string Agency { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public long Version { get; set; }
        public string LastUpdated { get; set; }
        public string Description { get; set; }
        public string ResponseUnit { get; set; }
        public string AnalysisUnit { get; set; }
        public string ClassificationLevel { get; set; }
        public string RepresentationType { get; set; }

        public List<FrequencyModel> Frequencies { get; set; }

        public double? Valid { get; set; }

        public double? Invalid { get; set; }

        public double? Minimum { get; set; }

        public double? Maximum { get; set; }

        public double? Mean { get; set; }

        public double? StandardDeviation { get; set; }

        public List<CommentModel> Comments { get; set; }

        public bool IsLabelOk { get; set; }

        public VariableModel()
        {
            Frequencies = new List<FrequencyModel>();
            Comments = new List<CommentModel>();
        }
    }

    public class FrequencyModel
    {
        public Guid CategoryId { get; set; }
        public string CategoryValue { get; set; }
        public string CategoryLabel { get; set; }
        public double Frequency { get; set; }
        public string NormalizedWidth { get; set; }
    }

    public class CommentModel
    {
        public string UserName { get; set; }

        public string Date { get; set; }

        public string Comment { get; set; }
    }
}
