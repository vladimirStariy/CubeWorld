using UnityEngine;



public sealed class WorldSettings

{

    public static WorldSettings Active { get; set; }



    public ContentId GeneratorId { get; set; } = new("cubeworld", "planet");

    public int Seed { get; set; }

    public int Height { get; set; } = WorldConstants.DefaultWorldHeight;

    public int BaseLayerY { get; set; }

    public int SeaLevel { get; set; } = WorldConstants.DefaultSeaLevel;

    public int SpawnX { get; set; }

    public int SpawnZ { get; set; }

    public float TemperateTemperature { get; set; } = 14f;

    public float PoleTemperatureDrop { get; set; } = 26f;

    public float ClimateLatitudeScale { get; set; } = WorldConstants.DefaultClimateLatitudeScale;

    public float TerrainNoiseScale { get; set; } = 0.02f;

    public int TerrainHeightVariation { get; set; } = 4;



    public Vector3 GetSpawnPosition()

    {

        return new Vector3(SpawnX + 0.5f, SeaLevel + 2f, SpawnZ + 0.5f);

    }



    public void ClampToLimits()

    {

        Height = Mathf.Clamp(Height, 4, 512);

        SeaLevel = Mathf.Clamp(SeaLevel, BaseLayerY + 1, Height - 2);

        ClimateLatitudeScale = Mathf.Max(100f, ClimateLatitudeScale);

    }

}


