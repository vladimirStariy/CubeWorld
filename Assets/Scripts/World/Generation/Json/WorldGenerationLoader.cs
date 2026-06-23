using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class WorldGenerationLoader
{
    private const string ContentFolderName = "Content";
    private const string WorldFileName = "world.json";
    private const string BiomesFolderName = "biomes";
    private const string VanillaPackFolderName = "cubeworld";

    public static WorldSettings LoadMergedSettings()
    {
        var settings = new WorldSettings();
        var contentRoot = Path.Combine(Application.streamingAssetsPath, ContentFolderName);
        if (!Directory.Exists(contentRoot))
        {
            settings.ClampToLimits();
            return settings;
        }

        var packDirectories = new List<string>(Directory.GetDirectories(contentRoot));
        packDirectories.Sort(ComparePackLoadOrder);

        for (int i = 0; i < packDirectories.Count; i++)
        {
            TryApplyWorldFile(Path.Combine(packDirectories[i], WorldFileName), settings);
        }

        settings.ClampToLimits();
        return settings;
    }

    public static void LoadBiomesFromPacks(BiomeRegistry registry)
    {
        if (registry == null)
        {
            return;
        }

        var contentRoot = Path.Combine(Application.streamingAssetsPath, ContentFolderName);
        if (!Directory.Exists(contentRoot))
        {
            return;
        }

        var packDirectories = new List<string>(Directory.GetDirectories(contentRoot));
        packDirectories.Sort(ComparePackLoadOrder);

        for (int i = 0; i < packDirectories.Count; i++)
        {
            LoadBiomeDirectory(Path.Combine(packDirectories[i], BiomesFolderName), registry, Path.GetFileName(packDirectories[i]));
        }
    }

    public static void RegisterBuiltInGenerators(WorldGeneratorRegistry registry, WorldSettings settings)
    {
        registry.Register(new PlanetWorldGenerator());
        registry.Register(new FlatWorldGenerator(new ContentId("cubeworld", "grass_block")));

        if (settings != null && !string.IsNullOrWhiteSpace(settings.GeneratorId.Name))
        {
            registry.DefaultGeneratorId = settings.GeneratorId;
        }
    }

    private static void TryApplyWorldFile(string path, WorldSettings settings)
    {
        if (!File.Exists(path))
        {
            return;
        }

        if (!TryReadJson(path, out var jsonText))
        {
            return;
        }

        var json = JsonUtility.FromJson<WorldSettingsJson>(jsonText);
        if (json == null)
        {
            return;
        }

        ApplyWorldJson(json, settings);
    }

    private static void ApplyWorldJson(WorldSettingsJson json, WorldSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(json.generator) && ContentId.TryParse(json.generator, out var generatorId))
        {
            settings.GeneratorId = generatorId;
        }

        settings.Seed = json.seed;

        if (json.size != null)
        {
            if (json.size.height > 0)
            {
                settings.Height = json.size.height;
            }

            settings.BaseLayerY = json.size.baseY;
            if (json.size.seaLevel > 0)
            {
                settings.SeaLevel = json.size.seaLevel;
            }
        }

        if (json.spawn != null)
        {
            settings.SpawnX = json.spawn.x;
            settings.SpawnZ = json.spawn.z;
        }

        if (json.climate != null)
        {
            if (json.climate.temperateTemperature != 0f)
            {
                settings.TemperateTemperature = json.climate.temperateTemperature;
            }

            if (json.climate.poleTemperatureDrop != 0f)
            {
                settings.PoleTemperatureDrop = json.climate.poleTemperatureDrop;
            }

            if (json.climate.latitudeScale > 0f)
            {
                settings.ClimateLatitudeScale = json.climate.latitudeScale;
            }
        }

        if (json.terrain != null)
        {
            if (json.terrain.noiseScale > 0f)
            {
                settings.TerrainNoiseScale = json.terrain.noiseScale;
            }

            if (json.terrain.heightVariation > 0)
            {
                settings.TerrainHeightVariation = json.terrain.heightVariation;
            }
        }
    }

    private static void LoadBiomeDirectory(string directory, BiomeRegistry registry, string packName)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var files = Directory.GetFiles(directory, "*.json");
        System.Array.Sort(files, string.CompareOrdinal);

        for (int i = 0; i < files.Length; i++)
        {
            if (!TryReadJson(files[i], out var jsonText))
            {
                continue;
            }

            var json = JsonUtility.FromJson<BiomeJson>(jsonText);
            if (!TryParseBiome(json, out var biome, out var error))
            {
                Debug.LogError($"Pack '{packName}' biomes/{Path.GetFileName(files[i])}: {error}");
                continue;
            }

            registry.Register(biome);
        }
    }

    private static bool TryParseBiome(BiomeJson json, out BiomeDefinition biome, out string error)
    {
        biome = null;
        error = null;

        if (json == null)
        {
            error = "Biome entry is null.";
            return false;
        }

        if (!ContentId.TryParse(json.id, out var biomeId))
        {
            error = $"Invalid biome id: {json.id}";
            return false;
        }

        if (!TryParseBlockContentId(json.surfaceBlock, out var surfaceBlockId, out error)
            || !TryParseBlockContentId(json.subsurfaceBlock, out var subsurfaceBlockId, out error)
            || !TryParseBlockContentId(json.fillerBlock, out var fillerBlockId, out error))
        {
            return false;
        }

        biome = new BiomeDefinition(
            biomeId,
            string.IsNullOrWhiteSpace(json.displayName) ? biomeId.Name : json.displayName,
            json.minTemperature,
            json.maxTemperature,
            surfaceBlockId,
            subsurfaceBlockId,
            fillerBlockId,
            json.priority);

        return true;
    }

    private static bool TryParseBlockContentId(string value, out ContentId blockId, out string error)
    {
        blockId = default;
        error = null;

        if (!ContentId.TryParse(value, out blockId))
        {
            error = $"Invalid block id: {value}";
            return false;
        }

        return true;
    }

    private static bool TryReadJson(string path, out string jsonText)
    {
        jsonText = null;
        try
        {
            jsonText = File.ReadAllText(path);
            return !string.IsNullOrWhiteSpace(jsonText);
        }
        catch (IOException exception)
        {
            Debug.LogError($"Failed to read world generation file '{path}': {exception.Message}");
            return false;
        }
    }

    private static int ComparePackLoadOrder(string left, string right)
    {
        var leftName = Path.GetFileName(left);
        var rightName = Path.GetFileName(right);
        if (leftName == VanillaPackFolderName)
        {
            return -1;
        }

        if (rightName == VanillaPackFolderName)
        {
            return 1;
        }

        return string.CompareOrdinal(leftName, rightName);
    }

    public static void RegisterFallbackBiomes(BiomeRegistry registry)
    {
        registry.Register(new BiomeDefinition(
            new ContentId("cubeworld", "tropical"),
            "Tropical",
            22f,
            42f,
            new ContentId("cubeworld", "grass_block"),
            new ContentId("cubeworld", "dirt"),
            new ContentId("cubeworld", "dirt"),
            0));
        registry.Register(new BiomeDefinition(
            new ContentId("cubeworld", "temperate"),
            "Temperate",
            8f,
            22f,
            new ContentId("cubeworld", "grass_block"),
            new ContentId("cubeworld", "dirt"),
            new ContentId("cubeworld", "dirt"),
            1));
        registry.Register(new BiomeDefinition(
            new ContentId("cubeworld", "cold"),
            "Cold",
            -4f,
            8f,
            new ContentId("cubeworld", "dirt"),
            new ContentId("cubeworld", "dirt"),
            new ContentId("cubeworld", "dirt"),
            0));
        registry.Register(new BiomeDefinition(
            new ContentId("cubeworld", "polar"),
            "Polar",
            -40f,
            -2f,
            new ContentId("cubeworld", "dirt"),
            new ContentId("cubeworld", "dirt"),
            new ContentId("cubeworld", "dirt"),
            0));
    }
}
