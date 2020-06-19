// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    [Flags]
    public enum EventNotifierFlags : byte
    {
        None = 0,
        SubscribeToEvents = 1,
        HistoryRead = 4,
        HistoryWrite = 8,
    }
}