// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

namespace Workstation.ServiceModel.Ua
{
    public static class DataValueExtensions
    {
        /// <summary>
        /// Gets the value of the DataValue, or the default value for the type.
        /// </summary>
        /// <param name="dataValue">The DataValue.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the Type's default value.</returns>
        public static object GetValue(this DataValue dataValue)
        {
            var value = dataValue.Value;

            if (value is ExtensionObject)
            {
                return ((ExtensionObject)value).Body;
            }

            if (value is ExtensionObject[])
            {
                return ((ExtensionObject[])value).Select(e => e.Body).ToArray();
            }

            return value;
        }

        /// <summary>
        /// Gets the value of the DataValue, or the default value for the type.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="dataValue">The DataValue.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the Type's default value.</returns>
        public static T GetValueOrDefault<T>(this DataValue dataValue)
        {
            var value = dataValue.GetValue();
            if (value != null)
            {
                if (typeof(T).GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
                {
                    return (T)value;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Gets the value of the DataValue, or the specified default value.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="dataValue">A DataValue</param>
        /// <param name="defaultValue">A default value.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the specified default value.</returns>
        public static T GetValueOrDefault<T>(this DataValue dataValue, T defaultValue)
        {
            var value = dataValue.GetValue();
            if (value != null)
            {
                if (typeof(T).GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
                {
                    return (T)value;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the value of the DataValue, or the default value of the type.
        /// </summary>
        /// <param name="dataValue">A DataValue</param>
        /// <param name="dataType">A data type.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the Type's default value.</returns>
        public static object GetValueOrDefault(this DataValue dataValue, Type dataType)
        {
            var value = dataValue.GetValue();
            if (value != null)
            {
                if (dataType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
                {
                    return value;
                }
            }

            if (dataType.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(dataType);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the value of the DataValue, or the type's default value.
        /// </summary>
        /// <param name="dataValue">A DataValue</param>
        /// <param name="dataType">The expected type.</param>
        /// <param name="defaultValue">A default value.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the specified default value.</returns>
        public static object GetValueOrDefault(this DataValue dataValue, Type dataType, object defaultValue)
        {
            var value = dataValue.GetValue();
            if (value != null)
            {
                if (dataType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
                {
                    return value;
                }
            }

            return defaultValue;
        }
    }
}
