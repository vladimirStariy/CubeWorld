using System;
using UnityEngine;

[Serializable]
public sealed class WorldSettingsJson
{
    public string generator;
    public int seed;
    public WorldSizeJson size;
    public WorldSpawnJson spawn;
    public WorldClimateJson climate;
    public WorldTerrainJson terrain;
}

[Serializable]
public sealed class WorldSizeJson
{
    public int width;
    public int depth;
    public int height;
    public int baseY;
    public int seaLevel;
}

[Serializable]
public sealed class WorldSpawnJson
{
    public int x;
    public int z;
}

[Serializable]
public sealed class WorldClimateJson
{
    public float temperateTemperature;
    public float poleTemperatureDrop;
    public float latitudeScale;
}

[Serializable]
public sealed class WorldTerrainJson
{
    public float noiseScale;
    public int heightVariation;
}

[Serializable]
public sealed class BiomeJson
{
    public string id;
    public string displayName;
    public float minTemperature;
    public float maxTemperature;
    public string surfaceBlock;
    public string subsurfaceBlock;
    public string fillerBlock;
    public int priority;
}
