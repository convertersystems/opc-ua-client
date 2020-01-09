// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Represents an event.
    /// </summary>
    public class BaseEvent
    {
        [EventField(typeDefinitionId: ObjectTypeIds.BaseEventType, browsePath: "EventId")]
        public byte[]? EventId { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.BaseEventType, browsePath: "EventType")]
        public NodeId? EventType { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.BaseEventType, browsePath: "SourceName")]
        public string? SourceName { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.BaseEventType, browsePath: "Time")]
        public DateTime Time { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.BaseEventType, browsePath: "Message")]
        public LocalizedText? Message { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.BaseEventType, browsePath: "Severity")]
        public ushort Severity { get; set; }

        [Obsolete]
        public virtual void Deserialize(Variant[] fields)
        {
            this.EventId = fields[0].GetValueOrDefault<byte[]>();
            this.EventType = fields[1].GetValueOrDefault<NodeId>();
            this.SourceName = fields[2].GetValueOrDefault<string>();
            this.Time = fields[3].GetValueOrDefault<DateTime>();
            this.Message = fields[4].GetValueOrDefault<LocalizedText>();
            this.Severity = fields[5].GetValueOrDefault<ushort>();
        }

        [Obsolete]
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
