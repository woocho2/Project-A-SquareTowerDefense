using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TowerImportAudit
{
    [Serializable] public class Row {public string name;public float rectWidth,rectHeight,ppu,boundsWidth,boundsHeight;public bool fullCanvasBounds;}
    [Serializable] public class Report {public string unityVersion;public int regular,synergy;public List<Row> rows=new List<Row>();}
    public static void Run()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report=new Report{unityVersion=Application.unityVersion};
            var regular=Resources.Load<TowerCharacterCatalog>("TowerCharacterCatalog");var synergy=Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog");
            if(regular==null||synergy==null)throw new Exception("Missing tower catalogs");
            for(int emblem=1;emblem<=14;emblem++)for(int attack=1;attack<=4;attack++){Record(regular.GetCharacter(emblem,attack),report);report.regular++;}
            for(int id=6001;id<=6014;id++){Record(synergy.GetFront(id),report);report.synergy++;}
            File.WriteAllText("../unity-tower-import-audit.json",JsonUtility.ToJson(report,true));Debug.Log("TOWER_IMPORT_AUDIT_PASS");EditorApplication.Exit(0);
        } catch(Exception ex) {File.WriteAllText("../unity-tower-audit-failure.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static void Record(Sprite sprite,Report report)
    {
        if(sprite==null)throw new Exception("Missing catalog portrait");
        if(sprite.rect.width!=256||sprite.rect.height!=256||sprite.pixelsPerUnit!=192||sprite.pivot!=new Vector2(128,128))throw new Exception("Sprite rect/pivot/PPU mismatch "+sprite.name);
        report.rows.Add(new Row{name=sprite.name,rectWidth=sprite.rect.width,rectHeight=sprite.rect.height,ppu=sprite.pixelsPerUnit,boundsWidth=sprite.bounds.size.x,boundsHeight=sprite.bounds.size.y,fullCanvasBounds=Mathf.Abs(sprite.bounds.size.x-256f/192f)<.0001f&&Mathf.Abs(sprite.bounds.size.y-256f/192f)<.0001f});
    }
}
