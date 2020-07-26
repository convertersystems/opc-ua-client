
using System;
using Workstation.ServiceModel.Ua;

[assembly: Workstation.ServiceModel.Ua.TypeLibrary()]

namespace CustomTypeLibrary
{
    [DataTypeId("nsu=http://www.unifiedautomation.com/DemoServer/;i=3002")]
    [BinaryEncodingId("nsu=http://www.unifiedautomation.com/DemoServer/;i=5054")]
    public class CustomVector : Structure
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public override void Encode(IEncoder encoder)
        {
            encoder.WriteDouble("X", this.X);
            encoder.WriteDouble("Y", this.Y);
            encoder.WriteDouble("Z", this.Z);
        }

        public override void Decode(IDecoder decoder)
        {
            this.X = decoder.ReadDouble("X");
            this.Y = decoder.ReadDouble("Y");
            this.Z = decoder.ReadDouble("Z");
        }

    }
}
