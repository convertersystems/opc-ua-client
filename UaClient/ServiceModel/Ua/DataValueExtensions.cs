// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Workstation.ServiceModel.Ua
{
    public static class DataValueExtensions
    {
        /// <summary>
        /// Gets the value of the DataValue.
        /// </summary>
        /// <param name="dataValue">The DataValue.</param>
        /// <returns>The value.</returns>
        public static object? GetValue(this DataValue dataValue)
        {
            var value = dataValue.Value;
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
        /// Gets the value of the DataValue, or the default value for the type.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="dataValue">The DataValue.</param>
        /// <returns>The value, if an instance of the specified Type, otherwise the Type's default value.</returns>
        [return: MaybeNull]
        public static T GetValueOrDefault<T>(this DataValue dataValue)
        {
            var value = dataValue.GetValue();
            if (value != null)
            {
                if (value is T)
                {
                    return (T)value;
                }
            }

            // While [MaybeNull] attribute signals to the caller
            // that the return value can be null. It is ignored
            // by the compiler inside of the method, hence we
            // have to use the bang operator.
            return default!;
        }

        /// <summary>
        /// Gets the value of the DataValue, or the specified default value.
        /// </summary>
        /// <typeparam name="T">The expected type.</typeparam>
        /// <param name="dataValue">A DataValue</param>
        /// <param name="defaultValue">A default value.</param>
        /// <returns>The value, if an instance of the specified Type, otherwise the specified default value.</returns>
        [return: NotNullIfNotNull("defaultValue")]
        public static T GetValueOrDefault<T>(this DataValue dataValue, T defaultValue)
        {
            var value = dataValue.GetValue();
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
