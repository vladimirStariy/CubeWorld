#ifndef CUBEWORLD_SCREEN_SPACE_LINE_INCLUDED
#define CUBEWORLD_SCREEN_SPACE_LINE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

float SignNonZero(float value)
{
    return value > 0.0 ? 1.0 : (value < 0.0 ? -1.0 : 0.0);
}

float3 ComputeEdgeExtrudeDirOS(float3 localPos, float3 otherPos)
{
    float3 edgeVector = otherPos - localPos;
    float edgeLengthSq = dot(edgeVector, edgeVector);
    if (edgeLengthSq < 1e-8)
    {
        return float3(0.0, 1.0, 0.0);
    }

    float3 edgeDir = edgeVector * rsqrt(edgeLengthSq);
    float3 extrude = float3(0.0, 0.0, 0.0);
    if (abs(edgeDir.x) < 0.9)
    {
        extrude.x = SignNonZero(localPos.x);
    }

    if (abs(edgeDir.y) < 0.9)
    {
        extrude.y = SignNonZero(localPos.y);
    }

    if (abs(edgeDir.z) < 0.9)
    {
        extrude.z = SignNonZero(localPos.z);
    }

    float extrudeLengthSq = dot(extrude, extrude);
    if (extrudeLengthSq > 1e-8)
    {
        return extrude * rsqrt(extrudeLengthSq);
    }

    float3 outward = dot(localPos, localPos) > 1e-8 ? normalize(localPos) : float3(0.0, 1.0, 0.0);
    extrude = cross(edgeDir, cross(outward, edgeDir));
    extrudeLengthSq = dot(extrude, extrude);
    if (extrudeLengthSq > 1e-8)
    {
        return extrude * rsqrt(extrudeLengthSq);
    }

    return normalize(cross(edgeDir, float3(0.0, 1.0, 0.0)));
}

float4 ApplyOutlineDepthBias(float4 clipPos, float bias)
{
    if (bias <= 0.0)
    {
        return clipPos;
    }

#if UNITY_REVERSED_Z
    clipPos.z += bias * clipPos.w;
#else
    clipPos.z -= bias * clipPos.w;
#endif

    return clipPos;
}

float3 BiasOutlinePosition(float3 positionOS, float3 otherOS, float surfaceLift, float viewBias)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    float3 extrudeDirWS = TransformObjectToWorldDir(ComputeEdgeExtrudeDirOS(positionOS, otherOS));
    float dirLenSq = dot(extrudeDirWS, extrudeDirWS);
    if (dirLenSq > 1e-8 && surfaceLift > 0.0)
    {
        extrudeDirWS *= rsqrt(dirLenSq);
        positionWS += extrudeDirWS * surfaceLift;
    }

    if (viewBias > 0.0)
    {
        positionWS += normalize(GetWorldSpaceViewDir(positionWS)) * viewBias;
    }

    return positionWS;
}

float3 BiasOutlineTowardCamera(float3 positionOS, float viewBias)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    if (viewBias > 0.0)
    {
        positionWS += normalize(GetWorldSpaceViewDir(positionWS)) * viewBias;
    }

    return positionWS;
}

float4 ApplyScreenSpaceLineExtrusion(float4 clipA, float4 clipB, float extrude, float lineWidthPixels)
{
    return ApplyScreenSpaceLineExtrusionMiter(clipA, clipB, clipB, extrude, lineWidthPixels, 0.0);
}

float4 ApplyScreenSpaceLineExtrusionMiter(
    float4 clipA,
    float4 clipB,
    float4 clipMiter,
    float extrude,
    float lineWidthPixels,
    float useMiter)
{
    float2 a = clipA.xy / clipA.w;
    float2 b = clipB.xy / clipB.w;
    float2 edgeNdc = b - a;

    float2 edgePixels = float2(edgeNdc.x * _ScreenParams.x, edgeNdc.y * _ScreenParams.y);
    if (dot(edgePixels, edgePixels) < 1e-4)
    {
        edgePixels = float2(1.0, 0.0);
    }
    else
    {
        edgePixels = normalize(edgePixels);
    }

    float2 normalPixels = float2(-edgePixels.y, edgePixels.x);
    float miterScale = 1.0;

    if (useMiter > 0.5)
    {
        float2 m = clipMiter.xy / clipMiter.w;
        float2 joinNdc = m - a;
        float2 joinPixels = float2(joinNdc.x * _ScreenParams.x, joinNdc.y * _ScreenParams.y);
        if (dot(joinPixels, joinPixels) > 1e-4)
        {
            joinPixels = normalize(joinPixels);
            float2 joinNormal = float2(-joinPixels.y, joinPixels.x);
            float2 miterDir = normalPixels + joinNormal;
            if (dot(miterDir, miterDir) > 1e-4)
            {
                miterDir = normalize(miterDir);
                miterScale = rcp(clamp(dot(miterDir, normalPixels), 0.35, 1.0));
                miterScale = min(miterScale, 2.5);
                normalPixels = miterDir;
            }
        }
    }

    float2 offsetPixels = normalPixels * extrude * (lineWidthPixels * 0.5) * miterScale;
    float2 offsetNdc = float2(
        offsetPixels.x * 2.0 / _ScreenParams.x,
        offsetPixels.y * 2.0 / _ScreenParams.y);

    clipA.xy += offsetNdc * clipA.w;
    return clipA;
}

#endif
