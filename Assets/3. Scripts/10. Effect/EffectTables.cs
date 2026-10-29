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
                { 101, EffectSpace.Make<SwordSplashAttackEffect>(parent, "Sword Saint Attack Effect") },
                { 201, EffectSpace.Make<SwordTargetAttackEffect>(parent, "Swordsman Attack Effect") },
                { 102, EffectSpace.Make<AxeSplashAttackEffect>(parent, "Viking Attack Effect") },
                { 202, EffectSpace.Make<AxeTargetAttackEffect>(parent, "Berserker Attack Effect") },
                { 103, EffectSpace.Make<ArrowSplashAttackEffect>(parent, "Ranger Attack Effect") },
                { 203, EffectSpace.Make<ArrowTargetAttackEffect>(parent, "Hunter Attack Effect") },
                { 104, EffectSpace.Make<SpearSplashAttackEffect>(parent, "Warlord Attack Effect") },
                { 204, EffectSpace.Make<SpearTargetAttackEffect>(parent, "Lancer Attack Effect") },
                { 107, EffectSpace.Make<FireSplashAttackEffect>(parent, "Pyromancer Attack Effect") },
                { 111, EffectSpace.Make<IceSplashAttackEffect>(parent, "Sorcerer Attack Effect") },
                { 114, EffectSpace.Make<DarknessSplashAttackEffect>(parent, "Dark Mage Attack Effect") },
            };
        }
    }

    public static class SkillEffectTable
    {
        private static GameObject s_holder;
        private static SwordSplashSkillEffect s_swordSplash;
        private static SwordTargetSkillEffect s_swordTarget;
        private static AxeTargetSkillEffect s_axeTarget;
        private static AxeSplashSkillEffect s_axeSplash;
        private static ArrowSplashSkillEffect s_bowSplash;
        private static ArrowTargetSkillEffect s_bowTarget;
        private static SpearSplashSkillEffect s_spearSplash;
        private static SpearTargetSkillEffect s_spearTarget;
        private static FireSplashSkillEffect s_fireSplash;
        private static IceSplashSkillEffect s_iceSplash;
        private static DarknessSplashSkillEffect s_darknessSplash;

        public static void Play(TowerController owner, Vector3? mainPosition, IReadOnlyList<Vector3> hitPositions)
        {
            if (!Application.isPlaying || owner == null || hitPositions == null || hitPositions.Count == 0) return;

            EnsureHolder();
            float cell = EffectSpace.CellWorldSize();
            Vector3 from = owner.transform.position;
            Vector3 center = mainPosition ?? hitPositions[0];
            switch ((owner.Definition.Emblem, owner.Definition.Color))
            {
                case (TowerEmblem.Axe, TowerColor.Red):
                    s_axeSplash.Play(from, from, (owner.Stats.Range * 2 + 1) * cell, cell);
                    break;
                case (TowerEmblem.Sword, TowerColor.Red):
                    s_swordSplash.Play(Camera.main, from, hitPositions, cell, null);
                    break;
                case (TowerEmblem.Sword, TowerColor.Blue):
                    s_swordTarget.Play(hitPositions[0], cell);
                    break;
                case (TowerEmblem.Axe, TowerColor.Blue):
                    s_axeTarget.Play(from, hitPositions[0], cell);
                    break;
                case (TowerEmblem.Bow, TowerColor.Blue):
                    s_bowTarget.Play(from, hitPositions[0], cell);
                    break;
                case (TowerEmblem.Spear, TowerColor.Red):
                    s_spearSplash.Play(Camera.main, from, center, hitPositions, cell);
                    break;
                case (TowerEmblem.Spear, TowerColor.Blue):
                    s_spearTarget.Play(from, hitPositions[0], cell);
                    break;
                case (TowerEmblem.Fire, TowerColor.Red):
                    s_fireSplash.Play(Camera.main, hitPositions, cell);
                    break;
                case (TowerEmblem.Ice, TowerColor.Red):
                    s_iceSplash.Play(Camera.main, hitPositions, cell);
                    break;
            }
        }

        public static void PlayCast(TowerController owner)
        {
            if (!Application.isPlaying || owner == null) return;

            EnsureHolder();
            float cell = EffectSpace.CellWorldSize();
            switch ((owner.Definition.Emblem, owner.Definition.Color))
            {
                case (TowerEmblem.Bow, TowerColor.Red):
                    s_bowSplash.PlayVolley(Camera.main, owner.transform.position, cell);
                    break;
                case (TowerEmblem.Darkness, TowerColor.Red):
                    s_darknessSplash.Sync();
                    break;
            }
        }

        public static void PlayBlackHoleTurn(BlackHoleGround ground, IReadOnlyList<Vector3> hitPositions)
        {
            if (!Application.isPlaying) return;

            EnsureHolder();
            s_darknessSplash.PlayTurn(ground, hitPositions);
        }

        public static void PlayArrowRain(TowerController owner, IReadOnlyList<Vector3> hitPositions)
        {
            if (!Application.isPlaying) return;

            EnsureHolder();
            s_bowSplash.PlayRain(Camera.main, hitPositions, EffectSpace.CellWorldSize());
        }

        private static void EnsureHolder()
        {
            if (s_holder != null) return;
            s_holder = new GameObject("Skill Effects");
            s_swordSplash = EffectSpace.Make<SwordSplashSkillEffect>(s_holder.transform, "Sword Saint Skill Effect");
            s_swordTarget = EffectSpace.Make<SwordTargetSkillEffect>(s_holder.transform, "Swordsman Skill Effect");
            s_axeTarget = EffectSpace.Make<AxeTargetSkillEffect>(s_holder.transform, "Berserker Skill Effect");
            s_axeSplash = EffectSpace.Make<AxeSplashSkillEffect>(s_holder.transform, "Viking Skill Effect");
            s_bowSplash = EffectSpace.Make<ArrowSplashSkillEffect>(s_holder.transform, "Ranger Skill Effect");
            s_bowTarget = EffectSpace.Make<ArrowTargetSkillEffect>(s_holder.transform, "Hunter Skill Effect");
            s_spearSplash = EffectSpace.Make<SpearSplashSkillEffect>(s_holder.transform, "Warlord Skill Effect");
            s_spearTarget = EffectSpace.Make<SpearTargetSkillEffect>(s_holder.transform, "Lancer Skill Effect");
            s_fireSplash = EffectSpace.Make<FireSplashSkillEffect>(s_holder.transform, "Meteor Skill Effect");
            s_iceSplash = EffectSpace.Make<IceSplashSkillEffect>(s_holder.transform, "Blizzard Skill Effect");
            s_darknessSplash = EffectSpace.Make<DarknessSplashSkillEffect>(s_holder.transform, "Black Hole Skill Effect");
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
