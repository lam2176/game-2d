// Sprite trong thế giới 3D (URP Universal Renderer) — nhân vật, quái, đạo cụ dạng ảnh phẳng:
//   • Nhận ánh sáng mặt trời (bóng đổ từ nhà, cây), đèn điểm (đèn lồng, lửa) và ánh sáng môi trường (SH),
//     nên nhân vật "chìm" vào màu ánh sáng của từng màn thay vì trông như dán lên.
//   • Đổ bóng thật xuống đất (pass ShadowCaster, cắt theo alpha).
//   • Cắt alpha (cutout) + ghi depth: tự sắp xếp trước/sau với địa hình 3D, không cần sorting order.
//   • Sương mù theo khoảng cách (màu xa nhạt dần vào trời).
// Không bật GPU instancing: khi instancing, SpriteRenderer gửi màu qua mảng riêng thay vì màu đỉnh → mất màu tô.
// Ảnh phẳng không có pháp tuyến → coi pháp tuyến hướng về camera, hơi ngửa lên trời; ánh sáng "quấn" (wrap) để
// mặt sau không đen kịt. Màu SpriteRenderer (HitFlash, nhiễm độc, tàng hình) nhân qua màu đỉnh như shader sprite thường.
Shader "Zodiac/Sprite Lit 3D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _Wrap ("Light Wrap", Range(0, 1)) = 0.6
        _PointWrap ("Point Light Wrap", Range(0, 1)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half _Cutoff;
            half _Wrap;
            half _PointWrap;
        CBUFFER_END

        // Pháp tuyến giả của ảnh phẳng (không gian thế giới): về phía camera (-Z), hơi ngửa lên.
        static const float3 SpriteNormalWS = float3(0.0, 0.34, -0.94);

        half4 SampleSprite(float2 uv, half4 vertexColor)
        {
            half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * vertexColor * _Color;
            clip(c.a - _Cutoff);
            return c;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half Wrapped(half ndl, half wrap)
            {
                return saturate((ndl + wrap) / (1.0h + wrap));
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 albedo = SampleSprite(i.uv, i.color);
                float3 n = SpriteNormalWS;

                // Lấy bóng tại điểm dời nhẹ về phía mặt trời: ảnh phẳng không tự đổ bóng lên chính nó.
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS + _MainLightPosition.xyz * 0.3);
                Light mainLight = GetMainLight(shadowCoord);
                half3 lighting = mainLight.color * (Wrapped(dot(n, mainLight.direction), _Wrap) *
                                                    mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                lighting += SampleSH(n);

                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = n;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                #if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
                // Đèn định hướng phụ (Forward+ tách riêng khỏi vòng lặp cụm)
                for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                {
                    Light dl = GetAdditionalLight(dirIndex, i.positionWS, half4(1, 1, 1, 1));
                    lighting += dl.color * (Wrapped(dot(n, dl.direction), _Wrap) * dl.distanceAttenuation * dl.shadowAttenuation);
                }
                #endif
                LIGHT_LOOP_BEGIN(lightCount)
                    Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                    // Đèn điểm chiếu gần như mọi phía: đèn lồng sau lưng vẫn hắt sáng viền lên nhân vật.
                    lighting += l.color * (Wrapped(dot(n, l.direction), _PointWrap) * l.distanceAttenuation * l.shadowAttenuation);
                LIGHT_LOOP_END
                #endif

                half3 color = albedo.rgb * lighting;
                color = MixFog(color, i.fogFactor);
                return half4(color, 1.0h);
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

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, SpriteNormalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target
            {
                SampleSprite(i.uv, i.color);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                return o;
            }

            half DepthFrag(Varyings i) : SV_Target
            {
                SampleSprite(i.uv, i.color);
                return i.positionCS.z;
            }
            ENDHLSL
        }

        // Cho SSAO (bóng đổ góc khuất) của renderer: ghi pháp tuyến giả của ảnh phẳng.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                return o;
            }

            half4 DepthNormalsFrag(Varyings i) : SV_Target
            {
                SampleSprite(i.uv, i.color);
                float3 normalWS = SpriteNormalWS;
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
                return half4(PackFloat2To888(remapped), 0.0);
                #else
                return half4(NormalizeNormalPerPixel(normalWS), 0.0);
                #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}
