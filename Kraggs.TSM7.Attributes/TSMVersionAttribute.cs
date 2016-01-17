#region License
/*
    TSM 7.1 Utility library.
    Copyright (C) 2015 Jarle Hansen
    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.
    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.
    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
*/
#endregion
using System;

namespace Kraggs.TSM7.Data
{
    /// <summary>
    /// Marks a class, struct, field or property as being dependent on a specific TSM Server Version.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
    public class TSMVersionAttribute : Attribute
    {
        /// <summary>
        /// Marks a class, struct, field or property as being dependent on a specific TSM Server Version.
        /// </summary>
        public Version ServerVersion { get; set; }

        /// <summary>
        /// Marks a class, struct, field or property as being dependent on a specific TSM Server Version.
        /// </summary>
        /// <param name="serverVersion"></param>
        public TSMVersionAttribute(string serverVersion)
        {
            this.ServerVersion = new Version(serverVersion);
        }
        /// <summary>
        /// Marks a class, struct, field or property as being dependent on a specific TSM Server Version.
        /// </summary>
        /// <param name="serverVersion"></param>
        public TSMVersionAttribute(Version serverVersion)
        {
            this.ServerVersion = serverVersion;
        }
    }
}
