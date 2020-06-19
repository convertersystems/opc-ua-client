// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    [Flags]
    public enum DiagnosticFlags : uint
    {
        None = 0x00000000,
        ServiceSymbolicId = 0x00000001,
        ServiceLocalizedText = 0x00000002,
        ServiceAdditionalInfo = 0x00000004,
        ServiceInnerStatusCode = 0x00000008,
        ServiceInnerDiagnostics = 0x00000010,
        OperationSymbolicId = 0x00000020,
        OperationLocalizedText = 0x00000040,
        OperationAdditionalInfo = 0x0000080,
        OperationInnerStatusCode = 0x00000100,
        OperationInnerDiagnostics = 0x0000200,
    }
}