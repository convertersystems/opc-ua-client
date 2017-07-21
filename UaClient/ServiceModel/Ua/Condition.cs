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
        public NodeId ConditionId { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.ConditionType, browsePath: "ConditionName")]
        public string ConditionName { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.ConditionType, browsePath: "BranchId")]
        public NodeId BranchId { get; set; }

        [EventField(typeDefinitionId: ObjectTypeIds.ConditionType, browsePath: "Retain")]
        public bool? Retain { get; set; }

        [Obsolete]
        public override void Deserialize(Variant[] fields)
        {
            base.Deserialize(fields);
            this.ConditionId = fields[6].GetValueOrDefault<NodeId>();
            this.ConditionName = fields[7].GetValueOrDefault<string>();
            this.BranchId = fields[8].GetValueOrDefault<NodeId>();
            this.Retain = fields[9].GetValue() as bool?;
        }

        [Obsolete]
        public override IEnumerable<SimpleAttributeOperand> GetSelectClauses()
        {
            foreach (var clause in base.GetSelectClauses())
            {
                yield return clause;
            }

            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.ConditionType), BrowsePath = new QualifiedName[0], AttributeId = AttributeIds.NodeId };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.ConditionType), BrowsePath = new[] { new QualifiedName("ConditionName") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.ConditionType), BrowsePath = new[] { new QualifiedName("BranchId") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.ConditionType), BrowsePath = new[] { new QualifiedName("Retain") }, AttributeId = AttributeIds.Value };
        }
    }
}
