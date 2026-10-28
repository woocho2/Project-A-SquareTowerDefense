using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 블레이드 댄서(단일 검) 스킬 2 블레이드 템페스트 연출 — 그림 + 셰이더 + 파티클 버전
    //   ① 표적: 적 위에 별빛이 번쩍
    //   ② 스물네 번 베기: 반달 궤적(FX_SlashTrail)이 사방으로 아주 빠르게. 벨 때마다 꽃잎(FX_PlumPetal 파티클)이 흩날림
    //      여섯 번마다 매화 한 송이(FX_PlumBlossom)가 핌
    //   ③ 마지막 일격: 잠깐 슬로우 → 크게 베고 분홍 임팩트 프레임 + 확대 + 큰 매화 + 꽃잎 폭발
    //   ④ 피었던 매화가 타들어 가듯(디졸브) 지고 꽃잎이 흩날리며 사라짐
    //   타워·규칙은 모름. 맞을 위치와 칸 크기만 받아서 그림 (24번은 기획 고정: 20번 4-3장)
    [DisallowMultipleComponent]
    public sealed class SwordTargetSkillEffect : MonoBehaviour
    {
        public const int SlashCount = 24;
        private const int BloomEvery = 6;
        private static readonly Color s_blade = new Color(1f, 0.92f, 0.96f);
        private static readonly Color s_pink = new Color(1f, 0.55f, 0.75f);

        [SerializeField] private float m_markDuration = 0.15f;
        [SerializeField] private float m_slashInterval = 0.045f;
        [SerializeField] private float m_slashDraw = 0.035f;
        [SerializeField] private float m_finisherPause = 0.12f;
        [SerializeField] private float m_glow = 2.2f;
        [SerializeField] private int m_burstPetals = 40;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
        private EffectQuadPool m_arcPool;
        private EffectQuadPool m_bigArcPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_blossomPool;
        private EffectParticles m_petals;
        private EffectParticles m_sparks;

        private void Awake()
        {
            m_arcPool = new EffectQuadPool(transform, "EffectSprite", "Plum Slash", 24, EffectTextures.SlashTrail, true, ArcMesh.Get(120f, 0.28f));
            m_bigArcPool = new EffectQuadPool(transform, "EffectSprite", "Plum Finisher", 25, EffectTextures.SlashTrail, true, ArcMesh.Get(150f, 0.4f));
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Plum Flare", 27, EffectTextures.Flare, true);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Plum Glow", 22, EffectTextures.Glow, true);
            m_blossomPool = new EffectQuadPool(transform, "EffectSprite", "Plum Blossom", 23, EffectTextures.PlumBlossom, false);
            m_petals = new EffectParticles(transform, "Plum Petals", EffectTextures.PlumPetal, false, 1.1f, 26, false, 0.25f, true);
            m_sparks = new EffectParticles(transform, "Plum Sparks", EffectTextures.Spark, true, 2.5f, 28, true, 0.8f, false);
        }

        private void OnDestroy()
        {
            m_arcPool?.Dispose(); m_bigArcPool?.Dispose(); m_flarePool?.Dispose(); m_glowPool?.Dispose(); m_blossomPool?.Dispose();
        }

        // target: 맞을 적의 위치 / cellWorldSize: 칸 한 변의 월드 길이
        public void Play(Vector3 target, float cellWorldSize)
        {
            if (!m_arcPool.IsReady || !m_blossomPool.IsReady) return;

            float size = Mathf.Max(0.5f, cellWorldSize);
            float finisherAt = m_markDuration + (SlashCount - 1) * m_slashInterval + m_finisherPause;
            float endAt = finisherAt + 0.9f;
            var blossoms = new List<EffectQuad>();

            TexturedFx.Pop(m_flarePool, target, 45f, 0f, size * 0.8f, s_blade, m_glow, 0f, m_markDuration, 0.1f, Ease.OutBack);   // 표적 번쩍

            for (int i = 0; i < SlashCount - 1; i++)
            {
                float at = m_markDuration + i * m_slashInterval;
                PlayArc(m_arcPool, target, Range(0f, 360f), size * Range(0.42f, 0.55f), Range(0f, 1f) < 0.5f,
                    i % 2 == 0 ? s_blade : s_pink, at, m_slashDraw, 0.1f);
                Vector3 at3 = target;
                DOVirtual.DelayedCall(at + m_slashDraw, () =>
                {
                    m_petals.Burst(at3, 2, Color.white, size * 0.8f, size * 1.8f, size * 0.12f, size * 0.2f, 0.8f, 1.3f);
                    m_sparks.Burst(at3, 2, s_blade, size * 2f, size * 4f, size * 0.06f, size * 0.1f, 0.15f, 0.25f);
                }).SetLink(gameObject, LinkBehaviour.KillOnDisable);

                if (i % BloomEvery == BloomEvery - 1)                                    // 여섯 번마다 매화가 핌
                {
                    Vector3 offset = new Vector3(Range(-0.4f, 0.4f), Range(-0.4f, 0.4f), 0f) * size;
                    blossoms.Add(PlayBlossom(target + offset, size * 0.5f, at));
                }
            }

            // 마지막 일격: 잠깐 느려졌다가 크게
            DOVirtual.DelayedCall(finisherAt - 0.06f, () => ImpactFx.Slow(0.3f, 0.12f)).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            PlayArc(m_bigArcPool, target, Range(20f, 160f), size * 0.8f, false, s_blade, finisherAt, m_slashDraw * 1.8f, 0.2f);
            float bloomAt = finisherAt + m_slashDraw * 1.8f;
            blossoms.Add(PlayBlossom(target, size * 1.3f, bloomAt));
            DOVirtual.DelayedCall(bloomAt, () =>
            {
                ImpactFx.Frame(new Color(1f, 0.75f, 0.88f, 0.55f), 0.04f);
                ImpactFx.Punch(0.025f, 0.3f);
                ScreenShake.Play(0.05f, 0.15f);
                TexturedFx.Pop(m_glowPool, target, 0f, size * 0.5f, size * 2.2f, s_pink, 1.6f, 0f, 0.2f, 0.4f);
                TexturedFx.Pop(m_flarePool, target, 0f, size * 0.3f, size * 1.8f, Color.white, 3f, 0f, 0.1f, 0.25f);
                m_petals.Burst(target, m_burstPetals, Color.white, size * 1.5f, size * 4f, size * 0.14f, size * 0.24f, 1.0f, 1.8f);
                m_sparks.Burst(target, 14, s_blade, size * 3f, size * 8f, size * 0.08f, size * 0.16f, 0.25f, 0.45f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            // 매화가 함께 타들어 가듯 짐
            DOVirtual.DelayedCall(endAt, () =>
            {
                foreach (EffectQuad blossom in blossoms)
                    TexturedFx.DissolveOut(blossom, 0.5f).OnKill(() => m_blossomPool.Release(blossom));
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        // 반달 궤적 한 줄: 궤적의 한가운데가 center를 지나게 원의 가운데를 비켜 둠
        private void PlayArc(EffectQuadPool pool, Vector3 center, float angle, float radius, bool mirror, Color color,
            float delay, float draw, float tail)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector3 pivot = center - new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
            TexturedFx.Swipe(pool, pivot, angle, new Vector3(radius, mirror ? -radius : radius, 1f), color, m_glow, delay, draw, 0.01f, tail);
        }

        // 매화 한 송이가 커지며 핌 (지는 것은 Play에서 한꺼번에)
        private EffectQuad PlayBlossom(Vector3 position, float size, float appearAt)
        {
            EffectQuad blossom = m_blossomPool.Get();
            blossom.Transform.position = position;
            blossom.Transform.rotation = Quaternion.Euler(0f, 0f, Range(0f, 72f));
            blossom.Transform.localScale = Vector3.zero;
            blossom.SetFloat(EffectQuad.IntensityId, 1.2f);

            Sequence sequence = DOTween.Sequence().SetLink(blossom.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(appearAt);
            sequence.Append(blossom.Transform.DOScale(new Vector3(size, size, 1f), 0.16f).SetEase(Ease.OutBack));
            sequence.Append(blossom.Transform.DORotate(new Vector3(0f, 0f, Range(-40f, 40f)), 1.2f, RotateMode.LocalAxisAdd).SetEase(Ease.OutSine));
            return blossom;
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
