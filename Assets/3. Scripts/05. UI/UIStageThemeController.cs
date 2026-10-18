using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// UI에 적용할 스테이지 테마입니다.
/// </summary>
public enum StageRealm
{
    Asgard,
    Alfheim,
    Vanaheim,
    Midgard,
    Jotunheim,
    Nidavellir,
    Niflheim,
    Muspelheim,
    Hel,
    Ragnarok
}

/// <summary>
/// 한 스테이지 테마의 배경 및 프레임 색상 정보입니다.
/// </summary>
[Serializable]
public struct RealmThemeColor
{
    public StageRealm realm;
    public string displayName;

    [Tooltip("이름에 _bg가 포함된 UI 이미지에 적용할 배경 색상")]
    public Color bgColor;

    [Tooltip("이름에 _frame이 포함된 UI 이미지에 적용할 프레임 색상")]
    public Color frameColor;

    [Tooltip("공통 아틀라스 B 레이어의 강조색. 알파가 0이면 기본 강조색을 사용합니다.")]
    public Color colorB;
    [Tooltip("공통 아틀라스 C 레이어의 짙은 배경색. 알파가 0이면 기본 테마 색을 사용합니다.")]
    public Color colorC;

    public RealmThemeColor(StageRealm realm, string displayName, Color bgColor, Color frameColor)
    {
        this.realm = realm;
        this.displayName = displayName;
        this.bgColor = bgColor;
        this.frameColor = frameColor;
        colorB = frameColor;
        colorC = default;
    }
}

/// <summary>
/// 각 스테이지 렐름에 적용할 전용 UI 스프라이트 세트입니다.
/// </summary>
[Serializable]
public class RealmRatioSprite
{
    public string key;
    public Sprite sprite;
}

[Serializable]
public class RealmThemeSprites
{
    public StageRealm realm;
    public string displayName;

    [Header("메인 정보창 (우측 패널)")]
    [Tooltip("우측 정보창 외곽 프레임 (BackGround_Frame)")]
    public Sprite panelMainFrame;
    [Tooltip("우측 정보창 배경 (BackGround_BG)")]
    public Sprite panelMainBG;

    [Header("주요 액션 버튼 (High: 턴 종료, 보스 소환)")]
    [Tooltip("턴 종료 / 보스 소환 버튼 프레임")]
    public Sprite btnHighFrame;
    [Tooltip("턴 종료 / 보스 소환 버튼 배경")]
    public Sprite btnHighBG;

    [Header("타워 조작 카드 (Middle: 생성, 컬러강화, 티어강화 등)")]
    [Tooltip("타워 조작 카드 프레임")]
    public Sprite btnMiddleFrame;
    [Tooltip("타워 조작 카드 배경")]
    public Sprite btnMiddleBG;

    [Header("기능 / 스탯 슬롯 (Low)")]
    [Tooltip("기능/옵션/스탯 슬롯 프레임")]
    public Sprite btnLowFrame;
    [Tooltip("기능/옵션/스탯 슬롯 배경")]
    public Sprite btnLowBG;

    [Header("128px 기준 비율별 UI")]
    public List<RealmRatioSprite> ratioSprites = new List<RealmRatioSprite>();
}

