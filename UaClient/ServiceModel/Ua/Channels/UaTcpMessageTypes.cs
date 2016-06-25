// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.ServiceModel.Ua
{
    public static class UaTcpMessageTypes
    {
        public const uint HELF = 'H' | 'E' << 8 | 'L' << 16 | 'F' << 24;
        public const uint ACKF = 'A' | 'C' << 8 | 'K' << 16 | 'F' << 24;
        public const uint ERRF = 'E' | 'R' << 8 | 'R' << 16 | 'F' << 24;
        public const uint OPNF = 'O' | 'P' << 8 | 'N' << 16 | 'F' << 24;
        public const uint CLOF = 'C' | 'L' << 8 | 'O' << 16 | 'F' << 24;
        public const uint MSGF = 'M' | 'S' << 8 | 'G' << 16 | 'F' << 24;
        public const uint OPNC = 'O' | 'P' << 8 | 'N' << 16 | 'C' << 24;
        public const uint CLOC = 'C' | 'L' << 8 | 'O' << 16 | 'C' << 24;
        public const uint MSGC = 'M' | 'S' << 8 | 'G' << 16 | 'C' << 24;
        public const uint OPNA = 'O' | 'P' << 8 | 'N' << 16 | 'A' << 24;
        public const uint CLOA = 'C' | 'L' << 8 | 'O' << 16 | 'A' << 24;
        public const uint MSGA = 'M' | 'S' << 8 | 'G' << 16 | 'A' << 24;
    }
}