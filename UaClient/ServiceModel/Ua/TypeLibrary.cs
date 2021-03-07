using System;
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
            _typeByDataTypeId = new Dictionary<ExpandedNodeId, Type>()
            {
                [ExpandedNodeId.Parse(DataTypeIds.Boolean)] = typeof(bool),
                [ExpandedNodeId.Parse(DataTypeIds.SByte)] = typeof(sbyte),
                [ExpandedNodeId.Parse(DataTypeIds.Byte)] = typeof(byte),
                [ExpandedNodeId.Parse(DataTypeIds.Int16)] = typeof(short),
                [ExpandedNodeId.Parse(DataTypeIds.UInt16)] = typeof(ushort),
                [ExpandedNodeId.Parse(DataTypeIds.Int32)] = typeof(int),
                [ExpandedNodeId.Parse(DataTypeIds.UInt32)] = typeof(uint),
                [ExpandedNodeId.Parse(DataTypeIds.Int64)] = typeof(long),
                [ExpandedNodeId.Parse(DataTypeIds.UInt64)] = typeof(ulong),
                [ExpandedNodeId.Parse(DataTypeIds.Float)] = typeof(float),
                [ExpandedNodeId.Parse(DataTypeIds.Double)] = typeof(double),
                [ExpandedNodeId.Parse(DataTypeIds.String)] = typeof(string),
                [ExpandedNodeId.Parse(DataTypeIds.DateTime)] = typeof(DateTime),
                [ExpandedNodeId.Parse(DataTypeIds.Guid)] = typeof(Guid),
                [ExpandedNodeId.Parse(DataTypeIds.ByteString)] = typeof(byte[]),
                [ExpandedNodeId.Parse(DataTypeIds.XmlElement)] = typeof(XElement),
                [ExpandedNodeId.Parse(DataTypeIds.NodeId)] = typeof(NodeId),
                [ExpandedNodeId.Parse(DataTypeIds.ExpandedNodeId)] = typeof(ExpandedNodeId),
                [ExpandedNodeId.Parse(DataTypeIds.StatusCode)] = typeof(StatusCode),
                [ExpandedNodeId.Parse(DataTypeIds.QualifiedName)] = typeof(QualifiedName),
                [ExpandedNodeId.Parse(DataTypeIds.LocalizedText)] = typeof(LocalizedText),
                [ExpandedNodeId.Parse(DataTypeIds.Structure)] = typeof(ExtensionObject),
                [ExpandedNodeId.Parse(DataTypeIds.DataValue)] = typeof(DataValue),
                [ExpandedNodeId.Parse(DataTypeIds.BaseDataType)] = typeof(Variant),
                [ExpandedNodeId.Parse(DataTypeIds.DiagnosticInfo)] = typeof(DiagnosticInfo),
            };
            _dataTypeIdByType = _typeByDataTypeId.ToDictionary(x => x.Value, x => x.Key);
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
            foreach (var type in assembly.GetExportedTypes())
            {
                var attr = type.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                if (attr != null)
                {
                    if (!_binaryEncodingIdByType.ContainsKey(type) && !_typeByBinaryEncodingId.ContainsKey(attr.NodeId))
                    {
                        _binaryEncodingIdByType.Add(type, attr.NodeId);
                        _typeByBinaryEncodingId.Add(attr.NodeId, type);
                    }
                }
                var attr2 = type.GetCustomAttribute<DataTypeIdAttribute>(false);
                if (attr2 != null)
                {
                    if (!_dataTypeIdByType.ContainsKey(type) && !_typeByDataTypeId.ContainsKey(attr2.NodeId))
                    {
                        _dataTypeIdByType.Add(type, attr2.NodeId);
                        _typeByDataTypeId.Add(attr2.NodeId, type);
                    }
                }
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
