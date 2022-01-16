// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Org.BouncyCastle.Crypto.Parameters;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A secure channel for communicating with OPC UA servers.
    /// </summary>
    public class ClientSecureChannel : ClientTransportChannel, IRequestChannel, IEncodingContext
    {
        /// <summary>
        /// The default timeout for requests.
        /// </summary>
        public const uint DefaultTimeoutHint = 15 * 1000; // 15 seconds

        /// <summary>
        /// the default diagnostic flags for requests.
        /// </summary>
        public const uint DefaultDiagnosticsHint = (uint)DiagnosticFlags.None;
        private const int _tokenRequestedLifetime = 60 * 60 * 1000; // 60 minutes

        private static readonly RecyclableMemoryStreamManager _streamManager = new RecyclableMemoryStreamManager();

        private readonly CancellationTokenSource _channelCts;
        private readonly ILogger? _logger;
        private readonly SemaphoreSlim _sendingSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _receivingSemaphore = new SemaphoreSlim(1, 1);
        private readonly ActionBlock<ServiceOperation> _pendingRequests;
        private readonly ConcurrentDictionary<uint, ServiceOperation> _pendingCompletions;

        private int _handle;
        private Task? _receiveResponsesTask;

        private DateTime _tokenRenewalTime = DateTime.MaxValue;
        private IConversation? _conversation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSecureChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The local description.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="remoteEndpoint">The remote endpoint</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The secure channel options.</param>
        public ClientSecureChannel(
            ApplicationDescription localDescription,
            ICertificateStore? certificateStore,
            EndpointDescription remoteEndpoint,
            ILoggerFactory? loggerFactory = null,
            ClientSecureChannelOptions? options = null,
            StackProfile? stackProfile = null)
            : base(remoteEndpoint, loggerFactory, options, stackProfile)
        {
            LocalDescription = localDescription ?? throw new ArgumentNullException(nameof(localDescription));
            CertificateStore = certificateStore;
            TimeoutHint = options?.TimeoutHint ?? DefaultTimeoutHint;
            DiagnosticsHint = options?.DiagnosticsHint ?? DefaultDiagnosticsHint;

            _logger = loggerFactory?.CreateLogger<ClientSecureChannel>();

            AuthenticationToken = null;
            NamespaceUris = new List<string> { "http://opcfoundation.org/UA/" };
            ServerUris = new List<string>();
            _channelCts = new CancellationTokenSource();
            _pendingRequests = new ActionBlock<ServiceOperation>(t => SendRequestActionAsync(t), new ExecutionDataflowBlockOptions { CancellationToken = _channelCts.Token });
            _pendingCompletions = new ConcurrentDictionary<uint, ServiceOperation>();
        }

        /// <summary>
        /// Gets the local description.
        /// </summary>
        public ApplicationDescription LocalDescription { get; }

        /// <summary>
        /// Gets the certificate store.
        /// </summary>
        public ICertificateStore? CertificateStore { get; }

        /// <summary>
        /// Gets the default number of milliseconds that may elapse before an operation is cancelled by the service.
        /// </summary>
        public uint TimeoutHint { get; }

        /// <summary>
        /// Gets the default diagnostics flags to be requested by the service.
        /// </summary>
        public uint DiagnosticsHint { get; }

        /// <summary>
        /// Gets the remote certificate.
        /// </summary>
        protected byte[]? RemoteCertificate => RemoteEndpoint?.ServerCertificate;

        /// <summary>
        /// Gets or sets the channel id.
        /// </summary>
        public uint ChannelId => _conversation?.ChannelId ?? 0;

        /// <summary>
        /// Gets or sets the token id.
        /// </summary>
        public uint TokenId => _conversation?.TokenId ?? 0;

        /// <summary>
        /// Gets or sets the authentication token.
        /// </summary>
        public NodeId? AuthenticationToken { get; protected set; }

        /// <summary>
        /// Gets or sets the namespace uris.
        /// </summary>
        public IReadOnlyList<string> NamespaceUris { get; protected set; }

        /// <summary>
        /// Gets or sets the server uris.
        /// </summary>
        public IReadOnlyList<string> ServerUris { get; protected set; }

        public int MaxStringLength => 65535;

        public int MaxArrayLength => 65535;

        public int MaxByteStringLength => 65535;

        /// <summary>
        /// Sends a <see cref="T:Workstation.ServiceModel.Ua.IServiceRequest"/> to the server.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual async Task<IServiceResponse> RequestAsync(IServiceRequest request, CancellationToken token = default)
        {
            ThrowIfClosedOrNotOpening();
            TimestampHeader(request);
            var operation = new ServiceOperation(request);
            // TimestampHeader takes care that the RequestHeader property will not be null
            using (var timeoutCts = new CancellationTokenSource((int)request.RequestHeader!.TimeoutHint))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _channelCts.Token, token))
            using (var registration = linkedCts.Token.Register(o => ((ServiceOperation)o!).TrySetException(new ServiceResultException(StatusCodes.BadRequestTimeout)), operation, false))
            {
                if (_pendingRequests.Post(operation))
                {
                    return await operation.Task.ConfigureAwait(false);
                }

                throw new ServiceResultException(StatusCodes.BadSecureChannelClosed);
            }
        }

        /// <inheritdoc/>
        protected override async Task OnOpeningAsync(CancellationToken token)
        {
            await base.OnOpeningAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.5.2/">OPC UA specification Part 4: Services, 5.5.2</seealso>
        protected override async Task OnOpenAsync(CancellationToken token = default)
        {
            await base.OnOpenAsync(token).ConfigureAwait(false);
            
            var options = new TransportConnectionOptions
            {
                ReceiveBufferSize = RemoteReceiveBufferSize,
                SendBufferSize = RemoteSendBufferSize,
                MaxMessageSize = RemoteMaxMessageSize,
                MaxChunkCount = RemoteMaxChunkCount
            };

            _conversation = await StackProfile.ConversationProvider.CreateAsync(RemoteEndpoint, LocalDescription, options, CertificateStore, _logger).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            _receiveResponsesTask = ReceiveResponsesAsync(_channelCts.Token);

            var openSecureChannelRequest = new OpenSecureChannelRequest
            {
                ClientProtocolVersion = ProtocolVersion,
                RequestType = SecurityTokenRequestType.Issue,
                SecurityMode = RemoteEndpoint.SecurityMode,
                ClientNonce = _conversation!.GetNextNonce(),
                RequestedLifetime = _tokenRequestedLifetime
            };

            var openSecureChannelResponse = (OpenSecureChannelResponse)await RequestAsync(openSecureChannelRequest).ConfigureAwait(false);

            if (openSecureChannelResponse.ServerProtocolVersion < ProtocolVersion)
            {
                throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
            }
        }

        /// <inheritdoc/>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.5.3/">OPC UA specification Part 4: Services, 5.5.3</seealso>
        protected override async Task OnCloseAsync(CancellationToken token = default)
        {
            try
            {
                var request = new CloseSecureChannelRequest { RequestHeader = new RequestHeader { TimeoutHint = TimeoutHint, ReturnDiagnostics = DiagnosticsHint } };
                await RequestAsync(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error closing secure channel. {ex.Message}");
            }

            await base.OnCloseAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task OnAbortAsync(CancellationToken token = default)
        {
            await base.OnAbortAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Send service request on transport channel.
        /// </summary>
        /// <param name="operation">A service operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendRequestActionAsync(ServiceOperation operation)
        {
            try
            {
                if (operation.Task.Status == TaskStatus.WaitingForActivation)
                {
                    await SendRequestAsync(operation, _channelCts.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error sending request. {ex.Message}");
                await FaultAsync(ex).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send service request on transport channel.
        /// </summary>
        /// <param name="operation">A service operation.</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendRequestAsync(ServiceOperation operation, CancellationToken token = default)
        {
            await _sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
            var request = operation.Request;
            try
            {
                ThrowIfClosedOrNotOpening();

                // Check if time to renew security token.
                if (DateTime.UtcNow > _tokenRenewalTime)
                {
                    _tokenRenewalTime = _tokenRenewalTime.AddMilliseconds(60000);
                    var openSecureChannelRequest = new OpenSecureChannelRequest
                    {
                        RequestHeader = new RequestHeader
                        {
                            TimeoutHint = TimeoutHint,
                            ReturnDiagnostics = DiagnosticsHint,
                            Timestamp = DateTime.UtcNow,
                            RequestHandle = GetNextHandle(),
                            AuthenticationToken = AuthenticationToken
                        },
                        ClientProtocolVersion = ProtocolVersion,
                        RequestType = SecurityTokenRequestType.Renew,
                        SecurityMode = RemoteEndpoint.SecurityMode,
                        ClientNonce = _conversation!.GetNextNonce(),
                        RequestedLifetime = _tokenRequestedLifetime
                    };
                    _logger?.LogTrace($"Sending {openSecureChannelRequest.GetType().Name}, Handle: {openSecureChannelRequest.RequestHeader.RequestHandle}");
                    _pendingCompletions.TryAdd(openSecureChannelRequest.RequestHeader.RequestHandle, new ServiceOperation(openSecureChannelRequest));
                    await SendOpenSecureChannelRequestAsync(openSecureChannelRequest, token).ConfigureAwait(false);
                }

                // RequestAsync takes care that every request has a non-null header
                var header = request.RequestHeader!;
                header.RequestHandle = GetNextHandle();
                header.AuthenticationToken = AuthenticationToken;

                _logger?.LogTrace($"Sending {request.GetType().Name}, Handle: {header.RequestHandle}");
                _pendingCompletions.TryAdd(header.RequestHandle, operation);
                if (request is OpenSecureChannelRequest)
                {
                    await SendOpenSecureChannelRequestAsync((OpenSecureChannelRequest)request, token).ConfigureAwait(false);
                }
                else if (request is CloseSecureChannelRequest)
                {
                    await SendCloseSecureChannelRequestAsync((CloseSecureChannelRequest)request, token).ConfigureAwait(false);
                    operation.TrySetResult(new CloseSecureChannelResponse { ResponseHeader = new ResponseHeader { RequestHandle = header.RequestHandle, Timestamp = DateTime.UtcNow } });
                }
                else
                {
                    await SendServiceRequestAsync(request, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error sending {request.GetType().Name}, Handle: {request.RequestHeader!.RequestHandle}. {ex.Message}");
                throw;
            }
            finally
            {
                _sendingSemaphore.Release();
            }
        }

        /// <summary>
        /// Send open secure channel service request on transport channel.
        /// </summary>
        /// <param name="request">A service request</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendOpenSecureChannelRequestAsync(OpenSecureChannelRequest request, CancellationToken token)
        {
            var bodyStream = _streamManager.GetStream("SendOpenSecureChannelRequestAsync");
            using (var bodyEncoder = StackProfile.EncodingProvider.CreateEncoder(bodyStream, this, keepStreamOpen: false))
            {
                bodyEncoder.WriteRequest(request);
                bodyStream.Position = 0;

                var handle = request.RequestHeader!.RequestHandle;
                await _conversation!.EncryptMessageAsync(bodyStream, MessageTypes.OPNF, handle, SendAsync, token);
            }
        }

        /// <summary>
        /// Send close secure channel request on transport channel.
        /// </summary>
        /// <param name="request">A service request</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendCloseSecureChannelRequestAsync(CloseSecureChannelRequest request, CancellationToken token)
        {
            var bodyStream = _streamManager.GetStream("SendCloseSecureChannelRequestAsync");
            using (var bodyEncoder = StackProfile.EncodingProvider.CreateEncoder(bodyStream, this, keepStreamOpen: false))
            {
                bodyEncoder.WriteRequest(request);
                bodyStream.Position = 0;

                var handle = request.RequestHeader!.RequestHandle;
                await _conversation!.EncryptMessageAsync(bodyStream, MessageTypes.CLOF, handle, SendAsync, token);
            }
        }

        /// <summary>
        /// Send service request on transport channel.
        /// </summary>
        /// <param name="request">A service request</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendServiceRequestAsync(IServiceRequest request, CancellationToken token)
        {
            var bodyStream = _streamManager.GetStream("SendServiceRequestAsync");
            using (var bodyEncoder = StackProfile.EncodingProvider.CreateEncoder(bodyStream, this, keepStreamOpen: false))
            {
                bodyEncoder.WriteRequest(request);
                bodyStream.Position = 0;

                var handle = request.RequestHeader!.RequestHandle;
                await _conversation!.EncryptMessageAsync(bodyStream, MessageTypes.MSGF, handle, SendAsync, token);
            }
        }

        /// <summary>
        /// Start a task to receive service responses from transport channel.
        /// </summary>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task ReceiveResponsesAsync(CancellationToken token = default)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var response = await ReceiveResponseAsync().ConfigureAwait(false);
                    if (response == null)
                    {
                        // Null response indicates socket closed. This is expected when closing secure channel.
                        _channelCts.Cancel();
                        if (State == CommunicationState.Closed || State == CommunicationState.Closing)
                        {
                            return;
                        }

                        throw new ServiceResultException(StatusCodes.BadServerNotConnected);
                    }

                    var header = response.ResponseHeader!;
                    if (_pendingCompletions.TryRemove(header.RequestHandle, out var tcs))
                    {
                        if (StatusCode.IsBad(header.ServiceResult))
                        {
                            var ex = new ServiceResultException(new ServiceResult(header.ServiceResult, header.ServiceDiagnostics, header.StringTable));
                            tcs.TrySetException(ex);
                        }
                        else
                        {
                            tcs.TrySetResult(response);
                        }

                        continue;
                    }

                    // TODO: remove when open62541 server corrected.
                    if (header.RequestHandle == 0)
                    {
                        ServiceOperation? tcs2 = null;
                        if (response is OpenSecureChannelResponse)
                        {
                            tcs2 = _pendingCompletions.OrderBy(k => k.Key).Select(k => k.Value).FirstOrDefault(o => o.Request is OpenSecureChannelRequest);
                        }
                        else if (response is CloseSecureChannelResponse)
                        {
                            tcs2 = _pendingCompletions.OrderBy(k => k.Key).Select(k => k.Value).FirstOrDefault(o => o.Request is CloseSecureChannelRequest);
                        }

                        if (tcs2 != null)
                        {
                            _pendingCompletions.TryRemove(tcs2.Request.RequestHeader!.RequestHandle, out _);
                            if (StatusCode.IsBad(header.ServiceResult))
                            {
                                var ex = new ServiceResultException(new ServiceResult(header.ServiceResult, header.ServiceDiagnostics, header.StringTable));
                                tcs2.TrySetException(ex);
                            }
                            else
                            {
                                tcs2.TrySetResult(response);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error receiving response. {ex.Message}");
                await FaultAsync(ex).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Receive next service response from transport channel.
        /// </summary>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<IServiceResponse?> ReceiveResponseAsync(CancellationToken token = default)
        {
            await _receivingSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                ThrowIfClosedOrNotOpening();

                var bodyStream = _streamManager.GetStream("ReceiveResponseAsync");
                var bodyDecoder = StackProfile.EncodingProvider.CreateDecoder(bodyStream, this, keepStreamOpen: false);
                try
                {
                    var ret = await _conversation!.DecryptMessageAsync(bodyStream, ReceiveAsync, token).ConfigureAwait(false);
                    if (ret == (0, 0))
                    {
                        return null;
                    }

                    bodyStream.Seek(0L, SeekOrigin.Begin);
                    var response = (IServiceResponse)bodyDecoder.ReadResponse();
                    
                    _logger?.LogTrace($"Received {response.GetType().Name}, Handle: {response.ResponseHeader!.RequestHandle} Result: {response.ResponseHeader.ServiceResult}");

                    // special inline processing for token renewal because we need to
                    // hold both the sending and receiving semaphores to update the security keys.
                    var openSecureChannelResponse = response as OpenSecureChannelResponse;
                    if (openSecureChannelResponse != null && StatusCode.IsGood(openSecureChannelResponse.ResponseHeader!.ServiceResult))
                    {
                        _tokenRenewalTime = DateTime.UtcNow.AddMilliseconds(0.8 * openSecureChannelResponse.SecurityToken!.RevisedLifetime);

                        await _sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
                        try
                        {
                            _conversation.ChannelId = openSecureChannelResponse.SecurityToken.ChannelId;
                            _conversation.TokenId = openSecureChannelResponse.SecurityToken.TokenId;
                            _conversation.RemoteNonce = openSecureChannelResponse.ServerNonce!;
                        }
                        finally
                        {
                            _sendingSemaphore.Release();
                        }
                    }

                    return response;
                }
                finally
                {
                    bodyDecoder.Dispose();
                }
            }
            finally
            {
                _receivingSemaphore.Release();
            }
        }

        /// <summary>
        /// Update request header with current time.
        /// </summary>
        /// <param name="request">The service request.</param>
        private void TimestampHeader(IServiceRequest request)
        {
            if (request.RequestHeader == null)
            {
                request.RequestHeader = new RequestHeader { TimeoutHint = TimeoutHint, ReturnDiagnostics = DiagnosticsHint, Timestamp = DateTime.UtcNow };
                return;
            }

            request.RequestHeader.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Get next request handle.
        /// </summary>
        /// <returns>A request handle.</returns>
        private uint GetNextHandle()
        {
            unchecked
            {
                int snapshot = _handle;
                int value = snapshot + 1;
                if (value == 0)
                {
                    value = 1;
                }

                if (Interlocked.CompareExchange(ref _handle, value, snapshot) != snapshot)
                {
                    var spinner = default(SpinWait);
                    do
                    {
                        spinner.SpinOnce();
                        snapshot = _handle;
                        value = snapshot + 1;
                        if (value == 0)
                        {
                            value = 1;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _handle, value, snapshot) != snapshot);
                }

                return (uint)value;
            }
        }
    }
}