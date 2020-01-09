// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Provides a method to send an <see cref="IServiceRequest" /> to a RemoteEndpoint.
    /// </summary>
    public interface IRequestChannel
    {
        /// <summary>Sends an IServiceRequest and returns the correlated IServiceResponse.</summary>
        /// <returns>The <see cref="T:ConverterSystems.ServiceModel.Ua.IServiceResponse" /> received in response to the request. </returns>
        /// <param name="request">The <see cref="T:ConverterSystems.ServiceModel.Ua.IServiceRequest" /> to be transmitted.</param>
        Task<IServiceResponse> RequestAsync(IServiceRequest request);
    }
}