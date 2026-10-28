using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace SquareTD
{
    public class ProjectileManager : Singleton<ProjectileManager>, IProjectileLauncher
    {
        [SerializeField] private Projectile m_prefab;
        [SerializeField] private ProjectileCatalog m_catalog;
        [SerializeField] private Transform m_container;
        [SerializeField] private int m_defaultCapacity = 40;
        [SerializeField] private int m_maxSize = 300;

        private ObjectPool<Projectile> m_pool;

        private readonly Dictionary<int, ObjectPool<HitEffect>> m_hitEffectPools = new Dictionary<int, ObjectPool<HitEffect>>();
        private Dictionary<int, IShotEffect> m_shotEffects;     // 투사체 대신 그리는 연출 (ShotEffectTable: 소드 마스터 101, 블레이드 댄서 201)

        public int FlyingCount { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            m_pool = new ObjectPool<Projectile>(
                CreateProjectile,
                OnGetProjectile,
                OnReleaseProjectile,
                OnDestroyProjectile,
                true,
                m_defaultCapacity,
                m_maxSize);
            m_shotEffects = ShotEffectTable.Create(transform);
        }

        public void Fire(int lookId, Vector3 from, Vector3 to, float speed, float hold, float effectScale, Action onArrived)
        {
            if (m_shotEffects != null && m_shotEffects.TryGetValue(lookId, out IShotEffect shot))
            {
                FlyingCount++;
                shot.FireShot(from, to, speed, hold, effectScale * EffectSpace.CellWorldSize(), () =>
                {
                    FlyingCount--;
                    onArrived?.Invoke();
                });
                return;
            }

            if (!m_catalog.TryGet(lookId, out ProjectileCatalog.Entry look))
            {
                Debug.LogWarning($"[ProjectileManager] 모양 {lookId}가 표에 없어 탄 없이 바로 맞힘");
                onArrived?.Invoke();
                return;
            }

            Projectile projectile = m_pool.Get();
            projectile.transform.position = from;
            projectile.ApplyLook(look);
            FlyingCount++;

            projectile.FlyTo(to, speed, hold).OnComplete(() =>
            {
                FlyingCount--;
                m_pool.Release(projectile);
                PlayHitEffect(lookId, look, to, effectScale);
                onArrived?.Invoke();
            });
        }

        private void PlayHitEffect(int lookId, ProjectileCatalog.Entry look, Vector3 position, float effectScale)
        {
            if (look.HitEffect == null) return;

            if (!m_hitEffectPools.TryGetValue(lookId, out ObjectPool<HitEffect> pool))
            {
                HitEffect prefab = look.HitEffect;
                pool = new ObjectPool<HitEffect>(
                    () =>
                    {
                        HitEffect created = Instantiate(prefab, m_container);
                        created.gameObject.SetActive(false);
                        return created;
                    },
                    null,
                    effect => effect.gameObject.SetActive(false),
                    effect => Destroy(effect.gameObject));
                m_hitEffectPools[lookId] = pool;
            }

            HitEffect effect = pool.Get();
            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * look.HitEffectScale * effectScale;
            effect.gameObject.SetActive(true);
            effect.Play(pool.Release);
        }


        private Projectile CreateProjectile()
        {
            Projectile projectile = Instantiate(m_prefab, m_container);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private void OnGetProjectile(Projectile projectile) => projectile.gameObject.SetActive(true);

        private void OnReleaseProjectile(Projectile projectile) => projectile.gameObject.SetActive(false);

        private void OnDestroyProjectile(Projectile projectile) => Destroy(projectile.gameObject);
    }
}
