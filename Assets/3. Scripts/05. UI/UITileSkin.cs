using UnityEngine;

/// <summary>색과 비율을 복제하지 않고 공유하는 세 가지 UI 원본입니다.</summary>
[CreateAssetMenu(menuName = "UI/타일 공통 스킨", fileName = "UITileSkin")]
public sealed class UITileSkin : ScriptableObject
{
    public Sprite background;
    public Sprite decoratedFrame;
    public Sprite compactFrame;
    [Min(1f)] public float decoratedStrokePixels = 8f;
    [Min(1f)] public float compactStrokePixels = 12f;

    private static UITileSkin cached;
    public static UITileSkin Default => cached != null ? cached : cached = Resources.Load<UITileSkin>("UITileSkin");

    public Sprite GetFrame(UITileFrameStyle style) => style == UITileFrameStyle.Compact ? compactFrame : decoratedFrame;
    public float GetStrokePixels(UITileFrameStyle style) => style == UITileFrameStyle.Compact ? compactStrokePixels : decoratedStrokePixels;
}

public enum UITileFrameStyle { Auto, Decorated, Compact }
public enum UITileColorRole { A, B, C, Frame }
