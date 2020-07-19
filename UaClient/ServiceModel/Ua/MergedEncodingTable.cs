// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Helper class to combine several encoding tables.
    /// </summary>
    public class MergedEncodingTable : IEnumerable<(ExpandedNodeId, Type)>
    {
        private IEnumerable<(ExpandedNodeId, Type)> table = Enumerable.Empty<(ExpandedNodeId,Type)>();

        /// <summary>
        /// Adds an encoding table.
        /// </summary>
        /// <param name="list">The encoding table to add.</param>
        public void Add(IEnumerable<(ExpandedNodeId, Type)> list)
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            this.table = this.table.Concat(list);
        }

        /// <inheritdoc/>
        public IEnumerator<(ExpandedNodeId, Type)> GetEnumerator()
            => this.table.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => this.table.GetEnumerator();
    }
}
