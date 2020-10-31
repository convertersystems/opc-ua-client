// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    [DataTypeId(DataTypeIds.ExpandedNodeId)]
    public sealed class ExpandedNodeId : IEquatable<ExpandedNodeId?>
    {
        public static readonly ExpandedNodeId Null = new ExpandedNodeId(0);

        public ExpandedNodeId(uint identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = new NodeId(identifier);
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public ExpandedNodeId(string identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = new NodeId(identifier);
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public ExpandedNodeId(Guid identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = new NodeId(identifier);
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public ExpandedNodeId(byte[] identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = new NodeId(identifier);
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public ExpandedNodeId(NodeId identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            NodeId = identifier;
            NamespaceUri = namespaceUri;
            ServerIndex = serverIndex;
        }

        public NodeId NodeId { get; }

        public string? NamespaceUri { get; }

        public uint ServerIndex { get; }

        public static bool IsNull(ExpandedNodeId? nodeId)
        {
            return (nodeId == null) || NodeId.IsNull(nodeId.NodeId);
        }

        public static NodeId ToNodeId(ExpandedNodeId value, IReadOnlyList<string> namespaceUris)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (namespaceUris is null)
            {
                throw new ArgumentNullException(nameof(namespaceUris));
            }
            if (string.IsNullOrEmpty(value.NamespaceUri))
            {
                return value.NodeId;
            }
            int ns = -1;
            for (int i = 0; i < namespaceUris.Count; i++)
            {
                if (namespaceUris[i].Equals(value.NamespaceUri!))
                {
                    ns = i;
                    break;
                }
            }
            if (ns < 0)
            {
                throw new IndexOutOfRangeException();
            }
            switch (value.NodeId.IdType)
            {
                case IdType.Numeric:
                    return new NodeId((uint)value.NodeId.Identifier, (ushort)ns);

                case IdType.String:
                    return new NodeId((string)value.NodeId.Identifier, (ushort)ns);

                case IdType.Guid:
                    return new NodeId((Guid)value.NodeId.Identifier, (ushort)ns);

                case IdType.Opaque:
                    return new NodeId((byte[])value.NodeId.Identifier, (ushort)ns);

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public static bool TryParse(string s, [NotNullWhen(returnValue: true)] out ExpandedNodeId? value)
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

                string? nsu = null;
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

                if (NodeId.TryParse(s, out var nodeId))
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
            if (!ExpandedNodeId.TryParse(s, out var value))
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdInvalid);
            }

            return value;
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

        public override bool Equals(object? obj)
        {
            return Equals(obj as ExpandedNodeId);
        }

        public bool Equals(ExpandedNodeId? other)
        {
            return other != null &&
                   EqualityComparer<NodeId>.Default.Equals(NodeId, other.NodeId) &&
                   NamespaceUri == other.NamespaceUri &&
                   ServerIndex == other.ServerIndex;
        }

        public override int GetHashCode()
        {
            int hashCode = -641591048;
            hashCode = hashCode * -1521134295 + EqualityComparer<NodeId>.Default.GetHashCode(NodeId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(NamespaceUri);
            hashCode = hashCode * -1521134295 + ServerIndex.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(ExpandedNodeId? left, ExpandedNodeId? right)
        {
            return EqualityComparer<ExpandedNodeId?>.Default.Equals(left, right);
        }

        public static bool operator !=(ExpandedNodeId? left, ExpandedNodeId? right)
        {
            return !(left == right);
        }
    }
}