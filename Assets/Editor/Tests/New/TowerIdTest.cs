using NUnit.Framework;
using SquareTD;

public class TowerIdTest
{
    [Test]
    public void GetTier_2107은_2()
    {
        Assert.AreEqual(2, TowerId.GetTier(2107));
    }

    [Test]
    public void GetColor_4312는_3()
    {
        Assert.AreEqual(TowerColor.Yellow, TowerId.GetColor(4312));
    }

    [Test]
    public void GetEmblem_5305는_05()
    {
        Assert.AreEqual(SquareTD.TowerEmblem.Shield, TowerId.GetEmblem(5305));
    }

    [Test]
    public void Make_2_Red_Fire는_2107()
    {
        Assert.AreEqual(2107, TowerId.Make(2,TowerColor.Red,SquareTD.TowerEmblem.Fire));
    }

    [Test]
    public void GetNextTier_2107은_3107()
    {
        Assert.AreEqual(3107, TowerId.GetNextTier(2107));
    }

    [Test]
    public void IsSynergy_6014는_true()
    {
        Assert.AreEqual(true, TowerId.IsSynergy(6014));
    }

    [Test]
    public void IsSynergy_2107는_false()
    {
        Assert.AreEqual(false, TowerId.IsSynergy(2107));
    }

    [Test]
    public void GetAttribute_Axe는_Slash()
    {
        Assert.AreEqual(AttackAttribute.Slash, TowerId.GetAttribute(SquareTD.TowerEmblem.Axe));
    }

    [Test]
    public void GetAttribute_Fire는_None()
    {
        Assert.AreEqual(AttackAttribute.None, TowerId.GetAttribute(SquareTD.TowerEmblem.Fire));
    }
}