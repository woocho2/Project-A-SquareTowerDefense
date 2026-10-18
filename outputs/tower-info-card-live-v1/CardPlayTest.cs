using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;

public sealed class CardPlayTest : MonoBehaviour
{
    static string Output=>Path.GetFullPath("../../tower-info-card-live-v1");
    int checks;
    void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static void Run(){SessionState.SetBool("TowerCardPlayTest",true);EditorApplication.EnterPlaymode();}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap(){if(SessionState.GetBool("TowerCardPlayTest",false))new GameObject("CardPlayTest").AddComponent<CardPlayTest>();}
    Button GetButton(TowerInfoCardView view,string name)=>(Button)new SerializedObject(view).FindProperty(name).objectReferenceValue;
    IEnumerator Start()
    {
        var routine=Checks();while(true){object current;try{if(!routine.MoveNext())break;current=routine.Current;}catch(Exception e){Debug.LogException(e);SessionState.SetBool("TowerCardPlayTest",false);EditorApplication.Exit(1);yield break;}yield return current;}
        SessionState.SetBool("TowerCardPlayTest",false);Time.timeScale=1;
        File.WriteAllText(Path.Combine(Output,"play-verification.json"),"{\"passed\":true,\"checks\":"+checks+",\"timeScaleZero\":true,\"flipButtons\":3,\"disableDuringFlip\":true,\"prefabRoundTrip\":true}");
        Debug.Log("PASS Tower card play checks: "+checks+", button flips, pause, interrupted animation and prefab bindings");EditorApplication.Exit(0);
    }
    IEnumerator Checks()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/UICommon/TowerInfoCard.prefab");Check(prefab!=null,"Card prefab missing");
        var canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var instance=Instantiate(prefab,canvas.transform);var view=instance.GetComponent<TowerInfoCardView>();Check(view!=null,"View missing");
        foreach(var text in instance.GetComponentsInChildren<TextMeshProUGUI>(true)){Check(text.font!=null,"Font missing on "+text.name);Check(text.name.StartsWith("CTxt_"),"Fixed-ivory text is not exempt from dark-stage auto contrast");}
        foreach(var value in new UnityEngine.Object[]{view.Level,view.TowerName,view.TierName,view.AttackName,view.EmblemName,view.Power,view.Range,view.Action,view.AttackCount,view.CriticalRate,view.CriticalDamage,view.AbilityValue,view.Duration,view.Mastery,view.BasicDescription,view.SkillDescription,view.PreviousTarget,view.NextTarget,view.CombineColor,view.CombineExact,view.CombineEmblem,view.Sell,view.SellValue,view.TargetPriority})Check(value!=null,"Missing card data/action binding");
        Check(view.AbilityValue.font.HasCharacters("x2 3x3"),"Detail symbols missing from project font");Check(!view.IsDetailVisible,"Prefab should open on front");
        Time.timeScale=0;GetButton(view,"skillButton").onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(view.IsDetailVisible&&!view.IsFlipping,"Skill click does not complete at timeScale zero");
        Check(new SerializedObject(view).FindProperty("skillPage").objectReferenceValue is GameObject skill&&skill.activeSelf,"Wrong detail page");
        GetButton(view,"returnButton").onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(!view.IsDetailVisible&&!view.IsFlipping,"Return click failed");
        GetButton(view,"basicButton").onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(view.IsDetailVisible,"Basic click failed");
        Check(new SerializedObject(view).FindProperty("basicPage").objectReferenceValue is GameObject basic&&basic.activeSelf,"Wrong basic page");
        view.ResetToFront();Check(!view.IsDetailVisible,"New selection reset failed");
        GetButton(view,"skillButton").onClick.Invoke();yield return new WaitForSecondsRealtime(.06f);instance.SetActive(false);instance.SetActive(true);yield return null;
        Check(!view.IsFlipping&&!view.IsDetailVisible,"Disabling mid-flip leaves stale detail or animation");
        var flip=(RectTransform)new SerializedObject(view).FindProperty("flipRoot").objectReferenceValue;Check(Quaternion.Angle(flip.localRotation,Quaternion.identity)<.001f,"Interrupted flip leaves card edge-on");
        GetButton(view,"basicButton").onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Check(view.IsDetailVisible&&!view.IsFlipping,"Card does not work after reopening");
        view.ResetToFront();
        // Real serialized prefab nesting is checked before transplanting the generated instance into Canvas.prefab.
        var host=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Validation/IntegrationHost.prefab");var nested=Instantiate(host);Check(nested.GetComponent<TowerCardHost>().card!=null,"Nested prefab view reference lost");
        Check(Mathf.Approximately(nested.GetComponent<TowerCardHost>().card.transform.localScale.x,.7f),"Panel fit scale lost");Destroy(nested);Destroy(instance);
    }
}
#endif
