using System.Collections.Generic;

namespace SquareTD
{
    // 물 단일 — 급류 (20번 4-10장)
    //   그 적에게 Power × AbilityValue (스택을 쓰면 ×), 피해를 준 뒤 밀어냄: 그 적의 다음 행동은 주사위 없이 −1칸 (뒤로 한 칸)
    //   젖음·침수인 적은 −2칸. 젖음인지는 급류를 맞은 때를 기준으로
    //   밀어내기는 (100 − 저항력)% 확률. 다시 맞아도 겹치지 않음 (다음 행동 한 번만)
    public class WaterTargetSkill : AttackSkill
    {
        public WaterTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            EnemyController enemy = target.Body;
            bool isWet = EmblemDebuffEffect.StageOn(target, TowerEmblem.Water) != ZoneStage.None;
            bool pushes = enemy != null && enemy.IsAlive && Chance.Roll(enemy.Resistance.ResistWeak(1f));

            DamageInfo torrent = hit;
            torrent.ChangesMovement = pushes;
            // [B] 피해 주기 — HitSingle 한 줄 (넘길 것은 바로 위에서 만든 torrent)

            if (pushes && enemy.IsAlive) enemy.Status.Add(new ForcedMoveEffect(isWet ? -2 : -1));
        }
    }
}
