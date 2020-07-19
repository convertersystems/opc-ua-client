// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Interface for attributes to tag <see cref="IEncodable"/> types with an encoding id.
    /// </summary>
    public interface IEncodingIdAttribute
    {
        /// <summary>
        /// The encoding id of the decorated type.
        /// </summary>
        ExpandedNodeId NodeId { get; }
    }
}
