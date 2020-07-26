// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public class NodeId : IEquatable<NodeId?>
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

        public static bool IsNull(NodeId nodeId)
        {
            return (nodeId == null) || nodeId == Null;
        }

        public static ExpandedNodeId ToExpandedNodeId(NodeId value, IList<string> namespaceUris)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (namespaceUris is null)
            {
                throw new ArgumentNullException(nameof(namespaceUris));
            }
            ushort ns = value.NamespaceIndex;
            if (ns == 0)
            {
                return new ExpandedNodeId(value);
            }
            if (ns > 0 && ns < namespaceUris.Count)
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

            throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
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
            if (!TryParse(s, out NodeId value))
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdInvalid);
            }

            return value;
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
        public override bool Equals(object? obj)
        {
            return Equals(obj as NodeId);
        }

        public bool Equals(NodeId? other)
        {
            if (other != null &&
                   NamespaceIndex == other.NamespaceIndex &&
                   IdType == other.IdType)
            {
                switch (this.IdType)
                {
                    case IdType.Numeric:
                        return EqualityComparer<uint>.Default.Equals((uint)Identifier, (uint)other.Identifier);

                    case IdType.String:
                        return EqualityComparer<string>.Default.Equals((string)Identifier, (string)other.Identifier);

                    case IdType.Guid:
                        return EqualityComparer<Guid>.Default.Equals((Guid)Identifier, (Guid)other.Identifier);

                    case IdType.Opaque:
                        return ByteSequenceComparer.Equals((byte[])Identifier, (byte[])other.Identifier);
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = 387039986;
            hashCode = hashCode * -1521134295 + NamespaceIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + IdType.GetHashCode();
            switch (this.IdType)
            {
                case IdType.Numeric:
                    hashCode = hashCode * -1521134295 + EqualityComparer<uint>.Default.GetHashCode((uint)Identifier);
                    break;

                case IdType.String:
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode((string)Identifier);
                    break;

                case IdType.Guid:
                    hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode((Guid)Identifier);
                    break;

                case IdType.Opaque:
                    hashCode = hashCode * -1521134295 + ByteSequenceComparer.GetHashCode((byte[])Identifier);
                    break;
            }

            return hashCode;
        }

        public static bool operator ==(NodeId? left, NodeId? right)
        {
            return EqualityComparer<NodeId?>.Default.Equals(left, right);
        }

        public static bool operator !=(NodeId? left, NodeId? right)
        {
            return !(left == right);
        }
    }
}