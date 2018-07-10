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

using System;
using System.Text;

namespace Colectica.Curation
{
    /// <summary>
    /// Represents information about the version of the software.
    /// </summary>
    /// <remarks>
    /// This class is automatically generated using the subwcrev program.
    /// </remarks>
    public static class RevisionInfo
    {
        /// <summary>
        /// Gets the version number of the software.
        /// </summary>
        public static string Version { get => "1.0.0"; }

        /// <summary>
        /// Gets the build number of the software.
        /// </summary>
        public static string BuildNumber { get => "LOCAL_BUILD"; }

        /// <summary>
        /// Gets the name of the software: "Colectica Curation".
        /// </summary>
        public static string ProgramName { get { return "Colectica Curation Tools"; } }

        /// <summary>
        /// Gets the full version of the software, including the major and minor versions, the revisions, and the tag.
        /// </summary>
        public static string FullVersionString { get => $"{ProgramName} {Version}.{BuildNumber}"; }
    }
}
