using System;
using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Specifies an assembly to be loaded as a custom type library for the encoders.
    /// <para>
    /// The assembly is searched for types with <see cref="BinaryEncodingIdAttribute" />.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class TypeLibraryAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApplicationPartAttribute" />.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        public TypeLibraryAttribute()
        {
        }

    }
}
