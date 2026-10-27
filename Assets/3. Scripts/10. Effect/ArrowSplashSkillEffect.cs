using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 레인저(광역 활) 스킬 2 화살비 연출 — 맵 전체에 화살이 쏟아짐 (2026-10-27 사용자: 맵 전체)
    //   ① 타워에서 화살 다발이 부채꼴로 하늘에 쏘아 올려짐 + 하늘이 어둑해짐
    //   ② 화면 전체에 화살이 비스듬히 쏟아져 꽂힘. 맞은 적마다 두 대씩 먼저 꽂힘
    //   ③ 꽂힌 화살들이 남았다가 흐려지고 하늘이 다시 밝아짐
    //   남아서 n턴 동안 매 턴 피해를 주는 화살비(장판)의 표시는 수업 29에서 규칙(ArrowRainGround)과 함께
    //   타워·규칙은 모름. 카메라·타워 위치·맞을 위치들만 받아서 그림
    [DisallowMultipleComponent]
    public sealed class ArrowSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_sky = new Color(0.05f, 0.05f, 0.1f);

        [SerializeField] private int m_arrowCount = 70;
        [SerializeField] private float m_arrowLength = 0.6f;       // 칸에 대한 비율
        [SerializeField] private float m_volleyDuration = 0.25f;
        [SerializeField] private float m_rainSpread = 1f;          // 쏟아지는 데 걸리는 시간
        [SerializeField] private float m_fallDuration = 0.2f;
        [SerializeField] private float m_slant = 16f;
        [SerializeField] private float m_stayDuration = 0.6f;
        [SerializeField, Range(0f, 1f)] private float m_skyDarkness = 0.35f;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
        private EffectQuadPool m_pool;

        private void Awake() => m_pool = new EffectQuadPool(transform, "SwordQi", "Arrow Rain", 30);

        private void OnDestroy() => m_pool?.Dispose();

        // camera: 화면 크기를 잴 카메라 / owner: 레인저 위치 / hitPositions: 맞을 적들의 위치 / cellWorldSize: 칸 한 변
        public void Play(Camera camera, Vector3 owner, IReadOnlyList<Vector3> hitPositions, float cellWorldSize)
        {
            if (camera == null || !m_pool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float length = cell * m_arrowLength;
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * camera.aspect;
            Vector3 center = new Vector3(camera.transform.position.x, camera.transform.position.y, owner.z);
            float rainAt = m_volleyDuration + 0.1f;

            PlaySkyDim(center, width, height, rainAt + m_rainSpread + m_fallDuration + m_stayDuration);
            for (int i = 0; i < 5; i++) PlayVolleyArrow(owner, length, i);

            for (int i = 0; i < m_arrowCount; i++)
            {
                Vector3 land = center + new Vector3(Range(-0.5f, 0.5f) * width, Range(-0.5f, 0.5f) * height, 0f);
                ArrowFx.PlayFallingArrow(m_pool, land, length, rainAt + Range(0f, m_rainSpread), m_slant + Range(-5f, 5f),
                    cell * 2.5f, m_fallDuration, m_stayDuration, ArrowFx.Wood, i % 3 == 0);
            }

            if (hitPositions == null) return;
            foreach (Vector3 hit in hitPositions)                                 // 맞은 적에게는 먼저 두 대씩
            {
                for (int k = 0; k < 2; k++)
                {
                    Vector3 land = hit + new Vector3(Range(-0.2f, 0.2f), Range(-0.15f, 0.15f), 0f) * cell;
                    ArrowFx.PlayFallingArrow(m_pool, land, length, rainAt + Range(0f, m_rainSpread * 0.4f), m_slant + Range(-5f, 5f),
                        cell * 2.5f, m_fallDuration, m_stayDuration, ArrowFx.Wood, true);
                }
            }
        }

        // 타워에서 화살이 부채꼴로 하늘에 쏘아 올려짐
        private void PlayVolleyArrow(Vector3 owner, float length, int index)
        {
            float angle = 90f + (index - 2) * 12f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);

            EffectQuad arrow = m_pool.Get();
            arrow.Transform.position = owner;
            arrow.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            arrow.Transform.localScale = new Vector3(length, length * 0.5f, 1f);
            arrow.SetColor(ArrowFx.Wood);
            arrow.SetFloat(EffectQuad.ShapeId, 8f);
            arrow.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(arrow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(index * 0.025f);
            sequence.Append(arrow.Transform.DOMove(owner + direction * (length * 6f), m_volleyDuration).SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; arrow.SetFloat(EffectQuad.AlphaId, value); },
                0f, m_volleyDuration).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(arrow));
        }

        // 화살이 하늘을 덮어 화면이 어둑해졌다가 밝아짐
        private void PlaySkyDim(Vector3 center, float width, float height, float duration)
        {
            EffectQuad sky = m_pool.Get();
            sky.Transform.position = center;
            sky.Transform.localScale = new Vector3(width * 1.8f, height * 1.8f, 1f);
            sky.SetColor(s_sky);
            sky.SetFloat(EffectQuad.ShapeId, 2f);
            sky.SetFloat(EffectQuad.AlphaId, 0f);
            sky.SetSortingOffset(-20);

            float alpha = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(sky.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; sky.SetFloat(EffectQuad.AlphaId, value); },
                m_skyDarkness, 0.25f).SetEase(Ease.OutQuad));
            sequence.AppendInterval(Mathf.Max(0f, duration - 0.25f));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; sky.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.4f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(sky));
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
