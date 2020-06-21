// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public class NodeId : IEquatable<NodeId>
    {
        public static readonly NodeId Null = new NodeId(0);

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeId"/> class.
        /// </summary>
        /// <param name="identifier">the identifier for a node within a namespace</param>
        /// <param name="namespaceIndex">the index of the namespace in the NamespaceArray. An index of 0 corresponds to "http://opcfoundation.org/UA/".</param>
        public NodeId(uint identifier, ushort namespaceIndex = 0)
        {
            this.Identifier = identifier;
            this.IdType = IdType.Numeric;
            this.NamespaceIndex = namespaceIndex;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeId"/> class.
        /// </summary>
        /// <param name="identifier">the identifier for a node within a namespace</param>
        /// <param name="namespaceIndex">the index of the namespace in the NamespaceArray. An index of 0 corresponds to "http://opcfoundation.org/UA/".</param>
        public NodeId(string identifier, ushort namespaceIndex = 0)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            this.Identifier = identifier;
            this.IdType = IdType.String;
            this.NamespaceIndex = namespaceIndex;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeId"/> class.
        /// </summary>
        /// <param name="identifier">the identifier for a node within a namespace</param>
        /// <param name="namespaceIndex">the index of the namespace in the NamespaceArray. An index of 0 corresponds to "http://opcfoundation.org/UA/".</param>
        public NodeId(Guid identifier, ushort namespaceIndex = 0)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            this.NamespaceIndex = namespaceIndex;
            this.Identifier = identifier;
            this.IdType = IdType.Guid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeId"/> class.
        /// </summary>
        /// <param name="identifier">the identifier for a node within a namespace</param>
        /// <param name="namespaceIndex">the index of the namespace in the NamespaceArray. An index of 0 corresponds to "http://opcfoundation.org/UA/".</param>
        public NodeId(byte[] identifier, ushort namespaceIndex = 0)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            this.NamespaceIndex = namespaceIndex;
            this.Identifier = identifier;
            this.IdType = IdType.Opaque;
        }

        public ushort NamespaceIndex { get; }

        public object Identifier { get; }

        public IdType IdType { get; }

        public static bool operator ==(NodeId? a, NodeId? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            switch (a.IdType)
            {
                case IdType.Numeric:
                    return (a.IdType == b.IdType) && ((uint)a.Identifier == (uint)b.Identifier) && (a.NamespaceIndex == b.NamespaceIndex);

                case IdType.String:
                    return (a.IdType == b.IdType) && ((string)a.Identifier == (string)b.Identifier) && (a.NamespaceIndex == b.NamespaceIndex);

                case IdType.Guid:
                    return (a.IdType == b.IdType) && ((Guid)a.Identifier == (Guid)b.Identifier) && (a.NamespaceIndex == b.NamespaceIndex);

                case IdType.Opaque:
                    return (a.IdType == b.IdType) && ((byte[])a.Identifier).SequenceEqual((byte[])b.Identifier) && (a.NamespaceIndex == b.NamespaceIndex);

                default:
                    return false;
            }
        }

        public static bool operator !=(NodeId? a, NodeId? b)
        {
            return !(a == b);
        }

        public static bool IsNull(NodeId nodeId)
        {
            return (nodeId == null) || nodeId == Null;
        }

        public static ExpandedNodeId ToExpandedNodeId(NodeId value, IList<string>? namespaceUris)
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(nameof(value));
            }

            ushort ns = value.NamespaceIndex;
            if (namespaceUris != null && ns > 0 && ns < namespaceUris.Count)
            {
                var nsu = namespaceUris[ns];

                switch (value.IdType)
                {
                    case IdType.Numeric:
                        return new ExpandedNodeId((uint)value.Identifier, nsu);

                    case IdType.String:
                        return new ExpandedNodeId((string)value.Identifier, nsu);

                    case IdType.Guid:
                        return new ExpandedNodeId((Guid)value.Identifier, nsu);

                    case IdType.Opaque:
                        return new ExpandedNodeId((byte[])value.Identifier, nsu);

                    default:
                        throw new InvalidOperationException();
                }
            }

            return new ExpandedNodeId(value);
        }

        public static bool TryParse(string s, [NotNullWhen(returnValue: true)] out NodeId value)
        {
            try
            {
                ushort ns = 0;
                if (s.StartsWith("ns=", StringComparison.Ordinal))
                {
                    int pos = s.IndexOf(';');
                    if (pos == -1)
                    {
                        throw new ServiceResultException(StatusCodes.BadNodeIdInvalid);
                    }

                    ns = ushort.Parse(s.Substring(3, pos - 3), CultureInfo.InvariantCulture);
                    s = s.Substring(pos + 1);
                }

                if (s.StartsWith("i=", StringComparison.Ordinal))
                {
                    value = new NodeId(uint.Parse(s.Substring(2), CultureInfo.InvariantCulture), ns);
                    return true;
                }
                else if (s.StartsWith("s=", StringComparison.Ordinal))
                {
                    value = new NodeId(s.Substring(2), ns);
                    return true;
                }
                else if (s.StartsWith("g=", StringComparison.Ordinal))
                {
                    value = new NodeId(Guid.Parse(s.Substring(2)), ns);
                    return true;
                }
                else if (s.StartsWith("b=", StringComparison.Ordinal))
                {
                    value = new NodeId(Convert.FromBase64String(s.Substring(2)), ns);
                    return true;
                }
                else
                {
                    value = Null;
                    return false;
                }
            }
            catch (Exception)
            {
                value = Null;
                return false;
            }
        }

        public static NodeId Parse(string s)
        {
            NodeId value;
            if (!NodeId.TryParse(s, out value))
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdInvalid);
            }

            return value;
        }

        public override bool Equals(object? o)
        {
            if (o is NodeId)
            {
                return this == (NodeId)o;
            }

            return false;
        }

        public bool Equals(NodeId? that)
        {
            return this == that;
        }

        public override int GetHashCode()
        {
            int result = this.NamespaceIndex.GetHashCode();

            if (this.IdType == IdType.Opaque)
            {
                foreach (var b in (byte[])this.Identifier)
                {
                    result = (397 * result) ^ b.GetHashCode();
                }
            }
            else
            {
                result = (397 * result) ^ this.Identifier.GetHashCode();
            }
            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (this.NamespaceIndex > 0)
            {
                sb.AppendFormat("ns={0};", this.NamespaceIndex);
            }

            switch (this.IdType)
            {
                case IdType.Numeric:
                    sb.AppendFormat("i={0}", this.Identifier);
                    break;
                case IdType.String:
                    sb.AppendFormat("s={0}", this.Identifier);
                    break;
                case IdType.Guid:
                    sb.AppendFormat("g={0}", this.Identifier);
                    break;
                case IdType.Opaque:
                    sb.AppendFormat("b={0}", Convert.ToBase64String((byte[])this.Identifier));
                    break;
            }

            return sb.ToString();
        }
    }
}