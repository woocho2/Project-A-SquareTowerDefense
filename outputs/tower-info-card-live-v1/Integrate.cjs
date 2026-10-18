const fs=require('fs'),path=require('path'),crypto=require('crypto'),cp=require('child_process');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/ui-common-kit-v1/unity-validation');
const canvasRel='Assets/2. Prefab/Canvas.prefab',prefabRel='Assets/2. Prefab/UICommon/TowerInfoCard.prefab';
const panelRect='8770730461144707938',panelComponent='6591381349295792321';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const before=path.join(__dirname,'before',canvasRel),current=path.join(root,canvasRel),manifest=path.join(__dirname,'applied-hashes.json');
const beforeText=fs.readFileSync(before,'utf8'),currentBytes=fs.readFileSync(current);
const expected=fs.existsSync(manifest)?JSON.parse(fs.readFileSync(manifest)).canvas:hash(Buffer.from(beforeText));
if(hash(currentBytes)!==expected)throw Error('Canvas changed since this task snapshot; refusing overwrite.');
function blocks(s){return s.split(/(?=^--- !u!)/m).filter(b=>b.startsWith('---'));}
function id(b){return b.match(/^--- !u!\d+ &(-?\d+)/)[1];}
const parts=blocks(beforeText),map=new Map(parts.map(b=>[id(b),b]));
const host=fs.readFileSync(path.join(fixture,'Assets/Validation/IntegrationHost.prefab'),'utf8'),hostParts=blocks(host);
const nested=hostParts.filter(b=>/^--- !u!1001 /.test(b)||/^--- !u!\d+ &-?\d+ stripped/m.test(b));
if(nested.filter(b=>/^--- !u!1001 /.test(b)).length!==1)throw Error('Expected one Unity-generated nested card instance.');
const hostView=hostParts.find(b=>/^  card: \{fileID: -?\d+\}/m.test(b))?.match(/^  card: \{fileID: (-?\d+)\}/m)[1];
const nestedRect=nested.find(b=>/^--- !u!224 /.test(b));if(!nestedRect||!hostView)throw Error('Nested card view or root reference missing');
const remap=new Map(nested.map((b,i)=>[id(b),(920500000000000001n+BigInt(i)).toString()]));
for(const next of remap.values())if(map.has(next))throw Error('ID collision');
const rectRef=remap.get(id(nestedRect)),viewRef=remap.get(hostView);if(!viewRef)throw Error('No stripped view reference');
const added=nested.map(b=>b.replace(/^--- !u!(\d+) &(-?\d+)/,(_,type,key)=>'--- !u!'+type+' &'+remap.get(key)).replace(/\{fileID: (-?\d+)\}/g,(all,key)=>remap.has(key)?'{fileID: '+remap.get(key)+'}':all).replace(/  m_TransformParent: \{fileID: -?\d+\}/,'  m_TransformParent: {fileID: '+panelRect+'}'));
map.set(panelRect,map.get(panelRect).replace('  m_Father:','  - {fileID: '+rectRef+'}\n  m_Father:'));
map.set(panelComponent,map.get(panelComponent).replace(/(  m_EditorClassIdentifier:.*\r?\n)/,'$1  cardView: {fileID: '+viewRef+'}\n'));
const oldChildren=(map.get(panelRect).split('  m_Children:')[1].split('  m_Father:')[0].match(/fileID: -?\d+/g)||[]).map(s=>s.split(': ')[1]);
let disabled=0;
for(const key of oldChildren){const rect=map.get(key);if(!rect)continue;const gid=rect.match(/m_GameObject: \{fileID: (-?\d+)\}/)?.[1],go=map.get(gid);if(!go)continue;const name=go.match(/m_Name: (.*)/)?.[1]?.trim();if(name==='Group_ChangeTilePanel'||name==='Group_ChangeTowerPanel')continue;map.set(gid,go.replace('  m_IsActive: 1','  m_IsActive: 0'));disabled++;}
if(disabled!==9)throw Error('Expected 9 obsolete layout groups, got '+disabled);
const output='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+parts.map(b=>map.get(id(b))).join('')+added.join('');
const allIds=[...output.matchAll(/^--- !u!\d+ &(-?\d+)/gm)].map(m=>m[1]);if(new Set(allIds).size!==allIds.length)throw Error('Duplicate object IDs');
const target=path.join(root,prefabRel);fs.mkdirSync(path.dirname(target),{recursive:true});
if(fs.existsSync(target)&&!fs.existsSync(manifest))throw Error('Unrelated card prefab exists');
for(const suffix of ['', '.meta'])fs.copyFileSync(path.join(fixture,prefabRel+suffix),target+suffix);
fs.writeFileSync(current,output);
// These two scripts were clean at task start; preserve their original tracked versions beside the dirty Canvas snapshot.
for(const rel of ['Assets/3. Scripts/05. UI/TowerInfoPanel.cs','Assets/3. Scripts/01. Tower/TowerVisual.cs'])fs.writeFileSync(path.join(__dirname,'before',rel),cp.execFileSync('git',['show','HEAD:'+rel],{cwd:root}));
fs.writeFileSync(manifest,JSON.stringify({canvas:hash(Buffer.from(output)),prefab:hash(fs.readFileSync(target)),prefabMeta:hash(fs.readFileSync(target+'.meta')),disabledLegacyGroups:disabled,preservedSwitchGroups:2,newNestedBlocks:added.length,cardViewFileID:viewRef},null,2));
console.log('Installed real nested TowerInfoCard prefab; preserved Canvas root and switching controls, disabled 9 superseded layout groups.');
