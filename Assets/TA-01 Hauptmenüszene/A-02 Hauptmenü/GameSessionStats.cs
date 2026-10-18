// Ergebnis vom letzten Durchlauf. static, damit es den Szenenwechsel ins Hauptmenü übersteht.
public static class GameSessionStats
{
    public static float lastRunTime;
    public static int lastClearedFirePoints;
    public static int lastWarnings;
    public static bool hasResult;

    // Zählt während des Spiels mit
    public static int currentWarnings;

    public static void RegisterWarning()
    {
        currentWarnings++;
    }

    public static void SaveResults(float time, int clearedFirePoints)
    {
        lastRunTime = time;
        lastClearedFirePoints = clearedFirePoints;
        lastWarnings = currentWarnings;
        hasResult = true;
    }

    // Beim Start eines neuen Durchlaufs, damit nach einem Abbruch kein altes Ergebnis angezeigt wird
    public static void ResetRun()
    {
        currentWarnings = 0;
        hasResult = false;
    }
}
