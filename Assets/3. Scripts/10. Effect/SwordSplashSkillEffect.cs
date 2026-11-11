using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SquareTD
{
    // 소드 마스터(광역 검) 스킬 2 보이드 슬래시 연출
    [DisallowMultipleComponent]
    public sealed class SwordSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_violet = new Color(0.45f, 0.34f, 1f);
        private static readonly Color s_white = new Color(0.94f, 0.93f, 1f);

        [Header("흑백으로 들어가며 모으기")]
        [SerializeField] private float m_enterMono = 0.7f;
        [SerializeField] private float m_gatherDuration = 1f;
        [SerializeField] private float m_gatherExposure = -0.8f;
        [Header("베기")]
        [SerializeField] private float m_slashDuration = 0.15f;
        [Tooltip("선 그림의 양 끝이 가늘어지는 만큼 화면 대각선보다 길게 그림")]
        [SerializeField] private float m_lineOvershoot = 1.4f;
        [Tooltip("베는 순간 게임이 멈추는 실제 시간 (0이면 안 멈춤)")]
        [SerializeField] private float m_hitStop = 0.08f;
        [Header("적이 맞는 참격")]
        [SerializeField] private float m_hitDelay = 0.25f;
        [SerializeField] private float m_hitLength = 2f;
        [Tooltip("줄의 한쪽 끝 적부터 반대쪽 끝 적까지 차례로 베이는 시간")]
        [SerializeField] private float m_hitSpread = 0.9f;
        [Header("원래 색으로")]
        [Tooltip("다 그은 뒤 흑백으로 있는 시간 (그동안 적들이 베임)")]
        [SerializeField] private float m_monoHold = 1.85f;
        [SerializeField] private float m_colorReturn = 1f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_cutPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ringPool;
        private EffectParticles m_sparks;
        private EffectParticles m_motes;
        private Volume m_volume;
        private VolumeProfile m_profile;
        private ColorAdjustments m_color;
        private Sequence m_sequence;
        private float m_slashAngle;

        private void Awake()
        {
            m_cutPool = new EffectQuadPool(transform, "EffectSprite", "Void Slash Cut", 31, EffectTextures.SlashLine, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Void Slash Flare", 32, EffectTextures.Flare, true);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Void Slash Glow", 30, EffectTextures.Glow, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Void Slash Ring", 30, EffectTextures.Ring, true);
            m_sparks = new EffectParticles(transform, "Void Slash Sparks", EffectTextures.Spark, true, 3f, 33, true, 1.2f, false);
            m_motes = new EffectParticles(transform, "Void Slash Motes", EffectTextures.Glow, true, 3f, 32, false, 0f, false);
            CreateVolume();
        }

        private void OnDisable() => StopAll();

        private void OnDestroy()
        {
            StopAll();
            m_cutPool?.Dispose(); m_flarePool?.Dispose(); m_glowPool?.Dispose(); m_ringPool?.Dispose();
            if (m_profile != null) Destroy(m_profile);
        }

        public void Play(Camera camera, IReadOnlyList<Vector3> hitPoints, float cellWorldSize)
        {
            if (camera == null || !camera.orthographic) return;
            StopAll();
            var hits = hitPoints != null ? new List<Vector3>(hitPoints) : new List<Vector3>();
            float cell = Mathf.Max(0.5f, cellWorldSize);
            float total = m_gatherDuration + m_slashDuration + m_monoHold + m_colorReturn;
            EffectActivity.HoldFor(total + 0.3f);

            Vector3 center = camera.transform.position;
            center.z = hits.Count > 0 ? hits[0].z : 0f;
            m_slashAngle = Mathf.Atan2(1f, camera.aspect) * Mathf.Rad2Deg;
            float length = 2f * camera.orthographicSize * Mathf.Sqrt(camera.aspect * camera.aspect + 1f) * m_lineOvershoot;

            m_volume.weight = 1f;
            SetColor(0f, 0f);
            Sequence s = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            m_sequence = s;

            s.Insert(0f, FloatTween(0f, -100f, m_enterMono, v => m_color.saturation.value = v).SetEase(Ease.OutQuad));
            s.Insert(0f, FloatTween(0f, m_gatherExposure, m_gatherDuration, v => m_color.postExposure.value = v).SetEase(Ease.OutQuad));
            s.InsertCallback(0f, () => PlayGather(center, cell));

            float slashAt = m_gatherDuration;
            s.InsertCallback(slashAt, () => PlaySlashLine(center, length, cell));

            float cutAt = slashAt + m_slashDuration;
            s.InsertCallback(cutAt, () =>
            {
                ScreenShake.Play(0.08f, 0.25f);
                ImpactFx.Punch(0.02f, 0.25f);
                if (m_hitStop > 0f) ImpactFx.Slow(0.03f, m_hitStop);
            });
            s.Insert(cutAt, FloatTween(0.6f, 0f, 0.3f, v => m_color.postExposure.value = v).SetEase(Ease.OutQuad));
            s.InsertCallback(cutAt + m_hitDelay, () => PlayHits(hits, cell));

            float colorAt = cutAt + m_monoHold;
            s.Insert(colorAt, FloatTween(-100f, 0f, m_colorReturn, v => m_color.saturation.value = v).SetEase(Ease.InOutSine));
            s.OnKill(ResetVolume);
        }

        private void PlayGather(Vector3 center, float cell)
        {
            m_motes.Gather(center, 24, s_violet, cell * 4f, cell * 0.12f, m_gatherDuration);
            TexturedFx.Pop(m_glowPool, center, 0f, cell * 0.2f, cell * 2.2f, s_violet, 1.6f, 0f, m_gatherDuration, 0.12f, Ease.InCubic);
            TexturedFx.Pop(m_flarePool, center, 0f, cell * 0.1f, cell * 1.4f, Color.white, 3f, 0f, m_gatherDuration, 0.1f, Ease.InCubic);
        }

        private void PlaySlashLine(Vector3 center, float length, float cell)
        {
            float hold = Mathf.Max(0f, m_monoHold - 0.4f);
            TexturedFx.Swipe(m_cutPool, center, m_slashAngle, new Vector3(length, cell * 0.32f, 1f), s_violet, 3f, 0f, m_slashDuration, hold, 0.6f);
            TexturedFx.Swipe(m_cutPool, center, m_slashAngle, new Vector3(length, cell * 0.1f, 1f), s_white, 4.5f, 0f, m_slashDuration, hold, 0.5f);
        }

        private void PlayHits(List<Vector3> hits, float cell)
        {
            if (hits.Count == 0) return;
            float radians = m_slashAngle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);

            float min = float.MaxValue, max = float.MinValue;
            foreach (Vector3 point in hits)
            {
                float along = Vector3.Dot(point, dir);
                min = Mathf.Min(min, along);
                max = Mathf.Max(max, along);
            }

            foreach (Vector3 point in hits)
            {
                float t = max > min ? (Vector3.Dot(point, dir) - min) / (max - min) : 0f;
                float delay = t * m_hitSpread + Range(0f, 0.03f);
                float angle = m_slashAngle + Range(-8f, 8f);
                TexturedFx.Swipe(m_cutPool, point, angle, new Vector3(cell * m_hitLength, cell * 0.3f, 1f), s_violet, 2.5f,
                    delay, 0.04f, 0.05f, 0.16f);
                TexturedFx.Swipe(m_cutPool, point, angle, new Vector3(cell * m_hitLength * 0.9f, cell * 0.1f, 1f), s_white, 4f,
                    delay, 0.04f, 0.05f, 0.14f);
                TexturedFx.Pop(m_flarePool, point, angle, cell * 0.2f, cell * 0.9f, Color.white, 3f, delay + 0.03f, 0.05f, 0.14f);
                TexturedFx.Pop(m_ringPool, point, 0f, cell * 0.2f, cell * 1.4f, s_violet, 2f, delay + 0.03f, 0.24f, 0.22f, Ease.OutCubic);
                Vector3 at = point;
                DOVirtual.DelayedCall(delay + 0.04f, () =>
                {
                    m_sparks.Burst(at, 5, s_white, cell * 3f, cell * 7f, cell * 0.07f, cell * 0.13f, 0.18f, 0.3f, new Vector2(dir.x, dir.y), 35f);
                    m_sparks.Burst(at, 5, s_violet, cell * 3f, cell * 7f, cell * 0.07f, cell * 0.13f, 0.18f, 0.3f, new Vector2(-dir.x, -dir.y), 35f);
                }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        private void CreateVolume()
        {
            var volumeObject = new GameObject("Void Slash Volume");
            volumeObject.transform.SetParent(transform, false);
            m_volume = volumeObject.AddComponent<Volume>();
            m_volume.isGlobal = true;
            m_volume.priority = 100f;
            m_volume.weight = 0f;
            m_profile = ScriptableObject.CreateInstance<VolumeProfile>();
            m_profile.name = "Void Slash (Runtime)";
            m_color = m_profile.Add<ColorAdjustments>();
            m_color.saturation.overrideState = true;
            m_color.postExposure.overrideState = true;
            m_volume.sharedProfile = m_profile;
        }

        private void SetColor(float exposure, float saturation)
        {
            if (m_color == null) return;
            m_color.postExposure.value = exposure;
            m_color.saturation.value = saturation;
        }

        private void ResetVolume()
        {
            SetColor(0f, 0f);
            if (m_volume != null) m_volume.weight = 0f;
        }

        private static Tween FloatTween(float from, float to, float duration, TweenCallback<float> setter)
        {
            float value = from;
            return DOTween.To(() => value, v => { value = v; setter(v); }, to, duration);
        }

        private void StopAll()
        {
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();
            m_sequence = null;
            ResetVolume();
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
