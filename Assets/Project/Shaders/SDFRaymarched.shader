Shader "Custom/SDFRaymarched"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _DiffuseTexture("Diffuse Texture", 2D) = "white" {}
        _NormalTexture("Normal Texture", 2D) = "bump" {}
        _SpecularMult("Specular", float) = 0.5
        _InsideTexture("Inside Texture", 2D) = "black" {}
        _SDFTexture("SDF Texture", 3D) = "black" {}
        _SDFTangentTexture("SDF Tangent Texture", 3D) = "black" {}
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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

            TEXTURE2D(_NormalTexture);
            SAMPLER(sampler_NormalTexture);

            TEXTURE2D(_InsideTexture);
            SAMPLER(sampler_InsideTexture);
            
            TEXTURE3D(_SDFTexture);
            SAMPLER(sampler_SDFTexture);

            TEXTURE3D(_SDFTangentTexture);
            SAMPLER(sampler_SDFTangentTexture);
            
            TEXTURE3D(_SDFUVTexture);
            SAMPLER(sampler_SDFUVTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _SpecularMult;
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

                int maxSteps = int(length(_TexSize)) * 8;

                dir = normalize(dir);
                float3 step = dir;

                float3 start = (camOS / _TexelSize + _TexSize.xyz / 2);

                float t = 0;

                float lastSDF = SAMPLE_TEXTURE3D(_SDFTexture, sampler_SDFTexture, ((start + step * t) / _TexSize.xyz).zyx);

                [loop] for (int i = 0; i < maxSteps; i++) {
                    float3 curr = (start + step * t);
                    if (curr.x < 0 || curr.x >= _TexSize.x
                        || curr.y < 0 || curr.y >= _TexSize.y
                        || curr.z < 0 || curr.z >= _TexSize.z) {
                        t += 2;
                        continue;
                    }
                    float4 sdf = SAMPLE_TEXTURE3D(_SDFTexture, sampler_SDFTexture, (curr / _TexSize.xyz).zyx);
                    if (abs(sdf.a) < 0.015) {
                        // Attribute calculation
                        float3 worldPos = camWS + normalize(IN.positionWS - camWS) * (t * _TexelSize);

                        float2 uv = SAMPLE_TEXTURE3D(_SDFUVTexture, sampler_SDFUVTexture, (curr / _TexSize.xyz).zyx).xy;

                        float3x3 M = transpose((float3x3)(_InvObjectTransform));

                        float4 T = SAMPLE_TEXTURE3D(_SDFTangentTexture, sampler_SDFTangentTexture, (curr / _TexSize.xyz).zyx);
                        float3 tangent = T.xyz;

                        float3 N = sdf.xyz;

                        float3x3 tbn = float3x3(tangent, normalize(cross(tangent, N)) * T.w, N);

                        float3 normal = mul(UnpackNormal(SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, uv)), tbn);
                        normal = mul(M, normal);

                        // Shading
                        float4 albedo = lerp(SAMPLE_TEXTURE2D(_DiffuseTexture, sampler_DiffuseTexture, uv),
                                            SAMPLE_TEXTURE2D(_InsideTexture, sampler_InsideTexture, uv), T.w);

                        float3 viewDir = normalize(camWS - IN.positionWS);

	                    float ratio = 1.00 / 1.52;
	                    float3 posDir = normalize(-viewDir);
	                    float3 reflDir = refract(posDir, normal, ratio);

                        float3 skyLight = SampleSH(normal);
	                    float3 lighting = albedo.xyz * 0.5 * skyLight;

                        Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPos));
	                    float shadow = mainLight.shadowAttenuation;

                        float3 lightDir = -mainLight.direction;
                        float diff = max(dot(normal, -lightDir), 0.0);

		                float3 reflectDir = reflect(lightDir, normal);
		                float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
		                float3 specular1 = _SpecularMult * spec * mainLight.color;

		                //float3 res = (diff * mainLight.color + specular1) * albedo.xyz * shadow;
		                float3 res = (diff * mainLight.color) * albedo.xyz * shadow;
		                lighting += res;

                        OUT.color = half4(lighting, 1);
                        //OUT.color = half4(T.w, T.w, T.w, 1);

                        // Depth
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

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float depth: SV_Depth;
            };
            
            TEXTURE3D(_SDFTexture);
            SAMPLER(sampler_SDFTexture);

            CBUFFER_START(UnityPerMaterial)
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

            FragOutput frag(Varyings IN) {
                FragOutput OUT;

                Light mainLight = GetMainLight();

                float3 lightDir = mainLight.direction;

                float3 camWS = IN.positionWS - lightDir * 4;
                float3 camOS = mul(_InvObjectTransform, float4(camWS, 1)).xyz;
                float3 positionOS = mul(_InvObjectTransform, float4(IN.positionWS, 1)).xyz;

                float3 dir = positionOS - camOS;

                int maxSteps = int(length(_TexSize)) * 8;

                dir = normalize(dir);
                float3 step = dir;

                float3 start = (camOS / _TexelSize + _TexSize.xyz / 2);

                float t = 0;

                float lastSDF = SAMPLE_TEXTURE3D(_SDFTexture, sampler_SDFTexture, ((start + step * t) / _TexSize.xyz).zyx);

                [loop] for (int i = 0; i < maxSteps; i++) {
                    float3 curr = (start + step * t);
                    if (curr.x < 0 || curr.x >= _TexSize.x
                        || curr.y < 0 || curr.y >= _TexSize.y
                        || curr.z < 0 || curr.z >= _TexSize.z) {
                        t += 2;
                        continue;
                    }
                    float4 sdf = SAMPLE_TEXTURE3D(_SDFTexture, sampler_SDFTexture, (curr / _TexSize.xyz).zyx);
                    if (abs(sdf.a) < 0.015) {
                        float3 worldPos = camWS + normalize(IN.positionWS - camWS) * (t * _TexelSize);

                        float3x3 M = transpose((float3x3)(_InvObjectTransform));
                        float3 normal = mul(M, sdf.xyz);

                        float4 posHCS = TransformWorldToHClip(ApplyShadowBias(worldPos, normal, lightDir));
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
