using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 레인저(광역 활) 기본 공격 연출 — 그 구간에 화살비
    //   ① 타워 주위에 화살이 하늘을 향해 메겨짐
    //   ② 하늘로 쏘아 올려 사라짐
    //   ③ 잠시 뒤 착탄 범위에 화살 여러 대가 비스듬히 쏟아져 땅에 꽂힘 (촉은 땅 아래로 지워짐) + 작은 흙먼지
    //   첫 화살이 꽂히는 순간 onArrived. 타워·규칙은 모름. 위치와 착탄 범위 크기만 받아서 그림
    [DisallowMultipleComponent]
    public sealed class ArrowSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        [SerializeField] private float m_arrowLength = 0.55f;      // 칸에 대한 비율
        [SerializeField] private float m_riseDuration = 0.14f;
        [SerializeField] private float m_rainDelay = 0.12f;
        [SerializeField] private float m_fallDuration = 0.16f;
        [SerializeField] private float m_arrowInterval = 0.03f;
        [SerializeField] private float m_slant = 18f;
        [SerializeField] private float m_stayDuration = 0.35f;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
        private EffectQuadPool m_pool;

        private void Awake() => m_pool = new EffectQuadPool(transform, "SwordQi", "Ranger Arrow", 24);

        private void OnDestroy() => m_pool?.Dispose();

        // 게임의 기본 공격 한 발 (SplashAction → ProjectileManager): hold만큼 메긴 채 기다렸다 하늘로 쏨, 첫 화살이 꽂히면 onArrived
        //   sizeWorld = 착탄 범위 한 변의 월드 길이
        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_pool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float length = cell * m_arrowLength;
            float area = Mathf.Max(cell, sizeWorld);

            // ① ② 위를 향해 메겼다가 하늘로
            EffectQuad volley = m_pool.Get();
            volley.Transform.position = from;
            volley.Transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            volley.Transform.localScale = Vector3.zero;
            volley.SetColor(ArrowFx.Wood);
            volley.SetFloat(EffectQuad.ShapeId, 8f);
            volley.SetFloat(EffectQuad.AlphaId, 1f);

            float shootAt = Mathf.Max(hold, 0.1f);
            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(volley.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(volley.Transform.DOScale(new Vector3(length, length * 0.5f, 1f), 0.1f).SetEase(Ease.OutBack));
            sequence.AppendInterval(shootAt - 0.1f);
            sequence.Append(volley.Transform.DOMoveY(from.y + cell * 3f, m_riseDuration).SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; volley.SetFloat(EffectQuad.AlphaId, value); },
                0f, m_riseDuration).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(volley));

            // ③ 착탄 범위에 쏟아짐 (범위가 넓을수록 많이)
            int side = Mathf.Max(1, Mathf.RoundToInt(area / cell));
            int count = Mathf.Clamp(4 + side * 2, 6, 14);
            float rainAt = shootAt + m_riseDuration + m_rainDelay;
            for (int i = 0; i < count; i++)
            {
                Vector3 land = to + new Vector3(Range(-0.45f, 0.45f) * area, Range(-0.45f, 0.45f) * area, 0f);
                ArrowFx.PlayFallingArrow(m_pool, land, length, rainAt + i * m_arrowInterval, m_slant + Range(-6f, 6f),
                    cell * 2.2f, m_fallDuration, m_stayDuration, ArrowFx.Wood, i % 2 == 0, i == 0 ? onArrived : null);
            }
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
