using System;
using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua.Channels
{
    public interface IEncodingContext
    {
        IReadOnlyList<string> NamespaceUris { get; }
        IReadOnlyList<string> ServerUris { get; }
        int MaxStringLength { get; }
        int MaxArrayLength { get; }
        int MaxByteStringLength { get; }
    }

    public class DefaultEncodingContext : IEncodingContext
    {
        public IReadOnlyList<string> NamespaceUris => new List<string> { "http://opcfoundation.org/UA/" };

        public IReadOnlyList<string> ServerUris => new List<string>();

        public int MaxStringLength => 65535;

        public int MaxArrayLength => 65535;

        public int MaxByteStringLength => 65535;

    }

}
