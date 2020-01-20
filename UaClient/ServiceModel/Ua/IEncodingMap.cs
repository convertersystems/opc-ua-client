// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    public interface IEncodingMap
    {
        /// <summary>
        /// Gets the namespace uris.
        /// </summary>
        List<string> NamespaceUris { get; }
        
        /// <summary>
        /// Gets the system type associated with the encoding ID.
        /// </summary>
        /// <param name="encodingId">The encoding ID.</param>
        /// <param name="type">The system type.</param>
        /// <returns>True if successfull.</returns>
        bool TryGetType(NodeId encodingId, out Type type);

        /// <summary>
        /// Gets the encoding ID associated with the system type.
        /// </summary>
        /// <param name="type">The system type.</param>
        /// <param name="encodingId">The encoding ID.</param>
        /// <returns>True if successfull.</returns>
        bool TryGetEncodingId(Type type, out NodeId encodingId);
    }
}