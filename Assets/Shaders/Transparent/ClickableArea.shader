// This shader fills the mesh shape with a color predefined in the code.
Shader "Example/URPUnlitShaderBasic"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _CornerRadius ("CornerRadius", Range(0,0.1)) = 0.01
        _EdgeThickness ("EdgeThickness", Range(0,0.3)) = 0.01
        _InsideAlpha ("InsideAlpha", Range(0,1)) = 0.1
        _ObjectScale ("ObjectScale", Vector) = (1,1,1,1)
    }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Glossiness;
            float _Metallic;
            float _CornerRadius;
            float _EdgeThickness;
            float _InsideAlpha;
            float4 _ObjectScale;

            // The vertex shader definition with properties defined in the Varyings 
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
           Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // The fragment shader definition.            
            float roundedRect(float2 uv, float2 size, float radius)
            {
                float2 halfSize = size * 0.5;
                float2 center = float2(0.5, 0.5);
                float2 p = abs(uv - center) - halfSize + radius;
                float d = length(max(p, 0.0)) - radius;
                return saturate(1.0 - step(0.0, d));
            }

            float roundedRectEdge(float2 uv, float2 size, float radius, float thickness, float2 scale)
            {
                float2 adjustedThickness = thickness / scale.xy;
                float outer = roundedRect(uv, size, radius);
                float inner = roundedRect(uv, size - adjustedThickness, max(0, radius - max(adjustedThickness.x, adjustedThickness.y) * 0.5));
                return saturate(outer - inner);
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv) * _Color;

                float2 rectSize = float2(1, 1);
                float cornerRadius = _CornerRadius;
                float edgeThickness = _EdgeThickness;
                float2 scale = float2(_ObjectScale.x, _ObjectScale.z);

                float fillMask = roundedRect(i.uv, rectSize, cornerRadius);
                float edgeMask = roundedRectEdge(i.uv, rectSize, cornerRadius, edgeThickness, scale);

                float edgeAlpha = 1.0;
                float fillAlpha = _InsideAlpha;

                float alpha = lerp(fillAlpha, edgeAlpha, edgeMask) * fillMask * _Color.a;

                // Simple lit color (no PBR)
                return float4(c.rgb, alpha);
            }
            ENDHLSL
        }
    }
}