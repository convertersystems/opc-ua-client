// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// This interface should be implemented by every OPC Ua data type which is
    /// derived from the OPC UA structure type and has one or more optional
    /// fields.
    /// 
    /// Classes, that introduce optional fields first, should implement the
    /// <see cref="OptionalFieldCount"/> property as a virtual property getter.
    /// This allows derived classes to override this property, and thus
    /// increase the number of optional fields. Classes should also provide
    /// a `protected` setter method for the <see cref="EncodingMask"/>
    /// property to enable derived classes to set the bits belonging to
    /// its fields/properties.
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.7/">OPC UA specification Part 6: Mappings, 5.2.7</seealso>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.3.6/">OPC UA specification Part 6: Mappings, 5.3.6</seealso>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.4.7/">OPC UA specification Part 6: Mappings, 5.4.7</seealso>
    public interface IOptionalFields
    {
        /// <summary>
        /// Retrieves the count of optional fields, that is properities in dotnet.
        /// </summary>
        int OptionalFieldCount { get; }

        /// <summary>
        /// Retrieves the encoding mask.
        /// </summary>
        uint EncodingMask { get; }
    }
}
