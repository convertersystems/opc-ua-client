
using System;
using Workstation.ServiceModel.Ua;

[assembly: TypeLibrary()]
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
            encoder.WriteDouble("X", X);
            encoder.WriteDouble("Y", Y);
            encoder.WriteDouble("Z", Z);
        }
        public override void Decode(IDecoder decoder)
        {
            X = decoder.ReadDouble("X");
            Y = decoder.ReadDouble("Y");
            Z = decoder.ReadDouble("Z");
        }
    }
}
