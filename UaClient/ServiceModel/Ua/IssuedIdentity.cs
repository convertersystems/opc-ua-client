// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    public class IssuedIdentity : IUserIdentity
    {
        public IssuedIdentity(byte[] tokenData)
        {
            this.TokenData = tokenData;
        }

        public byte[] TokenData { get; }
    }
}
