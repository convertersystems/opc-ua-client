// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    public sealed class ServiceResultException : Exception
    {
        public ServiceResultException(ServiceResult result)
            : base(result.ToString())
        {
            HResult = unchecked((int)(uint)result.StatusCode);
        }

        public ServiceResultException(StatusCode statusCode)
            : base(StatusCodes.GetDefaultMessage(statusCode))
        {
            HResult = unchecked((int)(uint)statusCode);
        }

        public ServiceResultException(StatusCode statusCode, string message)
            : base(message)
        {
            HResult = unchecked((int)(uint)statusCode);
        }

        public ServiceResultException(StatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = unchecked((int)(uint)statusCode);
        }
    }
}