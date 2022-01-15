// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.ServiceModel.Ua.Channels
{
    public static class StackProfiles
    {
        /// <summary>
        /// A stack profile for the UA-TCP UA-SC UA-Binary profile
        /// (<c>http://opcfoundation.org/UA-Profile/Transport/uatcp-uasc-uabinary</c>).
        /// </summary>
        public static StackProfile TcpUascBinary { get; }
            = new StackProfile(
                    new UaTcpConnectionProvider(),
                    new UaSecureConversationProvider(),
                    new BinaryEncodingProvider()
                );
    }
}
