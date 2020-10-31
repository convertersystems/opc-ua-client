// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Represents an acknowledgeable condition.
    /// </summary>
    public class AcknowledgeableCondition : Condition
    {
        [EventField(typeDefinitionId: ObjectTypeIds.AcknowledgeableConditionType, browsePath: "AckedState/Id")]
        public bool? AckedState { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.AcknowledgeableConditionType, browsePath: "ConfirmedState/Id")]
        public bool? ConfirmedState { get; set; }
    }
}
