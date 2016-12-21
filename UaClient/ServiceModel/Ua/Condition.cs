// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Represents a condition.
    /// </summary>
    public class Condition : BaseEvent
    {
        public string ConditionName { get; set; }

        public NodeId BranchId { get; set; }

        public bool? Retain { get; set; }

        public override void Deserialize(Variant[] fields)
        {
            base.Deserialize(fields);
            this.ConditionName = fields[6].GetValueOrDefault<string>();
            this.BranchId = fields[7].GetValueOrDefault<NodeId>();
            this.Retain = fields[8].GetValueOrDefault<bool>();
        }

        public override IEnumerable<SimpleAttributeOperand> GetSelectClauses()
        {
            foreach (var clause in base.GetSelectClauses())
            {
                yield return clause;
            }

            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.ConditionType), BrowsePath = new[] { new QualifiedName("ConditionName") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.ConditionType), BrowsePath = new[] { new QualifiedName("BranchId") }, AttributeId = AttributeIds.Value };
            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.ConditionType), BrowsePath = new[] { new QualifiedName("Retain") }, AttributeId = AttributeIds.Value };
        }
    }
}
