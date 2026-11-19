using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 전장 공용 누적값·시너지 효과·보스 전체 효과를 왼쪽 상단에 표시
    [DisallowMultipleComponent]
    public sealed class GlobalEffectsPanel : MonoBehaviour
    {
        private sealed class Row
        {
            public RectTransform Rect;
            public TextMeshProUGUI Title, Value, Hint;
            public Image Fill;
            public Image Icon;
            public TextMeshProUGUI Symbol;
            public string Detail;
        }
        private readonly List<Row> m_rows = new List<Row>();
        private RectTransform m_root, m_viewport, m_content;
        private TMP_FontAsset m_font;
        private float m_next;
        private int m_used;
        private static readonly Color Gold = new Color32(227, 197, 126, 255), Purple = new Color32(193, 156, 231, 255), Red = new Color32(249, 138, 131, 255), Blue = new Color32(122, 204, 226, 255);
        public RectTransform PanelRect => m_root;
        public string StateText
        {
            get
            {
                var text = new StringBuilder();
                foreach (Row row in m_rows) if (row.Rect.gameObject.activeSelf) text.AppendLine(row.Title.text + ": " + row.Value.text + " · " + row.Hint.text);
                return text.ToString();
            }
        }
        private void Start()
        {
            if (SimulationRunner.SuppressCosmetics || SimulationRunner.IsOn) { enabled = false; return; }
            Build(); Refresh();
        }
        private void Update()
        {
            if (m_root == null || Time.unscaledTime < m_next) return;
            m_next = Time.unscaledTime + .15f; Refresh();
        }
        public void Refresh()
        {
            GameSession session = GameSession.Instance; if (session == null || m_root == null) return;
            EmblemSharedState shared = session.Shared; m_used = 0;
            Put("초월", shared.IsTranscendWave ? "발동 중" : $"{shared.TranscendCount:0.#} / {EmblemSharedState.TranscendGoal:0}",
                shared.IsTranscendQueued ? "다음 웨이브 초월 예약" : shared.IsTranscendWave ? "현재 웨이브의 빛 효과 강화" : "목표 도달 시 다음 웨이브 발동", Gold,
                shared.IsTranscendWave ? 1 : shared.TranscendCount / EmblemSharedState.TranscendGoal,
                "빛 계열의 공용 초월 카운트입니다. 목표에 도달하면 다음 웨이브가 초월 상태가 됩니다.", TowerEmblem.Light);
            float next = (Mathf.Floor(shared.DarknessCount / 100) + 1) * 100;
            Put("침식", Mathf.Floor(shared.DarknessCount).ToString("N0"), shared.DarknessCount >= 1000 ? "공유 누적 · 1,000 단계 도달" : $"공유 누적 · 다음 단계 {next:N0}", Purple,
                shared.DarknessCount >= 1000 ? 1 : shared.DarknessCount % 100 / 100,
                "어둠 계열이 공유하는 침식 카운트입니다. 침식 버프의 단계와 어둠 스킬 피해에 사용됩니다.", TowerEmblem.Darkness);
            if (shared.GoldDoubleTurns > 0) Put("골드 2배", $"{shared.GoldDoubleTurns}턴", "프레이야 · 골드 보상 증가", Gold, 0, "프레이야의 골드 2배 효과입니다. 표시된 적 턴 동안 유지됩니다.");
            if (shared.LimitShields > 0) Put("한도 보호", $"{shared.LimitShields}회", "브륀힐드 · 한도 초과 패배 방지", Blue, 0, "적 수 한도 초과로 패배할 때 보호 횟수를 1 소모해 막습니다.");
            if (shared.StopTiles.Count > 0)
            {
                int longest = 0; foreach (int turns in shared.StopTiles.Values) longest = Mathf.Max(longest, turns);
                Put("멈춤 타일", $"{shared.StopTiles.Count}칸", $"뇨르드 · 최대 {longest}턴 남음", Blue, 0, "뇨르드가 만든 멈춤 칸입니다. 각 칸의 남은 턴은 개별적으로 감소합니다.");
            }
            int limit = 0; foreach (int amount in session.LimitModifiers.Values) limit += amount;
            if (limit != 0) Put("적 수 한도", limit.ToString("+0;-0;0"), $"기본 {session.BaseLimit} → 현재 {session.CurrentLimit}", limit > 0 ? Blue : Red, 0, "현재 적용 중인 한도 보정의 합계입니다. 개별 타워의 일시 효과는 만료되면 해제됩니다.");
            RefreshBossEffects(session);
            for (int i = m_used; i < m_rows.Count; i++) m_rows[i].Rect.gameObject.SetActive(false);
            float height = m_used * 46;
            m_content.sizeDelta = new Vector2(216, height);
            m_viewport.sizeDelta = new Vector2(216, Mathf.Min(216, height));
            m_root.sizeDelta = new Vector2(232, Mathf.Min(216, height) + 36);
        }
        private void RefreshBossEffects(GameSession session)
        {
            int wolves = 0, wolfMin = 5, wolfMax = 0, fires = 0, fireCount = 0; float fireMin = 1, fireMax = 0;
            BoardManager board = BoardManager.Instance;
            if (board != null)
            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i); if (tower == null || tower.State == null) continue;
                TowerBossEffects effects = tower.State.BossEffects;
                if (effects.ActionPenalty > 0) { wolves++; wolfMin = Mathf.Min(wolfMin, effects.ActionPenalty); wolfMax = Mathf.Max(wolfMax, effects.ActionPenalty); }
                if (effects.PowerReduction > 0 || effects.AttackCountPenalty > 0) { fires++; fireMin = Mathf.Min(fireMin, effects.PowerReduction); fireMax = Mathf.Max(fireMax, effects.PowerReduction); fireCount = Mathf.Max(fireCount, effects.AttackCountPenalty); }
            }
            if (wolves > 0) Put("펜리르", wolfMin == wolfMax ? $"+{wolfMax} 행동" : $"+{wolfMin}~{wolfMax}", $"{wolves}기 영향 · 개별 중첩·만료", Red, 0, "현재 전장 타워에 남아 있는 펜리르 효과입니다. 새 타워는 영향을 받지 않아 타워마다 중첩이 다를 수 있습니다.");
            if (fires > 0) Put("수르트", Mathf.Approximately(fireMin, fireMax) ? $"-{fireMax * 100:0}%" : $"-{fireMin * 100:0}~{fireMax * 100:0}%", $"{fires}기 · 공격 횟수 최대 -{fireCount}", Red, 0, "수르트의 공격력 감소와 공격 횟수 감소입니다. 각 타워의 실제 남은 효과를 집계합니다.");
            EnemyManager enemies = EnemyManager.Instance; if (enemies == null) return;
            int blessed = 0, turns = 0, death = -1;
            foreach (EnemyController enemy in enemies.Registry.Alive)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (enemy.Status?.Find(BossSpiritBlessing.Source) is BossSpiritBlessing blessing) { blessed++; turns = Mathf.Max(turns, blessing.RemainingActions); }
                if (enemy.BossMechanic == BossMechanicKind.Hel) death = Mathf.Max(death, enemy.DeathCount);
            }
            if (blessed > 0) Put("적 정령 가호", "+50%", $"{blessed}마리 · 최대 {turns}턴 남음", Red, 0, "적의 최대 체력·방어력·저항력이 50% 증가한 상태입니다. 남은 턴은 적마다 따로 계산합니다.");
            if (death >= 0) Put("죽음 카운트", $"{death} / {session.HelDeathTarget}", "목표 도달 시 아군 전장 타워 파괴", Red, (float)death / session.HelDeathTarget, "헬의 죽음 카운트입니다. 목표에 도달하면 전장 타워가 모두 파괴되고 카운트가 초기화됩니다.");
        }
        private void Put(string title, string value, string hint, Color color, float progress, string detail, TowerEmblem emblem = TowerEmblem.None)
        {
            if (m_used == m_rows.Count) m_rows.Add(CreateRow(emblem, color));
            Row row = m_rows[m_used]; row.Rect.gameObject.SetActive(true); row.Rect.anchoredPosition = new Vector2(0, -m_used * 46); m_used++;
            if (row.Title.text != title) row.Title.text = title;
            if (row.Value.text != value) row.Value.text = value;
            if (row.Hint.text != hint) row.Hint.text = hint;
            row.Title.color = row.Value.color = row.Fill.color = color; row.Fill.fillAmount = Mathf.Clamp01(progress); row.Detail = detail;
            row.Icon.color = color; if (row.Symbol != null) row.Symbol.color = color;
        }
        private void Build()
        {
            m_font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-Black SDF"); if (m_font == null) m_font = TMP_Settings.defaultFontAsset;
            m_root = Rect(transform, "GlobalEffects", 16, -14, 232, 128);
            Image background = m_root.gameObject.AddComponent<Image>(); background.color = new Color32(19, 29, 49, 242); background.raycastTarget = false;
            Text(m_root, "Title", 10, -4, 112, 24, 14).text = "전장 효과";
            TextMeshProUGUI help = Text(m_root, "Help", 124, -6, 96, 20, 10); help.text = "휠 / 클릭으로 확인"; help.color = new Color32(157, 177, 203, 255); help.alignment = TextAlignmentOptions.MidlineRight;
            m_viewport = Rect(m_root, "Viewport", 8, -28, 216, 92); m_viewport.gameObject.AddComponent<RectMask2D>();
            m_content = Rect(m_viewport, "Content", 0, 0, 216, 92);
            ScrollRect scroll = m_root.gameObject.AddComponent<ScrollRect>(); scroll.viewport = m_viewport; scroll.content = m_content; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 24;
        }
        private Row CreateRow(TowerEmblem emblem, Color color)
        {
            var row = new Row { Rect = Rect(m_content, "Effect_" + m_rows.Count, 0, 0, 216, 44) };
            Image background = row.Rect.gameObject.AddComponent<Image>(); background.color = new Color32(41, 53, 75, 225);
            Button button = row.Rect.gameObject.AddComponent<Button>(); button.targetGraphic = background; button.onClick.AddListener(() => GameNotice.Info(row.Detail));
            RectTransform icon = Rect(row.Rect, "Icon", 7, -8, 26, 26); Image image = icon.gameObject.AddComponent<Image>(); image.raycastTarget = false; image.preserveAspect = true;
            image.sprite = TowerIcons.Instance != null && emblem != TowerEmblem.None ? TowerIcons.Instance.GetEmblem(emblem) : null; image.color = color; image.enabled = image.sprite != null;
            row.Icon = image;
            if (image.sprite == null) { row.Symbol = Text(row.Rect, "Symbol", 7, -5, 26, 30, 21); row.Symbol.text = "◆"; row.Symbol.color = color; row.Symbol.alignment = TextAlignmentOptions.Center; }
            row.Title = Text(row.Rect, "Name", 40, -3, 94, 22, 15); row.Value = Text(row.Rect, "Value", 132, -3, 78, 22, 15); row.Value.alignment = TextAlignmentOptions.MidlineRight;
            row.Hint = Text(row.Rect, "Hint", 40, -25, 170, 16, 10); row.Hint.color = new Color32(168, 187, 209, 255);
            RectTransform track = Rect(row.Rect, "Progress", 40, -23, 170, 2); Image baseImage = track.gameObject.AddComponent<Image>(); baseImage.color = new Color32(56, 70, 93, 255); baseImage.raycastTarget = false;
            RectTransform fill = Rect(track, "Fill", 0, 0, 170, 2); row.Fill = fill.gameObject.AddComponent<Image>(); row.Fill.sprite = BoardOverlayStyle.Quad; row.Fill.type = Image.Type.Filled; row.Fill.fillMethod = Image.FillMethod.Horizontal; row.Fill.raycastTarget = false;
            return row;
        }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private TextMeshProUGUI Text(Transform parent, string name, float x, float y, float width, float height, int size)
        {
            TextMeshProUGUI text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>(); text.font = m_font; text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = size - 2; text.fontSizeMax = size;
            text.color = new Color32(237, 241, 249, 255); text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false; return text;
        }
    }
}
