// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// Provides a common base implementation for the basic state machine common to all communication-oriented objects in the system.
    /// </summary>
    public abstract class CommunicationObject : ICommunicationObject, IDisposable
    {
        private readonly ILogger? _logger;
        private bool _aborted;
        private bool _onClosingCalled;
        private bool _onClosedCalled;
        private bool _onOpeningCalled;
        private bool _onOpenedCalled;
        private bool _raisedClosed;
        private bool _raisedClosing;
        private bool _raisedFaulted;
        private bool disposed = false;
        private readonly SemaphoreSlim _semaphore;
        private readonly Lazy<ConcurrentQueue<Exception>> _exceptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationObject"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger.</param>
        public CommunicationObject(ILoggerFactory? loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger(GetType());
            _semaphore = new SemaphoreSlim(1);
            _exceptions = new Lazy<ConcurrentQueue<Exception>>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!disposed && disposing)
            {
                _semaphore.Dispose();

                while (_exceptions.Value.TryDequeue(out _)) { }
                _exceptions = null; // Optional, if you want full release

                disposed = true;
            }
        }

        public event EventHandler? Closed;

        public event EventHandler? Closing;

        public event EventHandler? Faulted;

        public event EventHandler? Opened;

        public event EventHandler? Opening;

        /// <summary>
        /// Gets or sets gets a value that indicates the current state of the communication object.
        /// </summary>
        /// <returns>A value from the <see cref="T:Workstation.ServiceModel.Ua.CommunicationState" /> enumeration that indicates the current state of the object.</returns>
        public CommunicationState State { get; protected set; }

        /// <summary>
        /// Causes a communication object to transition immediately from its current state into the closing state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AbortAsync(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_aborted || State == CommunicationState.Closed)
                {
                    return;
                }

                _aborted = true;
                State = CommunicationState.Closing;
            }
            finally
            {
                _semaphore.Release();
            }

            bool flag2 = true;
            try
            {
                await OnClosingAsync(token).ConfigureAwait(false);
                if (!_onClosingCalled)
                {
                    throw new InvalidOperationException($"Channel did not call base.OnClosingAsync");
                }

                await OnAbortAsync(token).ConfigureAwait(false);
                await OnClosedAsync(token).ConfigureAwait(false);
                if (!_onClosedCalled)
                {
                    throw new InvalidOperationException($"Channel did not call base.OnClosedAsync");
                }

                flag2 = false;
            }
            finally
            {
                if (flag2)
                {
                }
            }
        }

        /// <summary>
        /// Causes a communication object to transition from its current state into the closed state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CloseAsync(CancellationToken token = default)
        {
            CommunicationState communicationState;
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                communicationState = State;
                if (communicationState != CommunicationState.Closed)
                {
                    State = CommunicationState.Closing;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            switch (communicationState)
            {
                case CommunicationState.Created:
                case CommunicationState.Opening:
                case CommunicationState.Faulted:
                    await AbortAsync(token).ConfigureAwait(false);
                    if (communicationState == CommunicationState.Faulted)
                    {
                        throw new InvalidOperationException($"Channel faulted.");
                    }

                    break;

                case CommunicationState.Opened:
                    {
                        bool flag2 = true;
                        try
                        {
                            await OnClosingAsync(token).ConfigureAwait(false);
                            if (!_onClosingCalled)
                            {
                                throw new InvalidOperationException($"Channel did not call base.OnClosingAsync");
                            }

                            await OnCloseAsync(token).ConfigureAwait(false);
                            await OnClosedAsync(token).ConfigureAwait(false);
                            if (!_onClosedCalled)
                            {
                                throw new InvalidOperationException($"Channel did not call base.OnClosedAsync");
                            }

                            flag2 = false;
                            return;
                        }
                        finally
                        {
                            if (flag2)
                            {
                                _logger?.LogError($"Error closing channel.");
                                await AbortAsync(token).ConfigureAwait(false);
                            }
                        }
                    }

                case CommunicationState.Closing:
                case CommunicationState.Closed:
                    break;
            }
        }

        /// <summary>
        /// Causes a communication object to transition from the created state into the opened state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OpenAsync(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                ThrowIfDisposedOrImmutable();
                State = CommunicationState.Opening;
            }
            finally
            {
                _semaphore.Release();
            }

            bool flag2 = true;
            try
            {
                await OnOpeningAsync(token).ConfigureAwait(false);
                if (!_onOpeningCalled)
                {
                    throw new InvalidOperationException($"Channel did not call base.OnOpeningAsync");
                }

                await OnOpenAsync(token).ConfigureAwait(false);
                await OnOpenedAsync(token).ConfigureAwait(false);
                if (!_onOpenedCalled)
                {
                    throw new InvalidOperationException($"Channel did not call base.OnOpenedAsync");
                }

                flag2 = false;
            }
            finally
            {
                if (flag2)
                {
                    await FaultAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Causes a communication object to transition from its current state into the faulted state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task FaultAsync(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (State == CommunicationState.Closed || State == CommunicationState.Closing)
                {
                    return;
                }

                if (State == CommunicationState.Faulted)
                {
                    return;
                }

                State = CommunicationState.Faulted;
            }
            finally
            {
                _semaphore.Release();
            }

            await OnFaulted(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Causes a communication object to transition from its current state into the faulted state.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task FaultAsync(Exception exception, CancellationToken token = default)
        {
            AddPendingException(exception);
            await FaultAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Inserts processing on a communication object after it transitions to the closing state due to the invocation of the AbortAsync operation.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task OnAbortAsync(CancellationToken token = default);

        /// <summary>
        /// Inserts processing on a communication object after it transitions to the closing state due to the invocation of the CloseAsync operation.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task OnCloseAsync(CancellationToken token = default);

        /// <summary>
        /// Invoked during the transition of a communication object into the closed state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnClosedAsync(CancellationToken token = default)
        {
            _onClosedCalled = true;
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_raisedClosed)
                {
                    return;
                }

                _raisedClosed = true;
                State = CommunicationState.Closed;
            }
            finally
            {
                _semaphore.Release();
            }

            _logger?.LogTrace($"Channel closed.");
            EventHandler? closed = Closed;
            if (closed != null)
            {
                closed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the closing state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnClosingAsync(CancellationToken token = default)
        {
            _onClosingCalled = true;
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_raisedClosing)
                {
                    return;
                }

                _raisedClosing = true;
            }
            finally
            {
                _semaphore.Release();
            }

            _logger?.LogTrace($"Channel closing.");
            EventHandler? closing = Closing;
            if (closing != null)
            {
                closing(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Inserts processing on a communication object after it transitions to the faulted state due to the FaultAsync operation.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnFaulted(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_raisedFaulted)
                {
                    return;
                }

                _raisedFaulted = true;
            }
            finally
            {
                _semaphore.Release();
            }

            _logger?.LogTrace($"Channel faulted.");
            EventHandler? faulted = Faulted;
            if (faulted != null)
            {
                faulted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Inserts processing on a communication object after it transitions into the opening state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task OnOpenAsync(CancellationToken token = default);

        /// <summary>
        /// Invoked during the transition of a communication object into the opened state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnOpenedAsync(CancellationToken token = default)
        {
            _onOpenedCalled = true;
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_aborted || State != CommunicationState.Opening)
                {
                    return;
                }

                State = CommunicationState.Opened;
            }
            finally
            {
                _semaphore.Release();
            }

            _logger?.LogTrace($"Channel opened.");
            EventHandler? opened = Opened;
            if (opened != null)
            {
                opened(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the opening state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnOpeningAsync(CancellationToken token = default)
        {
            _onOpeningCalled = true;
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
            }
            finally
            {
                _semaphore.Release();
            }

            _logger?.LogTrace($"Channel opening.");
            EventHandler? opening = Opening;
            if (opening != null)
            {
                opening(this, EventArgs.Empty);
            }
        }

        protected void AddPendingException(Exception exception)
        {
            _exceptions?.Value.Enqueue(exception);
        }

        protected Exception? PeakFirstPendingException()
        {
            if (_exceptions.Value.TryPeek(out var ex))
            {
                return ex;
            }

            return null;
        }

        protected Exception? GetPendingException()
        {
            if (_exceptions.Value.TryDequeue(out var ex))
            {
                return ex;
            }

            return null;
        }

        protected void ThrowPending()
        {
            Exception? exception = GetPendingException();
            if (exception != null)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the communication object is not in the created state.
        /// </summary>
        protected void ThrowIfDisposedOrImmutable()
        {
            ThrowPending();
            switch (State)
            {
                case CommunicationState.Created:
                    return;

                case CommunicationState.Opening:
                case CommunicationState.Opened:
                case CommunicationState.Closing:
                case CommunicationState.Faulted:
                    throw new InvalidOperationException($"Channel not modifiable.");
                case CommunicationState.Closed:
                    throw new InvalidOperationException($"Channel disposed.");
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the communication object is not in the opening, open, or closing state.
        /// </summary>
        protected void ThrowIfClosedOrNotOpening()
        {
            ThrowPending();
            switch (State)
            {
                case CommunicationState.Created:
                    throw new InvalidOperationException($"Channel not opened.");

                case CommunicationState.Opening:
                case CommunicationState.Opened:
                case CommunicationState.Closing:
                    return;

                case CommunicationState.Closed:
                case CommunicationState.Faulted:
                    throw new InvalidOperationException($"Channel closed or faulted.");

            }
        }
    }
}