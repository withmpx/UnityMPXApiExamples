using System;

namespace MPXStructures
{
    [Serializable]
    public class TextTo3DModel
    {
        public string prompt;

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this, true);
        }
    }
}

