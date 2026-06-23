Shader "CubeWorld/ClayFormingOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _LineWidth("Line Width (pixels)", Float) = 2
        _ViewBias("View Bias", Float) = 0.002
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Pass
        {
            Name "ClayFormingOutline"
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Offset 0, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ScreenSpaceLine.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _LineWidth;
                float _ViewBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 lineData : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 otherOS = input.lineData.xyz;
                float extrude = input.lineData.w;

                float3 positionWS = BiasOutlineTowardCamera(input.positionOS.xyz, _ViewBias);
                float3 otherWS = BiasOutlineTowardCamera(otherOS, _ViewBias);

                float4 clipA = TransformWorldToHClip(positionWS);
                float4 clipB = TransformWorldToHClip(otherWS);
                clipA = ApplyScreenSpaceLineExtrusion(clipA, clipB, extrude, _LineWidth);

                output.positionHCS = clipA;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
