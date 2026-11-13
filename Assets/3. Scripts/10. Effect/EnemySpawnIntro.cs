using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 웨이브 소환 연출 — 큐브가 위에서 가속하며 떨어져 착지 때 눌리고, 작게 한 번 튀어 오른 뒤 제 모양으로
    public static class EnemySpawnIntro
    {
        private const float DropHeight = 2.4f;
        private const float DropTime = 0.34f;
        private const float MaxStagger = 0.06f;
        private const float MaxSpread = 1.4f;
        private const float BossDropHeight = 4f;
        private const float BossDropTime = 0.5f;

        public static IEnumerator Play(IReadOnlyList<EnemyController> enemies)
        {
            if (SimulationRunner.IsOn || enemies == null || enemies.Count == 0) yield break;

            float stagger = Mathf.Min(MaxStagger, MaxSpread / enemies.Count);
            float end = 0f;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null) continue;
                bool boss = enemy.IsBoss;
                float delay = boss ? enemies.Count * stagger + 0.25f : i * stagger;
                end = Mathf.Max(end, delay + Drop(enemy.transform, delay, boss));
            }

            yield return new WaitForSeconds(end);
        }

        private static float Drop(Transform target, float delay, bool boss)
        {
            Vector3 land = target.position;
            float height = boss ? BossDropHeight : DropHeight;
            float fall = boss ? BossDropTime : DropTime;
            target.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence().SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.AppendCallback(() =>
            {
                target.position = land + Vector3.up * height;
                target.localScale = new Vector3(0.75f, 1.3f, 1f);
            });
            sequence.Append(target.DOMove(land, fall).SetEase(Ease.InQuad));
            sequence.Join(target.DOScale(new Vector3(0.85f, 1.2f, 1f), fall));
            sequence.Append(target.DOScale(new Vector3(1.35f, 0.65f, 1f), 0.06f).SetEase(Ease.OutQuad));
            if (boss) sequence.AppendCallback(() => ScreenShake.Play(0.18f, 0.3f));
            sequence.Append(target.DOMove(land + Vector3.up * 0.22f, 0.12f).SetEase(Ease.OutQuad));
            sequence.Join(target.DOScale(new Vector3(0.92f, 1.1f, 1f), 0.12f));
            sequence.Append(target.DOMove(land, 0.1f).SetEase(Ease.InQuad));
            sequence.Join(target.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutBack));
            sequence.OnKill(() =>
            {
                if (target == null) return;
                target.position = land;
                target.localScale = Vector3.one;
            });
            return fall + 0.06f + 0.12f + 0.16f;
        }
    }
}
