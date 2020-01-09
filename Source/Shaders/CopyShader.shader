Shader "Hidden/CopyShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		
    }
    SubShader
    {
		Tags{"RenderType"="Transparent"}
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		//Blend SrcAlpha OneMinusSrcAlpha
		//Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
			sampler2D _Environment;
            sampler2D _MainTex;
			

			float GradientNoise(float2 uv)
			{
				uv = floor(uv * _ScreenParams.xy);
				float f = dot(float2(0.06711056f, 0.00583715f), uv);
				return frac(52.9829189f * frac(f));
			}

            float4 frag (v2f i) : SV_Target
            {
				float4 smoke = tex2D(_MainTex, i.uv);
				float4 env = tex2D(_Environment, i.uv);
                // just invert the colors
                
                float4 color = env*(1.0-smoke.w) + smoke.w*smoke;

				
						color.rgb = LinearToGammaSpace(color.rgb);
				
				float dither = GradientNoise(i.uv) * (2.0 / 255.0) - (0.5 / 255.0);
				color += dither*2;
				
					color.rgb = GammaToLinearSpace(color.rgb);
				
					return color;
            }
            ENDCG
        }
    }
}
