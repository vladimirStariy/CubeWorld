Shader "CubeWorld/SelectionOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _LineWidth("Line Width (pixels)", Float) = 2
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Pass
        {
            Name "FaceOutline"
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _LineWidth;
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

                float4 clipA = TransformObjectToHClip(input.positionOS.xyz);
                float4 clipB = TransformObjectToHClip(otherOS);

                float2 a = clipA.xy / clipA.w;
                float2 b = clipB.xy / clipB.w;
                float2 dir = b - a;

                if (dot(dir, dir) < 1e-8)
                {
                    dir = float2(1, 0);
                }
                else
                {
                    dir = normalize(dir);
                }

                float2 perp = float2(-dir.y, dir.x);
                float2 offset = perp * extrude * (_LineWidth * 0.5) / _ScreenParams.y;
                clipA.xy += offset * clipA.w;

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
