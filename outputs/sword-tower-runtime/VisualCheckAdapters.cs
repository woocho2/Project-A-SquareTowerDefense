// The isolated visual harness compiles the real TowerData and TowerVisual.
// Attack/stat dependencies are adapters only; full project compilation is checked separately.
using UnityEngine.Serialization;
[System.Serializable]
public struct TowerStats
{
    public int ID; public string Name; public int Level; public float AttackPower; public float Range;
    [FormerlySerializedAs("AttackSpeed")] public int AttackCount;
    public float CriticalRate; public float CriticalDamage; public float Duration; public float AbilityValue;
    public float ProjectileSpeed; public int ProjectileRadius; public int AdditionalHitCount;
}
public static class TowerAttackAction
{
    public static float ToWorldRange(float range)=>range*1.5f;
}
public class DebuffZone : UnityEngine.MonoBehaviour { }
