namespace SquareTD
{
    // 타워 ID(티어·컬러·문양 네 자리)를 읽고 만드는 도구
    public static class TowerId
    {
        public static int GetTier(int id)
        {
            return id / 1000;
        }

        public static TowerColor GetColor(int id)
        {
            return (TowerColor)(id / 100 % 10);
        }

        public static TowerEmblem GetEmblem(int id)
        {
            return (TowerEmblem)(id % 100);
        }

        public static int Make(int tier, TowerColor color, TowerEmblem emblem)
        {
            return tier * 1000 + (int)color * 100 + (int)emblem;
        }

        public static int GetNextTier(int id)
        {
            return id + 1000;
        }

        public static bool IsSynergy(int id)
        {
            return GetTier(id) == 6;
        }

        public static AttackAttribute GetAttribute(TowerEmblem emblem)
        {
            switch (emblem)
            {
                case TowerEmblem.Sword:
                case TowerEmblem.Axe:
                    return AttackAttribute.Slash;

                case TowerEmblem.Bow:
                case TowerEmblem.Spear:
                    return AttackAttribute.Pierce;

                case TowerEmblem.Shield:
                case TowerEmblem.Hammer:
                    return AttackAttribute.Blunt;

                default:
                    return AttackAttribute.None;
            }
        }
    }
}
