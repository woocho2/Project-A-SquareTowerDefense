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

    private static readonly List<int> s_selectedEmblemIDs = new();
    public static IReadOnlyList<int> SelectedEmblemIDs => s_selectedEmblemIDs;

    public static void SelectDifficulty(GameDifficulty difficulty)
    {
        if (SelectedDifficulty == difficulty) return;

        SelectedDifficulty = difficulty;
        s_selectedEmblemIDs.Clear();
    }

    public static bool TrySelectEmblem(int emblemID)
    {
        if (emblemID <= 0 || s_selectedEmblemIDs.Count >= PickLimit || s_selectedEmblemIDs.Contains(emblemID)) return false;

        s_selectedEmblemIDs.Add(emblemID);
        return true;
    }

    public static bool RemoveEmblem(int emblemID)
    {
        return s_selectedEmblemIDs.Remove(emblemID);
    }

    public static void ClearEmblems()
    {
        s_selectedEmblemIDs.Clear();
    }
}
