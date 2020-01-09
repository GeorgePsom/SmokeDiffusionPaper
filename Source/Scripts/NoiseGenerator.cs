using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Globalization;


public class NoiseGenerator : MonoBehaviour
{
    public const double SQRT3 = 1.7320508075688772935274463;
    public const double PI = 3.1415926535897932384626;
    const int computeThreadGroupSize = 16;
   
    public const string shapeNoiseName = "ShapeNoise";

    public enum TextureChannel { R, G, B, A }
    [Header("Noise Settings")]
    public Vector3Int shapeResolution;
    public ComputeShader noiseCompute;
    // Internal
    List<ComputeBuffer> buffersToRelease;
    [SerializeField, HideInInspector]
    public RenderTexture shapeTexture;
    private float[] co2 = new float[216000];
    private Vector3[] u = new Vector3[216000];
    private List<Vector3[]> points = new List<Vector3[]>();
    public void Start()
    {
       
        CreateTexture(ref shapeTexture, shapeResolution, shapeNoiseName);
     
        System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        customCulture.NumberFormat.NumberDecimalSeparator = ".";
        System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

        InitializePoints();
        String msec;
        int i = 1;
        while (i <1001)
        {
            int j = 2;
            float truncated = (float)(Math.Truncate((double)(j * i)) / 100.0);
            msec = truncated.ToString();
            
            int k = 0;
            using (StreamReader sr = new StreamReader("Assets/Data/" + msec + "/CO2"))
            {

                string line;
               
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        float f = float.Parse(line, CultureInfo.InvariantCulture.NumberFormat);
                        if (f < 2.0f)

                            co2[k++] = f;
                    }
                    catch (FormatException e)
                    {
                        //Debug.Log(e);
                    }


                }
                
            }
            float[] xyz = new float[3];
            int ind = 0;
            int c = 0;
            float f1, f2, f3;
            
