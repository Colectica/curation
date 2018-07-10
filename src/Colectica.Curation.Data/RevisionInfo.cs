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
            /// Gets the revision number of the software.
            /// </summary>
            public static int Revision { get{ return 507;} }

            /// <summary>
            /// Gets the revision number of the software as a string.
            /// </summary>
            public static string RevisionString { get{ return "507";} }

            /// <summary>
            /// Gets the date that the software was built.
            /// </summary>
            public static DateTime Date { get{ return DateTime.Parse("2018/02/09 10:29:28");} }

            /// <summary>
            /// Gets the date and time that the software was built.
            /// </summary>
            public static DateTime BuildTime { get{ return DateTime.Parse("2018/02/09 10:46:34");} }

            /// <summary>
            /// Gets the range of range of revisions included in this version.
            /// </summary>
            public static string WorkingCopyRange { get{ return "489:507";} }

            /// <summary>
            /// Gets the name of the software: "Colectica Curation".
            /// </summary>
            public static string ProgramName { get{ return "Colectica Curation";} }
                
            /// <summary>
            /// Gets the version major and minor version numbers of the software, as a string.
            /// </summary>
            public static string VersionName { get { return "0.9"; } } // always 2 digits for update check

            /// <summary>
            /// Gets the tag applied to this version of the software.
            /// </summary>
            public static string VersionTag { get { return "Preview"; } }

            /// <summary>
            /// Gets the full version of the software, including the major and minor versions, the revisions, and the tag.
            /// </summary>
            public static string FullVersionString { get { return VersionName + "." + RevisionString + " " + VersionTag; } }

            /// <summary>
            /// Gets a short description of the software.
            /// </summary>
            public static string Description { get{ return "Colectica® Data Management - http://www.colectica.com";} }
                
            /// <summary>
            /// Gets a unique identifier for the software.
            /// </summary>
            public static Guid SoftwareId { get{ return new Guid("3F2DF7A8-D458-458C-8948-3632B3C2055D");} }
        }
}
