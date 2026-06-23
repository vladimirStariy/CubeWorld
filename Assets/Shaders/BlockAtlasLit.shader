Shader "CubeWorld/BlockAtlasLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Atlas", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _TilePixelSize("Tile Pixel Size", Float) = 32
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _TilePixelSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 quadUV : TEXCOORD0;
                float4 tileRect : TEXCOORD1;
                float2 tileCount : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 quadUV : TEXCOORD0;
                float4 tileRect : TEXCOORD1;
                float2 tileCount : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float fogCoord : TEXCOORD4;
            };

            float2 BlockToAtlasUV(float2 quadUV, float2 tileCount, float4 tileRect)
            {
                float2 tileUV = quadUV * tileCount;
                // frac(1) == 0 maps the far edge back to the near edge; skip frac when only one tile fits.
                float2 tiled;
                tiled.x = tileCount.x > 1.00001 ? frac(tileUV.x) : saturate(tileUV.x);
                tiled.y = tileCount.y > 1.00001 ? frac(tileUV.y) : saturate(tileUV.y);
                float inset = 0.5 / max(_TilePixelSize, 1.0);
                tiled = lerp(inset, 1.0 - inset, tiled);
                return tileRect.xy + tiled * tileRect.zw;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.quadUV = input.quadUV;
                output.tileRect = input.tileRect;
                output.tileCount = input.tileCount;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogCoord = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 atlasUV = BlockToAtlasUV(input.quadUV, input.tileCount, input.tileRect);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, atlasUV) * _BaseColor;

                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 color = albedo.rgb * (mainLight.color * ndotl + half3(0.3, 0.3, 0.3));
                color = MixFog(color, input.fogCoord);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
