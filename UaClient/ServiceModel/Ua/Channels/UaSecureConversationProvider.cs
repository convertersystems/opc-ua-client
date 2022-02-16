// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// The <see cref="IConversationProvider"/> interface implementation
    /// for the OPC UA Secure Conversation (UASC).
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/6.7.1/">OPC UA specification Part 6: Mappings, 7.2</seealso>
    public class UaSecureConversationProvider : IConversationProvider
    {
        /// <inheritdoc />
        public async Task<IConversation> CreateAsync(EndpointDescription remoteEndpoint, ApplicationDescription localDescription, TransportConnectionOptions options, ICertificateStore? certificateStore, ILogger? logger, CancellationToken token)
        {
            var conversation = new UaSecureConversation(localDescription, options, certificateStore, logger)
            {
                SecurityMode = remoteEndpoint.SecurityMode
            };

            await conversation.SetRemoteCertificateAsync(remoteEndpoint.SecurityPolicyUri, remoteEndpoint.ServerCertificate, token).ConfigureAwait(false);

            return conversation;
        }
    }
}
