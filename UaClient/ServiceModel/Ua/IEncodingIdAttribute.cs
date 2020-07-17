using System;
using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public interface IEncodingIdAttribute
    {
        ExpandedNodeId NodeId { get; }
    }
}
