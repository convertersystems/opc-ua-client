using System;
using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua.Channels
{

    public interface IEncodingDictionary
    {
        bool TryGetEncodingId(Type type, out ExpandedNodeId id);
        bool TryGetType(ExpandedNodeId id, out Type type);
    }

    public class EncodingDictionary : IEncodingDictionary
    {
        IDictionary<Type, ExpandedNodeId> _encoderMap = new Dictionary<Type, ExpandedNodeId>();
        IDictionary<ExpandedNodeId, Type> _decoderMap = new Dictionary<ExpandedNodeId, Type>();

        List<EncodingDictionary>? _mergedDictionaries;

        ///<summary>
        ///     List of EncodingDictionaries merged into this EncodingDictionary
        ///</summary>
        public List<EncodingDictionary> MergedDictionaries
        {
            get
            {
                if (_mergedDictionaries == null)
                {
                    _mergedDictionaries = new List<EncodingDictionary>();
                }

                return _mergedDictionaries;
            }
        }

        public bool TryGetEncodingId(Type type, out ExpandedNodeId id)
        {
            return _encoderMap.TryGetValue(type, out id);
        }

        public bool TryGetType(ExpandedNodeId id, out Type type)
        {
            return _decoderMap.TryGetValue(id, out type);
        }

        public void Add(ExpandedNodeId id, Type type)
        {
            _decoderMap.Add(id, type);
            try
            {
                _encoderMap.Add(type, id);
            }
            catch
            {
                _decoderMap.Remove(id);
                throw;
            }
        }
    }
}
