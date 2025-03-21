using System;

namespace MPXStructures
{
    [Serializable]
    public class TextToImage
    {
        public string prompt;
        public int seed;
        public int numImages;
        public int numSteps;
        public string loraId;

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this, true);
        }
    }
}

