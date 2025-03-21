using System;

namespace MPXStructures
{
    [Serializable]
    public class AssetCreationResponse
    {
        public string requestId;
        public string assetUrl;
        public string status;
        public int balance;

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this, true);
        }
    }
}