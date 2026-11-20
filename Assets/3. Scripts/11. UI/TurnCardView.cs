using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SquareTD
{
    // 플레이어와 적의 현재 턴을 표시하고 글자를 순서대로 튀게 함
    public sealed class TurnCardView : MonoBehaviour
    {
        private const float LetterDelay = .06f;
        private const float RiseDuration = .13f;
        private const float FallDuration = .17f;
        private const float LoopPause = .6f;

        [SerializeField] private TextMeshProUGUI m_turnOrder;
        [SerializeField] private TextMeshProUGUI m_turnValue;
        [SerializeField, Min(0f)] private float m_jumpHeight = 6f;

        private TurnManager m_turns;
        private Tween m_bounce;
        private TMP_MeshInfo[] m_baseMesh;
        private float m_elapsed;

        private void Start()
        {
            m_turns = TurnManager.Instance;
            if (m_turns == null) return;

            m_turns.StateChanged += Refresh;
            m_turns.EnemyTurnStarted += HandleEnemyTurnStarted;
            Refresh(m_turns.Current);
        }

        private void OnEnable()
        {
            m_turnValue.OnPreRenderText += CacheMesh;
            if (m_turns != null) Refresh(m_turns.Current);
        }

        private void OnDisable()
        {
            m_turnValue.OnPreRenderText -= CacheMesh;
            StopBounce();
            m_baseMesh = null;
        }

        private void OnDestroy()
        {
            if (m_turns == null) return;
            m_turns.StateChanged -= Refresh;
            m_turns.EnemyTurnStarted -= HandleEnemyTurnStarted;
        }

        private void HandleEnemyTurnStarted() => Refresh(m_turns.Current);

        private void Refresh(ITurnState state)
        {
            if (!isActiveAndEnabled) return;

            if (state is GameOverState || state is GameClearState)
            {
                StopBounce();
                UpdateBounce(0);
                return;
            }

            bool enemy = state is EnemyTurnState;
            m_turnOrder.text = enemy ? "Enemy" : "Player";
            string value = enemy ? $"{m_turns.CurrentEnemyTurn}턴 진행중..." : "WATING...";
            if (m_bounce != null && m_turnValue.text == value) return;

            StopBounce();
            m_turnValue.text = value;
            m_turnValue.ForceMeshUpdate();
            TMP_TextInfo info = m_turnValue.textInfo;
            int visible = 0;
            for (int i = 0; i < info.characterCount; i++)
                if (info.characterInfo[i].isVisible) visible++;

            float duration = Mathf.Max(0, visible - 1) * LetterDelay + RiseDuration + FallDuration + LoopPause;
            m_bounce = DOVirtual.Float(0, duration, duration, UpdateBounce).SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true);
        }

        private void StopBounce()
        {
            m_bounce?.Kill();
            m_bounce = null;
            m_elapsed = 0;
        }

        private void CacheMesh(TMP_TextInfo text)
        {
            m_baseMesh = text.CopyMeshInfoVertexData();
            ApplyOffsets(text);
        }

        private void UpdateBounce(float elapsed)
        {
            m_elapsed = elapsed;
            if (m_baseMesh == null) return;

            ApplyOffsets(m_turnValue.textInfo);
            m_turnValue.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private void ApplyOffsets(TMP_TextInfo text)
        {
            int visible = 0;
            for (int i = 0; i < text.characterCount; i++)
            {
                TMP_CharacterInfo character = text.characterInfo[i];
                if (!character.isVisible) continue;

                float offset = GetHeight(m_elapsed - visible * LetterDelay);
                visible++;
                Vector3[] original = m_baseMesh[character.materialReferenceIndex].vertices;
                Vector3[] vertices = text.meshInfo[character.materialReferenceIndex].vertices;
                for (int v = character.vertexIndex; v < character.vertexIndex + 4; v++)
                    vertices[v] = original[v] + Vector3.up * offset;
            }
        }

        private float GetHeight(float time)
        {
            if (time <= 0 || time >= RiseDuration + FallDuration) return 0;
            if (time < RiseDuration) return DOVirtual.EasedValue(0, m_jumpHeight, time / RiseDuration, Ease.OutQuad);
            return DOVirtual.EasedValue(m_jumpHeight, 0, (time - RiseDuration) / FallDuration, Ease.InQuad);
        }
    }
}
