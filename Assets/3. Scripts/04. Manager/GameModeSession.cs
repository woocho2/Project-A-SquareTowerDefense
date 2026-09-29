using UnityEngine;
using System.Collections.Generic;

public enum GameDifficulty
{
    Normal,
    Hard
}

public class GameModeSession : MonoBehaviour
{
    public static GameDifficulty SelectedDifficulty { get; private set; } = GameDifficulty.Normal;

    public static int PickLimit => SelectedDifficulty == GameDifficulty.Hard ? 8 : 6;

    private static readonly List<int> s_selectedPatternIDs = new();
    public static IReadOnlyList<int> SelectedPatternIDs => s_selectedPatternIDs;

    public static void SelectDifficulty(GameDifficulty difficulty)
    {
        if (SelectedDifficulty == difficulty) return;

        SelectedDifficulty = difficulty;
        s_selectedPatternIDs.Clear();
    }

    public static bool TrySelectPattern(int patternID)
    {
        if (patternID <= 0 || s_selectedPatternIDs.Count >= PickLimit || s_selectedPatternIDs.Contains(patternID)) return false;

        s_selectedPatternIDs.Add(patternID);
        return true;
    }

    public static bool RemovePattern(int patternID)
    {
        return s_selectedPatternIDs.Remove(patternID);
    }

    public static void ClearPatterns()
    {
        s_selectedPatternIDs.Clear();
    }
}
