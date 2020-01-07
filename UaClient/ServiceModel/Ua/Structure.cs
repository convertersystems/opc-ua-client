// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A base implementation of a Structure.
    /// </summary>
    public class Structure : IEncodable
    {
        public virtual void Encode(IEncoder encoder)
        {
        }

        public virtual void Decode(IDecoder decoder)
        {
        }
    }
}
