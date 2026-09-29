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
    Hel
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

    public RealmThemeColor(StageRealm realm, string displayName, Color bgColor, Color frameColor)
    {
        this.realm = realm;
        this.displayName = displayName;
        this.bgColor = bgColor;
        this.frameColor = frameColor;
    }
}

/// <summary>
/// 각 스테이지 렐름에 적용할 전용 UI 스프라이트 세트입니다.
/// </summary>
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
    public static UIStageThemeController Instance { get; private set; }

    // CTxt_는 Custom Text의 약자입니다. 디자이너가 지정한 색을 유지하므로 자동 대비 처리에서 제외합니다.
    private const string CustomTextPrefix = "CTxt_";

    [Header("스테이지 테마")]
    [Tooltip("적용할 스테이지 테마입니다. 변경하면 Canvas 하위 UI 색상이 즉시 갱신됩니다.")]
    [SerializeField] private StageRealm currentRealm = StageRealm.Midgard;

    [Header("대상 Canvas")]
    [Tooltip("색상을 변경할 Canvas입니다. 비워두면 이 오브젝트 또는 하위 Canvas를 자동으로 찾습니다.")]
    [SerializeField] private Canvas targetCanvas;

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
            { StageRealm.Asgard,     (HexToColor("F5F5F7"), HexToColor("E5AC1C"), "아스가르드 (Asgard)") },
            { StageRealm.Alfheim,    (HexToColor("C8A882"), HexToColor("7BC728"), "알프헤임 (Alfheim)") },
            { StageRealm.Vanaheim,   (HexToColor("1B3D23"), HexToColor("2EBF5E"), "바나헤임 (Vanaheim)") },
            { StageRealm.Midgard,    (HexToColor("E6D5B5"), HexToColor("26A3BF"), "미드가르드 (Midgard)") },
            { StageRealm.Jotunheim,  (HexToColor("78808B"), HexToColor("587896"), "요툰헤임 (Jotunheim)") },
            { StageRealm.Nidavellir, (HexToColor("1E1C21"), HexToColor("E6A817"), "니다벨리르 (Nidavellir)") },
            { StageRealm.Niflheim,   (HexToColor("EAF2FA"), HexToColor("1B5299"), "니플헤임 (Niflheim)") },
            { StageRealm.Muspelheim, (HexToColor("8C1B1B"), HexToColor("FF6D0A"), "무스펠헤임 (Muspelheim)") },
            { StageRealm.Hel,        (HexToColor("16191D"), HexToColor("1FE0C3"), "헬 (Hel)") }
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
        EnsureDefaultAsgardSprites();
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
            EnsureDefaultAsgardSprites();
        }
#endif
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EnsureDefaultAsgardSprites();
        }
#endif
        ApplyTheme();
    }

#if UNITY_EDITOR
    public void EnsureDefaultAsgardSprites()
    {
        if (realmSprites == null) realmSprites = new List<RealmThemeSprites>();
        var asgard = realmSprites.Find(r => r.realm == StageRealm.Asgard);
        if (asgard == null)
        {
            asgard = new RealmThemeSprites { realm = StageRealm.Asgard, displayName = "아스가르드 (Asgard)" };
            realmSprites.Add(asgard);
        }

        if (asgard.panelMainFrame == null)
            asgard.panelMainFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Panel_Main_Frame.png");
        if (asgard.panelMainBG == null)
            asgard.panelMainBG = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Panel_Main_BG.png");
        if (asgard.btnHighFrame == null)
            asgard.btnHighFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Btn_High_Frame.png");
        if (asgard.btnHighBG == null)
            asgard.btnHighBG = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Btn_High_BG.png");
        if (asgard.btnMiddleFrame == null)
            asgard.btnMiddleFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Btn_Middle_Frame.png");
        if (asgard.btnMiddleBG == null)
            asgard.btnMiddleBG = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Btn_Middle_BG.png");
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
        if (!useThemeSprites || realmSprites == null) return null;
        return realmSprites.Find(s => s.realm == realm);
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

            // 1) 전용 테마 스프라이트 스왑
            if (img != null && spriteTheme != null)
            {
                Sprite customSprite = GetThemeSpriteForObject(objectName, spriteTheme);
                if (customSprite != null)
                {
                    img.sprite = customSprite;
                    img.type = Image.Type.Sliced;
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
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RecordObject(graphic, "스테이지 UI 테마 적용");
        }
#endif

        if (preserveOriginalAlpha)
        {
            float alpha = graphic.color.a;
            if (alpha <= 0.001f)
            {
                alpha = themeColor.a > 0.001f ? themeColor.a : 1f;
            }

            graphic.color = new Color(themeColor.r, themeColor.g, themeColor.b, alpha);
        }
        else
        {
            graphic.color = themeColor;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(graphic);
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

    [ContextMenu("아스가르드 기본 스프라이트 세트 등록 및 적용")]
    public void LoadDefaultAsgardSprites()
    {
        if (realmSprites == null) realmSprites = new List<RealmThemeSprites>();
        var asgard = realmSprites.Find(r => r.realm == StageRealm.Asgard);
        if (asgard == null)
        {
            asgard = new RealmThemeSprites { realm = StageRealm.Asgard, displayName = "아스가르드 (Asgard)" };
            realmSprites.Add(asgard);
        }

        asgard.panelMainFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Panel_Main_Frame.png");
        asgard.panelMainBG = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Panel_Main_BG.png");
        asgard.btnHighFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Btn_High_Frame.png");
        asgard.btnHighBG = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Btn_High_BG.png");
        asgard.btnMiddleFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Btn_Middle_Frame.png");
        asgard.btnMiddleBG = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_Btn_Middle_BG.png");

        CurrentRealm = StageRealm.Asgard;
        EditorUtility.SetDirty(this);
        ApplyTheme();
        Debug.Log("[UIStageThemeController] 아스가르드 전용 UI 스프라이트가 성공적으로 등록 및 적용되었습니다!");
    }
#endif
}
