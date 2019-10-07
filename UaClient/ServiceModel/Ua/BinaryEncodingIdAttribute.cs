﻿// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Attribute for classes of type IEncodable to indicate the binary encoding id.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class BinaryEncodingIdAttribute : Attribute
    {
        public BinaryEncodingIdAttribute(string s)
        {
            this.NodeId = ExpandedNodeId.Parse(s);
        }

        public ExpandedNodeId NodeId { get; }
    }
}