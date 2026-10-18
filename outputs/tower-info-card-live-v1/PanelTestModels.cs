// Isolated UI test doubles only. Never copied into the real game's Assets.
using System;
using UnityEngine;
public enum AttackType { Splash, Target, Buff, Debuff, None }
public enum TargetPriority { Default, Closest, First, Last, Strongest, Weakest }
public enum CombineMode { ColorMatch, EmblemMatch, ExactMatch }
public enum BuffTarget { None, Sword }
public enum DebuffTarget { None, Sword }
public static class TowerEmblem { public const int SWORD=1; }
public struct TowerStats {public int ID,Level,AttackCount,ProjectileRadius,AdditionalHitCount;public string Name;public float AttackPower,Range,CriticalRate,CriticalDamage,AbilityValue,Duration;}
public class TowerData : ScriptableObject {public int towerID,visualTowerID;public AttackType attackType;public BuffTarget buffTarget;public DebuffTarget debuffTarget;public int VisualTowerID=>visualTowerID>0?visualTowerID:towerID;}
public static class GameStateVersion {public static int Current;}
public sealed class GameManager {public static GameManager Instance=new GameManager();public bool CanPerformPlayerAction=true;}
public sealed class TowerManager {public static TowerManager Instance=new TowerManager();public event Action<TowerController> TowerTierUpgraded;public int sold=-1,combined=-1;public CombineMode mode;public bool CanCombine(int index,CombineMode mode)=>index>=0;public void SellTower(int index){sold=index;}public void ExecuteCombine(int index,CombineMode kind){combined=index;mode=kind;}public void Upgrade(TowerController tower)=>TowerTierUpgraded?.Invoke(tower);}
public sealed class UIManager {public static UIManager Instance=new UIManager();public TowerController SelectedTower;public int closed;public void CloseTowerInfoPanel(){closed++;}}
public static class BoardInputController {public static event Action<TowerController> OnTowerClickedAction;public static int canceled;public static void Click(TowerController tower){UIManager.Instance.SelectedTower=tower;GameStateVersion.Current++;OnTowerClickedAction?.Invoke(tower);}public static void CancelAllInteractions(){canceled++;}}
public static class TowerAttackAction {public static int ToTileRange(float range)=>Mathf.RoundToInt(range/1.5f);}
public static class BuffTargetNames {public static string GetBuffTargetName(BuffTarget t)=>t.ToString();public static string GetDebuffTargetName(DebuffTarget t)=>t.ToString();}
