// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Workstation.ServiceModel.Ua
{
    public static class EventHelper
    {
        public static T Deserialize<T>(Variant[] eventFields)
            where T : BaseEvent, new()
        {
            var e = new T();
            e.Deserialize(eventFields);
            return e;
        }

        public static BaseEvent Deserialize(Type type, Variant[] eventFields)
        {
            var e = (BaseEvent)Activator.CreateInstance(type);
            e.Deserialize(eventFields);
            return e;
        }

        public static SimpleAttributeOperand[] GetSelectClauses<T>()
            where T : BaseEvent, new()
        {
            var e = new T();
            return e.GetSelectClauses().ToArray();
        }

        public static SimpleAttributeOperand[] GetSelectClauses(Type type)
        {
            var e = (BaseEvent)Activator.CreateInstance(type);
            return e.GetSelectClauses().ToArray();
        }
    }
}
