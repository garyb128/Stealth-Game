Shader "Custom/LightCapture"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            // Calculate luminance contribution from a single light
            float LightLuminance(float3 normalWS, Light light)
            {
                float NdotL = dot(normalWS, normalize(light.direction));
                // Wrap to 0-1 so back faces still contribute partially
                // This is important for the octahedron since we need all faces to respond
                NdotL = (NdotL + 1) * 0.5;
                float3 contribution = saturate(NdotL)
                                    * light.color
                                    * light.distanceAttenuation
                                    * light.shadowAttenuation;

                // Convert to perceived luminance
                return dot(contribution, float3(0.2126, 0.7152, 0.0722));
            }

            half4 frag(Varyings IN) : SV_Target0
            {
                float3 normalWS = normalize(IN.normalWS);

                // Required by LIGHT_LOOP_BEGIN for Forward+
                InputData inputData = (InputData)0;
                inputData.positionWS            = IN.positionWS;
                inputData.normalWS              = normalWS;
                inputData.viewDirectionWS       = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                float totalLuminance = 0;

                // Main directional light
                Light mainLight = GetMainLight();
                totalLuminance += LightLuminance(normalWS, mainLight);

                // All additional lights (point, spot) — handles both Forward and Forward+
                #if defined(_ADDITIONAL_LIGHTS)

                // Forward+ non-main directional lights
                #if USE_CLUSTER_LIGHT_LOOP
                UNITY_LOOP for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                {
                    Light light = GetAdditionalLight(dirIndex, inputData.positionWS, half4(1,1,1,1));
                    totalLuminance += LightLuminance(normalWS, light);
                }
                #endif

                // Point and spot lights
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    totalLuminance += LightLuminance(normalWS, light);
                LIGHT_LOOP_END

                #endif

                // Output as greyscale — our readback script reads R channel for luminance
                return half4(totalLuminance, totalLuminance, totalLuminance, 1.0);
            }

            ENDHLSL
        }
    }
}
