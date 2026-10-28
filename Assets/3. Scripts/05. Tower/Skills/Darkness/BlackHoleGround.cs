namespace SquareTD
{
    // 블랙홀 (21번 #116, 20번 4-16장): 정한 적의 칸 위에 생겨 (컬러 강화 레벨 × 2)턴 남음. 크기는 Mastery (1 → 1×1, 2 → 3×3, 3 → 5×5)
    //   피해: ⑤마다 그 구역의 적에게 소환한 때 정해 둔 값 (화살비처럼). 블랙홀이 막타를 치면 카운트 +5 (EmblemSharedState)
    //   끌어당김: 맵 전체. 블랙홀의 길 칸 번호를 기준으로 (고리를 돌아 가까운 쪽을 따지지 않음)
    //                       번호가 낮은 적     번호가 높은 적
    //     보통 적            이동력 +2          이동력 +3, 60%로 뒤로
    //     잠식인 적          이동력 +3          이동력 +4, 80%로 뒤로
    //     침식인 적          이동력 +4          이동력 +5, 100%로 뒤로
    //   뒤로 가는 확률은 저항력만큼 줄어듦. 이동력 증가는 적에게 주는 것이라 저항과 무관
    public class BlackHoleGround : GroundEffect
    {
        private readonly BoardGrid m_grid;
        private readonly PathOccupancy m_occupancy;
        private readonly int m_center;
        private readonly int m_radius;

        public float Damage { get; }

        public BlackHoleGround(TowerController owner, BoardGrid grid, PathOccupancy occupancy, int centerPathIndex, int mastery, float damage, int turns)
            : base(owner, TowerEmblem.Darkness, turns)
        {
            m_grid = grid;
            m_occupancy = occupancy;
            m_center = centerPathIndex;
            m_radius = SplashAction.GetAreaRadius(mastery);
            Damage = damage;
        }

        public override int PullCenter => m_center;

        public override bool Covers(int pathIndex) =>
            BoardGrid.GetTileDistance(m_grid.GetPathCell(m_center), m_grid.GetPathCell(pathIndex)) <= m_radius;

        public override void OnTurn()
        {
            foreach (ITarget target in SkillArea.Impact(m_grid, m_occupancy, m_center, m_radius + 1))
            {
                if (!target.IsAlive) continue;
                ProjectileHitResolver.HitSingle(target, new DamageInfo
                {
                    Attacker = Owner != null ? Owner : null,
                    Source = DamageSource.Ground,
                    Emblem = TowerEmblem.Darkness,
                    BaseDamage = Damage,
                });
            }
        }

        // (낮은 쪽 +, 높은 쪽 +, 뒤로 갈 확률)
        public static void GetPull(ZoneStage darkness, out int lower, out int higher, out float backChance)
        {
            switch (darkness)
            {
                case ZoneStage.Skill2: lower = 4; higher = 5; backChance = 1f; break;
                case ZoneStage.Skill1: lower = 3; higher = 4; backChance = 0.8f; break;
                default: lower = 2; higher = 3; backChance = 0.6f; break;
            }
        }

        public override MoveRange ModifyMove(EnemyController enemy, MoveRange range)
        {
            if (enemy.PathIndex == m_center) return range;

            GetPull(EmblemDebuffEffect.StageOn(enemy, TowerEmblem.Darkness), out int lower, out int higher, out float backChance);

            if (enemy.PathIndex < m_center) return new MoveRange(range.Min + lower, range.Max + lower, range.Action);

            var faster = new MoveRange(range.Min + higher, range.Max + higher, range.Action);
            if (!Chance.Roll(enemy.Resistance.ResistWeak(backChance))) return faster;
            return new MoveRange(-faster.Max, -faster.Min, faster.Action);      // 뒤로 (하한 없음)
        }

        public override string Describe() => $"블랙홀: 매 턴 {Damage:0.##} 피해, 맵 전체를 {m_center}번 칸으로 끌어당김, {RemainingTurns}턴 남음";
    }
}
