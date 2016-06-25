// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Workstation.ServiceModel.Ua
{
    public static class VariantExtensions
    {
        /// <summary>
        /// Gets the value of the Variant, or the default value for the type.
        /// </summary>
        /// <param name="variant">The Variant.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the Type's default value.</returns>
        public static object GetValue(this Variant variant)
        {
            var value = variant.Value;
            if (value is ExtensionObject)
            {
                value = ((ExtensionObject)value).Body;
            }

            return value;
        }

        /// <summary>
        /// Gets the value of the Variant, or the default value for the type.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="variant">The Variant.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the Type's default value.</returns>
        public static T GetValueOrDefault<T>(this Variant variant)
        {
            var value = variant.GetValue();
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
        /// Gets the value of the Variant, or the specified default value.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="variant">A Variant</param>
        /// <param name="defaultValue">A default value.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the specified default value.</returns>
        public static T GetValueOrDefault<T>(this Variant variant, T defaultValue)
        {
            var value = variant.GetValue();
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
        /// Gets the value of the Variant, or the default value of the type.
        /// </summary>
        /// <param name="variant">A Variant</param>
        /// <param name="dataType">A data type.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the Type's default value.</returns>
        public static object GetValueOrDefault(this Variant variant, Type dataType)
        {
            var value = variant.GetValue();
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
        /// Gets the value of the Variant, or the type's default value.
        /// </summary>
        /// <param name="variant">A Variant</param>
        /// <param name="dataType">The expected type.</param>
        /// <param name="defaultValue">A default value.</param>
        /// <returns>The value if an instance of the specified Type, otherwise the specified default value.</returns>
        public static object GetValueOrDefault(this Variant variant, Type dataType, object defaultValue)
        {
            var value = variant.GetValue();
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
