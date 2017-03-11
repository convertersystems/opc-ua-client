// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Represents an acknowledgeable condition.
    /// </summary>
    public class AlarmCondition : AcknowledgeableCondition
    {
        public LocalizedText ActiveState { get; set; }

        public override void Deserialize(Variant[] fields)
        {
            base.Deserialize(fields);
            this.ActiveState = fields[12].GetValueOrDefault<LocalizedText>();
        }

        public override IEnumerable<SimpleAttributeOperand> GetSelectClauses()
        {
            foreach (var clause in base.GetSelectClauses())
            {
                yield return clause;
            }

            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.AlarmConditionType), BrowsePath = new[] { new QualifiedName("ActiveState") }, AttributeId = AttributeIds.Value };
        }
    }
}
