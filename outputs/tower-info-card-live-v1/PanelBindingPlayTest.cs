using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
public sealed class PanelBindingPlayTest : MonoBehaviour
{
    static string Output=>Path.GetFullPath("../../tower-info-card-live-v1");int checks;
    void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static void Run(){SessionState.SetBool("TowerPanelBindingTest",true);EditorApplication.EnterPlaymode();}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Bootstrap(){if(SessionState.GetBool("TowerPanelBindingTest",false))new GameObject("PanelBindingPlayTest").AddComponent<PanelBindingPlayTest>();}
    IEnumerator Start()
    {
        var routine=Checks();while(true){object value;try{if(!routine.MoveNext())break;value=routine.Current;}catch(Exception e){Debug.LogException(e);SessionState.SetBool("TowerPanelBindingTest",false);EditorApplication.Exit(1);yield break;}yield return value;}
        SessionState.SetBool("TowerPanelBindingTest",false);File.WriteAllText(Path.Combine(Output,"panel-binding-verification.json"),"{\"passed\":true,\"checks\":"+checks+",\"actualTowerInfoPanelSource\":true,\"gameDependencies\":\"isolated test doubles\",\"selectionSwitch\":true,\"supportAndSynergy\":true,\"targetAndUtilityActions\":true}");Debug.Log("PASS real TowerInfoPanel binding checks: "+checks+", live data, selection, support/synergy, target, sell/combine with isolated game doubles");EditorApplication.Exit(0);
    }
    TowerController Tower(string name,int id,AttackType type,Sprite portrait,Sprite emblem)
    {
        var go=new GameObject(name);var tower=go.AddComponent<TowerController>();tower.Data=ScriptableObject.CreateInstance<TowerData>();tower.Data.towerID=id;tower.Data.attackType=type;tower.BaseStats=new TowerStats{ID=id,Name=name,Level=2,AttackPower=31,Range=3,AttackCount=2,CriticalRate=.15f,CriticalDamage=.2f,AbilityValue=3,Duration=4,ProjectileRadius=2};tower.CanChangeTargetPriority=type==AttackType.Splash||type==AttackType.Target;
        var visual=go.AddComponent<TowerVisual>();visual.CharacterSprite=portrait;visual.ProjectileEmblemSprite=emblem;return tower;
    }
    IEnumerator Checks()
    {
        var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png").OfType<Sprite>().ToArray();
        var art=AssetDatabase.LoadAllAssetsAtPath("Assets/4. Asset/5. UI/Tower_Card_Icon_Atlas_v1.png").OfType<Sprite>().ToArray();var emblem=art.Single(s=>s.name=="CardBadge_EmblemSword");
        var hero=Tower("히어로",1101,AttackType.Splash,sprites.Single(s=>s.name=="Tower_Sword_Hero_Front_v2"),emblem);
        UIManager.Instance.SelectedTower=hero;
        var host=new GameObject("TowerInfoPanel",typeof(RectTransform));host.SetActive(false);
        var instance=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/UICommon/TowerInfoCard.prefab"),host.transform);var view=instance.GetComponent<TowerInfoCardView>();host.AddComponent<TowerInfoPanel>();host.SetActive(true);yield return null;
        Check(view.TowerName.text=="히어로"&&view.Level.text=="Lv.2","Selected tower identity not bound");Check(view.Power.text=="31"&&view.Range.text=="2칸","Final stats not bound");Check(view.AttackName.text=="광역"&&view.EmblemName.text=="검","Role and emblem names incorrect");Check(view.AbilityValue.text=="x3"&&view.Mastery.text=="3x3","Detail values not bound");
        view.NextTarget.onClick.Invoke();Check(view.TargetPriority.text=="체력 많은 적","Target priority action disconnected");view.PreviousTarget.onClick.Invoke();Check(view.TargetPriority.text=="가까운 적","Previous target disconnected");
        view.ShowSkill();yield return new WaitForSecondsRealtime(.5f);Check(view.IsDetailVisible,"Detail could not open with live presenter");
        var target=Tower("검객",1201,AttackType.Target,sprites.Single(s=>s.name=="Tower_Sword_Swordsman_Front_v2"),emblem);BoardInputController.Click(target);Check(!view.IsDetailVisible&&view.TowerName.text=="검객","Selecting another tower leaves stale detail");Check(view.AttackName.text=="단일"&&view.Duration.text=="같은 적 4회","Target role details incorrect");
        var portrait=(Image)new SerializedObject(view).FindProperty("portrait").objectReferenceValue;Check(portrait.sprite==target.GetComponent<TowerVisual>().CharacterSprite,"Portrait did not follow selection");
        var support=Tower("기사",1301,AttackType.Buff,sprites[0],emblem);BoardInputController.Click(support);Check(view.Range.text=="-"&&view.CriticalRate.text=="-"&&view.CriticalDamage.text=="-","Support stats do not preserve unused marks");Check(view.AttackCount.text=="2스택"&&view.AttackName.text=="버프","Support count/role incorrect");
        var synergy=Tower("시너지",6101,AttackType.None,sprites[0],emblem);synergy.Data.visualTowerID=1101;BoardInputController.Click(synergy);Check(view.TierName.text=="시너지"&&view.AttackName.text=="시너지","Synergy identity incorrect");Check(view.BasicDescription.text.Contains("시너지")&&!view.BasicDescription.text.Contains("능력"),"Synergy kept previous tower description");
        var badge=(TowerTierBadgeGraphic)new SerializedObject(view).FindProperty("fallbackTierBadge").objectReferenceValue;Check(badge.enabled,"Synergy tier badge missing");
        for(int tier=2;tier<=5;tier++){target.Data.towerID=tier*1000+201;target.BaseStats.ID=target.Data.towerID;BoardInputController.Click(target);Check(badge.enabled,"Missing tier sprite did not get fallback for tier "+tier);}
        BoardInputController.Click(target);GameManager.Instance.CanPerformPlayerAction=false;GameStateVersion.Current++;yield return null;Check(!view.Sell.interactable&&!view.CombineExact.interactable&&!view.NextTarget.interactable,"Enemy-turn controls remain active");
        GameManager.Instance.CanPerformPlayerAction=true;GameStateVersion.Current++;yield return null;Check(view.Sell.interactable&&view.CombineExact.interactable,"Player-turn controls did not recover");
        view.Sell.onClick.Invoke();Check(TowerManager.Instance.sold==target.SpawnIndex&&UIManager.Instance.closed==1&&BoardInputController.canceled==1,"Sell not connected to presenter");
        BoardInputController.Click(hero);view.CombineExact.onClick.Invoke();Check(TowerManager.Instance.combined==hero.SpawnIndex&&TowerManager.Instance.mode==CombineMode.ExactMatch&&UIManager.Instance.closed==2,"Combine action not connected");
        Destroy(host);
    }
}
#endif
