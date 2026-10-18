const fs=require('fs'),path=require('path'),cp=require('child_process'),crypto=require('crypto');
const root=path.resolve(__dirname,'../..'),rel='Assets/2. Prefab/Canvas.prefab',raw=fs.readFileSync(path.join(root,rel),'utf8'),before=fs.readFileSync(path.join(__dirname,'before',rel),'utf8');
const blocks=s=>s.split(/(?=^--- !u!)/m).filter(b=>b.startsWith('---'));
const id=b=>b.match(/^--- !u!\d+ &(-?\d+)/)[1];const map=new Map(blocks(raw).map(b=>[id(b),b])),old=new Map(blocks(before).map(b=>[id(b),b]));
let checks=0;function check(v,m){checks++;if(!v)throw Error(m);}
check(map.size===blocks(raw).length,'Duplicate IDs');
const panel='6591381349295792321',rect='8770730461144707938',reference=map.get(panel).match(/cardView: \{fileID: (-?\d+)\}/)?.[1];check(reference&&map.get(reference)?.includes('20307f5dfbdc44328b5148ce6cc412a5'),'Card view reference missing');
check(map.get(rect).includes('920500000000000003'),'Nested card not attached');
const roots=[...old.get(rect).split('  m_Children:')[1].split('  m_Father:')[0].matchAll(/fileID: (-?\d+)/g)].map(m=>m[1]);let hidden=0;const legacy=[];
for(const r of roots){const goID=old.get(r).match(/m_GameObject: \{fileID: (-?\d+)\}/)[1],go=old.get(goID),name=go.match(/m_Name: (.*)/)[1].trim();if(name==='Group_ChangeTilePanel'||name==='Group_ChangeTowerPanel'){check(map.get(goID)===go,'Panel switch button group changed');continue;}check(map.get(goID).includes('m_IsActive: 0'),'Legacy group still active '+name);hidden++;legacy.push(goID);}
check(hidden===9,'Unexpected hidden group count');
const changedExisting=[...old].filter(([key,value])=>map.get(key)!==value).map(([key])=>key);check(changedExisting.length===11,'Unexpected modifications outside panel controller/root and nine layout groups');
for(const file of cp.execFileSync('rg',['--files','Assets','-g','*.unity'],{cwd:root,encoding:'utf8'}).trim().split(/\r?\n/).filter(Boolean)){
 const scene=fs.readFileSync(path.join(root,file),'utf8');for(const key of legacy){const re=new RegExp('target: \\{fileID: '+key+', guid: 2a2dc2c1027adca49894008fc234f54c[^\\n]*\\r?\\n      propertyPath: m_IsActive\\r?\\n      value: 1');check(!re.test(scene),'Scene reactivates obsolete group '+file);}
}
const prefab='Assets/2. Prefab/UICommon/TowerInfoCard.prefab',actual=fs.readFileSync(path.join(root,prefab)),fixture=fs.readFileSync(path.join(root,'outputs/ui-common-kit-v1/unity-validation',prefab));check(actual.equals(fixture),'Export prefab differs from tested prefab');
const viewBlock=blocks(actual.toString()).find(b=>b.includes('guid: 20307f5dfbdc44328b5148ce6cc412a5'));check(!!viewBlock,'View component missing');for(const key of ['power','range','action','attackCount','criticalRate','criticalDamage','abilityValue','duration','mastery','basicDescription','skillDescription','previousTarget','nextTarget','combineColor','combineEmblem','combineExact','sell','sellValue','fallbackTierBadge'])check(new RegExp('^  '+key+': \\{fileID: (?!0\\})').test(viewBlock.split('\n').find(l=>l.startsWith('  '+key+':'))||''),'Empty serialized binding '+key);
const sha=x=>crypto.createHash('sha256').update(x).digest('hex');
fs.writeFileSync(path.join(__dirname,'export-verification.json'),JSON.stringify({passed:true,checks,changedExistingObjects:changedExisting.length,hiddenLegacyGroups:hidden,preservedSwitchGroups:2,sceneOverridesChecked:true,nestedPrefab:true,canvasSha256:sha(Buffer.from(raw)),prefabSha256:sha(actual)},null,2));console.log('PASS export checks: '+checks+', scoped Canvas changes, scene overrides and tested nested prefab bytes.');
