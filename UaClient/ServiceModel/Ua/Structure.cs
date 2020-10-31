// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A base implementation of a Structure.
    /// </summary>
    [DataTypeId(DataTypeIds.Structure)]
    public abstract class Structure : IEncodable
    {
        public abstract void Encode(IEncoder encoder);

        public abstract void Decode(IDecoder decoder);
    }
}
