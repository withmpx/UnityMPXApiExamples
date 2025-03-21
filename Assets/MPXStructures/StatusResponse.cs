using System;

namespace MPXStructures
{
    [Serializable]
    public class StatusResponse
    {
        public string requestId;
        public string status;
        public int balance;

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this, true);
        }
    }
}