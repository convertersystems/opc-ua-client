// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    public interface IServiceRequest : IEncodable
    {
        RequestHeader RequestHeader { get; set; }
    }
}