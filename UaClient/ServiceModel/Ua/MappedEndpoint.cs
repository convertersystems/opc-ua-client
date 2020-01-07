// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A map between a requested endpoint url and the endpoint.
    /// </summary>
    public class MappedEndpoint
    {
        public string? RequestedUrl { get; set; }

        public EndpointDescription? Endpoint { get; set; }
    }
}
