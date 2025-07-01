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
        public static object?[] ToObjectArray(this Variant[] array)
        {
            return array.Select(a => a.Value).ToArray();
        }

        /// <summary>
        /// Requests a Refresh of all Conditions.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="subscriptionId">The subscriptionId.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<StatusCode> ConditionRefreshAsync(this IRequestChannel channel, uint subscriptionId)
        {
            var response = await channel.CallAsync(new CallRequest
            {
                MethodsToCall = new[]
                {
                    new CallMethodRequest
                    {
                        ObjectId = NodeId.Parse(ObjectTypeIds.ConditionType),
                        MethodId = NodeId.Parse(MethodIds.ConditionType_ConditionRefresh),
                        InputArguments = new Variant[] { subscriptionId }
                    }
                }
            }).ConfigureAwait(false);

            var result = response.Results?[0];

            if (result == null)
            {
                throw new ServiceResultException(StatusCodes.BadDataEncodingInvalid, "The CallMethodeResult is null!");
            }

            return result.StatusCode;
        }

        /// <summary>
        /// Acknowledges a condition.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="condition">an AcknowledgeableCondition.</param>
        /// <param name="comment">a comment.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<StatusCode> AcknowledgeAsync(this IRequestChannel channel, AcknowledgeableCondition condition, LocalizedText? comment = null)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            var response = await channel.CallAsync(new CallRequest
            {
                MethodsToCall = new[]
                {
                    new CallMethodRequest
                    {
                        ObjectId = condition.ConditionId,
                        MethodId = NodeId.Parse(MethodIds.AcknowledgeableConditionType_Acknowledge),
                        InputArguments = new Variant[] { condition.EventId, comment } // ?? new LocalizedText(string.Empty) }
                    }
                }
            }).ConfigureAwait(false);
            
            var result = response.Results?[0];

            if (result == null)
            {
                throw new ServiceResultException(StatusCodes.BadDataEncodingInvalid, "The CallMethodeResult is null!");
            }

            return result.StatusCode;
        }

        /// <summary>
        /// Confirms a condition.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="condition">an AcknowledgeableCondition.</param>
        /// <param name="comment">a comment.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<StatusCode> ConfirmAsync(this IRequestChannel channel, AcknowledgeableCondition condition, LocalizedText? comment = null)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            var response = await channel.CallAsync(new CallRequest
            {
                MethodsToCall = new[]
                {
                    new CallMethodRequest
                    {
                        ObjectId = condition.ConditionId,
                        MethodId = NodeId.Parse(MethodIds.AcknowledgeableConditionType_Confirm),
                        InputArguments = new Variant[] { condition.EventId, comment } // ?? new LocalizedText(string.Empty) }
                    }
                }
            }).ConfigureAwait(false);

            var result = response.Results?[0];

            if (result == null)
            {
                throw new ServiceResultException(StatusCodes.BadDataEncodingInvalid, "The CallMethodeResult is null!");
            }
            
            return result.StatusCode;
        }

        public static Task WithCancellation(this Task task, CancellationToken cancellationToken)
            => task.TimeoutAfter(-1, cancellationToken);


        public static Task TimeoutAfter(this Task task, int millisecondsTimeout, CancellationToken token = default)
            => task.TimeoutAfter(TimeSpan.FromMilliseconds(millisecondsTimeout), token);


        public static async Task TimeoutAfter(this Task task, TimeSpan timeout, CancellationToken token = default)
        {
            var t = await Task.WhenAny(task, Task.Delay(timeout, token)).ConfigureAwait(false);
            if (task == t)
            {
                await task.ConfigureAwait(false);
            }
            else if (!t.IsCanceled)
            {
                throw new TimeoutException($"Task timed out after {timeout}");
            }

            await t.ConfigureAwait(false);
        }


        public static Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int millisecondsTimeout, CancellationToken token)
            => task.TimeoutAfter(TimeSpan.FromMilliseconds(millisecondsTimeout), token);


        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken token)
        {
            var t = await Task.WhenAny(task, Task.Delay(timeout, token)).ConfigureAwait(false);
            if (task != t)
            {
                if (!t.IsCanceled)
                {
                    throw new TimeoutException($"Task timed out after {timeout}");
                }

                throw t.Exception!;
            }

            return await task.ConfigureAwait(false);
        }
    }
}