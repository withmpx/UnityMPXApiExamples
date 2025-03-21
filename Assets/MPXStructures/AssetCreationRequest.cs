using System;

namespace MPXStructures
{
    [Serializable]
    public class AssetCreationRequest
    {
        public string name;
        public string description;
        public string type;

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this, true);
        }
    }
}
