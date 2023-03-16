// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Workstation.ServiceModel.Ua.Channels
{
    internal static class Stubs
    {
        public static readonly Action Nop = static () => { };
        public static readonly Action<Exception> Throw = static ex => { ExceptionDispatchInfo.Capture(ex).Throw(); };
    }

    /// <summary>
    /// Class to create an <see cref="IObserver{T}"/> instance from delegate-based implementations of the On* methods.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    public sealed class AnonymousObserver<T> : IObserver<T>, IDisposable
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;
        private int _isStopped;

        /// <summary>
        /// Creates an observer from the specified <see cref="IObserver{T}.OnNext(T)"/>, <see cref="IObserver{T}.OnError(Exception)"/>, and <see cref="IObserver{T}.OnCompleted()"/> actions.
        /// </summary>
        /// <param name="onNext">Observer's <see cref="IObserver{T}.OnNext(T)"/> action implementation.</param>
        /// <param name="onError">Observer's <see cref="IObserver{T}.OnError(Exception)"/> action implementation.</param>
        /// <param name="onCompleted">Observer's <see cref="IObserver{T}.OnCompleted()"/> action implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onNext"/> or <paramref name="onError"/> or <paramref name="onCompleted"/> is <c>null</c>.</exception>
        public AnonymousObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
            _onError = onError ?? throw new ArgumentNullException(nameof(onError));
            _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
            _isStopped = 0;
        }

        /// <summary>
        /// Creates an observer from the specified <see cref="IObserver{T}.OnNext(T)"/> action.
        /// </summary>
        /// <param name="onNext">Observer's <see cref="IObserver{T}.OnNext(T)"/> action implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onNext"/> is <c>null</c>.</exception>
        public AnonymousObserver(Action<T> onNext)
            : this(onNext, Stubs.Throw, Stubs.Nop)
        {
        }

        /// <summary>
        /// Creates an observer from the specified <see cref="IObserver{T}.OnNext(T)"/> and <see cref="IObserver{T}.OnError(Exception)"/> actions.
        /// </summary>
        /// <param name="onNext">Observer's <see cref="IObserver{T}.OnNext(T)"/> action implementation.</param>
        /// <param name="onError">Observer's <see cref="IObserver{T}.OnError(Exception)"/> action implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onNext"/> or <paramref name="onError"/> is <c>null</c>.</exception>
        public AnonymousObserver(Action<T> onNext, Action<Exception> onError)
            : this(onNext, onError, Stubs.Nop)
        {
        }

        /// <summary>
        /// Creates an observer from the specified <see cref="IObserver{T}.OnNext(T)"/> and <see cref="IObserver{T}.OnCompleted()"/> actions.
        /// </summary>
        /// <param name="onNext">Observer's <see cref="IObserver{T}.OnNext(T)"/> action implementation.</param>
        /// <param name="onCompleted">Observer's <see cref="IObserver{T}.OnCompleted()"/> action implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onNext"/> or <paramref name="onCompleted"/> is <c>null</c>.</exception>
        public AnonymousObserver(Action<T> onNext, Action onCompleted)
            : this(onNext, Stubs.Throw, onCompleted)
        {
        }

        /// <summary>
        /// Notifies the observer of a new element in the sequence.
        /// </summary>
        /// <param name="value">Next element in the sequence.</param>
        public void OnNext(T value)
        {
            if (Volatile.Read(ref _isStopped) == 0)
            {
                _onNext(value);
            }
        }

        /// <summary>
        /// Notifies the observer that an exception has occurred.
        /// </summary>
        /// <param name="error">The error that has occurred.</param>
        /// <exception cref="ArgumentNullException"><paramref name="error"/> is <c>null</c>.</exception>
        public void OnError(Exception error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            if (Interlocked.Exchange(ref _isStopped, 1) == 0)
            {
                _onError(error);
            }
        }

        /// <summary>
        /// Notifies the observer of the end of the sequence.
        /// </summary>
        public void OnCompleted()
        {
            if (Interlocked.Exchange(ref _isStopped, 1) == 0)
            {
                _onCompleted();
            }
        }

        /// <summary>
        /// Disposes the observer, causing it to transition to the stopped state.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Core implementation of <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="disposing"><c>true</c> if the Dispose call was triggered by the <see cref="IDisposable.Dispose"/> method; <c>false</c> if it was triggered by the finalizer.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Volatile.Write(ref _isStopped, 1);
            }
        }
    }
}