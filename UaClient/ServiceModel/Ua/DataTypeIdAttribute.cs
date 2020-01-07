// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Attribute for classes to indicate the data type id.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
    public sealed class DataTypeIdAttribute : Attribute
    {
        public DataTypeIdAttribute(string s)
        {
            this.NodeId = ExpandedNodeId.Parse(s);
        }

        public ExpandedNodeId NodeId { get; }
    }
}