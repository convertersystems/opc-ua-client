// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public interface ISessionClient
    {
        Task<IServiceResponse> RequestAsync(IServiceRequest request);
    }
}
