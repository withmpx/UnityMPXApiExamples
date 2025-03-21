using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

using UnityEngine.Networking;
using Proyecto26;
using MPXStructures;
using UnityEditor;
using Models;
using System.Threading;
using GLTFast.Schema;
using System.Net;

[System.Serializable]
public class RunDemo : MonoBehaviour
{
    [Header("Which demos to run?")]
    [SerializeField] public bool RunTextToImage = true;
    [SerializeField] public bool RunTextTo3DModel = true;
    [SerializeField] public bool RunImageTo3DModel = true;

    [Header("Text-To-Image Settings")]
    [SerializeField] public string PromptTextToImage = "a cute dog";
    [SerializeField] public int SeedTextToImage = 1;

    [Header("Text-To-3DModel Settings")]
    [SerializeField] public string PromptTextTo3DModel = "a cute dog";

    [Header("Image-To-3DModel Settings")]
    [SerializeField] public Texture2D InputImage = null;
    [SerializeField] public int SeedImageTo3DModel = 1;


    /// <summary>
    /// Replace this with your own API key that you can get from: https://developers.masterpiecex.com/
    /// Do _NOT_ commit this token to a repo!
    /// </summary>
    private readonly string MPXSecretToken = "XXXXXXXXXX";
    private readonly string PLACEHOLDER_TOKEN = "XXXXXXXXXX"; // only used to check against MPXSecretToken to ensure that the user is aware that they need to put their own API key in

    /// <summary>
    /// Specific endpoints to different functionality provided by the MPX API.
    /// </summary>
    private readonly string url_TestConnection = "https://api.genai.masterpiecex.com/v2/connection/test";
    private readonly string url_CheckStatus = "https://api.genai.masterpiecex.com/v2/status/";
    private readonly string url_CreateAssetID = "https://api.genai.masterpiecex.com/v2/assets/create";

    private readonly string url_TextToImage = "https://api.genai.masterpiecex.com/v2/components/text2image";
    private readonly string url_TextTo3DModel = "https://api.genai.masterpiecex.com/v2/functions/general";
    private readonly string url_ImageTo3DModel = "https://api.genai.masterpiecex.com/v2/functions/imageto3d";


    /// <summary>
    /// General delegate function definition so that we can implement different ways to handle different generate data outputs (e.g. image vs 3d-models)
    /// </summary>
    /// <param name="data"></param>
    private delegate void CompleteGenerationRequest(StatusCheckData data);


    /// <summary>
    /// Track the number of generations so that we can position the generated objects nicely in a line for demo purposes.
    /// Only used by the ApplyOffsetsForDemo function.
    /// </summary>
    private int Demo_NumGenerated = 0;


    void Start()
    {
        // ensure that the user has replaced the default placeholder value with an API key
        // NOTE: no actual checks
        if (MPXSecretToken == null || MPXSecretToken == PLACEHOLDER_TOKEN)
        {
            Debug.LogError("You need to edit the 'MPXSecretToken' with the API key generated from your account on https://developers.masterpiecex.com/ in order to use the MPX API.");
            return;
        }

        MPX_TestConnection();

        if (RunTextToImage)
        {
            MPX_TextToImage();
        }

        if (RunTextTo3DModel)
        {
            MPX_TextTo3DModel();
        }

        if (RunImageTo3DModel)
        {
            MPX_ImageTo3DModel();
        }
    }

    /// <summary>
    /// Test to see if the API key works.
    /// 
    /// See: https://docs.masterpiecex.com/reference/get_connection-test#/
    /// </summary>
    void MPX_TestConnection()
    {
        // add default request headers for all requests
        RestClient.DefaultRequestHeaders["Authorization"] = $"Bearer {MPXSecretToken}";
        RestClient.DefaultRequestHeaders["Accept"] = "Text/Plain";

        RestClient.Get(url_TestConnection).Then(res =>
        {
            Debug.Log($"MPX_TestConnection() -- {res.Text}");
            return res.Text;
        }).Then(res =>
        {
            // clear the default headers for all requests
            RestClient.ClearDefaultHeaders();

        }).Catch(err => Debug.Log(err.Message));
    }


