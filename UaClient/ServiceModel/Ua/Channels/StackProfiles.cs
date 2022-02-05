// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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

        /// <summary>
        /// Get the <see cref="StackProfile"/> for the given endpoint.
        /// </summary>
        /// <remarks>
        /// If no ´matching stack is found, <see cref="TcpUascBinary"/> will
        /// be returned.
        /// </remarks>
        /// <param name="remoteEndpoint">The endpoint.</param>
        /// <returns>A matching stack.</returns>
        public static StackProfile GetStackProfile(EndpointDescription remoteEndpoint)
        {
            switch (remoteEndpoint.TransportProfileUri)
            {
                case TransportProfileUris.UaTcpTransport:
                    return TcpUascBinary;
                 // Use TcpUascBinary as fallback, or should we throw here?
                default:
                    return TcpUascBinary;
            }
        }
    }
}