/// <summary>
/// Canvas 하위 UI 이미지의 이름을 기준으로 스테이지 테마 색상 및 전용 스프라이트를 적용합니다.
/// 이름에 _bg가 있으면 배경색, _frame이 있으면 프레임색을 적용합니다.
/// 전용 스프라이트가 설정된 경우 해당 스프라이트를 교체하고 오리지널 색감을 보존합니다.
/// TextMeshPro와 Legacy Text는 텍스트 가독성을 위해 변경하지 않습니다.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class UIStageThemeController : MonoBehaviour
{
    [Serializable]
    public class ImageColorOverride
    {
        public Image image;
        public Color color;
    }

    public static UIStageThemeController Instance { get; private set; }

    // CTxt_는 Custom Text의 약자입니다. 디자이너가 지정한 색을 유지하므로 자동 대비 처리에서 제외합니다.
    private const string CustomTextPrefix = "CTxt_";

    [Header("스테이지 테마")]
    [Tooltip("적용할 스테이지 테마입니다. 변경하면 Canvas 하위 UI 색상이 즉시 갱신됩니다.")]
    [SerializeField] private StageRealm currentRealm = StageRealm.Midgard;

    [Header("대상 Canvas")]
    [Tooltip("색상을 변경할 Canvas입니다. 비워두면 이 오브젝트 또는 하위 Canvas를 자동으로 찾습니다.")]
    [SerializeField] private Canvas targetCanvas;

    [Tooltip("공통 무채색 UI 아틀라스에 A/B/C/Frame 테마 색상을 적용합니다.")]
    [SerializeField] private bool useSharedAtlas = true;

    [Tooltip("Image의 Color를 직접 편집한 UI입니다. 자동 테마 적용과 저장 시 이 색상을 유지합니다.")]
    [SerializeField] private List<ImageColorOverride> imageColorOverrides = new List<ImageColorOverride>();

    private readonly Dictionary<Image, Color> lastAppliedColors = new Dictionary<Image, Color>();
    public int ManualColorCount => imageColorOverrides.Count;

    [Header("이름 매칭 규칙")]
    [Tooltip("이 문자열이 이름에 포함된 Graphic은 배경으로 처리합니다. 대소문자를 구분하지 않습니다.")]
    [SerializeField] private string bgKeyword = "_bg";

    [Tooltip("이 문자열이 이름에 포함된 Graphic은 프레임으로 처리합니다. 대소문자를 구분하지 않습니다.")]
    [SerializeField] private string frameKeyword = "_frame";

    [Header("스테이지별 전용 스프라이트 세트 (Atlas/Theme Sprites)")]
    [Tooltip("체크하면 각 스테이지 렐름 전용 스프라이트(아스가르드 황금/네이비 등)를 적용하고 오리지널 재질 색상을 유지합니다.")]
    [SerializeField] private bool useThemeSprites = true;

    [Tooltip("스테이지별 전용 스프라이트 목록입니다.")]
    [SerializeField] private List<RealmThemeSprites> realmSprites = new List<RealmThemeSprites>();

    [Header("색상 적용 옵션")]
    [Tooltip("기존 UI 이미지의 알파값을 유지합니다. 반투명 연출을 보존할 때 켜 둡니다.")]
    [SerializeField] private bool preserveOriginalAlpha = true;

    [Tooltip("기본 테마 대신 Inspector에서 직접 설정한 색상 목록을 사용합니다.")]
    [SerializeField] private bool useCustomColors;

    [Tooltip("useCustomColors가 켜졌을 때 사용할 스테이지별 색상 목록입니다.")]
    [SerializeField] private List<RealmThemeColor> customThemes = new List<RealmThemeColor>();

    [Header("TowerInfoPanel 강조")]
    [Tooltip("테마 배경색을 기준으로 명도를 보정할 TowerInfoPanel의 BackGround_BG 이미지를 직접 연결합니다.")]
    [SerializeField] private Image towerInfoBackground;

    [Tooltip("테마 배경이 밝으면 이 비율만큼 더 밝게, 어두우면 더 어둡게 보정합니다. 0이면 테마 배경색과 같습니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float towerInfoBackgroundBrightness = 0.35f;

    [Header("텍스트 대비")]
    [Tooltip("현재 스테이지 테마의 배경색 명도를 기준으로 모든 텍스트를 검정 또는 흰색으로 통일합니다.")]
    [SerializeField] private bool autoTextContrast = true;

    [Tooltip("이 값보다 밝은 배경은 검정 텍스트, 어두운 배경은 흰색 텍스트를 사용합니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float textContrastThreshold = 0.55f;

    private static readonly Dictionary<StageRealm, (Color bg, Color frame, string name)> s_defaultPresets =
        new Dictionary<StageRealm, (Color, Color, string)>
        {
            { StageRealm.Asgard,     (HexToColor("FFFCEC"), HexToColor("ECBB41"), "아스가르드 (Asgard)") },
            { StageRealm.Alfheim,    (HexToColor("F4F8EB"), HexToColor("A56E37"), "알프헤임 (Alfheim)") },
            { StageRealm.Vanaheim,   (HexToColor("EFF7F0"), HexToColor("A4BE8F"), "바나헤임 (Vanaheim)") },
            { StageRealm.Midgard,    (HexToColor("EDF8F5"), HexToColor("8FB6C8"), "미드가르드 (Midgard)") },
            { StageRealm.Jotunheim,  (HexToColor("F3F5F5"), HexToColor("9DA6B0"), "요툰헤임 (Jotunheim)") },
            { StageRealm.Nidavellir, (HexToColor("F4EFF9"), HexToColor("AB71D9"), "니다벨리르 (Nidavellir)") },
            { StageRealm.Niflheim,   (HexToColor("F7FBFF"), HexToColor("AEC2D4"), "니플헤임 (Niflheim)") },
            { StageRealm.Muspelheim, (HexToColor("FFF5EC"), HexToColor("D9A996"), "무스펠헤임 (Muspelheim)") },
            { StageRealm.Hel,        (HexToColor("F1F8F4"), HexToColor("43BEAE"), "헬 (Hel)") },
            { StageRealm.Ragnarok,   (HexToColor("DCD8CA"), HexToColor("B6A681"), "라그나로크 (Ragnarok)") }
        };

    public StageRealm CurrentRealm
    {
        get => currentRealm;
        set
        {
            if (currentRealm == value) return;

            currentRealm = value;
            ApplyTheme();
        }
    }

    private void Reset()
    {
        EnsureCanvasReference();
#if UNITY_EDITOR
        EnsureDefaultThemeSprites();
#endif
        ApplyTheme();
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        EnsureCanvasReference();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureDefaultThemeSprites();
        }
#endif
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureDefaultThemeSprites();
        }
#endif
        ApplyTheme();
    }

#if UNITY_EDITOR
    public void EnsureDefaultThemeSprites()
    {
        if (useSharedAtlas) return;
        if (realmSprites == null) realmSprites = new List<RealmThemeSprites>();
        string[] shapes = { "1x1", "1x2", "1x3", "1x4", "2x3", "2x1", "3x1", "4x1", "3x2", "1x1_Sliced" };
        string[] layers = { "A", "B", "C", "Frame" };
        foreach (StageRealm realm in Enum.GetValues(typeof(StageRealm)))
        {
            RealmThemeSprites sprites = realmSprites.Find(r => r.realm == realm);
            if (sprites == null)
            {
                sprites = new RealmThemeSprites { realm = realm, displayName = GetThemeColors(realm).name };
                realmSprites.Add(sprites);
            }
            if (sprites.ratioSprites == null) sprites.ratioSprites = new List<RealmRatioSprite>();
            foreach (string shape in shapes)
            foreach (string layer in layers)
            {
                string key = layer + "_" + shape;
                RealmRatioSprite entry = sprites.ratioSprites.Find(item => item != null && item.key == key);
                if (entry != null && entry.sprite != null) continue;
                string folder = shape.EndsWith("_Sliced", StringComparison.Ordinal) ? "Sliced" : "Simple";
                string path = $"Assets/4. Asset/5. UI/Themes/{realm}/{folder}/{realm}_{key}.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;
                if (entry == null)
                {
                    entry = new RealmRatioSprite { key = key };
                    sprites.ratioSprites.Add(entry);
                }
                entry.sprite = sprite;
            }
        }
    }
#endif

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Inspector 연결이 비어 있을 때 사용할 Canvas를 자동으로 찾습니다.
    /// </summary>
    public void EnsureCanvasReference()
    {
        if (targetCanvas != null) return;

        targetCanvas = GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }
    }

    /// <summary>
    /// 다른 매니저에서 스테이지 테마를 변경할 때 호출합니다.
    /// </summary>
    public void SetRealm(StageRealm realm)
    {
        CurrentRealm = realm;
    }

    public (Color bg, Color frame, string name) GetCurrentThemeColors()
    {
        return GetThemeColors(currentRealm);
    }

    public (Color bg, Color frame, string name) GetThemeColors(StageRealm realm)
    {
        if (useCustomColors && customThemes != null)
        {
            RealmThemeColor custom = customThemes.Find(theme => theme.realm == realm);
            if (!string.IsNullOrEmpty(custom.displayName))
            {
                return (custom.bgColor, custom.frameColor, custom.displayName);
            }
        }

        if (s_defaultPresets.TryGetValue(realm, out var preset))
        {
            return preset;
        }

        return (Color.white, Color.gray, realm.ToString());
    }

    public RealmThemeSprites GetThemeSprites(StageRealm realm)
    {
        if (useSharedAtlas) return null;
        if (!useThemeSprites || realmSprites == null) return null;
        return realmSprites.Find(s => s.realm == realm);
    }

    public static string GetSharedLayer(Sprite sprite)
    {
        if (sprite == null || !sprite.name.StartsWith("Shared_", StringComparison.Ordinal)) return null;
        int end = sprite.name.IndexOf('_', 7);
        return end > 7 ? sprite.name.Substring(7, end - 7) : null;
    }

    public static string GetImageLayer(Image image)
    {
        if (image == null) return null;
        return image.TryGetComponent<UITileSurface>(out var surface) ? surface.ThemeLayer : GetSharedLayer(image.sprite);
    }

    public void ApplySurfaceColor(Image image, string layer)
    {
        if (image != null) ApplyColor(image, GetLayerColor(layer));
    }

    public Color GetLayerColor(string layer) => GetLayerColor(currentRealm, layer);

    public Color GetLayerColor(StageRealm realm, string layer)
    {
        var theme = GetThemeColors(realm);
        RealmThemeColor custom = useCustomColors && customThemes != null
            ? customThemes.Find(item => item.realm == realm) : default;
        if (layer == "A") return theme.bg;
        if (layer == "B") return !string.IsNullOrEmpty(custom.displayName) && custom.colorB.a > 0f ? custom.colorB :
            realm == StageRealm.Asgard ? GetDefaultLayerColor(realm, "B") : theme.frame;
        if (layer == "C")
        {
            if (!string.IsNullOrEmpty(custom.displayName) && custom.colorC.a > 0f) return custom.colorC;
            return GetDefaultLayerColor(realm, "C");
        }
        return theme.frame;
    }

    /// <summary>지금 이 스테이지에 실제로 쓰이는 색 네 가지(배경, 프레임, B, C)를 한 벌로 돌려줍니다.</summary>
    public RealmThemeColor GetEffectiveThemeColor(StageRealm realm)
    {
        var theme = GetThemeColors(realm);
        return new RealmThemeColor(realm, theme.name, theme.bg, theme.frame)
        {
            colorB = GetLayerColor(realm, "B"),
            colorC = GetLayerColor(realm, "C")
        };
    }

    public bool HasCustomThemeColor(StageRealm realm) =>
        useCustomColors && customThemes != null &&
        customThemes.Exists(item => item.realm == realm && !string.IsNullOrEmpty(item.displayName));

    /// <summary>직접 고른 색을 이 스테이지의 사용자 정의 색으로 저장하고 바로 적용합니다.</summary>
    public void SetCustomThemeColor(RealmThemeColor themeColor)
    {
        if (customThemes == null) customThemes = new List<RealmThemeColor>();

        // 이름이 비어 있으면 GetThemeColors가 이 항목을 없는 것으로 봅니다.
        if (string.IsNullOrEmpty(themeColor.displayName))
        {
            themeColor.displayName = s_defaultPresets.TryGetValue(themeColor.realm, out var preset)
                ? preset.name : themeColor.realm.ToString();
        }

        // 알파가 0이면 B와 C는 기본색으로 읽히므로 1로 고정합니다.
        themeColor.bgColor.a = 1f;
        themeColor.frameColor.a = 1f;
        themeColor.colorB.a = 1f;
        themeColor.colorC.a = 1f;

        int index = customThemes.FindIndex(item => item.realm == themeColor.realm);
        if (index >= 0) customThemes[index] = themeColor;
        else customThemes.Add(themeColor);

        useCustomColors = true;
        ApplyTheme();
    }

    /// <summary>이 스테이지의 사용자 정의 색을 지우고 기본색으로 되돌립니다.</summary>
    public void ClearCustomThemeColor(StageRealm realm)
    {
        if (customThemes == null) return;

        customThemes.RemoveAll(item => item.realm == realm);
        ApplyTheme();
    }

    public static Color GetDefaultLayerColor(StageRealm realm, string layer)
    {
        if (!s_defaultPresets.TryGetValue(realm, out var preset)) return Color.white;
        if (layer == "A") return preset.bg;
        if (layer == "B") return realm == StageRealm.Asgard ? HexToColor("FFDF7D") : preset.frame;
        if (layer == "C")
        {
            string[] dark = { "263855", "6B4933", "254B35", "15364F", "35383C", "502F6D", "364B6A", "88421F", "101417", "424B58" };
            return HexToColor(dark[(int)realm]);
        }
        return preset.frame;
    }

    public bool ManagesImage(Image image)
    {
        EnsureCanvasReference();
        return image != null && targetCanvas != null && image.transform.IsChildOf(targetCanvas.transform) &&
               (!image.TryGetComponent<UITileSurface>(out var surface) || surface.UseStageTheme) &&
               (GetImageLayer(image) != null || IsBgName(image.name) || IsFrameName(image.name));
    }

    public bool IsAutomaticColor(Image image) => lastAppliedColors.TryGetValue(image, out Color color) && color == image.color;
    public bool HasImageColorOverride(Image image) => imageColorOverrides.Exists(item => item.image == image);

    /// <summary>선택한 이미지의 수동 색을 해제하고 레이어 테마색을 적용하며 알파는 유지합니다.</summary>
    public void ApplyImageThemeColor(Image image)
    {
        string layer = GetImageLayer(image);
        if (layer == null || !ManagesImage(image)) return;
        imageColorOverrides.RemoveAll(item => item.image == image);
        Color color = GetLayerColor(layer);
        color.a = image.color.a;
        ApplyColor(image, color);
    }

    public void RememberImageColor(Image image)
    {
        if (!ManagesImage(image)) return;
        ImageColorOverride entry = imageColorOverrides.Find(item => item.image == image);
        if (entry == null)
        {
            entry = new ImageColorOverride { image = image };
            imageColorOverrides.Add(entry);
        }
        entry.color = image.color;
    }

    public void ClearManualColors()
    {
        imageColorOverrides.Clear();
        ApplyTheme();
    }

    private Sprite GetThemeSpriteForObject(string objectName, RealmThemeSprites sprites)
    {
        if (sprites == null) return null;

        // 1. 메인 패널 (우측 정보창)
        if (objectName.IndexOf("BackGround_Frame", StringComparison.OrdinalIgnoreCase) >= 0)
            return sprites.panelMainFrame;
        if (objectName.IndexOf("BackGround_BG", StringComparison.OrdinalIgnoreCase) >= 0)
            return sprites.panelMainBG;

        // 2. High Tier 버튼 (턴 종료, 보스 소환)
        if (objectName.IndexOf("TurnEnd", StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf("SpawnMiddleBoss", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (IsFrameName(objectName)) return sprites.btnHighFrame;
            if (IsBgName(objectName)) return sprites.btnHighBG;
        }

        // 3. Middle Tier 버튼 / 카드 (타워 생성, 컬러강화, 티어강화, 합성, 판매 등)
        if (objectName.IndexOf("CreateTower", StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf("ColorUp", StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf("TierUp", StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf("Combine", StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf("SellTower", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (IsFrameName(objectName)) return sprites.btnMiddleFrame;
            if (IsBgName(objectName)) return sprites.btnMiddleBG;
        }

        // 4. Low Tier (기능/옵션/스탯 슬롯)
        if (objectName.IndexOf("Option", StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf("GameSpeed", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (IsFrameName(objectName) && sprites.btnLowFrame != null) return sprites.btnLowFrame;
            if (IsBgName(objectName) && sprites.btnLowBG != null) return sprites.btnLowBG;
        }

        return null;
    }

    private Sprite GetThemeSpriteForImage(Image image, RealmThemeSprites sprites)
    {
        // 프리팹에 지정된 A/B/C/Frame과 비율을 유지한 채 월드만 교체합니다.
        if (image.sprite != null && sprites.ratioSprites != null)
        {
            string spriteName = image.sprite.name;
            int separator = spriteName.IndexOf('_');
            if (separator >= 0)
            {
                string key = spriteName.Substring(separator + 1);
                RealmRatioSprite entry = sprites.ratioSprites.Find(item => item != null && item.key == key);
                if (entry != null && entry.sprite != null) return entry.sprite;
            }
        }
        return GetThemeSpriteForObject(image.gameObject.name, sprites);
    }

    /// <summary>
    /// Canvas 하위의 _bg, _frame Graphic에 현재 테마를 적용합니다.
    /// 전용 스프라이트가 등록된 경우 스프라이트를 교체하고 원본 색감을 보존합니다.
    /// Inspector에서 직접 연결한 TowerInfoPanel 배경만 일반 _bg보다 한 단계 밝거나 어둡게 보정합니다.
    /// </summary>
    [ContextMenu("테마 색상/스프라이트 적용")]
    public void ApplyTheme()
    {
        EnsureCanvasReference();
        if (targetCanvas == null)
        {
            Debug.LogWarning("[UIStageThemeController] 적용할 Canvas를 찾지 못했습니다.");
            return;
        }

        var theme = GetCurrentThemeColors();
        var spriteTheme = GetThemeSprites(currentRealm);
        Graphic[] graphics = targetCanvas.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic is TMP_Text || graphic is Text) continue;

            string objectName = graphic.gameObject.name;
            Image img = graphic as Image;

            if (img != null && img.TryGetComponent<UITileSurface>(out var surface))
            {
                surface.RefreshGeometry();
                if (surface.UseStageTheme) ApplyColor(graphic, GetLayerColor(surface.ThemeLayer));
                continue;
            }

            if (useSharedAtlas && img != null && GetImageLayer(img) is string layer)
            {
                if (img.type != Image.Type.Filled)
                {
                    Image.Type type = img.sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                    if (img.type != type)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying) Undo.RecordObject(img, "UI 표시 방식 적용");
#endif
                        img.type = type;
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            EditorUtility.SetDirty(img);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(img);
                        }
#endif
                    }
                }
                ApplyColor(graphic, GetLayerColor(layer));
                continue;
            }

            // 1) 전용 테마 스프라이트 스왑
            if (img != null && spriteTheme != null)
            {
                Sprite customSprite = GetThemeSpriteForImage(img, spriteTheme);
                if (customSprite != null)
                {
                    img.sprite = customSprite;
                    if (img.type != Image.Type.Filled)
                        img.type = customSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                    ApplyColor(graphic, Color.white);
                    continue;
                }
            }

            // 2) 기본 틴팅 로직
            if (IsFrameName(objectName))
            {
                ApplyColor(graphic, theme.frame);
            }
            else if (IsBgName(objectName))
            {
                ApplyColor(graphic, theme.bg);
            }
        }

        if (towerInfoBackground != null && (spriteTheme == null || spriteTheme.panelMainBG == null))
        {
            ApplyColor(towerInfoBackground, GetTowerInfoBackgroundColor(theme.bg));
        }

        if (autoTextContrast)
        {
            ApplyTextContrast();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(targetCanvas.gameObject);
        }
#endif
    }

    private bool IsBgName(string objectName)
    {
        return !string.IsNullOrEmpty(objectName) &&
               objectName.IndexOf(bgKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsFrameName(string objectName)
    {
        return !string.IsNullOrEmpty(objectName) &&
               objectName.IndexOf(frameKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private Color GetTowerInfoBackgroundColor(Color themeBackgroundColor)
    {
        Color extremeColor = GetLuminance(themeBackgroundColor) >= textContrastThreshold
            ? Color.white
            : Color.black;
        return Color.Lerp(themeBackgroundColor, extremeColor, towerInfoBackgroundBrightness);
    }

    /// <summary>
    /// 현재 스테이지의 기본 배경색을 기준으로 모든 TMP/Legacy 텍스트에
    /// 읽기 좋은 흰색 또는 검정색을 통일하여 적용합니다.
    /// 각 텍스트 주변의 Image 계층은 참고하지 않으므로, 배경과 텍스트가 형제여도 결과가 일관됩니다.
    /// </summary>
    private void ApplyTextContrast()
    {
        Color themeBackgroundColor = GetCurrentThemeColors().bg;
        float luminance = GetLuminance(themeBackgroundColor);
        Color contrastColor = luminance >= textContrastThreshold ? Color.black : Color.white;

        foreach (TMP_Text text in targetCanvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (IsCustomText(text)) continue;
            ApplyTextContrast(text, contrastColor);
        }

        foreach (Text text in targetCanvas.GetComponentsInChildren<Text>(true))
        {
            if (IsCustomText(text)) continue;
            ApplyTextContrast(text, contrastColor);
        }
    }

    private static bool IsCustomText(Graphic textGraphic)
    {
        return textGraphic != null &&
               textGraphic.gameObject.name.StartsWith(CustomTextPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyTextContrast(Graphic textGraphic, Color contrastColor)
    {
        contrastColor.a = textGraphic.color.a;
        textGraphic.color = contrastColor;
    }

    private static float GetLuminance(Color color)
    {
        return color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
    }

    private void ApplyColor(Graphic graphic, Color themeColor)
    {
        Image image = graphic as Image;
        ImageColorOverride manual = image != null ? imageColorOverrides.Find(item => item.image == image) : null;
        Color desired = manual != null ? manual.color : preserveOriginalAlpha
            ? new Color(themeColor.r, themeColor.g, themeColor.b, graphic.color.a) : themeColor;
        if (image != null) lastAppliedColors[image] = desired;
        if (graphic.color == desired) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RecordObject(graphic, "스테이지 UI 테마 적용");
        }
#endif

        graphic.color = desired;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(graphic);
            PrefabUtility.RecordPrefabInstancePropertyModifications(graphic);
        }
#endif
    }

    [ContextMenu("테마 대상 UI 목록 출력")]
    public void LogMatchingElements()
    {
        EnsureCanvasReference();
        if (targetCanvas == null)
        {
            Debug.LogWarning("[UIStageThemeController] 대상 Canvas가 설정되지 않았습니다.");
            return;
        }

        List<string> backgrounds = new List<string>();
        List<string> frames = new List<string>();

        foreach (Graphic graphic in targetCanvas.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic is TMP_Text || graphic is Text) continue;

            if (IsFrameName(graphic.gameObject.name)) frames.Add(graphic.gameObject.name);
            else if (IsBgName(graphic.gameObject.name)) backgrounds.Add(graphic.gameObject.name);
        }

        Debug.Log(
            $"[UIStageThemeController] 테마 대상 UI\n" +
            $"배경 ({backgrounds.Count}개): {string.Join(", ", backgrounds)}\n" +
            $"프레임 ({frames.Count}개): {string.Join(", ", frames)}");
    }

    private static Color HexToColor(string hex)
    {
        string colorCode = hex.StartsWith("#", StringComparison.Ordinal) ? hex : "#" + hex;
        return ColorUtility.TryParseHtmlString(colorCode, out Color color) ? color : Color.white;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyTheme();
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                ApplyTheme();
            }
        };
    }

#endif
}
