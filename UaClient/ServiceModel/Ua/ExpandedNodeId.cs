// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public sealed class ExpandedNodeId : IEquatable<ExpandedNodeId>
    {
        public static readonly ExpandedNodeId Null = new ExpandedNodeId(0);

        public ExpandedNodeId(uint identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            this.NodeId = new NodeId(identifier);
            this.NamespaceUri = namespaceUri;
            this.ServerIndex = serverIndex;
        }

        public ExpandedNodeId(string identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            this.NodeId = new NodeId(identifier);
            this.NamespaceUri = namespaceUri;
            this.ServerIndex = serverIndex;
        }

        public ExpandedNodeId(Guid identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            this.NodeId = new NodeId(identifier);
            this.NamespaceUri = namespaceUri;
            this.ServerIndex = serverIndex;
        }

        public ExpandedNodeId(byte[] identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            this.NodeId = new NodeId(identifier);
            this.NamespaceUri = namespaceUri;
            this.ServerIndex = serverIndex;
        }

        public ExpandedNodeId(NodeId identifier, string? namespaceUri = null, uint serverIndex = 0)
        {
            this.NodeId = identifier;
            this.NamespaceUri = namespaceUri;
            this.ServerIndex = serverIndex;
        }

        public NodeId NodeId { get; }

        public string? NamespaceUri { get; }

        public uint ServerIndex { get; }

        public static bool operator ==(ExpandedNodeId? a, ExpandedNodeId? b)
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

        public static bool operator !=(ExpandedNodeId? a, ExpandedNodeId? b)
        {
            return !(a == b);
        }

        public static bool IsNull(ExpandedNodeId? nodeId)
        {
            return (nodeId == null) || NodeId.IsNull(nodeId.NodeId);
        }

        public static NodeId ToNodeId(ExpandedNodeId value, IList<string>? namespaceUris)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(nameof(value));
            }

            ushort ns = value.NodeId.NamespaceIndex;
            var nsu = value.NamespaceUri;
            if (namespaceUris != null && !string.IsNullOrEmpty(nsu))
            {
                int i = namespaceUris.IndexOf(nsu!);
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

        public override bool Equals(object? o)
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
            int result = this.NodeId.GetHashCode();
            result = (397 * result) ^ (this.NamespaceUri != null ? this.NamespaceUri.GetHashCode() : 0);
            result = (397 * result) ^ (int)this.ServerIndex;
            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (this.ServerIndex > 0)
            {
                sb.AppendFormat("svr={0};", this.ServerIndex);
            }

            if (!string.IsNullOrEmpty(this.NamespaceUri))
            {
                sb.AppendFormat("nsu={0};", this.NamespaceUri);
            }

            sb.Append(this.NodeId.ToString());
            return sb.ToString();
        }
    }
}