using UnityEngine;

public static class GameConsoleLog
{
    private static GameCommandConsole console;

    public static void Bind(GameCommandConsole commandConsole)
    {
        console = commandConsole;
    }

    public static void Info(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        console?.Log(line);
        Debug.Log(line);
    }
}
