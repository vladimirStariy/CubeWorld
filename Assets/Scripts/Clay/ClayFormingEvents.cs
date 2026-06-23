using System;

public static class ClayFormingEvents
{
    public static event Action<ClayWorksiteKey> WorksiteChanged;

    public static void RaiseWorksiteChanged(ClayWorksiteKey key)
    {
        WorksiteChanged?.Invoke(key);
    }
}
