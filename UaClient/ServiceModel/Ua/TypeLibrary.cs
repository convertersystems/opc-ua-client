using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Specifies an assembly that provides custom types for the encoders.
    /// <para>
    /// The assembly is searched for types with <see cref="BinaryEncodingIdAttribute" />.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class TypeLibraryAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TypeLibraryAttribute" />.
        /// </summary>
        public TypeLibraryAttribute()
        {
        }
    }

    /// <summary>
    /// Stores the standard OPC UA and custom types.
    /// <para>
    /// Assemblies marked with the <see cref="TypeLibraryAttribute" /> are searched for types with <see cref="BinaryEncodingIdAttribute" />.
    /// </para>
    /// </summary>
    public class TypeLibrary
    {
        readonly ConcurrentDictionary<Type, ExpandedNodeId> _binaryEncodingIdByType;
        readonly ConcurrentDictionary<ExpandedNodeId, Type> _typeByBinaryEncodingId;

        public TypeLibrary()
        {
            _binaryEncodingIdByType = new ConcurrentDictionary<Type, ExpandedNodeId>(1,512);
            _typeByBinaryEncodingId = new ConcurrentDictionary<ExpandedNodeId, Type>(1,512);
            foreach (var assembly in from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                     where assembly.IsDefined(typeof(TypeLibraryAttribute), false)
                                     select assembly)
            {
                try
                {
                    AddTypesToLibrary(assembly.GetExportedTypes());
                }
                catch
                {
                    continue;
                }
            }
        }

        private void AddTypesToLibrary(Type[] types)
        {
            foreach (var type in types)
            {
                try
                {
                    var attr = type.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                    if (attr != null)
                    {
                        if (!_binaryEncodingIdByType.ContainsKey(type) && !_typeByBinaryEncodingId.ContainsKey(attr.NodeId))
                        {
                            _binaryEncodingIdByType.TryAdd(type, attr.NodeId);
                            _typeByBinaryEncodingId.TryAdd(attr.NodeId, type);
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }


        public bool TryGetTypeFromBinaryEncodingId(ExpandedNodeId id, [NotNullWhen(returnValue: true)] out Type? type)
        {
            return _typeByBinaryEncodingId.TryGetValue(id, out type);
        }

        public bool TryGetBinaryEncodingIdFromType(Type type, [NotNullWhen(returnValue: true)] out ExpandedNodeId? id)
        {
            return _binaryEncodingIdByType.TryGetValue(type, out id);
        }

    }
}
