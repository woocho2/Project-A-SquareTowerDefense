using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SquareTD
{
    // 발키리(검 버프) 스킬 2 글로리 소드 연출
    [DisallowMultipleComponent]
    public sealed class SwordBuffSkillEffect : MonoBehaviour
    {
        private static readonly Color s_gold = new Color(1f, 0.8f, 0.38f);
        private static readonly Color s_light = new Color(1f, 0.95f, 0.82f);

        [Header("모으기 · 하늘로")]
        [SerializeField] private float m_gatherDuration = 0.4f;
        [SerializeField] private float m_riseDuration = 0.25f;
        [Header("빛의 검")]
        [SerializeField] private float m_swordHeight = 1.8f;
        [SerializeField] private float m_dropHeight = 5f;
        [SerializeField] private float m_dropDuration = 0.18f;
        [Tooltip("가까운 타워부터 다음 검이 떨어지기까지의 간격")]
        [SerializeField] private float m_dropInterval = 0.08f;
        [SerializeField] private float m_stuckHold = 0.35f;
        [SerializeField] private float m_vanishDuration = 0.3f;
        [Header("맵 전체의 빛 (글로리 소드가 걸려 있는 동안)")]
        [SerializeField] private float m_ambientExposure = 0.2f;
        [SerializeField] private Color m_ambientFilter = new Color(1f, 0.95f, 0.84f);
        [SerializeField] private float m_ambientFade = 0.6f;
        [SerializeField] private float m_moteInterval = 0.12f;
        [Tooltip("2번 키로 미리 볼 때 빛이 남아 있는 시간")]
        [SerializeField] private float m_previewAmbient = 4f;

        private EffectQuadPool m_swordPool;
        private EffectQuadPool m_streakPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ringPool;
        private EffectParticles m_sparks;
        private EffectParticles m_motes;
        private Volume m_volume;
        private VolumeProfile m_profile;
        private ColorAdjustments m_color;
        private Sequence m_sequence;
        private float m_weight;
        private float m_holdOffUntil;
        private float m_forcedUntil;
        private float m_nextMote;

        private void Awake()
        {
            m_swordPool = new EffectQuadPool(transform, "EffectSprite", "Glory Sword", 34, EffectTextures.LightSword, true, null, true);
            m_streakPool = new EffectQuadPool(transform, "EffectSprite", "Glory Sword Streak", 33, EffectTextures.SlashLine, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Glory Sword Flare", 35, EffectTextures.Flare, true);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Glory Sword Glow", 21, EffectTextures.Glow, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Glory Sword Ring", 32, EffectTextures.Ring, true);
            m_sparks = new EffectParticles(transform, "Glory Sword Sparks", EffectTextures.Spark, true, 3f, 35, true, -0.2f, false);
            m_motes = new EffectParticles(transform, "Glory Sword Motes", EffectTextures.Glow, true, 2.5f, 20, false, 0f, false);
            CreateVolume();
        }

        private void OnDisable()
        {
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();
            m_sequence = null;
            m_weight = 0f;
            if (m_volume != null) m_volume.weight = 0f;
        }

        private void OnDestroy()
        {
            m_swordPool?.Dispose(); m_streakPool?.Dispose(); m_flarePool?.Dispose(); m_glowPool?.Dispose(); m_ringPool?.Dispose();
            if (m_profile != null) Destroy(m_profile);
        }

        public void Play(Camera camera, Vector3 origin, IReadOnlyList<Vector3> targets, float cellWorldSize, bool preview)
        {
            if (m_sequence != null && m_sequence.IsActive()) return;
            float cell = Mathf.Max(0.5f, cellWorldSize);
            var sorted = new List<Vector3>(targets ?? new List<Vector3>());
            sorted.Sort((a, b) => (a - origin).sqrMagnitude.CompareTo((b - origin).sqrMagnitude));

            float dropAt = m_gatherDuration + m_riseDuration * 0.6f;
            float lastLand = dropAt + Mathf.Max(0, sorted.Count - 1) * m_dropInterval + m_dropDuration;
            EffectActivity.HoldFor(lastLand + m_stuckHold + m_vanishDuration + 0.1f);
            m_holdOffUntil = Time.time + dropAt + m_dropDuration;
            if (preview) m_forcedUntil = m_holdOffUntil + m_previewAmbient;

            Sequence s = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            m_sequence = s;
            s.InsertCallback(0f, () => PlayGather(origin, cell));
            s.InsertCallback(m_gatherDuration, () => PlayRise(camera, origin, cell));
            for (int i = 0; i < sorted.Count; i++)
            {
                Vector3 target = sorted[i];
                bool first = i == 0;
                s.InsertCallback(dropAt + i * m_dropInterval, () => DropSword(target, cell, first));
            }
            s.InsertCallback(lastLand + m_stuckHold + m_vanishDuration, () => m_sequence = null);
        }

        private void PlayGather(Vector3 origin, float cell)
        {
            m_motes.Gather(origin, 16, s_gold, cell * 1.6f, cell * 0.1f, m_gatherDuration);
            TexturedFx.Pop(m_glowPool, origin, 0f, cell * 0.3f, cell * 1.8f, s_gold, 1.8f, 0f, m_gatherDuration, 0.15f, Ease.InCubic);
        }

        private void PlayRise(Camera camera, Vector3 origin, float cell)
        {
            float top = camera != null && camera.orthographic
                ? camera.transform.position.y + camera.orthographicSize + cell
                : origin.y + cell * 8f;
            float length = Mathf.Max(cell, top - origin.y);
            Vector3 middle = new Vector3(origin.x, origin.y + length * 0.5f, origin.z);
            TexturedFx.Swipe(m_streakPool, middle, 90f, new Vector3(length, cell * 0.35f, 1f), s_gold, 3f, 0f, m_riseDuration, 0.05f, 0.3f);
            TexturedFx.Swipe(m_streakPool, middle, 90f, new Vector3(length, cell * 0.12f, 1f), Color.white, 4f, 0f, m_riseDuration, 0.05f, 0.25f);
            TexturedFx.Pop(m_flarePool, origin, 0f, cell * 0.3f, cell * 1.6f, Color.white, 3.5f, 0f, 0.1f, 0.2f);
            ScreenShake.Play(0.025f, 0.1f);
        }

        private void DropSword(Vector3 target, float cell, bool first)
        {
            float height = cell * m_swordHeight;
            Vector3 rest = target + Vector3.up * (height * 0.5f - cell * 0.2f);
            Vector3 start = rest + Vector3.up * (cell * m_dropHeight);

            EffectQuad sword = m_swordPool.Get();
            sword.Transform.position = start;
            sword.Transform.localScale = new Vector3(height * 0.36f, height, 1f);
            sword.SetColor(s_light);
            sword.SetFloat(EffectQuad.IntensityId, 3f);

            Vector3 streakMiddle = (start + rest) * 0.5f + Vector3.up * (height * 0.3f);
            TexturedFx.Swipe(m_streakPool, streakMiddle, -90f, new Vector3((start - rest).magnitude + height * 0.6f, cell * 0.22f, 1f),
                s_gold, 2.5f, 0f, m_dropDuration, 0f, 0.25f);

            Sequence s = DOTween.Sequence().SetLink(sword.GameObject, LinkBehaviour.KillOnDisable);
            s.Append(sword.Transform.DOMove(rest, m_dropDuration).SetEase(Ease.InCubic));
            s.AppendCallback(() => Impact(target, cell, first));
            s.Append(sword.Transform.DOPunchScale(new Vector3(height * 0.05f, -height * 0.06f, 0f), 0.15f, 1, 0f));
            s.AppendInterval(m_stuckHold);
            s.Append(TexturedFx.DissolveOut(sword, m_vanishDuration));
            s.OnKill(() => m_swordPool.Release(sword));
        }

        private void Impact(Vector3 target, float cell, bool first)
        {
            TexturedFx.Pop(m_ringPool, target, 0f, cell * 0.2f, cell * 1.7f, s_gold, 2.4f, 0f, 0.3f, 0.3f, Ease.OutCubic);
            TexturedFx.Pop(m_flarePool, target, 0f, cell * 0.2f, cell * 1.2f, Color.white, 3.5f, 0f, 0.08f, 0.16f);
            TexturedFx.Pop(m_glowPool, target, 0f, cell * 0.5f, cell * 1.5f, s_gold, 1.6f, 0f, 0.3f, 0.5f);
            TexturedFx.Swipe(m_streakPool, target + Vector3.up * cell * 0.9f, 90f, new Vector3(cell * 1.8f, cell * 0.35f, 1f), s_light, 3f,
                0f, 0.12f, 0.05f, 0.3f);
            m_sparks.Burst(target, 10, s_gold, cell * 2f, cell * 5f, cell * 0.06f, cell * 0.12f, 0.25f, 0.45f, Vector2.up, 110f);
            ScreenShake.Play(first ? 0.045f : 0.02f, 0.1f);
        }

        private void Update()
        {
            bool on = Time.time >= m_holdOffUntil && (Time.time < m_forcedUntil || AnyGlorySword());
            float step = Time.deltaTime / Mathf.Max(0.01f, m_ambientFade);
            m_weight = Mathf.MoveTowards(m_weight, on ? 1f : 0f, step);
            if (m_volume != null) m_volume.weight = m_weight;

            if (m_weight > 0.01f && Time.time >= m_nextMote)
            {
                m_nextMote = Time.time + m_moteInterval;
                EmitMote();
            }
        }

        private static bool AnyGlorySword()
        {
            BoardManager board = BoardManager.Instance;
            if (board == null || board.Slots == null) return false;
            TowerSlots slots = board.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null && tower.IsAttackTower && BuffRule.HasSkill2(tower, TowerEmblem.Sword)) return true;
            }
            return false;
        }

        private void EmitMote()
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic) return;
            float cell = EffectSpace.CellWorldSize();
            float half = camera.orthographicSize;
            Vector3 center = camera.transform.position;
            var at = new Vector3(center.x + Random.Range(-half * camera.aspect, half * camera.aspect),
                                 center.y + Random.Range(-half * 0.2f, half), 0f);
            Color color = s_gold;
            color.a = m_weight * 0.8f;
            m_motes.Burst(at, 1, color, cell * 0.3f, cell * 0.7f, cell * 0.05f, cell * 0.12f, 2.5f, 3.5f, Vector2.down, 40f);
        }

        private void CreateVolume()
        {
            var volumeObject = new GameObject("Glory Sword Volume");
            volumeObject.transform.SetParent(transform, false);
            m_volume = volumeObject.AddComponent<Volume>();
            m_volume.isGlobal = true;
            m_volume.priority = 90f;
            m_volume.weight = 0f;
            m_profile = ScriptableObject.CreateInstance<VolumeProfile>();
            m_profile.name = "Glory Sword (Runtime)";
            m_color = m_profile.Add<ColorAdjustments>();
            m_color.postExposure.overrideState = true;
            m_color.postExposure.value = m_ambientExposure;
            m_color.colorFilter.overrideState = true;
            m_color.colorFilter.value = m_ambientFilter;
            m_volume.sharedProfile = m_profile;
        }
    }
}
