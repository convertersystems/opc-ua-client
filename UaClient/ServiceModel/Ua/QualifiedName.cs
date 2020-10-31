// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{

    [DataTypeId(DataTypeIds.QualifiedName)]
    public sealed class QualifiedName : IEquatable<QualifiedName?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QualifiedName"/> class.
        /// </summary>
        /// <param name="name">the text portion of the QualifiedName. </param>
        /// <param name="namespaceIndex">index that identifies the namespace that qualifies the name.</param>
        public QualifiedName(string? name, ushort namespaceIndex = 0)
        {
            Name = name;
            NamespaceIndex = namespaceIndex;
        }

        public string? Name { get; private set; }

        public ushort NamespaceIndex { get; private set; }

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

        public override string ToString()
        {
            return $"{NamespaceIndex}:{Name}";
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as QualifiedName);
        }

        public bool Equals(QualifiedName? other)
        {
            return other != null &&
                   Name == other.Name &&
                   NamespaceIndex == other.NamespaceIndex;
        }

        public override int GetHashCode()
        {
            int hashCode = 978021522;
            if (Name != null) hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + NamespaceIndex.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(QualifiedName? left, QualifiedName? right)
        {
            return EqualityComparer<QualifiedName?>.Default.Equals(left, right);
        }

        public static bool operator !=(QualifiedName? left, QualifiedName? right)
        {
            return !(left == right);
        }
    }
}