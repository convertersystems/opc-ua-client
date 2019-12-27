// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    public sealed class QualifiedName
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QualifiedName"/> class.
        /// </summary>
        /// <param name="name">the text portion of the QualifiedName. </param>
        /// <param name="namespaceIndex">index that identifies the namespace that qualifies the name.</param>
        public QualifiedName(string? name, ushort namespaceIndex = 0)
        {
            this.Name = name;
            this.NamespaceIndex = namespaceIndex;
        }

        public string? Name { get; private set; }

        public ushort NamespaceIndex { get; private set; }

        public static bool operator ==(QualifiedName? a, QualifiedName? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return (a.Name == b.Name) && (a.NamespaceIndex == b.NamespaceIndex);
        }

        public static bool operator !=(QualifiedName? a, QualifiedName? b)
        {
            return !(a == b);
        }

        public static bool TryParse(string s, out QualifiedName qname)
        {
            try
            {
                string[] ss = s.Split(new[] { ':' }, 2);
                ushort ns = 0;
                string name = s;
                if (ss.Length > 1)
                {
                    ns = ushort.Parse(ss[0]);
                    name = ss[1];
                }

                qname = new QualifiedName(name, ns);
                return true;
            }
            catch (Exception)
            {
                qname = new QualifiedName(string.Empty);
                return false;
            }
        }

        public static QualifiedName Parse(string s)
        {
            QualifiedName value;
            if (!QualifiedName.TryParse(s, out value))
            {
                throw new ArgumentException("Unable to parse QualifiedName.", nameof(s));
            }

            return value;
        }

        public override bool Equals(object? o)
        {
            if (o is QualifiedName)
            {
                return this == (QualifiedName)o;
            }

            return false;
        }

        public bool Equals(QualifiedName? that)
        {
            return this == that;
        }

        public override int GetHashCode()
        {
            int result = this.NamespaceIndex.GetHashCode();
            result = (397 * result) ^ (this.Name != null ? this.Name.GetHashCode() : 0);
            return result;
        }

        public override string ToString()
        {
            return $"{this.NamespaceIndex}:{this.Name}";
        }
    }
}