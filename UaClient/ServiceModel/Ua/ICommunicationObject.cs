// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    /// <summary>Defines the contract for the basic state machine for all communication-oriented objects in the system.</summary>
    public interface ICommunicationObject
    {
        /// <summary>Gets the current state of the communication-oriented object.</summary>
        /// <returns>The value of the <see cref="T:System.ServiceModel.CommunicationState" /> of the object.</returns>
        CommunicationState State { get; }

        /// <summary>Causes a communication object to transition immediately from its current state into the closed state.  </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies that the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AbortAsync(CancellationToken token = default(CancellationToken));

        /// <summary>Causes a communication object to transition from its current state into the closed state.  </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies that the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CloseAsync(CancellationToken token = default(CancellationToken));

        /// <summary>Causes a communication object to transition from the created state into the opened state.  </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies that the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task OpenAsync(CancellationToken token = default(CancellationToken));
    }
}