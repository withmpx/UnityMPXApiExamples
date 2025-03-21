using System;

namespace MPXStructures
{
    /// <summary>
    /// See: https://docs.masterpiecex.com/reference/get_status-requestid#/ for the params and the function that will return this object
    /// </summary>
    [Serializable]
    public class StatusCheckData
    {
        public string requestId; // The request id
        public string status; //The status of the request
        public float progress; // The progress of the request. Only available if status is pending | processing
        public float progressTime_s; // The processing time in seconds. Only available if status is complete
        public string outputUrl; // The URL of the output model. Only available if status is complete
        public ImageDataOutputs outputs; // dictionary of outputs

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this, true);
        }
    }

    [Serializable]
    public class ImageDataOutputs
    {
        public string fbx;
        public string glb;
        public string usdz;
        public string thumbnail;
        public string[] images; // NOTE: only exists if for text-to-image calls
    }
}