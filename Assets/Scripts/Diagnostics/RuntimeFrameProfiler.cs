using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

public static class RuntimeFrameProfiler
{
    private const int RollingWindow = 90;
    private const float SpikeFrameMs = 22f;

    private static readonly Dictionary<string, double> frameMs = new();
    private static readonly Dictionary<string, double> lastFrameBreakdown = new();
    private static readonly Dictionary<string, RollingStats> rolling = new();
    private static readonly Stack<(string name, long startTicks)> scopeStack = new();
    private static readonly List<string> spikeLogSections = new();

    private static double lastFrameMs;
    private static double worstFrameMs;
    private static int terrainDrawCount;

    public static double LastFrameMs => lastFrameMs;

    public static double GetLastSectionMs(string sectionName)
    {
        return lastFrameBreakdown.TryGetValue(sectionName, out var value) ? value : 0d;
    }

    public readonly struct Scope : System.IDisposable
    {
        private readonly string name;
        private readonly long startTicks;
        private readonly bool valid;

        public Scope(string sectionName)
        {
            name = sectionName;
            startTicks = Stopwatch.GetTimestamp();
            valid = !string.IsNullOrEmpty(sectionName);
            if (valid)
            {
                scopeStack.Push((name, startTicks));
            }
        }

        public void Dispose()
        {
            if (!valid)
            {
                return;
            }

            if (scopeStack.Count > 0 && scopeStack.Peek().name == name)
            {
                scopeStack.Pop();
            }

            var elapsedMs = TicksToMs(Stopwatch.GetTimestamp() - startTicks);
            AddSample(name, elapsedMs);
        }
    }

    public static Scope Begin(string sectionName) => new(sectionName);

    public static void Record(string sectionName, double milliseconds)
    {
        if (string.IsNullOrEmpty(sectionName))
        {
            return;
        }

        AddSample(sectionName, milliseconds);
    }

    public static void SetTerrainDrawCount(int count)
    {
        terrainDrawCount = count;
    }

    public static void EndFrame(float unityFrameMs)
    {
        lastFrameMs = unityFrameMs;
        if (unityFrameMs > worstFrameMs)
        {
            worstFrameMs = unityFrameMs;
        }

        foreach (var pair in frameMs)
        {
            GetRolling(pair.Key).Add(pair.Value);
        }

        lastFrameBreakdown.Clear();
        foreach (var pair in frameMs)
        {
            lastFrameBreakdown[pair.Key] = pair.Value;
        }

        if (unityFrameMs >= SpikeFrameMs)
        {
            LogSpike(unityFrameMs);
        }

        frameMs.Clear();
    }

    public static void AppendReport(StringBuilder builder)
    {
        builder.Append("\n--- Frame profiler (F3) ---");
        builder.Append("\nFrame: ").Append(lastFrameMs.ToString("0.0")).Append(" ms");
        builder.Append("  worst: ").Append(worstFrameMs.ToString("0.0")).Append(" ms");
        builder.Append("\nTerrain draws: ").Append(terrainDrawCount);

        spikeLogSections.Clear();
        foreach (var pair in rolling)
        {
            spikeLogSections.Add(pair.Key);
        }

        spikeLogSections.Sort((a, b) => rolling[b].Average.CompareTo(rolling[a].Average));

        var count = 0;
        for (int i = 0; i < spikeLogSections.Count && count < 8; i++)
        {
            var key = spikeLogSections[i];
            var stats = rolling[key];
            if (stats.Average < 0.05d)
            {
                continue;
            }

            builder.Append('\n')
                .Append(key)
                .Append(": last ")
                .Append(GetLast(key).ToString("0.00"))
                .Append(" ms, avg ")
                .Append(stats.Average.ToString("0.00"))
                .Append(" ms");
            count++;
        }
    }

    private static void AddSample(string sectionName, double milliseconds)
    {
        if (frameMs.TryGetValue(sectionName, out var existing))
        {
            frameMs[sectionName] = existing + milliseconds;
        }
        else
        {
            frameMs[sectionName] = milliseconds;
        }
    }

    private static double GetLast(string sectionName)
    {
        return lastFrameBreakdown.TryGetValue(sectionName, out var value) ? value : 0d;
    }

    private static RollingStats GetRolling(string sectionName)
    {
        if (!rolling.TryGetValue(sectionName, out var stats))
        {
            stats = new RollingStats(RollingWindow);
            rolling[sectionName] = stats;
        }

        return stats;
    }

    private static void LogSpike(float unityFrameMs)
    {
        var builder = new StringBuilder(256);
        builder.Append("[FrameSpike] ").Append(unityFrameMs.ToString("0.0")).Append(" ms");

        spikeLogSections.Clear();
        foreach (var pair in frameMs)
        {
            spikeLogSections.Add(pair.Key);
        }

        spikeLogSections.Sort((a, b) => frameMs[b].CompareTo(frameMs[a]));

        for (int i = 0; i < spikeLogSections.Count && i < 6; i++)
        {
            var key = spikeLogSections[i];
            builder.Append(" | ").Append(key).Append('=').Append(frameMs[key].ToString("0.0"));
        }

        UnityEngine.Debug.Log(builder.ToString());
    }

    private static double TicksToMs(long ticks)
    {
        return ticks * 1000d / Stopwatch.Frequency;
    }

    private sealed class RollingStats
    {
        private readonly double[] samples;
        private int index;
        private int count;
        private double sum;

        public double Average => count > 0 ? sum / count : 0d;

        public RollingStats(int window)
        {
            samples = new double[window];
        }

        public void Add(double value)
        {
            if (count == samples.Length)
            {
                sum -= samples[index];
            }
            else
            {
                count++;
            }

            samples[index] = value;
            sum += value;
            index = (index + 1) % samples.Length;
        }
    }
}
