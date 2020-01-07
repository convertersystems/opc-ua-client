// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    public static class EventHelper
    {
        private static readonly Dictionary<Type, SimpleAttributeOperand[]> SelectClauseCache = new Dictionary<Type, SimpleAttributeOperand[]>();
        private static readonly Dictionary<Type, PropertyInfo[]> DeserializerCache = new Dictionary<Type, PropertyInfo[]>();

        public static T Deserialize<T>(Variant[] eventFields)
            where T : BaseEvent, new()
        {
            var e = Activator.CreateInstance<T>();
            if (DeserializerCache.TryGetValue(typeof(T), out var infos))
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
            if (DeserializerCache.TryGetValue(type, out var infos))
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
            if (SelectClauseCache.TryGetValue(type, out var clauses))
            {
                return clauses;
            }

            RegisterSelectClauseAndDeserializer(type);
            return SelectClauseCache[type];
        }

        public static SimpleAttributeOperand[] GetSelectClauses(Type type)
        {
            if (SelectClauseCache.TryGetValue(type, out var clauses))
            {
                return clauses;
            }

            RegisterSelectClauseAndDeserializer(type);
            return SelectClauseCache[type];
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

            SelectClauseCache[type] = clauseList.ToArray();
            DeserializerCache[type] = infoList.ToArray();
        }
    }
}
