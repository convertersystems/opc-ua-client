// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// The <see cref="ITransportConnectionProvider"/> interface implementation
    /// for the OPC UA TCP transport protocol.
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/7.2/">OPC UA specification Part 6: Mappings, 7.2</seealso>
    public class UaTcpConnectionProvider : ITransportConnectionProvider
    {
        /// <summary>
        /// Connection timeout defaults to 5.0s
        /// </summary>
        public static int ConnectTimeout = 5000;

        /// <inheritdoc />
        public async Task<ITransportConnection> ConnectAsync(string connectionString, CancellationToken token)
        {
            var uri = new Uri(connectionString);
            var client = new TcpClient
            {
                NoDelay = true
            };

            await client.ConnectAsync(uri.Host, uri.Port).TimeoutAfter(ConnectTimeout, token).ConfigureAwait(false);

            // The stream will own the client and takes care on disposing/closing it
            return new UaClientConnection(client.GetStream(), uri);
        }
    }
}
