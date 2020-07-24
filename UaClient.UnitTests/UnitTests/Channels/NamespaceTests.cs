using FluentAssertions;
using FluentAssertions.Equivalency;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Xunit;
using Xunit.Abstractions;

namespace Workstation.UaClient.UnitTests.Channels
{
    public partial class NamespaceTests
    {

        private readonly ITestOutputHelper output;

        public NamespaceTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public bool TestDictionaryExpandedNodeId()
        {
            const int _max = 1000000;
            var ns = new List<string> { "http://opcfoundation.org/UA/" };
            var binaryEncodingIdToTypeDictionary = new Dictionary<ExpandedNodeId, Type>();
            foreach (var type in typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly.ExportedTypes)
            {
                var info = type.GetTypeInfo();
                if (info.ImplementedInterfaces.Contains(typeof(IEncodable)))
                {
                    var attr = info.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                    if (attr != null)
                    {
                        binaryEncodingIdToTypeDictionary[attr.NodeId] = type;
                    }
                }
            }
            var ids = new List<NodeId> {
                NodeId.Parse(ObjectIds.OpenSecureChannelRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.ReadRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.WriteRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.BrowseRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CallRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CreateSubscriptionRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.PublishRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CloseSessionRequest_Encoding_DefaultBinary),
            };
            var flag = true;
            var s1 = Stopwatch.StartNew();
            for (int i = 0; i < _max; i++)
            {
                foreach (var id in ids)
                {
                    flag &= binaryEncodingIdToTypeDictionary.TryGetValue(NodeId.ToExpandedNodeId(id, ns), out _);
                }
            }
            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds * 1000000) / _max / ids.Count).ToString("0.00 ns"));
            return flag;
        }

        [Fact]
        public bool TestDictionaryNodeId()
        {
            const int _max = 1000000;
            var binaryEncodingIdToTypeDictionary = new Dictionary<NodeId, Type>();
            foreach (var type in typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly.ExportedTypes)
            {
                var info = type.GetTypeInfo();
                if (info.ImplementedInterfaces.Contains(typeof(IEncodable)))
                {
                    var attr = info.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                    if (attr != null)
                    {
                        binaryEncodingIdToTypeDictionary[attr.NodeId.NodeId] = type;
                    }
                }
            }
            var namespaces = new List<Dictionary<NodeId, Type>>
            {
                binaryEncodingIdToTypeDictionary
            };
            var ids = new List<NodeId> {
                NodeId.Parse(ObjectIds.OpenSecureChannelRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.ReadRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.WriteRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.BrowseRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CallRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CreateSubscriptionRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.PublishRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CloseSessionRequest_Encoding_DefaultBinary),
            };
            var flag = true;
            var s1 = Stopwatch.StartNew();
            for (int i = 0; i < _max; i++)
            {
                foreach (var id in ids)
                {
                    flag &= namespaces[id.NamespaceIndex].TryGetValue(id, out _);
                }
            }
            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds * 1000000) / _max / ids.Count).ToString("0.00 ns"));
            return flag;
        }

        public class Namespace
        {
            readonly string _namespaceUri;
            readonly Dictionary<uint, Type> _nid;
            readonly Dictionary<string, Type> _sid;
            readonly Dictionary<Guid, Type> _gid;
            readonly Dictionary<byte[], Type> _bid;
            readonly Dictionary<Type, NodeId> _all;

            public Namespace(string namespaceUri)
            {
                _namespaceUri = namespaceUri;
                _nid = new Dictionary<uint, Type>();
                _sid = new Dictionary<string, Type>();
                _gid = new Dictionary<Guid, Type>();
                _bid = new Dictionary<byte[], Type>(ByteSequenceComparer.Instance);
                _all = new Dictionary<Type, NodeId>();
            }

            public bool TryAdd(ExpandedNodeId nodeId, Type typ)
            {
                if (nodeId.NamespaceUri != _namespaceUri)
                {
                    return false;
                }
                if (!_all.TryAdd(typ, nodeId.NodeId))
                {
                    return false;
                }
                switch (nodeId.NodeId.IdType)
                {
                    case IdType.Numeric:
                        return _nid.TryAdd((uint)nodeId.NodeId.Identifier, typ);
                    case IdType.String:
                        return _sid.TryAdd((string)nodeId.NodeId.Identifier, typ);
                    case IdType.Guid:
                        return _gid.TryAdd((Guid)nodeId.NodeId.Identifier, typ);
                    case IdType.Opaque:
                        return _bid.TryAdd((byte[])nodeId.NodeId.Identifier, typ);
                    default:
                        return false;
                }
            }

            public bool TryGetType(NodeId nodeId, out Type typ)
            {
                switch (nodeId.IdType)
                {
                    case IdType.Numeric:
                        return _nid.TryGetValue((uint)nodeId.Identifier, out typ);
                    case IdType.String:
                        return _sid.TryGetValue((string)nodeId.Identifier, out typ);
                    case IdType.Guid:
                        return _gid.TryGetValue((Guid)nodeId.Identifier, out typ);
                    case IdType.Opaque:
                        return _bid.TryGetValue((byte[])nodeId.Identifier, out typ);
                }
                typ = null;
                return false;
            }
            public bool TryGetEncodingId(Type typ, out ExpandedNodeId nodeId)
            {
                if (_all.TryGetValue(typ, out NodeId value))
                {
                    nodeId = new ExpandedNodeId(value, _namespaceUri);
                    return true;
                }
                nodeId = null;
                return false;
            }
        }

        [Fact]
        public bool TestNamespace()
        {
            const int _max = 1000000;
            var ns = new Namespace("http://opcfoundation.org/UA/");
            foreach (var type in typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly.ExportedTypes)
            {
                var info = type.GetTypeInfo();
                if (info.ImplementedInterfaces.Contains(typeof(IEncodable)))
                {
                    var attr = info.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                    if (attr != null)
                    {
                        ns.TryAdd(attr.NodeId, type);
                    }
                }
            }
            var namespaces = new List<Namespace> { ns };
            var ids = new List<NodeId> {
                NodeId.Parse(ObjectIds.OpenSecureChannelRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.ReadRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.WriteRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.BrowseRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CallRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CreateSubscriptionRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.PublishRequest_Encoding_DefaultBinary),
                NodeId.Parse(ObjectIds.CloseSessionRequest_Encoding_DefaultBinary),
            };
            var flag = true;
            var s1 = Stopwatch.StartNew();
            for (int i = 0; i < _max; i++)
            {
                foreach (var id in ids)
                {
                    flag &= namespaces[id.NamespaceIndex].TryGetType(id, out _);
                }
            }
            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds * 1000000) / _max / ids.Count).ToString("0.00 ns"));
            return flag;
        }


        // Test getting encoding id from class attribute. Store in cache
        [Fact]
        public bool TestGetEncodingIdFromAttributeWithCache()
        {
            const int _max = 1000000;
            var typs = new List<Type> {
                typeof(OpenSecureChannelRequest),
                typeof(ReadRequest),
                typeof(WriteRequest),
                typeof(BrowseRequest),
                typeof(CallRequest),
                typeof(CreateSubscriptionRequest),
                typeof(PublishRequest),
                typeof(CloseSessionRequest),
            };
            var cache = new ConcurrentDictionary<Type, ExpandedNodeId>();
            // pre-load for better comparison
            foreach (var typ in typs)
            {
                var id = cache.GetOrAdd(typ, typ => typ.GetTypeInfo().GetCustomAttribute<BinaryEncodingIdAttribute>(false).NodeId);
            }

            var flag = true;
            var s1 = Stopwatch.StartNew();
            for (int i = 0; i < _max; i++)
            {
                foreach (var typ in typs)
                {
                    var id = cache.GetOrAdd(typ, typ => typ.GetTypeInfo().GetCustomAttribute<BinaryEncodingIdAttribute>(false).NodeId);
                }
            }
            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds * 1000000) / _max / typs.Count).ToString("0.00 ns"));
            return flag;
        }


        // Test getting encoding id from prepared dictionary.
        [Fact]
        public bool TestGetEncodingIdFromDictionary()
        {
            const int _max = 1000000;
            var typeToBinaryEncodingIdDictionary = new Dictionary<Type, ExpandedNodeId>();
            foreach (var type in typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly.ExportedTypes)
            {
                var info = type.GetTypeInfo();
                if (info.ImplementedInterfaces.Contains(typeof(IEncodable)))
                {
                    var attr = info.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                    if (attr != null)
                    {
                        typeToBinaryEncodingIdDictionary[type] = attr.NodeId;
                    }
                }
            }
            var typs = new List<Type> {
                typeof(OpenSecureChannelRequest),
                typeof(ReadRequest),
                typeof(WriteRequest),
                typeof(BrowseRequest),
                typeof(CallRequest),
                typeof(CreateSubscriptionRequest),
                typeof(PublishRequest),
                typeof(CloseSessionRequest),
            };
            var flag = true;
            var s1 = Stopwatch.StartNew();
            for (int i = 0; i < _max; i++)
            {
                foreach (var typ in typs)
                {
                    flag &= typeToBinaryEncodingIdDictionary.TryGetValue(typ, out ExpandedNodeId _);
                }
            }
            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds * 1000000) / _max / typs.Count).ToString("0.00 ns"));
            return flag;
        }

        [Fact]
        public bool TestAssemblyScan1()
        {
            var binaryEncodingIdToTypeDictionary = new Dictionary<ExpandedNodeId, Type>();
            var s1 = Stopwatch.StartNew();
            var types = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                         where !assembly.IsDynamic
                         from type in assembly.GetTypes()
                         where Attribute.IsDefined(type, typeof(BinaryEncodingIdAttribute))
                         select type).ToList();

            foreach (var type in types)
            {
                var info = type.GetTypeInfo();
                var attr = info.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                if (attr != null)
                {
                    binaryEncodingIdToTypeDictionary.TryAdd(attr.NodeId, type);
                }
            }
            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds)).ToString("0.00 ms"));
            output.WriteLine(types.Count.ToString("0 type(s) found"));
            return true;
        }

        [Fact]
        public bool TestAssemblyScan2()
        {
            var binaryEncodingIdToTypeDictionary = new Dictionary<ExpandedNodeId, Type>();
            var s1 = Stopwatch.StartNew();
            var count = 0;
            var types = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                         where !assembly.IsDynamic
                         from type in assembly.GetTypes()
                         select type).ToList();
            foreach (var type in types)
            {
                var info = type.GetTypeInfo();
                var attr = info.GetCustomAttribute<BinaryEncodingIdAttribute>(false);
                if (attr != null)
                {
                    binaryEncodingIdToTypeDictionary.TryAdd(attr.NodeId, type);
                    count++;
                }
            }
            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds)).ToString("0.00 ms"));
            output.WriteLine(count.ToString("0 type(s) found"));
            return true;
        }
        //DefinedTypes
        [Fact]
        public bool TestAssemblyScan3()
        {
            var binaryEncodingIdToTypeDictionary = new Dictionary<ExpandedNodeId, Type>();
            var s1 = Stopwatch.StartNew();
            var count = 0;

            foreach (var (info, attr) in from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                         where !assembly.IsDynamic
                                         from info in assembly.DefinedTypes
                                         let attr = info.GetCustomAttribute<BinaryEncodingIdAttribute>(false)
                                         where attr != null
                                         select (info, attr))
            {
                binaryEncodingIdToTypeDictionary.TryAdd(attr.NodeId, info.AsType());
                count++;
            }

            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds)).ToString("0.00 ms"));
            output.WriteLine(count.ToString("0 type(s) found"));
            return true;
        }
        //    from a in AppDomain.CurrentDomain.GetAssemblies()
        //from t in a.GetTypes()
        //let attributes = t.GetCustomAttributes(typeof(HelpAttribute), true)
        //where attributes != null && attributes.Length > 0
        //select new { Type = t, Attributes = attributes.Cast<HelpAttribute>()
        //};

        [Fact]
        public bool TestAssemblyScan4()
        {
            var binaryEncodingIdToTypeDictionary = new Dictionary<ExpandedNodeId, Type>();
            var s1 = Stopwatch.StartNew();
            var count = 0;

            foreach (var (type, attr) in from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                         where !assembly.IsDynamic
                                         from type in assembly.GetExportedTypes()
                                         let attr = type.GetCustomAttribute<BinaryEncodingIdAttribute>(false)
                                         where attr != null
                                         select (type, attr))
            {
                binaryEncodingIdToTypeDictionary.TryAdd(attr.NodeId, type);
                count++;
            }

            s1.Stop();
            output.WriteLine(((double)(s1.Elapsed.TotalMilliseconds)).ToString("0.00 ms"));
            output.WriteLine(count.ToString("0 type(s) found"));
            return true;
        }

    }
}
