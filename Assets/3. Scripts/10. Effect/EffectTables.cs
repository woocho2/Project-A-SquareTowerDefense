using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 타워별 기본 공격·스킬 연출 표
    public interface IShotEffect
    {
        void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived);
    }

    public static class ShotEffectTable
    {
        public static Dictionary<int, IShotEffect> Create(Transform parent)
        {
            return new Dictionary<int, IShotEffect>
            {
                { 101, EffectSpace.Make<SwordSplashAttackEffect>(parent, "Aura Blade Basic Attack") },
                { 201, EffectSpace.Make<SwordTargetAttackEffect>(parent, "Crescent Assault Basic Attack") },
                { 102, EffectSpace.Make<AxeSplashAttackEffect>(parent, "Half Cleave Basic Attack") },
                { 202, EffectSpace.Make<AxeTargetAttackEffect>(parent, "Rage Tomahawk Basic Attack") },
                { 103, WeaponAttackEffect.Make(parent, WeaponAttackEffect.Attack.BurstShot) },
                { 203, EffectSpace.Make<ArrowTargetAttackEffect>(parent, "Rapid Shot Basic Attack") },
                { 104, EffectSpace.Make<SpearSplashAttackEffect>(parent, "Grand Thrust Basic Attack") },
                { 204, WeaponAttackEffect.Make(parent, WeaponAttackEffect.Attack.Javelin) },
                { 105, WeaponAttackEffect.Make(parent, WeaponAttackEffect.Attack.ShieldBash) },
                { 205, WeaponAttackEffect.Make(parent, WeaponAttackEffect.Attack.ShieldToss) },
                { 106, WeaponAttackEffect.Make(parent, WeaponAttackEffect.Attack.HeavySmash) },
                { 206, WeaponAttackEffect.Make(parent, WeaponAttackEffect.Attack.ThrowingHammer) },
                { 107, EffectSpace.Make<FireSplashAttackEffect>(parent, "Pyromancer Attack Effect") },
                { 111, EffectSpace.Make<IceSplashAttackEffect>(parent, "Sorcerer Attack Effect") },
                { 114, EffectSpace.Make<DarknessSplashAttackEffect>(parent, "Dark Mage Attack Effect") },
                { 207, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.Fireball) },
                { 108, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.AquaBlast) },
                { 208, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.AquaShot) },
                { 109, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.RockBlast) },
                { 209, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.MudShot) },
                { 110, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.Gust) },
                { 210, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.WindCutter) },
                { 211, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.Snowball) },
                { 112, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.Sparkling) },
                { 212, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.ShockBolt) },
                { 113, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.StarDust) },
                { 213, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.LaserBeam) },
                { 214, ElementalAttackEffect.Make(parent, ElementalAttackEffect.Attack.ShadowBullet) },
            };
        }
    }

    public static class SkillEffectTable
    {
        private static GameObject s_holder;
        private static SwordSplashSkillEffect s_swordSplash;
        private static SwordTargetSkillEffect s_swordTarget;
        private static AxeSplashSkillEffect s_axeSplash;
        private static ArrowSplashSkillEffect s_bowSplash;
        private static ArrowTargetSkillEffect s_bowTarget;
        private static FireSplashSkillEffect s_fireSplash;
        private static IceSplashSkillEffect s_iceSplash;
        private static DarknessSplashSkillEffect s_darknessSplash;
        private static SwordBuffSkillEffect s_swordBuff;
        private static FullSkillEffect s_full;

        private static bool CanPlay(TowerController owner) =>
            Application.isPlaying && owner != null && owner.State != null && owner.Definition != null &&
            (int)owner.Definition.Emblem >= 1 && (int)owner.Definition.Emblem <= 14;

        public static void Play(TowerController owner, Vector3? mainPosition, IReadOnlyList<Vector3> hitPositions)
        {
            if (!CanPlay(owner) || hitPositions == null || hitPositions.Count == 0) return;
            EnsureHolder();
            float cell = EffectSpace.CellWorldSize();
            Vector3 from = owner.transform.position;
            Vector3 center = mainPosition ?? hitPositions[0];
            switch ((owner.Definition.Emblem, owner.Definition.Color))
            {
                case (TowerEmblem.Sword, TowerColor.Red): s_swordSplash.Play(Camera.main, hitPositions, cell); break;
                case (TowerEmblem.Sword, TowerColor.Blue): s_swordTarget.Play(hitPositions[0], cell); break;
                case (TowerEmblem.Axe, TowerColor.Red): s_axeSplash.Play(Camera.main, from, hitPositions, owner.Stats.Range, cell); break;
                case (TowerEmblem.Bow, TowerColor.Blue): s_bowTarget.Play(from, hitPositions[0], cell); break;
                case (TowerEmblem.Fire, TowerColor.Red): s_fireSplash.Play(Camera.main, hitPositions, cell); break;
                case (TowerEmblem.Ice, TowerColor.Red): s_iceSplash.Play(Camera.main, hitPositions, cell); break;
                default: s_full.PlayAttack(owner, center, hitPositions); break;
            }
        }

        public static void PlaySupportSkill(TowerController owner, IReadOnlyList<TowerController> receivers, bool preview = false)
        {
            if (!CanPlay(owner)) return;
            EnsureHolder();
            var points = new List<Vector3>();
            if (receivers != null) foreach (var receiver in receivers)
                if (receiver != null) points.Add(receiver.transform.position);
            if (owner.Definition.Color == TowerColor.Yellow && owner.Definition.Emblem == TowerEmblem.Sword)
                s_swordBuff.Play(Camera.main, owner.transform.position, points, EffectSpace.CellWorldSize(), preview);
            else if (owner.Definition.Color == TowerColor.Yellow) s_full.PlayBuff(owner, points);
        }

        public static void PlayDebuffSkill(TowerController owner, IReadOnlyList<Vector3> targets)
        {
            if (!CanPlay(owner)) return;
            EnsureHolder();
            s_full.PlayDebuff(owner, targets);
        }
        public static void PlaySupportContact(TowerController owner, Vector3 point, bool debuff)
        {
            if (!CanPlay(owner)) return;
            EnsureHolder();
            s_full.SupportContact(owner.Definition.Emblem, point, debuff);
        }

        // CastInstead skills have no ordinary hit list. Their target-specific visuals are called at the real application/tick.
        public static void PlayCast(TowerController owner)
        {
            if (!CanPlay(owner)) return;
            EnsureHolder();
            switch ((owner.Definition.Emblem, owner.Definition.Color))
            {
                case (TowerEmblem.Bow, TowerColor.Red):
                    s_bowSplash.PlayVolley(Camera.main, owner.transform.position, EffectSpace.CellWorldSize()); break;
                case (TowerEmblem.Darkness, TowerColor.Red): s_darknessSplash.Sync(); break;
                case (TowerEmblem.Axe, TowerColor.Blue): s_full.Rage(owner); break;
                case (TowerEmblem.Fire, TowerColor.Blue): s_full.Charge(owner); break;
            }
        }
        public static void PlayRally(TowerController owner, IReadOnlyList<TowerController> receivers)
        {
            if (!CanPlay(owner)) return; EnsureHolder(); s_full.Rally(owner, receivers);
        }
        public static void PlayPrism(TowerController owner, IReadOnlyList<TowerController> receivers)
        {
            if (!CanPlay(owner)) return; EnsureHolder(); s_full.Prism(owner, receivers);
        }
        public static void PlayHeadhunt(TowerController owner, IReadOnlyList<Vector3> path)
        {
            if (!CanPlay(owner)) return; EnsureHolder(); s_full.Headhunt(owner, path);
        }
        public static void PlayRebound(TowerController owner, IReadOnlyList<Vector3> path)
        {
            if (!CanPlay(owner)) return; EnsureHolder(); s_full.Rebound(owner, path);
        }
        public static void PlayBreath(TowerController owner, ITarget target)
        {
            if (!CanPlay(owner) || target == null || target.Transform == null) return;
            EnsureHolder(); s_full.Breath(owner, target.Transform.position, target.Transform);
        }
        public static void PlayPrison(TowerController owner, EnemyController target, bool locked, bool opening)
        {
            if (!CanPlay(owner) || target == null) return; EnsureHolder(); s_full.Prison(owner, target, locked, opening);
        }
        public static void PlayHyperion(TowerController owner, EnemyController target, int stage, bool opening)
        {
            if (!CanPlay(owner) || target == null) return; EnsureHolder(); s_full.Hyperion(owner, target, stage, opening);
        }
        public static void PlayTempest(TowerController owner)
        {
            if (!CanPlay(owner)) return; EnsureHolder(); s_full.Tempest(owner);
        }
        public static void PlayBlackHoleTurn(BlackHoleGround ground, IReadOnlyList<Vector3> hitPositions)
        {
            if (!Application.isPlaying) return;
            EnsureHolder(); s_darknessSplash.PlayTurn(ground, hitPositions);
        }
        public static void PlayArrowRain(TowerController owner, IReadOnlyList<Vector3> hitPositions)
        {
            if (!CanPlay(owner)) return;
            EnsureHolder(); s_bowSplash.PlayRain(Camera.main, hitPositions, EffectSpace.CellWorldSize());
        }

        // Developer preview dispatches visuals only. No skill is fired, no status or ground effect is created.
        public static void PlayPreview(TowerController owner, IReadOnlyList<Vector3> hits)
        {
            if (!CanPlay(owner)) return;
            EnsureHolder();
            float cell = EffectSpace.CellWorldSize();
            var points = new List<Vector3>();
            if (hits != null) foreach (Vector3 hit in hits) points.Add(hit);
            if (points.Count == 0) points.Add(owner.transform.position + Vector3.right * cell * 2);
            if (owner.Definition.Color == TowerColor.Yellow) { PlaySupportSkill(owner, Allies(), true); return; }
            if (owner.Definition.Color == TowerColor.Purple) { PlayDebuffSkill(owner, points); return; }
            switch ((owner.Definition.Emblem, owner.Definition.Color))
            {
                case (TowerEmblem.Axe, TowerColor.Blue): s_full.Rage(owner, true); return;
                case (TowerEmblem.Spear, TowerColor.Red): s_full.Rally(owner, Allies(owner)); return;
                case (TowerEmblem.Light, TowerColor.Red): s_full.Prism(owner, Allies()); return;
                case (TowerEmblem.Spear, TowerColor.Blue):
                case (TowerEmblem.Shield, TowerColor.Blue):
                    var route = new List<Vector3> { points[0] };
                    int count = Mathf.Max(1, SkillFormula.EffectDuration(owner));
                    for (int i = 0; i < count; i++) route.Add(points[(i + 1) % points.Count]);
                    if (owner.Definition.Emblem == TowerEmblem.Spear) s_full.Headhunt(owner, route);
                    else s_full.Rebound(owner, route);
                    return;
                case (TowerEmblem.Fire, TowerColor.Blue): s_full.Breath(owner, points[0]); return;
                case (TowerEmblem.Wind, TowerColor.Blue): s_full.Tempest(owner, true); return;
                case (TowerEmblem.Water, TowerColor.Blue):
                case (TowerEmblem.Light, TowerColor.Blue):
                case (TowerEmblem.Darkness, TowerColor.Red): s_full.PreviewDelayed(owner, points[0]); return;
                case (TowerEmblem.Bow, TowerColor.Red):
                    s_bowSplash.PlayVolley(Camera.main, owner.transform.position, cell);
                    s_bowSplash.PlayRain(Camera.main, points, cell);
                    return;
                default: Play(owner, points[0], points); return;
            }
        }
        private static List<TowerController> Allies(TowerController rangedOwner = null)
        {
            var result = new List<TowerController>();
            var board = BoardManager.Instance;
            if (board == null || board.Slots == null) { if (rangedOwner != null) result.Add(rangedOwner); return result; }
            for (int i = 0; i < board.Slots.Count; i++)
            {
                var tower = board.Slots.Get(i);
                if (tower == null || !tower.IsAttackTower) continue;
                if (rangedOwner != null && BoardGrid.GetTileDistance(board.Grid.GetSpawnCell(rangedOwner.SlotIndex),
                    board.Grid.GetSpawnCell(i)) > rangedOwner.Stats.Range) continue;
                result.Add(tower);
            }
            return result;
        }
        private static void EnsureHolder()
        {
            if (s_holder != null) return;
            s_holder = new GameObject("Skill Effects");
            s_swordSplash = EffectSpace.Make<SwordSplashSkillEffect>(s_holder.transform, "Void Slash");
            s_swordTarget = EffectSpace.Make<SwordTargetSkillEffect>(s_holder.transform, "Blade Storm");
            s_axeSplash = EffectSpace.Make<AxeSplashSkillEffect>(s_holder.transform, "Whirlwind");
            s_bowSplash = EffectSpace.Make<ArrowSplashSkillEffect>(s_holder.transform, "Arrow Rain");
            s_bowTarget = EffectSpace.Make<ArrowTargetSkillEffect>(s_holder.transform, "Heavy Bolt");
            s_fireSplash = EffectSpace.Make<FireSplashSkillEffect>(s_holder.transform, "Meteor");
            s_iceSplash = EffectSpace.Make<IceSplashSkillEffect>(s_holder.transform, "Blizzard");
            s_darknessSplash = EffectSpace.Make<DarknessSplashSkillEffect>(s_holder.transform, "Black Hole");
            s_swordBuff = EffectSpace.Make<SwordBuffSkillEffect>(s_holder.transform, "Glory Sword");
            s_full = EffectSpace.Make<FullSkillEffect>(s_holder.transform, "Named Skill Compositions");
        }
    }

    public static class EffectSpace
    {
        public static T Make<T>(Transform parent, string name) where T : Component
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.AddComponent<T>();
        }

        public static float CellWorldSize()
        {
            BoardManager board = BoardManager.Instance;
            if (board == null || board.Grid == null || board.Grid.PathCount == 0) return 1f;
            Vector3Int cell = board.Grid.GetPathCell(0);
            return (board.GetWorldPosition(cell + Vector3Int.right) - board.GetWorldPosition(cell)).magnitude;
        }
    }
}
