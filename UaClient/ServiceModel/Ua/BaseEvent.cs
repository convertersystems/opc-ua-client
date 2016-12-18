// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Represents an event.
    /// </summary>
    public class BaseEvent
    {
        public byte[] EventId { get; set; }

        public NodeId EventType { get; set; }

        public string SourceName { get; set; }

        public DateTime Time { get; set; }

        public LocalizedText Message { get; set; }

        public ushort Severity { get; set; }

        public virtual void Deserialize(Variant[] fields)
        {
            EventId = fields[0].GetValueOrDefault<byte[]>();
            EventType = fields[1].GetValueOrDefault<NodeId>();
            SourceName = fields[2].GetValueOrDefault<string>();
            Time = fields[3].GetValueOrDefault<DateTime>();
            Message = fields[4].GetValueOrDefault<LocalizedText>();
            Severity = fields[5].GetValueOrDefault<ushort>();
        }

        public virtual IEnumerable<SimpleAttributeOperand> GetSelectClauses()
        {
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.BaseEventType), BrowsePath = new[] { new QualifiedName("EventId") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.BaseEventType), BrowsePath = new[] { new QualifiedName("EventType") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.BaseEventType), BrowsePath = new[] { new QualifiedName("SourceName") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.BaseEventType), BrowsePath = new[] { new QualifiedName("Time") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.BaseEventType), BrowsePath = new[] { new QualifiedName("Message") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.BaseEventType), BrowsePath = new[] { new QualifiedName("Severity") }, AttributeId = AttributeIds.Value };
        }
    }
}