    /// <summary>
    /// Keep querying the requestId for a status of 'complete' or 'failed'. 
    /// Then run the given completionFunction delegate function at the end to do post-processing like displaying the result.
    /// It is up to the delegate function to handle both 'complete' and 'failed' statuses.
    /// </summary>
    /// <param name="requestId"></param>
    /// <param name="completionFunction"></param>
    /// <returns></returns>
    IEnumerator MPX_QueryStatus(string requestId, CompleteGenerationRequest completionFunction)
    {
        bool isComplete = false;
        StatusCheckData finalStatusResult = null;
        while (!isComplete)
        {
            // add default request headers for all requests
            RestClient.DefaultRequestHeaders["Authorization"] = $"Bearer {MPXSecretToken}";
            RestClient.DefaultRequestHeaders["Accept"] = "Text/Plain";

            RestClient.Get<StatusCheckData>(url_CheckStatus + requestId).Then(res =>
            {
                Debug.Log($"MPX_QueryStatus -- {JsonUtility.ToJson(res, true)}");
                if (res.status == "complete" || res.status == "failed")
                {
                    finalStatusResult = res;
                    isComplete = true;
                }
                return res;
            }).Then(res =>
            {
                // clear the default headers for all requests
                RestClient.ClearDefaultHeaders();

            }).Catch(err => Debug.Log(err.Message));

            yield return new WaitForSeconds(10.0f);
        }
        completionFunction(finalStatusResult);
    }


    #region All the different ways to generate using the MPX API

    /// <summary>
    /// Generate an image from text using the MPX API and display the results as a quad.
    /// 
    /// See: https://docs.masterpiecex.com/reference/post_components-text2image-1#/ for more details.
    /// </summary>
    void MPX_TextToImage()
    {
        RestClient.DefaultRequestHeaders["Authorization"] = $"Bearer {MPXSecretToken}";
        RestClient.DefaultRequestHeaders["Accept"] = "Application/Json";
        RestClient.DefaultRequestHeaders["Content-Type"] = "Application/Json";

        RequestHelper request_TextToImage = new RequestHelper
        {
            Uri = url_TextToImage,
            Params = new Dictionary<string, string> {
            },
            Body = new TextToImage
            {
                prompt = PromptTextToImage,
                seed = SeedTextToImage,
                numImages = 1,          // the max number of images that can be generated in API one call is 4 and the min is 1
                numSteps = 4,           // the max number of steps is 4 the min is 1
                loraId = "mpx_game"     // stylization of the image, valid options are: mpx_plush, mpx_iso, mpx_game, more options to be added so stay tuned!
            },
            EnableDebug = true
        };
        RestClient.Post<StatusResponse>(request_TextToImage).Then(res => 
        {
            RestClient.ClearDefaultParams();
            Debug.Log(JsonUtility.ToJson(res, true));

            // constantly poll to get the status of the request
            // when the request is complete, run the CompleteGenerationRequest_Images function
            StartCoroutine(MPX_QueryStatus(res.requestId, CompleteGenerationRequest_Images));

        }).Catch(err => Debug.Log(err.Message));
    }


    /// <summary>
    /// Generals a 3D model from just a single text prompt.
    /// 
    /// See: https://docs.masterpiecex.com/reference/post_functions-general#/ for more details.
    /// </summary>
    void MPX_TextTo3DModel()
    {
        RestClient.DefaultRequestHeaders["Authorization"] = $"Bearer {MPXSecretToken}";
        RestClient.DefaultRequestHeaders["Accept"] = "Application/Json";
        RestClient.DefaultRequestHeaders["Content-Type"] = "Application/Json";

        RequestHelper request_TextTo3DModel = new RequestHelper
        {
            Uri = url_TextTo3DModel,
            Params = new Dictionary<string, string>
            {
            },
            Body = new TextTo3DModel
            {
                prompt = PromptTextTo3DModel,
            },
            EnableDebug = true
        };
        RestClient.Post<StatusResponse>(request_TextTo3DModel).Then(res =>
        {
            RestClient.ClearDefaultParams();
            Debug.Log(JsonUtility.ToJson(res, true));

            // constantly poll to get the status of the request
            // when the request is complete, run the CompleteGenerationRequest_3DModel function
            StartCoroutine(MPX_QueryStatus(res.requestId, CompleteGenerationRequest_3DModel));

        }).Catch(err => Debug.Log(err.Message));
    }

