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

﻿using Colectica.Curation.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Test
{
    [TestClass]
    public class RDataTest
    {
        [TestMethod]
        public void ImportRDataTest()
        {
            var importer = new RDataImporter();
            //var rp = importer.Import(@"D:/sandbox/YaleIspsDataMigrator/data/output/D081/GerberGreenBook_Chapter2_HowToRandomize_7treat2.RData", "int.example");
            var rp = importer.Import(@"D:/sandbox/YaleIspsDataMigrator/data/output/D032/Gerber_et_al_APSR_2011_texasri-0610.rdata", "int.example");

            Assert.IsNotNull(rp);
            Assert.AreEqual(1, rp.PhysicalInstances.Count);
            Assert.AreEqual(1, rp.PhysicalInstances[0].DataRelationships.Count);
            Assert.AreEqual(2, rp.PhysicalInstances[0].DataRelationships[0].LogicalRecords.Count);
            Assert.AreEqual(58, rp.PhysicalInstances[0].DataRelationships[0].LogicalRecords[0].VariablesInRecord.Count);
            Assert.IsNotNull(rp.PhysicalInstances[0].DataRelationships[0].LogicalRecords[0].VariablesInRecord[2].CodeRepresentation);
            Assert.IsNotNull(rp.PhysicalInstances[0].DataRelationships[0].LogicalRecords[0].VariablesInRecord[2].CodeRepresentation.Codes);
            Assert.AreEqual(20, rp.PhysicalInstances[0].DataRelationships[0].LogicalRecords[0].VariablesInRecord[2].CodeRepresentation.Codes.Codes.Count);
            Assert.AreEqual("El Paso", rp.PhysicalInstances[0].DataRelationships[0].LogicalRecords[0].VariablesInRecord[2].CodeRepresentation.Codes.Codes[0].Category.Label.Current);
        }
    }
}
