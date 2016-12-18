// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    public sealed class DataValue
    {
        public DataValue(object value, StatusCode statusCode = default(StatusCode), DateTime sourceTimestamp = default(DateTime), ushort sourcePicoseconds = 0, DateTime serverTimestamp = default(DateTime), ushort serverPicoseconds = 0)
        {
            Variant = new Variant(value);
            StatusCode = statusCode;
            SourceTimestamp = sourceTimestamp;
            SourcePicoseconds = sourcePicoseconds;
            ServerTimestamp = serverTimestamp;
            ServerPicoseconds = serverPicoseconds;
        }

        public DataValue(Variant value, StatusCode statusCode = default(StatusCode), DateTime sourceTimestamp = default(DateTime), ushort sourcePicoseconds = 0, DateTime serverTimestamp = default(DateTime), ushort serverPicoseconds = 0)
        {
            Variant = value;
            StatusCode = statusCode;
            SourceTimestamp = sourceTimestamp;
            SourcePicoseconds = sourcePicoseconds;
            ServerTimestamp = serverTimestamp;
            ServerPicoseconds = serverPicoseconds;
        }

        public object Value
        {
            get { return Variant.Value; }
        }

        public StatusCode StatusCode { get; }

        public DateTime SourceTimestamp { get; }

        public ushort SourcePicoseconds { get; }

        public DateTime ServerTimestamp { get; }

        public ushort ServerPicoseconds { get; }

        public Variant Variant { get; }

        public override string ToString()
        {
            return $"{Value}; status: {StatusCode}; timestamp: {SourceTimestamp}";
        }
    }
}