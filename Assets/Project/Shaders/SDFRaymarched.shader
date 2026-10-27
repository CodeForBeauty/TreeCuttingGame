Shader "Custom/SDFRaymarched"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _DiffuseTexture("Diffuse Texture", 2D) = "white" {}
        _SDFTexture("SDF Texture", 3D) = "black" {}
        _SDFUVTexture("SDF UV Texture", 3D) = "black" {}
        _TexelSize("Texel Size", float) = 0.1
        _TexSize("Texture Size", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct FragOutput {
                half4 color: SV_Target;
                float depth: SV_Depth;
            };

            TEXTURE2D(_DiffuseTexture);
            SAMPLER(sampler_DiffuseTexture);
            
            TEXTURE3D(_SDFTexture);
            SAMPLER(sampler_SDFTexture);

            TEXTURE3D(_SDFUVTexture);
            SAMPLER(sampler_SDFUVTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4x4 _InvObjectTransform;
                float _TexelSize;
                float4 _TexSize;
            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 forward = GetViewForwardDir();

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = mul(unity_ObjectToWorld, IN.positionOS).xyz;

                return OUT;
            }

            FragOutput frag(Varyings IN)
            {
                FragOutput OUT;

                float3 camWS = GetCameraPositionWS();
                float3 camOS = mul(_InvObjectTransform, float4(camWS, 1)).xyz;
                float3 positionOS = mul(_InvObjectTransform, float4(IN.positionWS, 1)).xyz;

                float3 dir = positionOS - camOS;

                int maxSteps = int(length(_TexSize)) * 4;

                dir = normalize(dir);
                float3 step = dir;

                float3 start = (camOS / _TexelSize + _TexSize.xyz / 2);

                float t = 0;

                float lastSDF = SAMPLE_TEXTURE3D(_SDFTexture, sampler_SDFTexture, (start + step * t) / _TexSize.xyz);

                [loop] for (int i = 0; i < maxSteps; i++) {
                    float3 curr = (start + step * t);
                    if (curr.x < 0 || curr.x >= _TexSize.x
                        || curr.y < 0 || curr.y >= _TexSize.y
                        || curr.z < 0 || curr.z >= _TexSize.z) {
                        t += 2;
                        continue;
                    }
                    float4 sdf = SAMPLE_TEXTURE3D(_SDFTexture, sampler_SDFTexture, curr / _TexSize.xyz);
                    if (abs(sdf.a) < 0.015) {
                        float2 uv = SAMPLE_TEXTURE3D(_SDFUVTexture, sampler_SDFUVTexture, curr / _TexSize.xyz).xy;
                        float4 diffuse = SAMPLE_TEXTURE2D(_DiffuseTexture, sampler_DiffuseTexture, uv);
                        OUT.color = _BaseColor * diffuse * dot(sdf.xyz, float3(0, 0.75, -0.75));
                        float3 worldPos = camWS + normalize(IN.positionWS - camWS) * (t * _TexelSize);
                        float4 posHCS = TransformWorldToHClip(worldPos);
                        OUT.depth = posHCS.z / posHCS.w;

                        #if UNITY_REVERSED_Z

                        #else
                            OUT.depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, OUT.depth);
                        #endif
                        return OUT;
                    }
                    
                    t += sdf.a;
                }

                discard;
                return OUT;
            }
            ENDHLSL
        }
    }
}
