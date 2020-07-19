// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Helper class to build an encoding table.
    /// </summary>
    public class CustomEncodingTable : IEnumerable<(ExpandedNodeId, Type)>
    {
        protected readonly List<(ExpandedNodeId, Type)> encodingTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomEncodingTable"/> class.
        /// </summary>
        public CustomEncodingTable()
        {
            this.encodingTable = new List<(ExpandedNodeId, Type)>();
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomEncodingTable"/> class.
        /// </summary>
        /// <param name="encodingTable">The encoding table to add.</param>
        public CustomEncodingTable(IEnumerable<(ExpandedNodeId, Type)> encodingTable)
        {
            if (encodingTable is null)
            {
                throw new ArgumentNullException(nameof(encodingTable));
            }

            this.encodingTable = encodingTable.ToList();
        }
        
        /// <summary>
        /// Adds an encoding id and system type pair.
        /// </summary>
        /// <param name="encodingId">The encoding id.</param>
        /// <param name="type">The system type.</param>
        public void Add(ExpandedNodeId encodingId, Type type)
        {
            this.encodingTable.Add((encodingId, type));
        }
       
        /// <summary>
        /// Adds an encoding id and system type pair.
        /// </summary>
        /// <param name="item">The encoding id and system type pair.</param>
        public void Add((ExpandedNodeId, Type) item)
        {
            this.encodingTable.Add(item);
        }

        /// <inheritdoc/>
        public IEnumerator<(ExpandedNodeId, Type)> GetEnumerator()
            => this.encodingTable.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();
    }

    /// <summary>
    /// Helper class to build an encoding table from attribute annotations.
    /// </summary>
    public class CustomEncodingTable<TAttribute> : CustomEncodingTable
        where TAttribute : Attribute, IEncodingIdAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomEncodingTable"/> class.
        /// </summary>
        public CustomEncodingTable()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomEncodingTable"/> class.
        /// </summary>
        /// <param name="encodingTable">The encoding table to add.</param>
        public CustomEncodingTable(IEnumerable<(ExpandedNodeId, Type)> encodingTable)
            : base(encodingTable)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomEncodingTable"/> class
        /// and adds the given system types to the table. The types need to 
        /// implement the <see cref="IEncodable"/> interface and be
        /// annotated by the <see cref="TAttribute"/> attribute.
        /// </summary>
        /// <param name="types">The system types to add.</param>
        public CustomEncodingTable(IEnumerable<Type> types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (var type in types)
            {
                this.Add(type);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomEncodingTable"/> class
        /// and adds all <see cref="IEncodable"/> types from the given assembly
        /// that are annotated by the <see cref="TAttribute"/> attribute.
        /// </summary>
        /// <param name="types">The assembly to scan.</param>
        public CustomEncodingTable(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var table = from type in assembly.ExportedTypes
                        let info = type.GetTypeInfo()
                        where info.ImplementedInterfaces.Contains(typeof(IEncodable))
                        let attr = info.GetCustomAttribute<TAttribute>(false)
                        where attr != null
                        select (attr.NodeId, type);
            this.encodingTable.AddRange(table);
        }
       
        /// <summary>
        /// Adds a system type to the encoding table. The type needs to 
        /// implement the <see cref="IEncodable"/> interface and be
        /// annotated by the <see cref="TAttribute"/> attribute.
        /// </summary>
        /// <param name="type">The system type.</param>
        public void Add(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            var info = type.GetTypeInfo();

            if (!info.ImplementedInterfaces.Contains(typeof(IEncodable)))
            {
                throw new ArgumentException($"Type '{type}' does not implement the {typeof(IEncodable)} interface.");
            }

            var attr = info.GetCustomAttribute<TAttribute>(false);
            if (attr == null)
            {
                throw new ArgumentException($"Type '{type}' does not have a {typeof(TAttribute)} attribute.");
            }
            
            this.Add(attr.NodeId, type);
        }
    }
}
