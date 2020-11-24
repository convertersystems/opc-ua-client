using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

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
        /// Initializes a new instance of <see cref="ApplicationPartAttribute" />.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        public TypeLibraryAttribute()
        {
        }
    }

    /// <summary>
    /// Stores the standard OPC UA and custom types.
    /// <para>
    /// Assemblies marked with the <see cref="TypeLibraryAttribute" /> are searched for types with <see cref="DataTypeIdAttribute" />.
    /// </para>
    /// </summary>
    public class TypeLibrary
    {
        static readonly Lazy<TypeLibrary> _instance = new Lazy<TypeLibrary>(() => new TypeLibrary());
        readonly Dictionary<Type, ExpandedNodeId> _dataTypeIdByType;
        readonly Dictionary<ExpandedNodeId, Type> _typeByDataTypeId;
        readonly Dictionary<Type, ExpandedNodeId> _binaryEncodingIdByType;
        readonly Dictionary<ExpandedNodeId, Type> _typeByBinaryEncodingId;

        public static TypeLibrary Default => _instance.Value;

        private TypeLibrary()
        {
            _typeByDataTypeId = new Dictionary<ExpandedNodeId, Type>();
            _dataTypeIdByType = new Dictionary<Type, ExpandedNodeId>();
            _binaryEncodingIdByType = new Dictionary<Type, ExpandedNodeId>();
            _typeByBinaryEncodingId = new Dictionary<ExpandedNodeId, Type>();
            foreach (var assembly in from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                     where assembly.IsDefined(typeof(TypeLibraryAttribute), false)
                                     select assembly)
            {
                try
                {
                    AddTypesToLibrary(assembly);
                }
                catch
                {
                    continue;
                }
            }
        }

        private void AddTypesToLibrary(Assembly assembly)
        {
            foreach (var (type, attr) in from type in assembly.GetExportedTypes()
                                         let attr = type.GetCustomAttribute<BinaryEncodingIdAttribute>(false)
                                         where attr != null
                                         select (type, attr))
                if (!_binaryEncodingIdByType.ContainsKey(type) && !_typeByBinaryEncodingId.ContainsKey(attr.NodeId))
                {
                    _binaryEncodingIdByType.Add(type, attr.NodeId);
                    _typeByBinaryEncodingId.Add(attr.NodeId, type);
                }
        }

        public static bool TryGetTypeFromDataTypeId(ExpandedNodeId id, [NotNullWhen(returnValue: true)] out Type? type)
        {
            return TypeLibrary._instance.Value._typeByDataTypeId.TryGetValue(id, out type);
        }

        public static bool TryGetDataTypeIdFromType(Type type, [NotNullWhen(returnValue: true)] out ExpandedNodeId? id)
        { 
            return TypeLibrary._instance.Value._dataTypeIdByType.TryGetValue(type, out id);
        }

        public static bool TryGetTypeFromBinaryEncodingId(ExpandedNodeId id, [NotNullWhen(returnValue: true)] out Type? type)
        {
            return TypeLibrary._instance.Value._typeByBinaryEncodingId.TryGetValue(id, out type);
        }

        public static bool TryGetBinaryEncodingIdFromType(Type type, [NotNullWhen(returnValue: true)] out ExpandedNodeId? id)
        {
            return TypeLibrary._instance.Value._binaryEncodingIdByType.TryGetValue(type, out id);
        }

    }
}
