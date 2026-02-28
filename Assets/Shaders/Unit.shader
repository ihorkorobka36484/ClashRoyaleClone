Shader "Custom/Unit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _TeamColorBase ("TeamColorBase", Color) = (1,1,1,1)
        _TeamColor ("TeamColor", Color) = (1,1,1,1)
        _TeamColorPosConstrainU1 ("TeamColorPosConstrainU1", Range(0,1)) = 0
        _TeamColorPosConstrainV1 ("TeamColorPosConstrainV1", Range(0,1)) = 0
        _TeamColorPosConstrainU2 ("TeamColorPosConstrainU2", Range(0,1)) = 1
        _TeamColorPosConstrainV2 ("TeamColorPosConstrainV2", Range(0,1)) = 1
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionStrength ("Emission Strength", Range(0,1)) = 1
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1

        [HideInInspector] _SrcBlend ("", Float) = 1
        [HideInInspector] _DstBlend ("", Float) = 0
        [HideInInspector] _ZWrite ("", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile  _MAIN_LIGHT_SHADOWS
            #pragma multi_compile  _SHADOWS_SOFT
            #pragma multi_compile  _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv: TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3; // Add this line
                float3 bitangentWS : TEXCOORD4; // Add this line
                float4 shadowCoord : TEXCOORD5;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Glossiness;
            float _Metallic;
            float4 _TeamColorBase;
            float4 _TeamColor;
            float4 _EmissionColor;
            float _EmissionStrength;
            sampler2D _BumpMap;
            float _BumpScale;
            sampler2D _OcclusionMap;
            float _OcclusionStrength;
            float _TeamColorPosConstrainU1;
            float _TeamColorPosConstrainV1;
            float _TeamColorPosConstrainU2;
            float _TeamColorPosConstrainV2;

            float3 getTeamColor(float3 col) {
                if (abs(col.r - _TeamColorBase.r) < 0.1 && abs(col.g - _TeamColorBase.g) < 0.1 && abs(col.b - _TeamColorBase.b) < 0.1)
                    return _TeamColor.rgb;
                else
                    return col;
            }

            Varyings vert(Attributes v)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(v.uv, _MainTex);
                OUT.normalWS = TransformObjectToWorldNormal(v.normalOS);

                // Tangent to world
                float3 tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                float3 normalWS = OUT.normalWS;
                float tangentSign = v.tangentOS.w * unity_WorldTransformParams.w;
                float3 bitangentWS = cross(normalWS, tangentWS) * tangentSign;
                OUT.tangentWS = tangentWS;
                OUT.bitangentWS = bitangentWS;

                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                return OUT;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Albedo and team color logic
                float4 albedoTex = tex2D(_MainTex, i.uv) * _Color;
                float3 albedo = albedoTex.rgb;
                if (i.uv.x > _TeamColorPosConstrainU1 && i.uv.x < _TeamColorPosConstrainU2 &&
                    i.uv.y > _TeamColorPosConstrainV1 && i.uv.y < _TeamColorPosConstrainV2)
                {
                    albedo = getTeamColor(albedoTex.rgb);
                }

                // Normal map (tangent space)
                float3 tangentNormal = UnpackNormalScale(tex2D(_BumpMap, i.uv), _BumpScale);
                float3x3 TBN = float3x3(normalize(i.tangentWS), normalize(i.bitangentWS), normalize(i.normalWS));
                float3 normalWS = normalize(mul(tangentNormal, TBN));

                // Occlusion map
                float occlusion = lerp(1.0, tex2D(_OcclusionMap, i.uv).g, _OcclusionStrength);

                // PBR inputs
                float metallic = _Metallic;
                float smoothness = _Glossiness;
                float3 positionWS = i.positionWS;
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);

                // Setup PBRData
                SurfaceData surfaceData;
                surfaceData.albedo = albedo;
                surfaceData.metallic = metallic;
                surfaceData.specular = 0; // Not used in metallic workflow
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalWS;
                surfaceData.emission = _EmissionColor.rgb * _EmissionStrength;
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = 1;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = i.shadowCoord;
                inputData.fogCoord = 0;
                inputData.vertexLighting = 0;
                inputData.bakedGI = 0;

                // Lighting calculation (GGX, shadows, ambient, etc.)
                float4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.a = _Color.a;

                return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}