﻿// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        // ##############################################################################################################################
        // Properties
        // ##############################################################################################################################

        #region Properties

        // ##########################################################################################
        // Public Properties
        // ##########################################################################################

        /// <summary>
        /// Pushes the actual communication state
        /// </summary>
        public IObservable<CommunicationState> StateStream => communicationStateSubject.AsObservable().DistinctUntilChanged();

        /// <summary>
        /// Gets or sets gets a value that indicates the current state of the communication object.
        /// </summary>
        /// <returns>A value from the <see cref="T:Workstation.ServiceModel.Ua.CommunicationState" /> enumeration that indicates the current state of the object.</returns>
        public CommunicationState State
        {
            get => communicationStateSubject.Value;
            protected set => communicationStateSubject.OnNext(value);
        }

        [Obsolete("use StateStream instead of", false)]
        public event EventHandler Closed;
        [Obsolete("use StateStream instead of", false)]
        public event EventHandler Closing;
        [Obsolete("use StateStream instead of", false)]
        public event EventHandler Faulted;
        [Obsolete("use StateStream instead of", false)]
        public event EventHandler Opened;
        [Obsolete("use StateStream instead of", false)]
        public event EventHandler Opening;

        // ##########################################################################################
        // Private Properties
        // ##########################################################################################

        private readonly ILogger logger;
        private bool aborted;
        private bool onClosingCalled;
        private bool onClosedCalled;
        private bool onOpeningCalled;
        private bool onOpenedCalled;
        private bool raisedClosed;
        private bool raisedClosing;
        private bool raisedFaulted;
        private SemaphoreSlim semaphore;
        private Lazy<ConcurrentQueue<Exception>> exceptions;

        private readonly BehaviorSubject<CommunicationState> communicationStateSubject;

        #endregion
        
        // ##############################################################################################################################
        // Constructor
        // ##############################################################################################################################

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationObject"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger.</param>
        public CommunicationObject(ILoggerFactory loggerFactory = null)
        {
            this.logger = loggerFactory?.CreateLogger(this.GetType());
            this.semaphore = new SemaphoreSlim(1);
            this.exceptions = new Lazy<ConcurrentQueue<Exception>>();

            communicationStateSubject = new BehaviorSubject<CommunicationState>(CommunicationState.Created);
            StateStream.Subscribe(state =>
            {
                switch (state)
                {
                    case CommunicationState.Created:
                        break;
                    case CommunicationState.Opening:
                        Opening?.Invoke(this, EventArgs.Empty);
                        break;
                    case CommunicationState.Opened:
                        Opened?.Invoke(this, EventArgs.Empty);
                        break;
                    case CommunicationState.Closing:
                        Closing?.Invoke(this, EventArgs.Empty);
                        break;
                    case CommunicationState.Closed:
                        Closed?.Invoke(this, EventArgs.Empty);
                        break;
                    case CommunicationState.Faulted:
                        Faulted?.Invoke(this, EventArgs.Empty);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            });
        }

        #endregion
        
        /// <summary>
        /// Causes a communication object to transition immediately from its current state into the closing state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AbortAsync(CancellationToken token = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (this.aborted || this.State == CommunicationState.Closed)
                {
                    return;
                }

                this.aborted = true;
                this.State = CommunicationState.Closing;
            }
            finally
            {
                this.semaphore.Release();
            }

            bool flag2 = true;
            try
            {
                await this.OnClosingAsync(token).ConfigureAwait(false);
                if (!this.onClosingCalled)
                {
                    throw new InvalidOperationException($"Channel did not call base.OnClosingAsync");
                }

                await this.OnAbortAsync(token).ConfigureAwait(false);
                await this.OnClosedAsync(token).ConfigureAwait(false);
                if (!this.onClosedCalled)
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
        public async Task CloseAsync(CancellationToken token = default(CancellationToken))
        {
            CommunicationState communicationState;
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                communicationState = this.State;
                if (communicationState != CommunicationState.Closed)
                {
                    this.State = CommunicationState.Closing;
                }
            }
            finally
            {
                this.semaphore.Release();
            }

            switch (communicationState)
            {
                case CommunicationState.Created:
                case CommunicationState.Opening:
                case CommunicationState.Faulted:
                    await this.AbortAsync(token).ConfigureAwait(false);
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
                            await this.OnClosingAsync(token).ConfigureAwait(false);
                            if (!this.onClosingCalled)
                            {
                                throw new InvalidOperationException($"Channel did not call base.OnClosingAsync");
                            }

                            await this.OnCloseAsync(token).ConfigureAwait(false);
                            await this.OnClosedAsync(token).ConfigureAwait(false);
                            if (!this.onClosedCalled)
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
                                this.logger?.LogError($"Error closing channel.");
                                await this.AbortAsync(token).ConfigureAwait(false);
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
        public async Task OpenAsync(CancellationToken token = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                this.ThrowIfDisposedOrImmutable();
                this.State = CommunicationState.Opening;
            }
            finally
            {
                this.semaphore.Release();
            }

            bool flag2 = true;
            try
            {
                await this.OnOpeningAsync(token).ConfigureAwait(false);
                if (!this.onOpeningCalled)
                {
                    throw new InvalidOperationException($"Channel did not call base.OnOpeningAsync");
                }

                await this.OnOpenAsync(token).ConfigureAwait(false);
                await this.OnOpenedAsync(token).ConfigureAwait(false);
                if (!this.onOpenedCalled)
                {
                    throw new InvalidOperationException($"Channel did not call base.OnOpenedAsync");
                }

                flag2 = false;
            }
            finally
            {
                if (flag2)
                {
                    await this.FaultAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Causes a communication object to transition from its current state into the faulted state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task FaultAsync(CancellationToken token = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (this.State == CommunicationState.Closed || this.State == CommunicationState.Closing)
                {
                    return;
                }

                if (this.State == CommunicationState.Faulted)
                {
                    return;
                }

                this.State = CommunicationState.Faulted;
            }
            finally
            {
                this.semaphore.Release();
            }

            await this.OnFaulted(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Causes a communication object to transition from its current state into the faulted state.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task FaultAsync(Exception exception, CancellationToken token = default(CancellationToken))
        {
            this.AddPendingException(exception);
            await this.FaultAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Inserts processing on a communication object after it transitions to the closing state due to the invocation of the AbortAsync operation.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task OnAbortAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Inserts processing on a communication object after it transitions to the closing state due to the invocation of the CloseAsync operation.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task OnCloseAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Invoked during the transition of a communication object into the closed state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnClosedAsync(CancellationToken token = default(CancellationToken))
        {
            this.onClosedCalled = true;
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (this.raisedClosed)
                {
                    return;
                }

                this.raisedClosed = true;
                this.State = CommunicationState.Closed;
            }
            finally
            {
                this.semaphore.Release();
            }

            this.logger?.LogTrace($"Channel closed.");
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the closing state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnClosingAsync(CancellationToken token = default(CancellationToken))
        {
            this.onClosingCalled = true;
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (this.raisedClosing)
                {
                    return;
                }

                this.raisedClosing = true;
            }
            finally
            {
                this.semaphore.Release();
            }

            this.logger?.LogTrace($"Channel closing.");
        }

        /// <summary>
        /// Inserts processing on a communication object after it transitions to the faulted state due to the FaultAsync operation.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnFaulted(CancellationToken token = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (this.raisedFaulted)
                {
                    return;
                }

                this.raisedFaulted = true;
            }
            finally
            {
                this.semaphore.Release();
            }

            this.logger?.LogTrace($"Channel faulted.");
        }

        /// <summary>
        /// Inserts processing on a communication object after it transitions into the opening state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task OnOpenAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Invoked during the transition of a communication object into the opened state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnOpenedAsync(CancellationToken token = default(CancellationToken))
        {
            this.onOpenedCalled = true;
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (this.aborted || this.State != CommunicationState.Opening)
                {
                    return;
                }

                this.State = CommunicationState.Opened;
            }
            finally
            {
                this.semaphore.Release();
            }

            this.logger?.LogTrace($"Channel opened.");
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the opening state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OnOpeningAsync(CancellationToken token = default(CancellationToken))
        {
            this.onOpeningCalled = true;
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
            }
            finally
            {
                this.semaphore.Release();
            }

            this.logger?.LogTrace($"Channel opening.");
        }

        protected void AddPendingException(Exception exception)
        {
            this.exceptions.Value.Enqueue(exception);
        }

        protected Exception GetPendingException()
        {
            Exception ex;
            if (this.exceptions.Value.TryDequeue(out ex))
            {
                return ex;
            }

            return null;
        }

        protected void ThrowPending()
        {
            Exception exception = this.GetPendingException();
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
            this.ThrowPending();
            switch (this.State)
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
            this.ThrowPending();
            switch (this.State)
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

        // ##############################################################################################################################
        // IDisposable
        // ##############################################################################################################################

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                semaphore?.Dispose();
                communicationStateSubject?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}