            using (StreamReader sr= new StreamReader("Assets/Data/"+msec + "/U"))
            {
                string line;
                while((line = sr.ReadLine())!=null)
                {
                   
                    if (line == "216000")
                        continue;
                    line = line.Trim('(');
                    line = line.Trim(')');
                    string[] sArray = line.Split(' ');
                    try
                    {
                        f1 = float.Parse(sArray[0], CultureInfo.InvariantCulture.NumberFormat);
                    }
                    catch(FormatException e)
                    {
                        continue;
                    }
                    try
                    {
                         f2 = float.Parse(sArray[1], CultureInfo.InvariantCulture.NumberFormat);
                    }
                    catch (FormatException e)
                    {
                        continue;
                    }
                    try
                    {
                         f3 = float.Parse(sArray[2], CultureInfo.InvariantCulture.NumberFormat);
                    }
                    catch (FormatException e)
                    {
                        continue;
                    }
                    u[c++]= new Vector3(-f1,f3,-f2);

                   

                }

            }
            i++;
            if (noiseCompute)
            {
                    buffersToRelease = new List<ComputeBuffer>();
                    noiseCompute.SetVector("resolution", (Vector3)shapeResolution);
                    // Set noise gen kernel data:
                    noiseCompute.SetTexture(0, "Result", shapeTexture);
                    var minMaxBuffer = CreateBuffer(new int[] { int.MaxValue, 0 }, sizeof(int), "minMax", 0);
                    UpdateWorley( i-1);
                    noiseCompute.SetTexture(0, "Result", shapeTexture);
                    int numThreadGroupsx = Mathf.CeilToInt(shapeResolution.x / 8.0f);
                    int numThreadGroupsy = Mathf.CeilToInt(shapeResolution.y / 8.0f);
                    int numThreadGroupsz = Mathf.CeilToInt(shapeResolution.z / (float)computeThreadGroupSize);
                    var minMax = new int[2];
                    //for (int j = 0; j < 8; j++)            //Depends on the GPU, you can decrease the dispatch groups and increase the iterations on the cpu
                    // {                                    //If you do so, decrease the z group and also change the zero at the offset.
                    noiseCompute.SetInt("offset", 8 * 0);   //For instance for 8 iterations, noiseCompute.Dispatch(0,16,8,1) and 
                    noiseCompute.Dispatch(0, 16, 8, 8);     //noiseCompute.SetInt("offset",8*i);
                    minMaxBuffer.GetData(minMax);
                    // }

                    foreach (var buffer in buffersToRelease)
                    {
                        buffer.Release();
                    }
                    var slice = FindObjectOfType<Save3D>();
                    slice.Save(shapeTexture, (i-1).ToString());             
            }
        }
    }

  void InitializePoints()
    {
        for(int i = 0; i< 32; i ++)
        {
            Vector3[] arrayPoints = new Vector3[216000];
            for(int j =0; j< 216000;j++)
            {
                arrayPoints[j] = new Vector3(10.0f, 10.0f, 10.0f);
            }
            points.Add(arrayPoints);
        }

    }
    void UpdateWorley(int index)
    {

        var prng = new System.Random(10);
        for(int i =0; i <32; i++)
        {
            CreateSmokePoints(prng, "points"+i.ToString(), index, i);
        }

        noiseCompute.SetInt("numCellsA", 90*60*40);
        noiseCompute.SetBool("invertNoise", true);
    }
    Vector4 IndexToIndex(int i)
    {
        int z = i / (60*90);
        int r = i - z*(60*90);
        int y = r/90;
        int x = r - y * 90;
        int newZ = y;
        int newY = z;
        int index = x + newZ * 90 + newY * 90 * 60;
        Vector3 v = new Vector3(-x , newY , -newZ );
        return new Vector4(v.x,v.y,v.z,index);
    }
    

    Vector3 CubicRoots(double a, double b, double c)
    {
        double p = b - (a * a) / 3.0;
        double q = 2.0 * (a * a * a) / 27.0 - (a * b) / 3.0 + c;
        double D = (q * q) / 4.0 + (p * p * p) / 27.0;
        if(D >0)
        {
            double sign1 = System.Math.Sign(-q / 2.0 + System.Math.Sqrt(D));
            double sign2 = System.Math.Sign(-q / 2.0 - System.Math.Sqrt(D));
            double x1 = sign1*System.Math.Pow(sign1*(-q / 2.0 + System.Math.Sqrt(D)), (1.0/3.0)) + sign2*System.Math.Pow(sign2*(-q / 2.0 - System.Math.Sqrt(D)), (1.0/3.0)) - a / 3.0;
            return  new Vector3((float)x1,-100000.0f,-100000.0f);
        }
        else if ( D ==0.0)
        {
            double x1 = -2.0 * System.Math.Pow(0.5 * q, 0.333333) - a / 3.0;
            double x2 = System.Math.Pow(0.5 * q, 0.3333333) - a / 3.0;
            return new Vector3((float)x1,(float)x2,(float)x2);

        }
        else
        {
            double sqrtmP = System.Math.Sqrt(-p);
            double k = 3.0 * SQRT3 * q / (2.0f * System.Math.Pow(sqrtmP, 3.0));
            double x1 = (2.0 / SQRT3) * sqrtmP * System.Math.Sin(0.333333 * System.Math.Asin(k)) - a / 3.0;
            double x2 = -(2.0 / SQRT3) * sqrtmP * System.Math.Sin(0.333333 * System.Math.Asin(k) + PI / 3.0) - a / 3.0;
            double x3 = (2.0 / SQRT3) * sqrtmP * System.Math.Cos(0.333333 * System.Math.Asin(k) + PI / 6.0) - a / 3.0;
            return new Vector3((float)x1, (float)x2, (float)x3);
        }
    }

    void CreateSmokePoints(System.Random prng, string bufferName, int time, int k)
    {
        for(int i =0; i <216000; i++)
        {
            float density = co2[i];
            if (density < 0.03125f)
            {
                if(points[k][i].x < 9.0f)
                {
                    points[k][i] = new Vector3(10.0f, 10.0f, 10.0f);
                }
                continue;
            }
            Vector4 voxel = IndexToIndex(i);
            if (points[k][i].x > 9.0f)
            {
                
                float offsetX = -(float)prng.NextDouble();
                float offsetY = (float)prng.NextDouble();
                float offsetZ = -(float)prng.NextDouble();
                Vector3 point = new Vector3(voxel.x + offsetX, voxel.y + offsetY, voxel.z + offsetZ);
                points[k][i] = point;
            }
            else
            {
                Vector3 velocity =100.0f*0.02f*u[i];
                Vector3 prevPos = points[k][i];
                Vector3 decimalPos = new Vector3(prevPos.x % 1.0f, prevPos.y % 1.0f, prevPos.z % 1.0f);
                decimalPos = new Vector3(decimalPos.x + velocity.x, decimalPos.y + velocity.y + decimalPos.z + velocity.z);
                decimalPos = new Vector3(decimalPos.x % 1.0f, decimalPos.y % 1.0f, decimalPos.z % 1.0f);
                points[k][i] = new Vector3(voxel.x + decimalPos.x, voxel.y + decimalPos.y, voxel.z + decimalPos.z);

            }
            co2[i] -= 0.03125f;
                

        }
        CreateBuffer(points[k], sizeof(float) * 3, bufferName);
    }

    void CreateWorleyPointsBuffer(System.Random prng, int numCellsPerAxis, string bufferName)
    {
        var points = new Vector3[numCellsPerAxis * numCellsPerAxis * numCellsPerAxis];
        float cellSize = 1f / numCellsPerAxis;

        for (int x = 0; x < numCellsPerAxis; x++)
        {
            for (int y = 0; y < numCellsPerAxis; y++)
            {
                for (int z = 0; z < numCellsPerAxis; z++)
                {
                    float randomX = (float)prng.NextDouble();
                    float randomY = (float)prng.NextDouble();
                    float randomZ = (float)prng.NextDouble();
                    Vector3 randomOffset = new Vector3(randomX, randomY, randomZ) * cellSize;
                    Vector3 cellCorner = new Vector3(x, y, z) * cellSize;

                    int index = x + numCellsPerAxis * (y + z * numCellsPerAxis);
                    points[index] = cellCorner + randomOffset;
                }
            }
        }

        CreateBuffer(points, sizeof(float) * 3, bufferName);
    }

  
    ComputeBuffer CreateBuffer(System.Array data, int stride, string bufferName, int kernel = 0)
    {
        var buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Structured);
        buffersToRelease.Add(buffer);
        buffer.SetData(data);
        noiseCompute.SetBuffer(kernel, bufferName, buffer);
        return buffer;
    }

    void CreateTexture(ref RenderTexture texture, Vector3Int resolution, string name)
    {
        var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;
        if (texture == null || !texture.IsCreated() || texture.width != resolution.x || texture.height != resolution.y || texture.volumeDepth != resolution.z || texture.graphicsFormat != format)
        {
            if (texture != null)
            {
                texture.Release();
            }
            texture = new RenderTexture(resolution.x, resolution.y, 0);
            texture.graphicsFormat = format;
            texture.volumeDepth = resolution.z;
            texture.enableRandomWrite = true;
            texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            texture.name = name;

            texture.Create();
           
        }
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
    }

  
}