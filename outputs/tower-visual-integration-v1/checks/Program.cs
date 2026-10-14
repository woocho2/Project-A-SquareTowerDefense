using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

class Program {
    static Dictionary<string,Sprite> sprites = new Dictionary<string,Sprite>();
    static string root;
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static Sprite SpriteRef(string text) {
        var match=Regex.Match(text,@"fileID: (\d+), guid: ([a-f0-9]{32})");
        Check(match.Success,"Malformed sprite reference");
        string key=match.Groups[2].Value+":"+match.Groups[1].Value;
        Check(sprites.ContainsKey(key),"Missing sprite: "+key);return sprites[key];
    }
    static Sprite[] ReadArray(string yaml,string field) {
        var match=Regex.Match(yaml,@"(?m)^  "+field+@":\n(?:  - [^\n]+\n)+");
        Check(match.Success,"Missing sprite array: "+field);
        return Regex.Matches(match.Value,@"\{fileID: \d+, guid: [a-f0-9]{32}, type: 3\}").Select(m=>SpriteRef(m.Value)).ToArray();
    }
    static void Main(string[] args) {
        root=Path.GetFullPath(args[0]);
        foreach(string path in Directory.EnumerateFiles(Path.Combine(root,"Assets"),"*.png.meta",SearchOption.AllDirectories)) {
            string meta=File.ReadAllText(path).Replace("\r","");
            string guid=Regex.Match(meta,@"(?m)^guid: (\w+)").Groups[1].Value;
            var entries=Regex.Matches(meta,@"(?ms)^      name: ([^\n]+)\n.*?^      internalID: (\d+)");
            foreach(Match entry in entries)sprites[guid+":"+entry.Groups[2].Value]=new Sprite{name=entry.Groups[1].Value};
            if(entries.Count==0)sprites[guid+":21300000"]=new Sprite{name=Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path))};
        }
        string regularYaml=File.ReadAllText(Path.Combine(root,"Assets/Resources/TowerCharacterCatalog.asset")).Replace("\r","");
        var entriesRegular=new List<TowerCharacterCatalog.Entry>();
        foreach(Match entry in Regex.Matches(regularYaml,@"(?ms)^  - emblemId: (\d+)\n    emblemName: ([^\n]+)\n    characters:\n(.*?)(?=^  - emblemId:|\z)")) {
            Sprite[] characters=Regex.Matches(entry.Groups[3].Value,@"\{fileID: \d+, guid: [a-f0-9]{32}, type: 3\}").Select(m=>SpriteRef(m.Value)).ToArray();
            Check(characters.Length==4,"Missing attack classes");
            entriesRegular.Add(new TowerCharacterCatalog.Entry{emblemId=int.Parse(entry.Groups[1].Value),emblemName=entry.Groups[2].Value,characters=characters});
        }
        Check(entriesRegular.Count==14,"Expected 14 emblems");
        var catalog=new TowerCharacterCatalog();Set(catalog,"m_entries",entriesRegular.ToArray());
        string synergyYaml=File.ReadAllText(Path.Combine(root,"Assets/Resources/SynergyCharacterCatalog.asset")).Replace("\r","");
        var entriesSynergy=Regex.Matches(synergyYaml,@"(?m)^  - towerID: (\d+)\n    characterName: ([^\n]+)\n    front: ([^\n]+)").Select(m=>new SynergyCharacterCatalog.Entry{towerID=int.Parse(m.Groups[1].Value),characterName=m.Groups[2].Value,front=SpriteRef(m.Groups[3].Value)}).ToArray();
        Check(entriesSynergy.Length==14,"Expected 14 synergy towers");
        var synergyCatalog=new SynergyCharacterCatalog();Set(synergyCatalog,"m_entries",entriesSynergy);
        string prefab=File.ReadAllText(Path.Combine(root,"Assets/2. Prefab/2. Tower/TowerBase.prefab")).Replace("\r","");
        string yaml=Regex.Match(prefab,@"(?ms)^--- !u!114 &3374333988479624537\n.*?(?=^--- !u!|\z)").Value;
        var visual=new TowerVisual();var bg=new SpriteRenderer();var portrait=new SpriteRenderer{sortingOrder=3,sortingLayerID=421727787};var deco=new SpriteRenderer();var emblem=new SpriteRenderer();
        Set(visual,"m_backgroundRenderer",bg);Set(visual,"m_characterRenderer",portrait);Set(visual,"m_decorationRenderer",deco);Set(visual,"m_emblemRenderer",emblem);
        Set(visual,"m_regularCharacters",catalog);Set(visual,"m_synergyCharacters",synergyCatalog);
        foreach(string field in new[]{"m_backgroundSprites","m_decorationSprites","m_emblemSprites","m_tierSprites"})Set(visual,field,ReadArray(yaml,field));
        Set(visual,"m_backgroundSprite",SpriteRef(Regex.Match(yaml,@"(?m)^  m_backgroundSprite: (.+)").Groups[1].Value));
        var backgrounds=ReadArray(yaml,"m_backgroundSprites");var decorations=ReadArray(yaml,"m_decorationSprites");var emblems=ReadArray(yaml,"m_emblemSprites");
        Check(backgrounds.Length==6&&decorations.Length==6&&emblems.Length==14,"Incomplete layer tables");
        int count=0;
        foreach(var entry in entriesRegular)for(int tier=1;tier<=5;tier++)for(int attack=1;attack<=4;attack++) {
            int id=tier*1000+attack*100+entry.emblemId;visual.Apply(id);
            Check(portrait.sprite==entry.characters[attack-1]&&bg.sprite==backgrounds[tier-1]&&deco.sprite==decorations[tier-1],"Wrong normal portrait/tier: "+id);
            Check(emblem.sprite==emblems[entry.emblemId-1]&&emblem.enabled&&visual.ProjectileEmblemSprite==emblem.sprite,"Wrong normal emblem: "+id);
            Check(visual.UsesCharacterVisual&&portrait.color.Equals(Color.white)&&bg.color.Equals(Color.white)&&deco.color.Equals(Color.white),"Whole portrait tinted");
            Check(emblem.color.Equals(visual.ProjectileEmblemColor),"Emblem color mismatch");
            Check(bg.sortingOrder<portrait.sortingOrder&&portrait.sortingOrder<deco.sortingOrder&&deco.sortingOrder<emblem.sortingOrder,"Invalid layer order");
            Check(bg.transform.localScale.Equals(portrait.transform.localScale)&&deco.transform.localScale.Equals(portrait.transform.localScale),"Unequal canvas scale");count++;
        }
        foreach(var entry in entriesSynergy) {
            visual.Apply(entry.towerID);
            Check(portrait.sprite==entry.front&&bg.sprite==backgrounds[5]&&deco.sprite==decorations[5],"Wrong synergy portrait or ruby layer");
            Check(!emblem.enabled&&emblem.sprite==null&&visual.UsesCharacterVisual,"Stale normal emblem on synergy");count++;
        }
        visual.Apply(new TowerData{towerID=6001,Tier=6,VisualTowerID=5114});
        Check(portrait.sprite==entriesSynergy[0].front&&visual.ProjectileEmblemSprite==emblems[13],"Synergy projectile visual identity lost");
        visual.Apply(1201);Check(portrait.sprite==entriesRegular[0].characters[1]&&emblem.enabled&&bg.sprite==backgrounds[0],"Normal visual did not restore after synergy");
        int warnings=Debug.Warnings;visual.Apply((TowerData)null);Check(Debug.Warnings==warnings+1,"Null data guard failed");
        Console.WriteLine("PASS: actual TowerVisual/catalog C# compiled; "+count+" IDs resolved against current prefab/catalog sprite references. Layer order, uniform size, emblem-only tint, synergy reuse and projectile identity passed.");
        Console.WriteLine("UnityEngine stand-ins used for logic checks; Unity rendering/import/playmode not executed.");
    }
}
