using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 기본 공격 한 발을 그리는 연출 (투사체 대신). 도착하는 순간 onArrived를 부름 → 그때 피해가 들어감
    public interface IShotEffect
    {
        void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived);
    }

    // 연출을 부를 자리 ① 기본 공격: 모양 번호(lookId = 타워 ID % 1000) → 연출
    //   ProjectileManager가 이 표에 있는 번호면 투사체 대신 이 연출로 쏨. 없는 번호는 ProjectileCatalog의 투사체·명중 이펙트
    public static class ShotEffectTable
    {
        public static Dictionary<int, IShotEffect> Create(Transform parent)
        {
            return new Dictionary<int, IShotEffect>
            {
                { 101, EffectSpace.Make<SwordSplashAttackEffect>(parent, "Sword Saint Attack Effect") },   // 검성 (광역 검)
                { 201, EffectSpace.Make<SwordTargetAttackEffect>(parent, "Swordsman Attack Effect") },     // 검객 (단일 검)
                { 102, EffectSpace.Make<AxeSplashAttackEffect>(parent, "Viking Attack Effect") },          // 바이킹 (광역 도끼)
                { 202, EffectSpace.Make<AxeTargetAttackEffect>(parent, "Berserker Attack Effect") },       // 버서커 (단일 도끼)
                { 103, EffectSpace.Make<ArrowSplashAttackEffect>(parent, "Ranger Attack Effect") },        // 레인저 (광역 활)
                { 203, EffectSpace.Make<ArrowTargetAttackEffect>(parent, "Hunter Attack Effect") },        // 헌터 (단일 활)
                { 104, EffectSpace.Make<SpearSplashAttackEffect>(parent, "Warlord Attack Effect") },       // 워로드 (광역 창)
                { 204, EffectSpace.Make<SpearTargetAttackEffect>(parent, "Lancer Attack Effect") },        // 창술사 (단일 창)
            };
        }
    }

    // 연출을 부를 자리 ② 스킬 2: (문양, 컬러) → 연출. AttackSkill.Fire가 맞힌 뒤 부름. 표에 없으면 연출 없음
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

        // owner: 쏜 타워 / mainPosition: 스킬을 일으킨 대상의 위치 (광역은 Duration번째 착탄의 대상, 없으면 null)
        // hitPositions: 맞은 대상들의 위치 (피해가 들어가기 전에 모아 둔 것)
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
                    s_axeSplash.Play(from, center, SplashAction.GetAreaSide(owner.Stats.Mastery) * cell, cell);   // 바이킹 강타
                    break;
                case (TowerEmblem.Sword, TowerColor.Red):
                    s_swordSplash.Play(Camera.main, from, hitPositions, cell);                         // 검성 천지일섬
                    break;
                case (TowerEmblem.Sword, TowerColor.Blue):
                    s_swordTarget.Play(hitPositions[0], cell);                                       // 검객 이십사수매화검법
                    break;
                case (TowerEmblem.Axe, TowerColor.Blue):
                    s_axeTarget.Play(from, hitPositions[0], cell);                                   // 버서커 쪼개기
                    break;
                case (TowerEmblem.Bow, TowerColor.Red):
                    s_bowSplash.Play(Camera.main, from, hitPositions, cell);                         // 레인저 화살비 (맵 전체)
                    break;
                case (TowerEmblem.Bow, TowerColor.Blue):
                    s_bowTarget.Play(from, hitPositions[0], cell);                                   // 헌터 은화살
                    break;
                case (TowerEmblem.Spear, TowerColor.Red):
                    s_spearSplash.Play(Camera.main, from, center, hitPositions, cell);               // 워로드 선두 지휘
                    break;
                case (TowerEmblem.Spear, TowerColor.Blue):
                    s_spearTarget.Play(from, hitPositions[0], cell);                                 // 창술사 뽑아찢기
                    break;
            }
        }

        private static void EnsureHolder()
        {
            if (s_holder != null) return;                       // 씬을 다시 불러오면 지워졌으므로 새로 만듦
            s_holder = new GameObject("Skill Effects");
            s_swordSplash = EffectSpace.Make<SwordSplashSkillEffect>(s_holder.transform, "Sword Saint Skill Effect");
            s_swordTarget = EffectSpace.Make<SwordTargetSkillEffect>(s_holder.transform, "Swordsman Skill Effect");
            s_axeTarget = EffectSpace.Make<AxeTargetSkillEffect>(s_holder.transform, "Berserker Skill Effect");
            s_axeSplash = EffectSpace.Make<AxeSplashSkillEffect>(s_holder.transform, "Viking Skill Effect");
            s_bowSplash = EffectSpace.Make<ArrowSplashSkillEffect>(s_holder.transform, "Ranger Skill Effect");
            s_bowTarget = EffectSpace.Make<ArrowTargetSkillEffect>(s_holder.transform, "Hunter Skill Effect");
            s_spearSplash = EffectSpace.Make<SpearSplashSkillEffect>(s_holder.transform, "Warlord Skill Effect");
            s_spearTarget = EffectSpace.Make<SpearTargetSkillEffect>(s_holder.transform, "Lancer Skill Effect");
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

        // 칸 한 변의 월드 길이 (판이 없으면 1)
        public static float CellWorldSize()
        {
            BoardManager board = BoardManager.Instance;
            if (board == null || board.Grid == null || board.Grid.PathCount == 0) return 1f;
            Vector3Int cell = board.Grid.GetPathCell(0);
            return (board.GetWorldPosition(cell + Vector3Int.right) - board.GetWorldPosition(cell)).magnitude;
        }
    }
}
