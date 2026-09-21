// Numbers about the current run. "static" means there is only one copy,
// shared by every script, so any script can read or change it.
public static class GameStats
{
    public static int Kills;
    public static float SurvivedSeconds;
    public static bool IsGameOver;

    public static void Reset()
    {
        Kills = 0;
        SurvivedSeconds = 0f;
        IsGameOver = false;
    }
}
