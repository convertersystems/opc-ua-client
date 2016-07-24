// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    public class ServiceTask : TaskCompletionSource<IServiceResponse>
    {
        public ServiceTask(IServiceRequest request)
            : base(request, TaskCreationOptions.RunContinuationsAsynchronously)
        {
        }

        public IServiceRequest Request => (IServiceRequest)this.Task.AsyncState;
    }
}