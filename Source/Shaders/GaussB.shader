Shader "PostProcess/GaussB"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Sigma("Sigma",float) = 0.8
		KERNEL_SIZE("Kernel Size",int) = 3
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment frag

            #include "UnityCG.cginc"

           
            sampler2D _MainTex;
			uniform float4 _MainTex_TexelSize;
			uniform float _Sigma;
			uniform int KERNEL_SIZE;
			#define PI 3.14159265

			float gauss(float x, float y, float sigma)
			{
				return  1.0f / (2.0f * PI * sigma * sigma) * exp(-(x * x + y * y) / (2.0f * sigma * sigma));
			}

			float4 frag(v2f_img i) : COLOR
			{
				float4 o = 0;
				float sum = 0;
				float2 uvOffset;
				float weight;

				for (int x = -KERNEL_SIZE; x <= KERNEL_SIZE; ++x)
				{
					for (int y = -KERNEL_SIZE; y <= KERNEL_SIZE; ++y)
					{
						uvOffset = i.uv;
						uvOffset.x += x * _MainTex_TexelSize.x;
						uvOffset.y += y * _MainTex_TexelSize.y;
						weight = gauss(x, y, _Sigma);
						o += tex2D(_MainTex, uvOffset) * weight;
						sum += weight;
					}
				}
				o *= (1.0f / sum);
				return o;

            }
            ENDCG
        }
    }
}
