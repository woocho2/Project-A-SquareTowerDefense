using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 레인저(광역 활) 스킬 2 화살비 연출 (2026-10-28 사용자: 쏘아 올리기와 떨어지기를 나눔)
    //   PlayVolley (발동, ④) 타워에서 화살 다발이 세 번 부채꼴로 하늘에 쏘아 올려져 화면 위로 사라짐
    //   PlayRain   (⑤마다)  하늘이 어둑해지고 화면 전체에 화살이 비스듬히 쏟아져 꽂힘. 맞은 적마다 두 대씩 먼저 꽂힘
    //   타워·규칙은 모름. 카메라·타워 위치·맞을 위치들만 받아서 그림
    [DisallowMultipleComponent]
    public sealed class ArrowSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_sky = new Color(0.05f, 0.05f, 0.1f);

        [SerializeField] private int m_arrowCount = 70;
        [SerializeField] private float m_arrowLength = 0.6f;       // 칸에 대한 비율
        [SerializeField] private float m_volleyDuration = 0.35f;
        [SerializeField] private int m_volleyWaves = 3;
        [SerializeField] private float m_volleyWaveGap = 0.12f;
        [SerializeField] private float m_rainSpread = 1f;          // 쏟아지는 데 걸리는 시간
        [SerializeField] private float m_fallDuration = 0.2f;
        [SerializeField] private float m_slant = 16f;
        [SerializeField] private float m_stayDuration = 0.6f;
        [SerializeField, Range(0f, 1f)] private float m_skyDarkness = 0.35f;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
        private EffectQuadPool m_pool;

        private void Awake() => m_pool = new EffectQuadPool(transform, "SwordQi", "Arrow Rain", 30);

        private void OnDestroy() => m_pool?.Dispose();

        // 발동: 하늘로 쏘아 올리기만. owner: 레인저 위치 / cellWorldSize: 칸 한 변
        public void PlayVolley(Camera camera, Vector3 owner, float cellWorldSize)
        {
            if (!m_pool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float length = cell * m_arrowLength;
            float height = camera != null && camera.orthographic ? camera.orthographicSize * 2f : 10f;   // 화면 위로 벗어날 만큼 날아감
            for (int wave = 0; wave < m_volleyWaves; wave++)
            {
                for (int i = 0; i < 5; i++) PlayVolleyArrow(owner, length, i, wave * m_volleyWaveGap, height);
            }
        }

        // ⑤: 쏘아 올린 화살이 떨어짐. camera: 화면 크기를 잴 카메라 / hitPositions: 맞을 적들의 위치
        public void PlayRain(Camera camera, IReadOnlyList<Vector3> hitPositions, float cellWorldSize)
        {
            if (camera == null || !m_pool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float length = cell * m_arrowLength;
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * camera.aspect;
            Vector3 center = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);
            float rainAt = 0.2f;                                                    // 하늘이 어두워진 뒤

            PlaySkyDim(center, width, height, rainAt + m_rainSpread + m_fallDuration + m_stayDuration);

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

        // 타워에서 화살이 부채꼴로 하늘에 쏘아 올려짐 (delay 뒤, 화면 높이만큼 날아가며 흐려짐)
        private void PlayVolleyArrow(Vector3 owner, float length, int index, float delay, float travel)
        {
            float angle = 90f + (index - 2) * 10f + Range(-3f, 3f);
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
            arrow.Transform.localScale = Vector3.zero;
            sequence.AppendInterval(delay + index * 0.025f);
            sequence.AppendCallback(() => arrow.Transform.localScale = new Vector3(length, length * 0.5f, 1f));
            sequence.Append(arrow.Transform.DOMove(owner + direction * Mathf.Max(length * 6f, travel), m_volleyDuration).SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; arrow.SetFloat(EffectQuad.AlphaId, value); },
                0f, m_volleyDuration).SetEase(Ease.InCubic));
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
