using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Save3D : MonoBehaviour
{
    const int threadGroupSize = 32;
    public ComputeShader slicer;
    public int resolutionz, resolutionx, resolutiony;
    public void Save(RenderTexture volumeTex, string name)
    {
        resolutionz = volumeTex.volumeDepth;
        resolutionx = volumeTex.width;
        resolutiony = volumeTex.height;
        Texture2D[] slices = new Texture2D[resolutionz];

        //slicer.SetInt("resolution", resolutionz);
        slicer.SetTexture(0, "volumeTex", volumeTex);

        for (int layer =0; layer < resolutionz; layer++)
        {
            var slice = new RenderTexture(resolutionx, resolutiony, 0);
            slice.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            slice.enableRandomWrite = true;
            slice.Create();

            slicer.SetTexture(0, "slice", slice);
            slicer.SetInt("layer", layer);
            int numThreadGroupsx = Mathf.CeilToInt(resolutionx / (float)threadGroupSize);
            int numThreadGroupsy = Mathf.CeilToInt(resolutionx / (float)threadGroupSize);
            slicer.Dispatch(0, numThreadGroupsx, numThreadGroupsy, 1);

            slices[layer] = ConvertFromRenderTexture(slice);
        }
#if UNITY_EDITOR 
        var x = Tex3DFromTex2DArray(slices, resolutionz);
       AssetDatabase.CreateAsset(x, "Assets/Textures/"+name+".asset");
#endif
    }

    Texture2D ConvertFromRenderTexture (RenderTexture rt)
    {
        Texture2D output = new Texture2D(rt.width, rt.height);
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        output.Apply();
        return output;
    }

    Texture3D Tex3DFromTex2DArray(Texture2D[] slices, int resolution)
    {
        Texture3D tex3D = new Texture3D(resolutionx, resolutiony, resolutionz, TextureFormat.ARGB32, false);
        tex3D.filterMode = FilterMode.Trilinear;
        Color[] outputPixels = tex3D.GetPixels();

        for (int z =0; z < resolutionz;z++)
        {
            Color c = slices[z].GetPixel(0, 0);
            Color[] layerPixels = slices[z].GetPixels();
            for(int x = 0; x < resolutionx; x++)
            {
                for(int y=0; y<resolutiony; y++)
                {
                    outputPixels[x + resolutionx * (y + z * resolutiony)] = layerPixels[x + resolutionx * y];
                }
            }
        }
        tex3D.SetPixels(outputPixels);
        tex3D.Apply();
        return tex3D;
    }
}
