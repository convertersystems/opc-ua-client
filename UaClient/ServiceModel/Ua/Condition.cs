// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Represents a condition.
    /// </summary>
    public class Condition : BaseEvent
    {
        [EventField(typeDefinitionId: ObjectTypeIds.ConditionType, attributeId: AttributeIds.NodeId)]
        public NodeId? ConditionId { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.ConditionType, browsePath: "ConditionName")]
        public string? ConditionName { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.ConditionType, browsePath: "BranchId")]
        public NodeId? BranchId { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.ConditionType, browsePath: "Retain")]
        public bool? Retain { get; set; }
    }
}
