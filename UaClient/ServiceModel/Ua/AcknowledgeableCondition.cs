// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

#nullable enable

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

        [Obsolete]
        public override void Deserialize(Variant[] fields)
        {
            base.Deserialize(fields);
            this.AckedState = fields[10].GetValue() as bool?;
            this.ConfirmedState = fields[11].GetValue() as bool?;
        }

        [Obsolete]
        public override IEnumerable<SimpleAttributeOperand> GetSelectClauses()
        {
            foreach (var clause in base.GetSelectClauses())
            {
                yield return clause;
            }

            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.AcknowledgeableConditionType), BrowsePath = new[] { new QualifiedName("AckedState"), new QualifiedName("Id") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.AcknowledgeableConditionType), BrowsePath = new[] { new QualifiedName("ConfirmedState"), new QualifiedName("Id") }, AttributeId = AttributeIds.Value };
        }
    }
}
