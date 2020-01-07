// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Specifies the EventField that will be created for this property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EventFieldAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventFieldAttribute"/> class.
        /// </summary>
        /// <param name="typeDefinitionId">the TypeDefinitionId.</param>
        /// <param name="browsePath">the browse names, separated by '/'.</param>
        /// <param name="attributeId">the attribute.</param>
        /// <param name="indexRange">the range of array indexes.</param>
        public EventFieldAttribute(string? typeDefinitionId = null, string? browsePath = null, uint attributeId = AttributeIds.Value, string? indexRange = null)
        {
            this.TypeDefinitionId = typeDefinitionId;
            this.BrowsePath = browsePath;
            this.AttributeId = attributeId;
            this.IndexRange = indexRange;
        }

        /// <summary>
        /// Gets the TypeDefinitionId.
        /// </summary>
        public string? TypeDefinitionId { get; }

        /// <summary>
        /// Gets the BrowsePath.
        /// </summary>
        public string? BrowsePath { get; }

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        public uint AttributeId { get; }

        /// <summary>
        /// Gets the range of array indexes.
        /// </summary>
        public string? IndexRange { get; }
    }
}