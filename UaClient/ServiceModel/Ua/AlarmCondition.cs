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

        [Obsolete]
        public override void Deserialize(Variant[] fields)
        {
            base.Deserialize(fields);
            this.ActiveState = fields[12].GetValue() as bool?;
        }

        [Obsolete]
        public override IEnumerable<SimpleAttributeOperand> GetSelectClauses()
        {
            foreach (var clause in base.GetSelectClauses())
            {
                yield return clause;
            }

            yield return new SimpleAttributeOperand { TypeDefinitionId = NodeId.Parse(ObjectTypeIds.AlarmConditionType), BrowsePath = new[] { new QualifiedName("ActiveState"), new QualifiedName("Id") }, AttributeId = AttributeIds.Value };
        }
    }
}
