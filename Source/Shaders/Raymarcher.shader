	Shader "Custom/Raymarcher"
{
    Properties
    {
		_Color("Smoke Color",Color) = (0.05,0.05,0.05,1.0)
		_BlueNoise("Blue Noise",2D) = ""{}
		_StepM("Steps per Depth(10cm)",Range(1,100)) = 5
		_Intensity("Intensity",Range(0.1,10.0)) = 1.0
		_DensityMultipler("Density Multiplier", Range(0.1,100.0)) = 1.0
		_DensityOffset("Density Offset", Range(-10.0,10.0)) = 0.0
		_RayOffsetMultiplier("Ray Offset Multiplier",Range(0.1,20)) = 5
		_ForwardScattering("Forward Scattering", Range(0.0,1.0)) = 0.5
		_BackScattering("Back Scattering", Range(0.0,1.0)) = 0.5
		_Base("Base Brightness",Range(0.0,1.0)) = 0.5
		_PhaseFactor("Phase Factor", Range(-1.0,1.0)) = 0.5
		_NumStepsLight("Number of Steps Light", Int) = 4
		_LightAbsorptionTowardLight("Light Absorption Toward Light",Range(0.0,1.0)) = 0.5
		_DarknessThreshold("Darkness Threshold",Range(0.0,1.0)) = 0.1
		_LightAbsorptionThroughSmoke("Light Absorption Through Smoke",Range(0.1,1.0)) = 0.8
		_ScatteringCoeff("Scattering Coefficient",Range(0.0,1.0)) = 1.0

		
		
		
    }
    SubShader
    {
		
       
		Tags{"Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
			Tags{"LightMode"="ForwardBase"}

		Cull Off ZWrite Off ZTest Always
		//Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			
            #include "UnityCG.cginc"
			
			#include "AutoLight.cginc"
			


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
			
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float2 uv : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
				


            };

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
				/*o.uv = v.uv;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);*/
				
				
                return o;
            }

			float4 frag(v2f i) : SV_Target
			{
				float4 col = float4(0,0,0,0);
				return col;
            }
            ENDCG
        }





		Pass
		{
			Tags{ "LightMode" = "ForwardAdd"}

		Cull Off ZWrite Off ZTest Always
		 Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma enable_d3d11_debug_symbols
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"



			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float2 uv : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
				float2 uv1 : TEXCOORD4;
				UNITY_SHADOW_COORDS(5)



			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				

				return o;
			}

			Texture3D<float4> Test;
			SamplerState samplerTest;
			Texture3D<float4> Shape2;
			SamplerState samplerShape2;
			sampler2D _ShadowMapTexture;
			sampler2D _CameraDepthTexture;
			float4 _Color;
			float _StepM;
			sampler2D _BlueNoise;
			float4 _ShapeWeight;
			float _Intensity;
			float _DensityMultipler;
			float _RayOffsetMultiplier;
			float _ForwardScattering;
			float _BackScattering;
			float _Base;
			float _PhaseFactor;
			int _NumStepsLight;
			float _LightAbsorptionTowardLight;
			float _DarknessThreshold;
			float _LightAbsorptionThroughSmoke;
			float _DensityOffset;
			float _ScatteringCoeff;
			uniform float4 _LightColor0;

			float sdBox(float3 p, float3 b)
			{
				float3 d = abs(p) - b;
				return min(max(d.x, max(d.y, d.z)), 0.0) +
					length(max(d, 0.0));
			}
			float distanceFieldHouse(float3 p)
			{
				float box = sdBox(p - float3(-4.5, 2.0, -3.0), float3(4.5, 2.0, 3.0));
				return (box);
			}
			float sampleDensity(float3 p)
			{
				//float time = _Time.y / 0.02f;
				//float l = frac(time);
				float3 uvw = float3(1 - (-9.0 - p.x) / (-9.0), 1 - (4.0 - p.y) / 4.0, 1.0 - (-6.0 - p.z) / -6.0);
				//float4 shape = (1 - l) * Test.SampleLevel(samplerTest, uvw, 0) + l * Shape2.SampleLevel(samplerShape2, uvw, 0); //linear interpolation
				float4 shape = Test.SampleLevel(samplerTest, uvw, 0);
				 float baseDensity= _Intensity*shape.a;
				 baseDensity *= _DensityMultipler;
				 if (baseDensity > 0)
					 baseDensity += 0.1 * _DensityOffset;
				return baseDensity ;
			}
			float HeneyeyGreenstein(float a, float g) {
				float g2 = g * g;
				return (1 - g2) / (4 * 3.1415 * pow(1 + g2 - 2 * g * (a), 1.5));
			}
			float Shlick(float a, float k)
			{
				return (1 - k * k) / (4 * 3.1415 * (1 + k * a) * (1 + k * a));
			}
			float phaseFunction(float a) {
				float blend = .5;
				float blendedScattering = HeneyeyGreenstein(a, _ForwardScattering) * (1 - blend) + HeneyeyGreenstein(a, -_BackScattering) * blend;
				return _Base + blendedScattering * _PhaseFactor;
			}
			float lightmarch(float3 position) {
				float3 dirToLight = normalize(_WorldSpaceLightPos0.xyz - position);
				float distance = length(_WorldSpaceLightPos0.xyz-position);
				float stepSize2 = distance / _NumStepsLight;
				float totalDensity = 0;
				float transmittance = 1.0;
				float density;
				for (int step = 0; step < _NumStepsLight; step++)
				{
					position += dirToLight * stepSize2;
					density = sampleDensity(position);
					totalDensity += max(0, density) * stepSize2;
				}
			    transmittance = exp(-totalDensity * _LightAbsorptionTowardLight);
				return _DarknessThreshold + transmittance * (1 - _DarknessThreshold);
			}
			float2 raymarch(float3 rayDir, float3 rayPos,float depth,v2f i)
			{
				float result = 0;
				int  iterations = 100*1024;
				float offset = tex2D(_BlueNoise, 2.0*i.uv);
				float step = 1 / _StepM;
				float t= step * offset * _RayOffsetMultiplier;
				float transmittance = 1.0;
				float lightEnergy = 0.0;
				float3 p;
				float cosAngle = dot(rayDir, normalize(_WorldSpaceLightPos0.xyz-rayPos));
				float lightTransmittance;
				float density;
				//UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
				float ph = phaseFunction(cosAngle);
				for (int i = 0; i < iterations; i++)
				{
					p = rayPos + t * rayDir;
					float dH = distanceFieldHouse(p);
					if (t > depth+0.2 || dH > -0.01)
						return float2(lightEnergy,transmittance);
					density = sampleDensity(p);
					if (density > 0.01)
					{
						lightTransmittance = lightmarch(p);
						transmittance *= exp(-density * step * _LightAbsorptionThroughSmoke);
						lightEnergy += density *_ScatteringCoeff* step * transmittance * lightTransmittance * ph;
						
					}
					if (transmittance < 0.0001)
						return float2(lightEnergy,transmittance);
					t += step;
				}
				return float2(lightEnergy,transmittance);
			}
			float4 frag(v2f i) : SV_Target
			{
				float3 rayDir = normalize(i.worldPos - _WorldSpaceCameraPos.xyz);
				float3 rayPos = _WorldSpaceCameraPos;

				float depth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r);

				float2 result = raymarch(rayDir, rayPos,depth,i);

				float4 col = float4(result.x * _Color.rgb*_LightColor0.rgb,1-result.y);

				return col;
			}
			ENDCG
		}
    }
}
