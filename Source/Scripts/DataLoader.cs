using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataLoader : MonoBehaviour
{
    private Texture3D[] shapes = new Texture3D[1001];
    public Texture3D shape;
    private Material mat;
    private float time =0.0f;
    private Texture3D shape1,shape2;
    void Awake()
    {
        for (int i = 0; i < 1001; i++)
        {
            shapes[i] = (Texture3D)Resources.Load(i.ToString());
        }

        mat = GetComponent<MeshRenderer>().material;

    }

   
    void OnWillRenderObject()
    {

        time += Time.deltaTime;
        time = time %20.0f;
        int index = (int)(time * 50.0f);
        shape1 = shapes[index];
        //shape2 = shapes[(index + 1) % 1000];
        mat.SetTexture("Test",shape1);  
        //mat.SetTexture("Shape2", shape2);     //For linear interpolation between two instances
    }
}
