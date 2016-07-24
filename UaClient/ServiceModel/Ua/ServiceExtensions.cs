// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Converts an array of objects to an array of <see cref="T:ConverterSystems.ServiceModel.Ua.Variant" />.
        /// </summary>
        /// <param name="array">The object array.</param>
        /// <returns>The <see cref="T:ConverterSystems.ServiceModel.Ua.Variant" /> array.</returns>
        public static Variant[] ToVariantArray(this object[] array)
        {
            return array.Select(a => new Variant(a)).ToArray();
        }

        /// <summary>
        /// Converts an array of <see cref="T:ConverterSystems.ServiceModel.Ua.Variant" /> to an array of objects.
        /// </summary>
        /// <param name="array">The <see cref="T:ConverterSystems.ServiceModel.Ua.Variant" /> array.</param>
        /// <returns>The object array.</returns>
        public static object[] ToObjectArray(this Variant[] array)
        {
            return array.Select(a => a.Value).ToArray();
        }

        /// <summary>
        /// Wraps a task with one that may complete as cancelled based on a cancellation token,
        /// allowing someone to await a task but be able to break out early by cancelling the token.
        /// </summary>
        /// <typeparam name="T">The type of value returned by the task.</typeparam>
        /// <param name="task">The task to wrap.</param>
        /// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
        /// <returns>The wrapping task.</returns>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Wraps a task with one that may complete as cancelled based on a cancellation token,
        /// allowing someone to await a task but be able to break out early by cancelling the token.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <param name="cancellationToken">The token that can be canceled to break out of the await.</param>
        /// <returns>The wrapping task.</returns>
        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Wraps a task with one that may complete as faulted based on a timeout,
        /// allowing someone to await a task but be able to break out early by a timeout.
        /// </summary>
        /// <typeparam name="T">The type of value returned by the task.</typeparam>
        /// <param name="task">The task to wrap.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to.</param>
        /// <returns>The wrapping task.</returns>
        public static async Task<T> WithTimeoutAfter<T>(this Task<T> task, int millisecondsTimeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)).ConfigureAwait(false))
            {
                return await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        /// Wraps a task with one that may complete as faulted based on a timeout,
        /// allowing someone to await a task but be able to break out early by a timeout.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to.</param>
        /// <returns>The wrapping task.</returns>
        public static async Task WithTimeoutAfter(this Task task, int millisecondsTimeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)).ConfigureAwait(false))
            {
                await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException();
            }
        }
    }
}