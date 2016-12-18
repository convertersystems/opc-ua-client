// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public sealed class ExpandedNodeId : IEquatable<ExpandedNodeId>
    {
        public ExpandedNodeId(uint identifier, string namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = new NodeId(identifier);
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public ExpandedNodeId(string identifier, string namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = new NodeId(identifier);
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public ExpandedNodeId(Guid identifier, string namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = new NodeId(identifier);
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public ExpandedNodeId(byte[] identifier, string namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = new NodeId(identifier);
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public ExpandedNodeId(NodeId identifier, string namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = identifier;
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public NodeId NodeId { get; }

        public string NamespaceUri { get; }

        public uint ServerIndex { get; }

        public static bool operator ==(ExpandedNodeId a, ExpandedNodeId b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return (a.NodeId == b.NodeId) && (a.NamespaceUri == b.NamespaceUri) && (a.ServerIndex == b.ServerIndex);
        }

        public static bool operator !=(ExpandedNodeId a, ExpandedNodeId b)
        {
            return !(a == b);
        }

        public static bool IsNull(ExpandedNodeId nodeId)
        {
            return (nodeId == null) || NodeId.IsNull(nodeId.NodeId);
        }

        public static NodeId ToNodeId(ExpandedNodeId value, IList<string> namespaceUris)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException("value");
            }

            ushort ns = value.NodeId.NamespaceIndex;
            string nsu = value.NamespaceUri;
            if (namespaceUris != null && !string.IsNullOrEmpty(nsu))
            {
                int i = namespaceUris.IndexOf(nsu);
                if (i != -1)
                {
                    ns = (ushort)i;
                }
            }

            switch (value.NodeId.IdType)
            {
                case IdType.Numeric:
                    return new NodeId((uint)value.NodeId.Identifier, ns);

                case IdType.String:
                    return new NodeId((string)value.NodeId.Identifier, ns);

                case IdType.Guid:
                    return new NodeId((Guid)value.NodeId.Identifier, ns);

                default:
                    return new NodeId((byte[])value.NodeId.Identifier, ns);
            }
        }

        public static bool TryParse(string s, out ExpandedNodeId value)
        {
            try
            {
                uint svr = 0;
                if (s.StartsWith("svr=", StringComparison.Ordinal))
                {
                    int pos = s.IndexOf(';');
                    if (pos == -1)
                    {
                        throw new ServiceResultException(StatusCodes.BadNodeIdInvalid);
                    }

                    svr = uint.Parse(s.Substring(4, pos - 4), CultureInfo.InvariantCulture);
                    s = s.Substring(pos + 1);
                }

                string nsu = null;
                if (s.StartsWith("nsu=", StringComparison.Ordinal))
                {
                    int pos = s.IndexOf(';');
                    if (pos == -1)
                    {
                        throw new ServiceResultException(StatusCodes.BadNodeIdInvalid);
                    }

                    nsu = s.Substring(4, pos - 4);
                    s = s.Substring(pos + 1);
                }

                NodeId nodeId = null;
                if (NodeId.TryParse(s, out nodeId))
                {
                    value = new ExpandedNodeId(nodeId, nsu, svr);
                    return true;
                }

                value = null;
                return false;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        public static ExpandedNodeId Parse(string s)
        {
            ExpandedNodeId value;
            if (!ExpandedNodeId.TryParse(s, out value))
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdInvalid);
            }

            return value;
        }

        public ExpandedNodeId Clone()
        {
            return new ExpandedNodeId(NodeId.Clone(), NamespaceUri, ServerIndex);
        }

        public override bool Equals(object o)
        {
            if (o is ExpandedNodeId)
            {
                return this == (ExpandedNodeId)o;
            }

            return false;
        }

        public bool Equals(ExpandedNodeId that)
        {
            return this == that;
        }

        public override int GetHashCode()
        {
            int result = NodeId.GetHashCode();
            result = (31 * result) + (NamespaceUri != null ? NamespaceUri.GetHashCode() : 0);
            result = (31 * result) + (int)(ServerIndex ^ ((long)((ulong)ServerIndex >> 32)));
            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (ServerIndex > 0)
            {
                sb.AppendFormat("svr={0};", ServerIndex);
            }

            if (!string.IsNullOrEmpty(NamespaceUri))
            {
                sb.AppendFormat("nsu={0};", NamespaceUri);
            }

            sb.Append(NodeId.ToString());
            return sb.ToString();
        }
    }
}