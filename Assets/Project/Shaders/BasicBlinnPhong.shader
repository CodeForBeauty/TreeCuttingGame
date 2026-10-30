Shader "Custom/BasicBlinnPhong"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _DiffuseTexture("Diffuse Texture", 2D) = "white" {}
        _NormalTexture("Normal Texture", 2D) = "bump" {}
        _SpecularMult("Specular", Range(0, 1)) = 0.5
        _AlphaCut("Alpha Cut Threshold", Range(-0.5, 1)) = -0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
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
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 tangent : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3x3 tbn : TEXCOORD2;
            };

            TEXTURE2D(_DiffuseTexture);
            SAMPLER(sampler_DiffuseTexture);

            TEXTURE2D(_NormalTexture);
            SAMPLER(sampler_NormalTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _SpecularMult;
                float _AlphaCut;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                float3 normal = TransformObjectToWorldNormal(IN.normal);
                float3 tangent = TransformObjectToWorldNormal(IN.tangent);
                OUT.tbn = float3x3(
                    tangent,
                    cross(tangent, normal),
                    normal);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 albedo = _BaseColor * SAMPLE_TEXTURE2D(_DiffuseTexture, sampler_DiffuseTexture, IN.uv);

                if (albedo.a < _AlphaCut) {
                    discard;
                }

                float3 normal = mul(UnpackNormal(SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, IN.uv)), IN.tbn);

                float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);

	            float ratio = 1.00 / 1.52;
	            float3 posDir = normalize(-viewDir);
	            float3 reflDir = refract(posDir, normal, ratio);

                float3 skyLight = SampleSH(normal);
	            float3 lighting = albedo.xyz * 0.5 * skyLight;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
	            float shadow = mainLight.shadowAttenuation;

                float3 lightDir = -mainLight.direction;
                float diff = max(dot(normal, -lightDir), 0.0);

		        float3 reflectDir = reflect(lightDir, normal);
		        float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
		        float3 specular1 = _SpecularMult * spec * mainLight.color;

		        float3 res = (diff * mainLight.color + specular1) * albedo.xyz * shadow;
		        lighting += res;

                return half4(lighting, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ColorMask 0
            Cull Back

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                return 0;
            }

            ENDHLSL
        }
    }
}
