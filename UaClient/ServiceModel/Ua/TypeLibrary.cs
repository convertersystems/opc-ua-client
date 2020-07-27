using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    /// <summary>
    /// Stores the standard OPC UA and custom types for the encoders.
    /// <para>
    /// Assemblies marked with the <see cref="TypeLibraryAttribute" /> are searched for types with <see cref="BinaryEncodingIdAttribute" />.
    /// </para>
    /// </summary>
    public class TypeLibrary
    {
        public static TypeLibrary Default { get; } = new TypeLibrary();

        public TypeLibrary()
        {
            var fwd = new Dictionary<Type, ExpandedNodeId>();
            var rev = new Dictionary<ExpandedNodeId, Type>();
            foreach (var assembly in from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                     where assembly.IsDefined(typeof(TypeLibraryAttribute), false)
                                     select assembly)
            {
                try
                {
                    foreach (var (type, attr) in from type in assembly.GetExportedTypes()
                                                 let attr = type.GetCustomAttribute<BinaryEncodingIdAttribute>(false)
                                                 where attr != null
                                                 select (type, attr))
                        if (!fwd.ContainsKey(type) && !rev.ContainsKey(attr.NodeId))
                        {
                            fwd.Add(type, attr.NodeId);
                            rev.Add(attr.NodeId, type);
                        }
                }
                catch
                {
                    continue;
                }
            }
            EncodingDictionary = fwd;
            DecodingDictionary = rev;
        }

        public IReadOnlyDictionary<Type, ExpandedNodeId> EncodingDictionary { get; }
        public IReadOnlyDictionary<ExpandedNodeId, Type> DecodingDictionary { get; }

    }
}
