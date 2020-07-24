using System;
using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua.Channels
{
    public interface IEncodingContext
    {
        List<string> NamespaceUris { get; }
        List<string> ServerUris { get; }
        IEncodingDictionary EncodingDictionary { get; }
        int MaxStringLength { get; }
        int MaxArrayLength { get; }
    }

    public class DefaultEncodingContext : IEncodingContext
    {
        public List<string> NamespaceUris => throw new NotImplementedException();

        public List<string> ServerUris => throw new NotImplementedException();

        public IEncodingDictionary EncodingDictionary => throw new NotImplementedException();

        public int MaxStringLength => throw new NotImplementedException();

        public int MaxArrayLength => throw new NotImplementedException();
    }
}
