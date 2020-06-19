// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    [Flags]
    public enum AccessLevelFlags : byte
    {
        None = 0,
        CurrentRead = 1,
        CurrentWrite = 2,
        HistoryRead = 4,
        HistoryWrite = 8,
        SemanticChange = 16,
        StatusWrite = 32,
        TimestampWrite = 64,
    }
}