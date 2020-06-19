// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Sets the result of create, read, write, or publish service calls.
    /// </summary>
    public interface ISetDataErrorInfo
    {
        /// <summary>
        /// Sets the result of a create, read, write, or publish service call.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="errors">The error messages.</param>
        void SetErrors(string propertyName, IEnumerable<string>? errors);
    }
}
