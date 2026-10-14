const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..'),out=__dirname;
const manifest=JSON.parse(fs.readFileSync(path.join(out,'asset-manifest.json'),'utf8'));
if(!manifest.complete||manifest.assets.length!==28)throw Error('All 28 validated exports required before hookup');
const target=path.join(root,'Assets/2. Prefab/4. Effect/EffectLibrary.asset');
let text=fs.readFileSync(target,'utf8');
const before=path.join(out,'before','EffectLibrary.asset');
if(!fs.existsSync(before)){fs.mkdirSync(path.dirname(before),{recursive:true});fs.writeFileSync(before,text);}
const records=[];
for(const entry of manifest.assets){
  const regex=new RegExp('(  - effectID: '+entry.effectID+'\\r?\\n    effectPrefab: )\\{[^}]+\\}');
  if(!regex.test(text))throw Error('Missing library key '+entry.effectID);
  const old=text.match(regex)[0];
  const replacement='{fileID: 5220447335354414950, guid: '+entry.guids.prefab+', type: 3}';
  text=text.replace(regex,(_,prefix)=>prefix+replacement);
  records.push({effectID:entry.effectID,key:entry.key,kind:entry.kind,prefab:entry.base+'.prefab',guid:entry.guids.prefab,previous:old});
}
fs.writeFileSync(target,text);
fs.writeFileSync(path.join(out,'effect-bindings.json'),JSON.stringify(records,null,2)+'\n');
manifest.connected=true;
fs.writeFileSync(path.join(out,'asset-manifest.json'),JSON.stringify(manifest,null,2)+'\n');
console.log('Connected 28 normal hit effects, preserving other library entries.');
