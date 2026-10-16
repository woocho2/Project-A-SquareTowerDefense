// Display stubs for running the source-extracted TileManager occupancy methods without Unity.
using System;
using System.Linq;

public record struct Vector3Int(int x, int y, int z = 0);
public record struct Vector2(float x, float y);
public record struct Vector3(float x, float y, float z = 0)
{
    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
}
public class Transform { public Vector3 position; public void SetParent(Transform parent, bool world) {} }
public class RectTransform { public Vector2 sizeDelta; }
public class Renderer { public int sortingLayerID, sortingOrder; }
public class GameObject
{
    public int layer;
    public bool activeInHierarchy = true;
    public Transform transform = new();
    private TextMeshPro component;
    public GameObject(string name = "", params Type[] types) { }
    public T GetComponent<T>() where T : class => (component ??= new TextMeshPro(this)) as T;
    public void SetActive(bool active) => activeInHierarchy = active;
}
public class Tilemap { public Vector3 cellSize = new(1.5f, 1.5f, 1); public Vector3 GetCellCenterWorld(Vector3Int cell) => new(cell.x, cell.y); }
public class TMP_FontAsset { }
public static class TMP_Settings { public static TMP_FontAsset defaultFontAsset = new(); }
public struct Color { public static Color white => new(); public static Color black => new(); }
public enum FontStyles { Bold }
public enum TextAlignmentOptions { Center }
public enum TextWrappingModes { NoWrap }
public static class SortingLayer { public static int NameToID(string name) => 1; }
public static class Mathf { public static int Max(int a, int b) => Math.Max(a, b); }
public class TextMeshPro
{
    public GameObject gameObject;
    public Transform transform = new();
    public RectTransform rectTransform = new();
    public Renderer renderer = new();
    public TMP_FontAsset font;
    public float fontSize, outlineWidth;
    public FontStyles fontStyle;
    public Color color, outlineColor;
    public TextAlignmentOptions alignment;
    public TextWrappingModes textWrappingMode;
    public bool raycastTarget;
    public string text;
    public TextMeshPro(GameObject owner) => gameObject = owner;
    public void SetText(string format, float count) => text = format.Replace("{0}", count.ToString());
}
public class EnemyMoveController
{
    public int Order;
    public bool IsTop;
    public void SetStackRenderOrder(int order, bool isTop) { Order = order; IsTop = isTop; }
}
public class EnemyHealthController
{
    public GameObject gameObject = new();
    public EnemyMoveController Movement = new();
    public float CurrentHP = 100;
}
public static class Program
{
    private static int assertions;
    private static void Check(bool pass, string message) { assertions++; if (!pass) throw new Exception(message); }
    public static void Main()
    {
        var tile = new Vector3Int(0, 0);
        var next = new Vector3Int(1, 0);
        var manager = new TileManager();
        manager.AddPath(0, tile); manager.AddPath(1, next); manager.AddPath(2, tile);
        var enemies = Enumerable.Range(0, 12).Select(_ => new EnemyHealthController()).ToArray();
        int changes = 0;
        manager.EnemyCountChanged += () => changes++;
        foreach (var enemy in enemies) Check(manager.SetEnemyAtPathIndex(enemy, 0, out _), "More than nine enemies must be admitted");
        Check(manager.Label(tile).text == "x12", "Display twelve occupants");
        Check(enemies[0].Movement.IsTop && !enemies[1].Movement.IsTop, "First arrival stays on top");
        Check(enemies.Zip(enemies.Skip(1)).All(pair => pair.First.Movement.Order > pair.Second.Movement.Order), "Render order follows arrival");
        manager.SetEnemyAtPathIndex(enemies[0], 0, out var previous);
        Check(previous == 0 && changes == 12 && manager.Label(tile).text == "x12", "Repeated registration must not duplicate, reorder or notify");
        manager.SetEnemyAtPathIndex(enemies[0], 1, out previous);
        Check(previous == 0 && manager.Label(tile).text == "x11" && manager.Label(next).text == "x1", "Movement updates both counts");
        Check(enemies[1].Movement.IsTop && enemies[0].Movement.IsTop, "Departure promotes next arrival");
        manager.SetEnemyAtPathIndex(enemies[0], 0, out _);
        Check(manager.GetEnemyOccupantsAtPathCell(tile).Last() == enemies[0], "Returning enemy joins behind current occupants");
        manager.RemoveEnemyFromPath(enemies[1], out previous);
        Check(previous == 0 && enemies[2].Movement.IsTop && manager.Label(tile).text == "x11", "Removal promotes and decrements");
        Check(!manager.RemoveEnemyFromPath(enemies[1], out _), "Pool double removal is harmless");
        manager.SetEnemyAtPathIndex(enemies[1], 2, out _);
        Check(manager.Label(tile).text == "x12" && manager.GetEnemyOccupantsAtPathCell(tile).Last() == enemies[1], "Pool reuse and duplicate path visits share physical tile count");
        manager.SetEnemyAtPathIndex(enemies[2], 2, out _);
        Check(manager.GetEnemyOccupantsAtPathCell(tile).First() == enemies[2] && enemies[2].Movement.IsTop, "Same physical cell keeps original arrival order");
        Check(!manager.SetEnemyAtPathIndex(enemies[2], 99, out _) && manager.Label(tile).text == "x12", "Invalid index preserves occupancy");
        Check(manager.GetLivingEnemiesAtPathCell(tile).Count == 12 && manager.GetAllLivingEnemies().Count == 12, "All overlapped enemies remain targetable");
        foreach (var enemy in enemies) manager.RemoveEnemyFromPath(enemy, out _);
        Check(!manager.Label(tile).gameObject.activeInHierarchy && !manager.Label(next).gameObject.activeInHierarchy, "Empty tiles hide labels");
        Check(manager.GetAllEnemiesOnPath().Count == 0, "No stale pool entries");
        // Index enumeration must not override actual arrival order at a repeated physical cell.
        manager.SetEnemyAtPathIndex(enemies[0], 2, out _);
        manager.SetEnemyAtPathIndex(enemies[1], 0, out _);
        Check(manager.GetEnemyOccupantsAtPathCell(tile).First() == enemies[0], "Arrival wins over path-index ordering");
        enemies[0].gameObject.SetActive(false);
        manager.RemoveEnemyFromPath(enemies[0], out _);
        Check(manager.Label(tile).text == "x1" && enemies[1].Movement.IsTop, "Disable/removal promotes the survivor");
        Console.WriteLine($"PASS: {assertions} occupancy, arrival-order and count-label assertions (display stubs; no Unity execution).");
    }
}
