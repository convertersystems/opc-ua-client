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
    }
}