    /// <summary>
    /// Runs the image-to-3d endpoint by uploading a Texture2D that was assigned via the inspector.
    /// Note that the Texture2D needs to have read/write enabled otherwise we won't be able to convert it to a PNG to upload to the servers.
    /// 
    /// See: 
    ///     https://docs.masterpiecex.com/reference/post_assets-create#/
    ///     https://docs.masterpiecex.com/reference/post_functions-imageto3d#/    
    /// for more details on the two API calls used.
    /// </summary>
    void MPX_ImageTo3DModel()
    {
        if (InputImage == null)
        {
            Debug.Log("InputImage is NULL! Please assign an image to the InputImage field in the inspector to proceed.");
            return;
        }

        RestClient.DefaultRequestHeaders["Authorization"] = $"Bearer {MPXSecretToken}";
        RestClient.DefaultRequestHeaders["Accept"] = "Application/Json";
        RestClient.DefaultRequestHeaders["Content-Type"] = "Application/Json";

        string image_requestId = null;
        string image_assetUrl = null;

        // generate the asset request Id and url to upload the image to
        RequestHelper request_CreateImageAsset = new RequestHelper
        {
            Uri = url_CreateAssetID,
            Params = new Dictionary<string, string>
            {
            },
            Body = new AssetCreationRequest
            {
                name = "input_image.png", // NOTE: the name cannot contain any spaces
                description = "image for generation",
                type = "image/png"
            },
            EnableDebug = true
        };
        RestClient.Post<AssetCreationResponse>(request_CreateImageAsset).Then(res =>
        {
            RestClient.ClearDefaultParams();
            Debug.Log(JsonUtility.ToJson(res, true));

            image_requestId = res.requestId;
            image_assetUrl = res.assetUrl;

            // we have valid data so let's try to run image-to-3d
            if ((image_requestId != null) && (image_assetUrl != null))
            {
                // upload texture to the given asset URL
                Texture2D decopmpresseTex = InputImage.DeCompress();
                byte[] pngBytes = decopmpresseTex.EncodeToPNG();

                RestClient.DefaultRequestHeaders["Authorization"] = $"Bearer {MPXSecretToken}";
                RestClient.DefaultRequestHeaders["Content-Type"] = "image/png";
                RequestHelper request_UploadAsset = new RequestHelper
                {
                    Uri = image_assetUrl,
                    BodyRaw = pngBytes,
                    EnableDebug = true
                };
                RestClient.Put(request_UploadAsset, (err, res) =>
                {
                    if (err != null)
                    {
                        Debug.LogError($"Error uploading image to server: {err.Message}");
                    }

                    // image was successfully uploaded? now run the image-to-3d endpoint using the requestId from /asset/create eariler
                    else
                    {
                        Debug.Log("Successfully uploaded the image.");

                        RestClient.DefaultRequestHeaders["Authorization"] = $"Bearer {MPXSecretToken}";
                        RestClient.DefaultRequestHeaders["Accept"] = "Application/Json";
                        RestClient.DefaultRequestHeaders["Content-Type"] = "Application/Json";

                        RequestHelper request_ImageTo3DModel = new RequestHelper
                        {
                            Uri = url_ImageTo3DModel,
                            Params = new Dictionary<string, string>
                            {
                            },
                            Body = new ImageTo3DModelRequest
                            {
                                imageRequestId = image_requestId,
                                seed = SeedImageTo3DModel,
                                textureSize = 1024,
                                imageUrl = null
                            },
                            EnableDebug = true
                        };
                        RestClient.Post<StatusResponse>(request_ImageTo3DModel).Then(res =>
                        {

                            RestClient.ClearDefaultParams();
                            Debug.Log(JsonUtility.ToJson(res, true));

                            // constantly poll to get the status of the request
                            // when the request is complete, run the CompleteGenerationRequest_3DModel function
                            StartCoroutine(MPX_QueryStatus(res.requestId, CompleteGenerationRequest_3DModel));

                        }).Catch(err => Debug.Log(err.Message));
                    }
                });
            }
            else
            {
                Debug.LogError("requestId and assertUrl are NULL when trying to use /asset/create endpoint.");
            }

        }).Catch(err => Debug.Log(err.Message));
    }

