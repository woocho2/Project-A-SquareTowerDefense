using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class TowerRuntimeCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    public static void Validate()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/2. Tower/TowerBase.prefab");
        Check(prefab != null, "Missing actual tower prefab");
        var managerRoot = new GameObject("RuntimeDataCheck");
        var manager = managerRoot.AddComponent<TowerManager>();
        typeof(TowerManager).GetField("m_towerBasePrefab", PrivateInstance).SetValue(manager, prefab);
        typeof(TowerManager).GetMethod("Awake", PrivateInstance).Invoke(manager, null);
        typeof(TowerManager).GetMethod("LoadTowerDataFromCSV", PrivateInstance).Invoke(manager, null);
        var data = manager.GetTowerDataList();
        Check(data.Count == 280 && data.Select(d => d.towerID).Distinct().Count() == 280, "Actual CSV loader lost rows");
        Check(data.Count(d => d.Tier == 1) == 56, "Water missing from buildable tier");
        foreach (var entry in data)
        {
            Check(entry.EmblemId >= 1 && entry.EmblemId <= 14, "Noncanonical emblem ID");
            if (entry.attackType == AttackType.Splash || entry.attackType == AttackType.Target)
                Check(entry.projectilePrefab != null && entry.projectilePrefab.GetComponent<ProjectileHit2D>() != null, "Missing projectile " + entry.towerID);
        }
        var water = data.Where(d => d.EmblemId == TowerEmblem.WATER).ToArray();
        Check(water.Length == 20, "Missing Water tier/class combinations");
        foreach (var entry in water)
        {
            var tower = UnityEngine.Object.Instantiate(entry.towerPrefab);
            var controller = tower.GetComponent<TowerController>();
            Check(controller != null, "Missing real controller");
            typeof(TowerController).GetMethod("Awake", PrivateInstance).Invoke(controller, null);
            controller.Init(entry, manager.GetGlobalStats(entry.towerID));
            var visual = tower.GetComponent<TowerVisual>();
            Check(visual != null && visual.UsesCharacterVisual && visual.EmblemSprite.name.StartsWith("Tower_Water_"), "Actual Water spawn/init did not display Water " + entry.towerID);
            if (entry.ColorId == 3) Check(entry.buffTarget == BuffTarget.Water, "Wrong Water buff enum");
            if (entry.ColorId == 4) Check(entry.debuffTarget == DebuffTarget.Water, "Wrong Water debuff enum");
            UnityEngine.Object.DestroyImmediate(tower);
        }
        var definitions = (Array)typeof(TowerManager).GetField("s_synergyDefinitions", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        Check(definitions.Length == 14, "Missing synergy definition");
        var appearances = Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog");
        int[] usage = new int[15];
        foreach (object definition in definitions)
        {
            Type type = definition.GetType();
            int id = (int)type.GetField("TowerID").GetValue(definition);
            int[] materials = (int[])type.GetField("RequiredEmblems").GetValue(definition);
            Check(materials.Length == 3 && materials.Distinct().Count() == 3, "Bad recipe " + id);
            foreach (int material in materials) { usage[material]++; Check(data.Any(d => d.Tier >= 4 && d.EmblemId == material), "Unobtainable material"); }
            var complete = new HashSet<int>(materials);
            Check((bool)type.GetMethod("IsSatisfiedBy").Invoke(definition, new object[] { complete }), "Satisfied recipe rejected");
            complete.Remove(materials[0]);
            Check(!(bool)type.GetMethod("IsSatisfiedBy").Invoke(definition, new object[] { complete }), "Incomplete recipe accepted");
            var synergyData = (TowerData)typeof(TowerManager).GetMethod("CreateSynergyTowerData", PrivateInstance).Invoke(manager, new[] { definition });
            var tower = UnityEngine.Object.Instantiate(prefab);
            var controller = tower.GetComponent<TowerController>();
            typeof(TowerController).GetMethod("Awake", PrivateInstance).Invoke(controller, null);
            controller.Init(synergyData, synergyData.ToTowerStats());
            Check(tower.GetComponent<TowerVisual>().EmblemSprite == appearances.GetFront(id), "Actual synergy init selected wrong portrait " + id);
            UnityEngine.Object.DestroyImmediate(tower); UnityEngine.Object.DestroyImmediate(synergyData);
        }
        Check(usage.Skip(1).All(count => count == 3), "Recipe material counts differ from planning");
        var library = AssetDatabase.LoadAssetAtPath<EffectLibrary>("Assets/2. Prefab/4. Effect/EffectLibrary.asset");
        for (int attack = 1; attack <= 2; attack++) for (int emblem = 1; emblem <= 14; emblem++)
            Check(library.GetEffectPrefab(attack * 100 + emblem) != null, "Missing attack effect " + (attack * 100 + emblem));
        Debug.Log("PASS actual 280-row CSV loader, 56 buildable tier-one entries, all 28 projectile prefabs, 20 Water prefab/controller initializations, 14 real synergy recipes and character initializations, all 28 attack effects");
        foreach (var entry in data) UnityEngine.Object.DestroyImmediate(entry);
        UnityEngine.Object.DestroyImmediate(managerRoot);
    }
}
