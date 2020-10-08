// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    [DataTypeId(DataTypeIds.DataValue)]
    public sealed class DataValue
    {
        public DataValue(object? value, StatusCode statusCode = default(StatusCode), DateTime sourceTimestamp = default(DateTime), ushort sourcePicoseconds = 0, DateTime serverTimestamp = default(DateTime), ushort serverPicoseconds = 0)
        {
            this.Variant = new Variant(value);
            this.StatusCode = statusCode;
            this.SourceTimestamp = sourceTimestamp;
            this.SourcePicoseconds = sourcePicoseconds;
            this.ServerTimestamp = serverTimestamp;
            this.ServerPicoseconds = serverPicoseconds;
        }

        public DataValue(Variant value, StatusCode statusCode = default(StatusCode), DateTime sourceTimestamp = default(DateTime), ushort sourcePicoseconds = 0, DateTime serverTimestamp = default(DateTime), ushort serverPicoseconds = 0)
        {
            this.Variant = value;
            this.StatusCode = statusCode;
            this.SourceTimestamp = sourceTimestamp;
            this.SourcePicoseconds = sourcePicoseconds;
            this.ServerTimestamp = serverTimestamp;
            this.ServerPicoseconds = serverPicoseconds;
        }

        public object? Value
        {
            get { return this.Variant.Value; }
        }

        public StatusCode StatusCode { get; }

        public DateTime SourceTimestamp { get; }

        public ushort SourcePicoseconds { get; }

        public DateTime ServerTimestamp { get; }

        public ushort ServerPicoseconds { get; }

        public Variant Variant { get; }

        public override string ToString()
        {
            return $"{this.Value}; status: {this.StatusCode}; ts: {this.SourceTimestamp}";
        }
    }
}