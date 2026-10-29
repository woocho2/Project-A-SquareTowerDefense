using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 도끼가 땅을 찍은 자리 연출
    public static class AxeImpact
    {
        public static readonly Color Ember = new Color(1f, 0.45f, 0.2f);
        public static readonly Color Hot = new Color(1f, 0.75f, 0.45f);
        private static readonly Color s_scorch = new Color(0.12f, 0.06f, 0.04f);
        private static readonly Color s_crack = new Color(0.13f, 0.08f, 0.06f);
        private static readonly Color s_rock = new Color(0.58f, 0.44f, 0.34f);
        private static readonly Color s_dust = new Color(0.78f, 0.66f, 0.54f);

        public static void PlayGround(EffectQuadPool shapePool, EffectQuadPool slashPool, System.Random random,
            Vector3 center, float size, int cracks, float delay, float weight)
        {
            size = Mathf.Max(0.5f, size);
            weight = Mathf.Clamp01(weight);

            PlayScorch(shapePool, center, size, delay);
            float startAngle = Range(random, 0f, 360f);
            for (int i = 0; i < cracks; i++)
            {
                float angle = startAngle + i * (360f / cracks) + Range(random, -20f, 20f);
                PlayCrack(slashPool, random, center, angle, size * Range(random, 0.5f, 0.75f), size * 0.12f, delay);
            }
            PlayDust(shapePool, center, size, delay);

            int chunks = Mathf.RoundToInt(4f + 10f * weight);
            for (int i = 0; i < chunks; i++) PlayChunk(shapePool, random, center, size, delay);

            DOVirtual.DelayedCall(delay, () => ScreenShake.Play(0.05f + 0.15f * weight, 0.12f + 0.15f * weight));
        }

        public static void PlayDustRing(EffectQuadPool shapePool, Vector3 center, float size, float delay)
        {
            EffectQuad ring = shapePool.Get();
            ring.Transform.position = center;
            ring.Transform.localScale = Vector3.zero;
            ring.SetColor(s_dust);
            ring.SetFloat(EffectQuad.ShapeId, 6f);
            ring.SetFloat(EffectQuad.AlphaId, 0.8f);
            ring.SetSortingOffset(-10);

            float alpha = 0.8f;
            Sequence sequence = DOTween.Sequence().SetLink(ring.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(ring.Transform.DOScale(new Vector3(size * 2.2f, size * 1.7f, 1f), 0.5f).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; ring.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.5f).SetEase(Ease.InQuad));
            sequence.OnKill(() => shapePool.Release(ring));
        }

        public static void PlaySmall(EffectQuadPool shapePool, System.Random random, Vector3 center, float size, float delay)
        {
            size = Mathf.Max(0.3f, size);
            PlayFlash(shapePool, center, size * 0.6f, delay);
            for (int i = 0; i < 3; i++) PlayChunk(shapePool, random, center, size * 0.6f, delay);
        }

        private static void PlayCrack(EffectQuadPool slashPool, System.Random random, Vector3 center, float angle,
            float length, float thickness, float delay)
        {
            Vector3 point = center;
            float direction = angle;
            for (int segment = 0; segment < 3; segment++)
            {
                direction += Range(random, -28f, 28f);
                float segmentLength = length / 3f * Range(random, 0.8f, 1.2f);
                float radians = direction * Mathf.Deg2Rad;
                Vector3 step = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * segmentLength;
                SlashStroke.Play(slashPool, point + step * 0.5f, direction, segmentLength * 1.15f, thickness * (1f - segment * 0.25f),
                    0.3f, s_crack, delay + segment * 0.03f, 0.04f, 0.45f, 0.4f, 0f);
                point += step;
            }
        }

        public static void Play(EffectQuadPool shapePool, EffectQuadPool slashPool, System.Random random,
            Vector3 center, float size, int cracks, float delay, float weight)
        {
            size = Mathf.Max(0.5f, size);
            weight = Mathf.Clamp01(weight);

            PlayScorch(shapePool, center, size, delay);
            float startAngle = Range(random, 0f, 360f);
            for (int i = 0; i < cracks; i++)
            {
                float angle = startAngle + i * (360f / cracks) + Range(random, -18f, 18f);
                float length = size * Range(random, 0.5f, 0.8f);
                float radians = angle * Mathf.Deg2Rad;
                Vector3 middle = center + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * (length * 0.5f);
                SlashStroke.Play(slashPool, middle, angle, length, size * 0.28f, 0.22f, Ember,
                    delay + Range(random, 0f, 0.03f), 0.05f, 0.22f, 0.4f);
            }
            PlayRing(shapePool, center, size, delay);
            PlayFlash(shapePool, center, size * 0.75f, delay);
            PlayDust(shapePool, center, size, delay);

            int chunks = Mathf.RoundToInt(4f + 10f * weight);
            for (int i = 0; i < chunks; i++) PlayChunk(shapePool, random, center, size, delay);

            DOVirtual.DelayedCall(delay, () => ScreenShake.Play(0.05f + 0.15f * weight, 0.12f + 0.15f * weight));
        }

        public static void PlayGhost(EffectQuadPool shapePool, Transform source, Color color, float alpha, float fade)
            => PlayGhostShape(shapePool, source, color, 5f, alpha, fade);

        public static void PlayGhostShape(EffectQuadPool shapePool, Transform source, Color color, float shape, float alpha, float fade)
        {
            EffectQuad ghost = shapePool.Get();
            ghost.Transform.position = source.position;
            ghost.Transform.rotation = source.rotation;
            ghost.Transform.localScale = source.localScale;
            ghost.SetColor(color);
            ghost.SetFloat(EffectQuad.ShapeId, shape);
            ghost.SetFloat(EffectQuad.AlphaId, alpha);
            ghost.SetSortingOffset(-1);

            float current = alpha;
            DOTween.To(() => current, value => { current = value; ghost.SetFloat(EffectQuad.AlphaId, value); }, 0f, fade)
                .SetEase(Ease.InQuad)
                .SetLink(ghost.GameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => shapePool.Release(ghost));
        }

        private static void PlayScorch(EffectQuadPool pool, Vector3 center, float size, float delay)
        {
            EffectQuad scorch = pool.Get();
            scorch.Transform.position = center;
            scorch.Transform.localScale = Vector3.zero;
            scorch.SetColor(s_scorch);
            scorch.SetFloat(EffectQuad.ShapeId, 2f);
            scorch.SetFloat(EffectQuad.AlphaId, 0.7f);
            scorch.SetSortingOffset(-20);

            float alpha = 0.7f;
            Sequence sequence = DOTween.Sequence().SetLink(scorch.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(scorch.Transform.DOScale(new Vector3(size * 1.15f, size * 0.9f, 1f), 0.08f).SetEase(Ease.OutQuad));
            sequence.AppendInterval(0.35f);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; scorch.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.6f).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(scorch));
        }

        private static void PlayRing(EffectQuadPool pool, Vector3 center, float size, float delay)
        {
            EffectQuad ring = pool.Get();
            ring.Transform.position = center;
            ring.Transform.localScale = Vector3.zero;
            ring.SetColor(Hot);
            ring.SetFloat(EffectQuad.ShapeId, 6f);
            ring.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(ring.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(ring.Transform.DOScale(new Vector3(size * 1.5f, size * 1.2f, 1f), 0.3f).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; ring.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.3f).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(ring));
        }

        private static void PlayFlash(EffectQuadPool pool, Vector3 center, float size, float delay)
        {
            EffectQuad flash = pool.Get();
            flash.Transform.position = center;
            flash.Transform.localScale = Vector3.zero;
            flash.SetColor(Hot);
            flash.SetFloat(EffectQuad.ShapeId, 2f);
            flash.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(flash.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(flash.Transform.DOScale(new Vector3(size, size, 1f), 0.05f).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; flash.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.2f).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(flash));
        }

        private static void PlayDust(EffectQuadPool pool, Vector3 center, float size, float delay)
        {
            EffectQuad dust = pool.Get();
            dust.Transform.position = center;
            dust.Transform.localScale = Vector3.zero;
            dust.SetColor(s_dust);
            dust.SetFloat(EffectQuad.ShapeId, 2f);
            dust.SetFloat(EffectQuad.AlphaId, 0.45f);
            dust.SetSortingOffset(-10);

            float alpha = 0.45f;
            Sequence sequence = DOTween.Sequence().SetLink(dust.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay + 0.03f);
            sequence.Append(dust.Transform.DOScale(new Vector3(size * 1.8f, size * 1.3f, 1f), 0.5f).SetEase(Ease.OutCubic));
            sequence.Join(dust.Transform.DOMoveY(center.y + size * 0.15f, 0.5f));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; dust.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.5f).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(dust));
        }

        private static void PlayChunk(EffectQuadPool pool, System.Random random, Vector3 center, float size, float delay)
        {
            EffectQuad chunk = pool.Get();
            chunk.Transform.position = center;
            chunk.Transform.rotation = Quaternion.Euler(0f, 0f, Range(random, 0f, 360f));
            float chunkSize = size * Range(random, 0.07f, 0.14f);
            chunk.Transform.localScale = Vector3.zero;
            chunk.SetColor(s_rock);
            chunk.SetFloat(EffectQuad.ShapeId, 7f);
            chunk.SetFloat(EffectQuad.AlphaId, 1f);

            float direction = Range(random, 0f, Mathf.PI * 2f);
            Vector3 land = center + new Vector3(Mathf.Cos(direction), Mathf.Sin(direction) * 0.7f, 0f) * (size * Range(random, 0.45f, 0.95f));
            float duration = Range(random, 0.35f, 0.55f);
            float alpha = 1f;

            Sequence sequence = DOTween.Sequence().SetLink(chunk.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.AppendCallback(() => chunk.Transform.localScale = new Vector3(chunkSize, chunkSize, 1f));
            sequence.Append(chunk.Transform.DOJump(land, size * Range(random, 0.3f, 0.6f), 1, duration));
            sequence.Join(chunk.Transform.DORotate(new Vector3(0f, 0f, Range(random, -540f, 540f)), duration, RotateMode.LocalAxisAdd));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; chunk.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.2f));
            sequence.OnKill(() => pool.Release(chunk));
        }

        private static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
