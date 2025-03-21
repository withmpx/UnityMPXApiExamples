using System;

namespace MPXStructures
{
    /// <summary>
    /// See: https://docs.masterpiecex.com/reference/post_functions-imageto3d#/
    /// </summary>
    [Serializable]
    public class ImageTo3DModelRequest
    {
        // The requestId from the /assets/create endpoint that the image was uploaded to.
        // Do not use this if you have an imageUrl.
        public string imageRequestId; 

        // Seed used to generate the 3D model
        public int seed;

        // Select from [512, 1024, 2048]
        // Other numbers might be supported if they are a power of 2. Anything larger than 2048 may cause errors or long processing times.
        public int textureSize;

        // The URL of the image to use for the generation.
        // Use this instead of imageRequestId if you did not upload the image to our servers using the /assets/create endpoint.
        public string imageUrl;

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this, true);
        }
    }
}
