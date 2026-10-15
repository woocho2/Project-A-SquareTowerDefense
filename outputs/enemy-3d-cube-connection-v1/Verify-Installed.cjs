const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'installed.json'))),guids=new Map();
function walk(p){for(const e of fs.readdirSync(p,{withFileTypes:true})){const f=path.join(p,e.name);if(e.isDirectory())walk(f);else if(e.name.endsWith('.meta')){const g=/^guid: (\w+)/m.exec(fs.readFileSync(f,'utf8'))?.[1];if(g)guids.set(g,f.slice(0,-5));}}}walk(path.join(root,'Assets'));
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
function reference(id,g){assert(guids.has(g),'Missing asset GUID '+g);if(id==='0'||id==='100100000'||g.startsWith('0000000000000000'))return;const file=guids.get(g);if(/\.(prefab|asset)$/.test(file))assert(new RegExp('--- !u!\\d+ &'+id+'(?: |\\r?\\n)').test(fs.readFileSync(file,'utf8')),'Missing fileID '+id+' in '+file);}
const prefab=read(manifest.prefab);
for(const m of prefab.matchAll(/\{fileID: (-?\d+), guid: (\w+), type: \d+\}/g))reference(m[1],m[2]);
assert.equal((prefab.match(/guid: ea8d8c52caba825459d4d344983bbcca/g)||[]).length,1);
assert.equal((prefab.match(/guid: b392b1ddd5591a446a78cd7d2fd749f3/g)||[]).length,1);
const pool=read('Assets/2. Prefab/=====Pool=====.prefab'),scene=read('Assets/1. Scenes/3. Stage1.unity');
for(const p of manifest.pools){
 const block=new RegExp('--- !u!114 &'+p.id+'\\r?\\n[\\s\\S]*?(?=--- !u!|$)').exec(pool)?.[0];assert(block,'Pool component missing '+p.id);
 assert(block.includes('enemyPrefab: '+p.prefab));assert(block.includes('m_enemyData: {fileID: 11400000, guid: '+p.data));assert(block.includes('m_poolEnemyType: '+p.type));
 for(const m of block.matchAll(/\{fileID: (-?\d+), guid: (\w+), type: \d+\}/g))reference(m[1],m[2]);
 for(const [field,value] of [['enemyPrefab',p.prefab],['m_enemyData',`{fileID: 11400000, guid: ${p.data}, type: 2}`]]){
  const re=new RegExp('- target: \\{fileID: '+p.id+', guid: 8156ef0932ada0f4493f3267e52967bc, type: 3\\}\\r?\\n      propertyPath: '+field+'\\r?\\n      value: *\\r?\\n      objectReference: ([^\\r\\n]+)');assert.equal(re.exec(scene)?.[1],value);
 }
}
const matFolder=path.join(root,manifest.meshAndMaterials),materials=fs.readdirSync(matFolder).filter(n=>n.endsWith('.mat'));assert.equal(materials.filter(n=>n!=='EnemyCube_Shadow.mat').length,6);
for(const n of materials){const mat=fs.readFileSync(path.join(matFolder,n),'utf8');for(const m of mat.matchAll(/\{fileID: (-?\d+), guid: (\w+), type: \d+\}/g))reference(m[1],m[2]);}
const retired=['59fc314a09822ce449024d418dc667f4','c76796b80392aa945b78f7e93474582f','d359c7964242f4c418f6b8c3247704c8','fb027507efe6ced45b5a0ff06d4ddf46'];for(const g of retired)assert(!scene.includes(g)&&!pool.includes(g));
console.log('PASS installed prefab component references, six materials/textures/shader, all five pool data/prefab fileIDs, Stage1 overrides and retired-reference removal');

