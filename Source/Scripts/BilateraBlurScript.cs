using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class BilateraBlurScript : MonoBehaviour
{
    public Shader CopyShader;
    public Material GaussianBlurMaterial; 
    private Camera camera;
    private Material copyMat;
    private Mesh mesh;
    private Vector3 position;
    private Quaternion rotation;
    private Material smokeMat;
   
    private RenderTexture houseTexture,smokeTexture,resultTexture;
    private CommandBuffer houseCB,smokeCB,smokeCB2,retrieveCB;
    private RenderTexture _houseTexture, _SmokeTexture, _SmokeTexture2;
    private RenderTextureDescriptor RTdesc;
    private void Start()
    {
        copyMat = new Material(CopyShader);
        
        CameraEvent beforeSky = CameraEvent.BeforeSkybox;
        camera = GetComponent<Camera>();
        Initialize();
        houseCB = new CommandBuffer() { name = "house" };
        RTdesc = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGBHalf);
        _houseTexture = RenderTexture.GetTemporary(RTdesc);
        _SmokeTexture = RenderTexture.GetTemporary(RTdesc);
        _SmokeTexture2 = RenderTexture.GetTemporary(RTdesc);
        houseCB.Blit(BuiltinRenderTextureType.CurrentActive, _houseTexture);
        camera.AddCommandBuffer(beforeSky, houseCB);
        
        smokeCB= new CommandBuffer() { name = "smoke" };
        smokeCB.ClearRenderTarget(false, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        smokeCB.SetRenderTarget(_SmokeTexture);
        camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, smokeCB);
        smokeCB2 = new CommandBuffer() { name = "smoke2" };
        smokeCB2.Blit(BuiltinRenderTextureType.CurrentActive, _SmokeTexture2);
        camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, smokeCB2);

    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var temporaryTexture = RenderTexture.GetTemporary(RTdesc);
        var temporaryTexture2 = RenderTexture.GetTemporary(RTdesc);
        Graphics.Blit(_SmokeTexture2, temporaryTexture, GaussianBlurMaterial,0);
        Graphics.Blit(temporaryTexture, temporaryTexture2, GaussianBlurMaterial, 1);

        copyMat.SetTexture("_Environment", _houseTexture);
        Graphics.Blit(temporaryTexture2, destination, copyMat);
        RenderTexture.ReleaseTemporary(temporaryTexture);
        RenderTexture.ReleaseTemporary(temporaryTexture2);
    }
    void Initialize()
    {
        RTdesc = new RenderTextureDescriptor(Screen.width, Screen.height);
       
    }
}
