// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    public static class VariantExtensions
    {
        /// <summary>
        /// Gets the value of the Variant.
        /// </summary>
        /// <param name="variant">The Variant.</param>
        /// <returns>The value.</returns>
        public static object? GetValue(this Variant variant)
        {
            var value = variant.Value;
            switch (value)
            {
                case ExtensionObject obj:

                    return obj.BodyType == BodyType.Encodable ? obj.Body : obj;

                case ExtensionObject[] objArray:

                    return objArray.Select(e => e.BodyType == BodyType.Encodable ? e.Body : e).ToArray();

                default:

                    return value;
            }
        }

        /// <summary>
        /// Gets the value of the Variant, or the default value for the type.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="variant">The Variant.</param>
        /// <returns>The value, if an instance of the specified Type, otherwise the Type's default value.</returns>
        [return: MaybeNull]
        public static T GetValueOrDefault<T>(this Variant variant)
        {
            var value = variant.GetValue();
            if (value != null)
            {
                if (value is T)
                {
                    return (T)value;
                }
            }

            return default(T)!;
        }

        /// <summary>
        /// Gets the value of the Variant, or the specified default value.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="variant">A Variant</param>
        /// <param name="defaultValue">A default value.</param>
        /// <returns>The value, if an instance of the specified Type, otherwise the specified default value.</returns>
        [return: NotNullIfNotNull("defaultValue")]
        public static T GetValueOrDefault<T>(this Variant variant, T defaultValue)
        {
            var value = variant.GetValue();
            if (value != null)
            {
                if (value is T)
                {
                    return (T)value;
                }
            }

            return defaultValue;
        }
    }
}
