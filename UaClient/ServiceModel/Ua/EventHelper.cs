// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Workstation.ServiceModel.Ua
{
    public static class EventHelper
    {
        private static readonly Dictionary<Type, SimpleAttributeOperand[]> _selectClauseCache = new Dictionary<Type, SimpleAttributeOperand[]>();
        private static readonly Dictionary<Type, PropertyInfo[]> _deserializerCache = new Dictionary<Type, PropertyInfo[]>();

        public static T Deserialize<T>(Variant[] eventFields)
            where T : BaseEvent, new()
        {
            var e = Activator.CreateInstance<T>();
            if (_deserializerCache.TryGetValue(typeof(T), out var infos))
            {
                for (int i = 0; i < eventFields.Length; i++)
                {
                    infos[i].SetValue(e, eventFields[i].GetValue());
                }
            }

            return e;
        }

        public static BaseEvent Deserialize(Type type, Variant[] eventFields)
        {
            var e = (BaseEvent)Activator.CreateInstance(type)!;
            if (_deserializerCache.TryGetValue(type, out var infos))
            {
                for (int i = 0; i < eventFields.Length; i++)
                {
                    infos[i].SetValue(e, eventFields[i].GetValue());
                }
            }

            return e;
        }

        public static SimpleAttributeOperand[] GetSelectClauses<T>()
            where T : BaseEvent, new()
        {
            var type = typeof(T);
            if (_selectClauseCache.TryGetValue(type, out var clauses))
            {
                return clauses;
            }

            RegisterSelectClauseAndDeserializer(type);
            return _selectClauseCache[type];
        }

        public static SimpleAttributeOperand[] GetSelectClauses(Type type)
        {
            if (_selectClauseCache.TryGetValue(type, out var clauses))
            {
                return clauses;
            }

            RegisterSelectClauseAndDeserializer(type);
            return _selectClauseCache[type];
        }

        private static void RegisterSelectClauseAndDeserializer(Type type)
        {
            var clauseList = new List<SimpleAttributeOperand>();
            var infoList = new List<PropertyInfo>();
            foreach (var info in type.GetRuntimeProperties())
            {
                var efa = info.GetCustomAttribute<EventFieldAttribute>();
                if (efa == null || string.IsNullOrEmpty(efa.TypeDefinitionId))
                {
                    continue;
                }

                var clause = new SimpleAttributeOperand
                {
                    TypeDefinitionId = NodeId.Parse(efa.TypeDefinitionId!),
                    BrowsePath = !String.IsNullOrWhiteSpace(efa.BrowsePath) ? efa.BrowsePath!.Split('/').Select(s => QualifiedName.Parse(s)).ToArray() : new QualifiedName[0],
                    AttributeId = efa.AttributeId,
                    IndexRange = efa.IndexRange
                };

                clauseList.Add(clause);
                infoList.Add(info);
            }

            _selectClauseCache[type] = clauseList.ToArray();
            _deserializerCache[type] = infoList.ToArray();
        }
    }
}
