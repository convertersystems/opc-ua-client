// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    public class ServiceOperation : TaskCompletionSource<IServiceResponse>
    {
        public ServiceOperation(IServiceRequest request)
#if NET45
            : base(request)
#else
            : base(request, TaskCreationOptions.RunContinuationsAsynchronously)
#endif
        {
        }

        /// <summary>
        /// Gets the request.
        /// </summary>
        public IServiceRequest Request => (IServiceRequest)this.Task.AsyncState;
    }
}