    #endregion


    #region Delegate functions to handle what happens when a generation request is completed.

    /// <summary>
    /// Download all generated images, construct a quad per image that is generated and apply the downloaded image as the texture to the corresponding quad.
    /// </summary>
    /// <param name="data"></param>
    void CompleteGenerationRequest_Images(StatusCheckData data)
    {
        // we got some sort of result that's either a complete or a fail
        if (data != null)
        {
            // request is complete? download the image result to disk and show a quad with the image
            if (data.status == "complete")
            {
                Debug.Log($"[TextToImage] Request: {data.requestId} -- COMPLETED!");

                // generate quads to show all the generated images
                var imageUrls = data.outputs.images;
                for (int i = 0; i < imageUrls.Length; i++)
                {
                    StartCoroutine(DownloadAndDisplay_GeneratedImage(imageUrls[i]));
                }
            }

            // request failed? log this result
            else if (data.status == "failed")
            {
                Debug.Log($"[TextToImage] Request: {data.requestId} -- FAILED!");
            }
        }
    }

    /// <summary>
    /// Download the GLB model that's generated by either text-to-3d or image-to-3d.
    /// </summary>
    /// <param name="data"></param>
    void CompleteGenerationRequest_3DModel(StatusCheckData data)
    {
        // we got some sort of result that's either a complete or a fail
        if (data != null)
        {
            // request is complete? download the image result to disk and show a quad with the image
            if (data.status == "complete")
            {
                Debug.Log($"[3DModel] Request: {data.requestId} -- COMPLETED!");

                StartCoroutine(DownloadAndDisplay_Generated3DModel(data.outputs.glb));
            }

            // request failed? log this result
            else if (data.status == "failed")
            {
                Debug.Log($"[3DModel] Request: {data.requestId} -- FAILED!");
            }
        }
    }

    #endregion


    #region Functions to download generated results and display them in Unity

    /// <summary>
    /// Download an image from an URL and generate a quad to display it.
    /// </summary>
    /// <param name="imageURL"></param>
    /// <returns></returns>
    IEnumerator DownloadAndDisplay_GeneratedImage(string imageURL)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageURL);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(request.error);
        }
        else
        {
            GameObject generatedImageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            generatedImageQuad.name = "Generated Image";
            MeshRenderer meshRenderer = generatedImageQuad.GetComponent<MeshRenderer>();
            meshRenderer.material.mainTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;

            // for demo purposes, apply a position offset and rotate the 3D model by 180 degrees to face the camera
            ApplyOffsetsForDemo(generatedImageQuad, false);
        }
    }

    /// <summary>
    /// Download a GLB model from an URL and display it.
    /// </summary>
    /// <param name="glbURL"></param>
    /// <returns></returns>
    IEnumerator DownloadAndDisplay_Generated3DModel(string glbURL)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(glbURL);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(request.error);
        }
        else
        {
            GameObject go = new GameObject("Generated 3D Model");
            var gltf = go.AddComponent<GLTFast.GltfAsset>();
            gltf.Url = glbURL;

            // for demo purposes, apply a position offset and rotate the 3D model by 180 degrees to face the camera
            ApplyOffsetsForDemo(go, true);
        }
    }

    /// <summary>
    /// This function is used just for demo purposes so that generated objects appear in a line as each generation is completed.
    /// Optionally rotate the model by 180 degrees.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="flipRotation"></param>
    void ApplyOffsetsForDemo(GameObject go, bool flipRotation)
    {
        go.transform.position = new Vector3(Demo_NumGenerated, 0, 0);
        if (flipRotation)
        {
            go.transform.Rotate(new Vector3(0, 180, 0));
        }
        Demo_NumGenerated++;
    }

    #endregion


    void Update()
    {
    }
}
