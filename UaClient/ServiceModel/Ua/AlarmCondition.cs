// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Represents an alarm condition.
    /// </summary>
    public class AlarmCondition : AcknowledgeableCondition
    {
        [EventField(typeDefinitionId: ObjectTypeIds.AlarmConditionType, browsePath: "ActiveState/Id")]
        public bool? ActiveState { get; set; }
    }
}
