Shader "Custom/SDFRaymarched"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _SDFTexture("SDF Texture", 3D) = "black" {}
        _TexelSize("Texel Size", float) = 0.1
        _TexSize("Texture Size", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
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
            
            TEXTURE3D(_SDFTexture);
            SAMPLER(sampler_SDFTexture);

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

            FragOutput frag(Varyings IN) : SV_Target
            {
                FragOutput OUT;

                float3 camOS = mul(_InvObjectTransform, float4(GetCameraPositionWS(), 1)).xyz;
                float3 positionOS = mul(_InvObjectTransform, float4(IN.positionWS, 1)).xyz;

                float3 dir = positionOS - camOS;

                int maxSteps = int(length(dir) / _TexelSize + length(_TexSize.xyz));

                dir = normalize(dir);
                float3 step = dir;

                float3 curr = (camOS / _TexelSize + _TexSize.xyz / 2);

                float lastSDF = SAMPLE_TEXTURE3D(_SDFTexture, sampler_SDFTexture, curr / _TexSize.xyz);

                [loop] for (int i = 0; i < maxSteps; i++) {
                    if (curr.x < 0 || curr.x >= _TexSize.x
                        || curr.y < 0 || curr.y >= _TexSize.y
                        || curr.z < 0 || curr.z >= _TexSize.z) {
                        curr += step;
                        continue;
                    }
                    float4 sdf = SAMPLE_TEXTURE3D(_SDFTexture, sampler_SDFTexture, curr / _TexSize.xyz);
                    if (sdf.a < 0.01) {
                        OUT.color = _BaseColor;
                        OUT.depth = IN.positionHCS.w;
                        return OUT;
                    }
                    else if (lastSDF < sdf.a) {
                        curr -= step * sdf.a;
                    }
                    else {
                        curr += step;
                    }

                    lastSDF = sdf.a;
                }

                discard;
                return OUT;
            }
            ENDHLSL
        }
    }
}
