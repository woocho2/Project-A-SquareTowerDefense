using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Runs the production palette lookup code with managed data-only Unity stand-ins.
public static class LookupChecks
{
    private static int checks;
    private static void Set(SharedBoardVisuals settings, string field, object value) =>
        typeof(SharedBoardVisuals).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(settings, value);
    private static void Check(bool result, string label)
    {
        if (!result) throw new Exception(label);
        checks++;
    }
    public static void Main()
    {
        var settings = new SharedBoardVisuals();
        var bronze = new BoardLayerColors { background = new Color(.9f,.8f,.7f), decoration = new Color(.7f,.4f,.2f) };
        var custom = new BoardLayerColors { background = new Color(.2f,.3f,.4f), decoration = new Color(.4f,.5f,.6f) };
        Set(settings,"m_tiers",new List<TowerTierPalette> { new TowerTierPalette { tier=1,colors=bronze } });
        Set(settings,"m_towers",new List<TowerPaletteOverride> { new TowerPaletteOverride {towerID=1205,colors=custom} });
        Check(settings.GetTower(1101).background.Equals(bronze.background),"Tier default applies to regular towers");
        Check(settings.GetTower(1205).decoration.Equals(custom.decoration),"Exact tower ID overrides tier");
        Check(settings.GetTower(1206).decoration.Equals(bronze.decoration),"ID override does not affect neighboring tower IDs");
        Check(settings.GetTower(9999).background.Equals(Color.white),"Unknown tier uses visible neutral fallback");
        var asgard = new StageBoardPalette {realm=StageRealm.Asgard,towerSpawn=bronze,path=custom,arrow=new Color(.8f,.6f,.2f)};
        var hel = new StageBoardPalette {realm=StageRealm.Hel,towerSpawn=custom,path=bronze};
        Set(settings,"m_stages",new List<StageBoardPalette> {asgard,hel});
        Check(ReferenceEquals(settings.GetStage(StageRealm.Hel),hel),"Stage lookup uses its own entry");
        Check(settings.GetStage(StageRealm.Asgard).towerSpawn.background.Equals(bronze.background),"Spawn palette independent of path");
        Check(settings.GetStage(StageRealm.Asgard).path.background.Equals(custom.background),"Path palette independent of spawn");
        Check(settings.GetStage(StageRealm.Midgard)==null,"Missing stage is explicit");
        var valkyrie=new EnemyCubeAppearance();var loki=new EnemyCubeAppearance();
        var heal=new EnemyFacePalette {state=EnemyCubeFaceAsset.SelfHeal,background=new Color(.9f,.7f,.7f),decoration=new Color(.8f,.3f,.4f),equipment=new Color(.5f,.2f,.3f)};
        var move=new EnemyFacePalette {state=EnemyCubeFaceAsset.Mobility,background=new Color(.8f,.9f,1f)};
        var lokiMove=new EnemyFacePalette {state=EnemyCubeFaceAsset.Mobility,background=new Color(.3f,.4f,.5f)};
        Set(settings,"m_enemyStates",new List<EnemyFacePalette> {heal,move});
        Set(settings,"m_enemies",new List<EnemyAppearancePalette> {
            new EnemyAppearancePalette {appearance=valkyrie},
            new EnemyAppearancePalette {appearance=loki,states=new List<EnemyFacePalette> {lokiMove}}
        });
        Check(ReferenceEquals(settings.GetEnemy(valkyrie,EnemyCubeFaceAsset.SelfHeal),heal),"Empty character override inherits state colors");
        Check(ReferenceEquals(settings.GetEnemy(loki,EnemyCubeFaceAsset.Mobility),lokiMove),"Character state override takes priority");
        Check(ReferenceEquals(settings.GetEnemy(loki,EnemyCubeFaceAsset.SelfHeal),heal),"Partial character override falls back for other states");
        Check(ReferenceEquals(settings.GetEnemy(null,EnemyCubeFaceAsset.Mobility),move),"Missing character uses state default");
        Check(!heal.decoration.Equals(heal.equipment),"Decoration and equipment colors remain independent");
        move.background=new Color(.1f,.2f,.3f);
        Check(settings.GetEnemy(valkyrie,EnemyCubeFaceAsset.Mobility).background.Equals(move.background),"Palette edits are read immediately without baked material copies");
        Console.WriteLine($"PASS: {checks} production palette lookup checks.");
    }
}

namespace UnityEngine
{
    public class Object { }
    public class ScriptableObject : Object { }
    public class Texture2D : Object { }
    public class Sprite : Object { }
    public static class Resources {public static T Load<T>(string path) where T:class => null;}
    public struct Color {
        public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white => new Color(1,1,1,1);
    }
    public class HeaderAttribute:Attribute {public HeaderAttribute(string value){}}
    public class SerializeField:Attribute {}
    public class InspectorNameAttribute:Attribute {public InspectorNameAttribute(string value){}}
    public class RangeAttribute:Attribute {public RangeAttribute(float min,float max){}}
    public class CreateAssetMenuAttribute:Attribute {public string menuName,fileName;}
    public enum RuntimeInitializeLoadType {SubsystemRegistration}
    public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType value){}}
}
namespace UnityEngine.Tilemaps {public class Tile:UnityEngine.Object {}}
public enum StageRealm {Asgard,Alfheim,Vanaheim,Midgard,Jotunheim,Nidavellir,Niflheim,Muspelheim,Hel,Ragnarok}
public enum EnemyCubeFaceAsset {SelfHeal,Defense,Action,Mobility,Locked,Hourglass}
public class EnemyCubeAppearance:UnityEngine.Object {}
