using NUnit.Framework;

/// <summary>
/// 게임 규칙 중 "계산만 하는 부분"을 자동으로 확인하는 테스트입니다.
/// 실행: Unity 메뉴 Window > General > Test Runner > EditMode 탭 > Run All.
/// 규칙의 출처는 「27. 기획 통합.txt」입니다. 규칙을 바꾸면 여기의 기대값도 함께 바꿉니다.
/// </summary>
public class GameRuleTests
{
    // ------------------------------------------------------------------
    // 버프·디버프 스킬 2의 문턱 = 60 ÷ 컬러 강화 레벨 (27번 4-1장)
    // ------------------------------------------------------------------

    [TestCase(1, 60)]
    [TestCase(2, 30)]
    [TestCase(3, 20)]
    [TestCase(4, 15)]
    [TestCase(5, 12)]
    public void 스킬2_문턱은_60을_컬러강화_레벨로_나눈_값(int level, int expected)
    {
        Assert.AreEqual(expected, TowerAttackAction.GetStackSkillThreshold(level));
    }

    [TestCase(0, 60)]
    [TestCase(-3, 60)]
    [TestCase(6, 12)]
    [TestCase(99, 12)]
    public void 스킬2_문턱은_레벨이_범위를_벗어나면_1과_5로_맞춘다(int level, int expected)
    {
        Assert.AreEqual(expected, TowerAttackAction.GetStackSkillThreshold(level));
    }

    // ------------------------------------------------------------------
    // 사거리: 월드 1.5 = 1칸 (27번 2장, 4장)
    // ------------------------------------------------------------------

    [TestCase(1.5f, 1)]
    [TestCase(3.0f, 2)]
    [TestCase(4.5f, 3)]
    [TestCase(15f, 10)]
    public void 사거리는_월드_1점5마다_1칸(float worldRange, int expectedTiles)
    {
        Assert.AreEqual(expectedTiles, TowerAttackAction.ToTileRange(worldRange));
    }

    [Test]
    public void 사거리는_최소_1칸()
    {
        Assert.AreEqual(1, TowerAttackAction.ToTileRange(0f));
        Assert.AreEqual(1, TowerAttackAction.ToTileRange(0.2f));
    }

    [TestCase(1, 1.5f)]
    [TestCase(2, 3.0f)]
    [TestCase(5, 7.5f)]
    public void 칸_수를_월드_거리로_바꾸면_1점5배(int tiles, float expectedWorld)
    {
        Assert.AreEqual(expectedWorld, TowerAttackAction.ToWorldRange(tiles), 0.0001f);
    }

    // ------------------------------------------------------------------
    // 처치 보상: 적이 소환된 웨이브 기준 (27번 8장)
    // ------------------------------------------------------------------

    [TestCase(1, 10)]
    [TestCase(11, 110)]
    [TestCase(40, 400)]
    public void 일반_적은_웨이브당_10골드이고_젬은_없다(int wave, int expectedGold)
    {
        WaveManager.GetKillReward(EnemyType.Normal, wave, out int gold, out int gem);

        Assert.AreEqual(expectedGold, gold);
        Assert.AreEqual(0, gem);
    }

    [Test]
    public void 속도형과_방어형도_일반_적과_같은_보상()
    {
        WaveManager.GetKillReward(EnemyType.Speed, 7, out int speedGold, out int speedGem);
        WaveManager.GetKillReward(EnemyType.Depend, 7, out int defendGold, out int defendGem);

        Assert.AreEqual(70, speedGold);
        Assert.AreEqual(70, defendGold);
        Assert.AreEqual(0, speedGem);
        Assert.AreEqual(0, defendGem);
    }

    [TestCase(10, 1000, 20)]
    [TestCase(20, 4000, 80)]
    [TestCase(30, 9000, 180)]
    [TestCase(40, 16000, 320)]
    public void 보스는_웨이브와_사이클에_비례한_골드와_젬(int wave, int expectedGold, int expectedGem)
    {
        WaveManager.GetKillReward(EnemyType.Boss, wave, out int gold, out int gem);

        Assert.AreEqual(expectedGold, gold);
        Assert.AreEqual(expectedGem, gem);
    }

    // 중간 보스는 실제 소환 웨이브(5~7)와 무관하게 기준 웨이브(5 / 15 / 25 / 35)로 계산합니다.
    [TestCase(5, 250, 5)]
    [TestCase(6, 250, 5)]
    [TestCase(7, 250, 5)]
    [TestCase(15, 1500, 30)]
    [TestCase(17, 1500, 30)]
    [TestCase(25, 3750, 75)]
    [TestCase(35, 7000, 140)]
    public void 중간_보스는_기준_웨이브로_보상을_계산한다(int wave, int expectedGold, int expectedGem)
    {
        WaveManager.GetKillReward(EnemyType.MiddleBoss, wave, out int gold, out int gem);

        Assert.AreEqual(expectedGold, gold);
        Assert.AreEqual(expectedGem, gem);
    }

    [Test]
    public void 웨이브가_범위를_벗어나면_1과_40으로_맞춘다()
    {
        WaveManager.GetKillReward(EnemyType.Normal, 0, out int lowGold, out _);
        WaveManager.GetKillReward(EnemyType.Normal, 99, out int highGold, out _);

        Assert.AreEqual(10, lowGold);
        Assert.AreEqual(400, highGold);
    }
}
