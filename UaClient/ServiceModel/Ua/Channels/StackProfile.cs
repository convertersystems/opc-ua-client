// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// The stack profile class.
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/4/">OPC UA specification Part 6: Mappings, 4</seealso>
    public class StackProfile
    {
        /// <summary>
        /// The transport connection provider.
        /// </summary>
        public ITransportConnectionProvider TransportConnectionProvider { get; }

        /// <summary>
        /// The conversation provider.
        /// </summary>
        public IConversationProvider ConversationProvider { get; }

        /// <summary>
        /// The encoding provider.
        /// </summary>
        public IEncodingProvider EncodingProvider { get; }

        /// <summary>
        /// Creates a stack profile instance.
        /// </summary>
        /// <param name="transportConnectionProvider">The transport connection provider.</param>
        /// <param name="conversationProvider">The conversation provider.</param>
        /// <param name="encodingProvider">The encoding provider.</param>
        public StackProfile(ITransportConnectionProvider transportConnectionProvider, IConversationProvider conversationProvider, IEncodingProvider encodingProvider)
        {
            TransportConnectionProvider = transportConnectionProvider;
            ConversationProvider = conversationProvider;
            EncodingProvider = encodingProvider;
        }
    }
}
