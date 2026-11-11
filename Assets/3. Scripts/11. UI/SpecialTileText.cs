namespace SquareTD
{
    // 특수 타일의 이름과 설명 문구
    public static class SpecialTileText
    {
        public static string Describe(PathTileType type, int pathIndex, BoardGrid grid)
        {
            switch (type)
            {
                case PathTileType.Purify:       return "정화 타일\n멈춘 적의 모든 디버프를 지움";
                case PathTileType.Bifrost:      return $"비프로스트\n멈춘 적은 맞은편 {grid.GetBifrostPartner(pathIndex)}번 칸으로 건너감";
                default:                        return null;
            }
        }

    }
}
