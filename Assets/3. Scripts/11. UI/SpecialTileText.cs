namespace SquareTD
{
    // 특수 타일의 이름과 설명 (타일 정보 패널, 적 정보 패널이 같이 씀). 수치는 20번 2장
    public static class SpecialTileText
    {
        public static string Describe(PathTileType type, int pathIndex, BoardGrid grid)
        {
            switch (type)
            {
                case PathTileType.Defense:      return "방어 타일\n멈춰 있는 동안 방어력 +20%";
                case PathTileType.Speed:        return "이동력 타일\n멈춰 있는 동안 이동력의 최소와 최대 +2";
                case PathTileType.ActionCount:  return $"행동 카운트 타일\n멈춰 있는 동안 매 턴 행동 카운트 +{ActionCountTileEffect.Amount}";
                case PathTileType.Heal:         return $"회복 타일\n멈춰 있는 동안 매 턴 최대 체력의 {HealTileEffect.Rate * 100f:0}% 회복";
                case PathTileType.Purify:       return "정화 타일\n멈춘 적의 모든 디버프를 지움";
                case PathTileType.Bifrost:      return $"비프로스트\n멈춘 적은 맞은편 {grid.GetBifrostPartner(pathIndex)}번 칸으로 건너감";
                default:                        return null;
            }
        }

        public static string Describe(SpawnTileType type)
        {
            switch (type)
            {
                case SpawnTileType.Power:       return "공격력 타일\n공격 타워의 공격력 +30%";
                case SpawnTileType.ActionCount: return "행동 카운트 타일\n공격 타워가 매 턴 행동 카운트 +1";
                case SpawnTileType.AttackCount: return "공격 횟수 타일\n공격 타워의 공격 횟수 +1";
                case SpawnTileType.Mastery:     return "마스터리 타일\n공격 타워의 Mastery +1";
                default:                        return null;
            }
        }

        // 적이 "서 있는 동안" 받는 타일만 (정화·비프로스트는 멈춘 순간이라 버프 목록에 넣지 않음)
        public static bool IsStandingBuff(PathTileType type) =>
            type == PathTileType.Defense || type == PathTileType.Speed || type == PathTileType.ActionCount || type == PathTileType.Heal;
    }
}
