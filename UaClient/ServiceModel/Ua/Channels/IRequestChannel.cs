// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// Provides a method to send a <see cref="T:ConverterSystems.ServiceModel.Ua.IServiceRequest" /> to a RemoteEndpoint.
    /// </summary>
    public interface IRequestChannel
    {
        /// <summary>Gets the remote address to which the request channel sends messages. </summary>
        /// <returns>The <see cref="T:ConverterSystems.ServiceModel.Ua.EndpointDescription" /> to which the request channel sends messages. </returns>
        EndpointDescription RemoteEndpoint
        {
            get;
        }

        /// <summary>Sends an IServiceRequest and returns the correlated IServiceResponse.</summary>
        /// <returns>The <see cref="T:ConverterSystems.ServiceModel.Ua.IServiceResponse" /> received in response to the request. </returns>
        /// <param name="request">The <see cref="T:ConverterSystems.ServiceModel.Ua.IServiceRequest" /> to be transmitted.</param>
        Task<IServiceResponse> RequestAsync(IServiceRequest request);
    }
}