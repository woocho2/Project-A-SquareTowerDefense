using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
public static class CheckCanvasExport
{
    public static void Run()
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
        try{
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/Canvas.prefab");check(prefab!=null,"Canvas import failed");
            var views=prefab.GetComponentsInChildren<TowerInfoCardView>(true);check(views.Length==1,"Expected exactly one card");var view=views[0];
            var panel=view.GetComponentInParent<TowerInfoPanel>(true);check(panel!=null,"Card is not under TowerInfoPanel");
            check(new SerializedObject(panel).FindProperty("cardView").objectReferenceValue==view,"Nested view link did not deserialize");
            check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(view.gameObject)=="Assets/2. Prefab/UICommon/TowerInfoCard.prefab","Card is not a reusable nested prefab");
            foreach(Transform child in panel.transform){if(child.name.StartsWith("Group_")&&!child.name.Contains("Change"))check(!child.gameObject.activeSelf,"Old layout remains visible: "+child.name);}
            check(panel.transform.Find("Group_ChangeTilePanel").gameObject.activeSelf,"Tile switch removed");check(panel.transform.Find("Group_ChangeTowerPanel").gameObject.activeSelf,"Tower switch removed");
            check(Mathf.Approximately(view.transform.localScale.x,.7f)&&Mathf.Approximately(view.GetComponent<RectTransform>().anchoredPosition.y,-20),"Card fit changed");
            foreach(var text in view.GetComponentsInChildren<TextMeshProUGUI>(true))check(text.font!=null&&text.name.StartsWith("CTxt_"),"Font or contrast protection missing");
            File.WriteAllText(Path.GetFullPath("../../tower-info-card-live-v1/canvas-import-verification.json"),"{\"passed\":true,\"checks\":"+checks+",\"realCanvasPrefab\":true,\"nestedViewReference\":true,\"legacyLayoutHidden\":true}");
            Debug.Log("PASS exported Canvas import: "+checks+" checks, nested card and presenter reference resolved");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
