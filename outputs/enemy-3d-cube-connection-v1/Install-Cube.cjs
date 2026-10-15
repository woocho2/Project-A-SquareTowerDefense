const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),cp=require('node:child_process');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/all-tower-assets-connection-v1/UnityValidation');
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const write=(p,s)=>{fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s)};
const copy=(a,b)=>{fs.mkdirSync(path.dirname(b),{recursive:true});fs.copyFileSync(a,b)};
const guid=s=>crypto.createHash('md5').update('EnemyCube3D-v1:'+s).digest('hex');
function folder(p){fs.mkdirSync(path.join(root,p),{recursive:true});if(!fs.existsSync(path.join(root,p+'.meta')))write(p+'.meta',`fileFormatVersion: 2\nguid: ${guid(p)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);}
if(!read('outputs/enemy-3d-cube-connection-v1/unity-validation.log').includes('PASS legacy sprite cube compatibility; all enemy 3D cube checks passed'))throw Error('Native Unity runtime tests must pass before install');
const enemyPath='Assets/2. Prefab/3. Enemy/Enemy.prefab';
const enemyText=fs.readFileSync(path.join(fixture,enemyPath),'utf8');
const healthBlocks=enemyText.split(/(?=--- !u!)/).filter(s=>s.includes('guid: ea8d8c52caba825459d4d344983bbcca'));
if(healthBlocks.length!==1||healthBlocks[0].includes('m_hpBar: {fileID: 0}'))throw Error('Exactly one wired health controller is required');
const healthId=/--- !u!114 &(\d+)/.exec(healthBlocks[0])[1];
const enemyGuid=/guid: (\w+)/.exec(read(enemyPath+'.meta'))[1];
copy(path.join(fixture,enemyPath),path.join(root,enemyPath));
const dest='Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/StateCube3D_v1';folder(dest);
for(const name of fs.readdirSync(path.join(fixture,'Assets/EnemyCubeGenerated')))if(/\.(mat|asset)(\.meta)?$/.test(name))copy(path.join(fixture,'Assets/EnemyCubeGenerated',name),path.join(root,dest,name));
const dataFolder='Assets/2. Prefab/3. Enemy/Data';folder(dataFolder);
const variants=[['Speed','2. Enemy_speed.asset'],['Defense','3. Enemy_depend.asset']];
const dataGuids={};
for(const [key,oldName] of variants){
  const p=dataFolder+'/Enemy_Wave'+key+'_Stats.asset',g=guid(p);dataGuids[key]=g;
  const original=cp.execFileSync('git',['show','HEAD:Assets/2. Prefab/3. Enemy/'+oldName],{cwd:root,encoding:'utf8'});
  write(p,original.replace(/^  m_Name:.*$/m,'  m_Name: Enemy_Wave'+key+'_Stats'));
  write(p+'.meta',`fileFormatVersion: 2\nguid: ${g}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
const ref=(id,g,type)=>`{fileID: ${id}, guid: ${g}, type: ${type}}`;
const newRef=ref(healthId,enemyGuid,3);
const pools=[
  ['2714449143849703359',newRef,'4fa5a9b0b1bf45942916e4a72ecac46c',0],
  ['9141810548019818990',newRef,dataGuids.Speed,1],
  ['5954208249013597334',newRef,dataGuids.Defense,2],
  ['7503588851756311437',ref('8667325651164272175','6eb6174d1ae64f5449b881462ba06aec',3),'a7f97c2ce48afde47b934739b24d9111',3],
  ['789117182362090262',ref('1541420121208455735','68a2d2e6e734e2f4f889e39e452619a0',3),'6aaa53e87325d014cacb08571783b02d',4]
];
const poolPath='Assets/2. Prefab/=====Pool=====.prefab';let pool=read(poolPath);
for(const [id,prefab,data,type] of pools){
 const re=new RegExp('--- !u!114 &'+id+'\\r?\\n[\\s\\S]*?(?=--- !u!|$)');let block=re.exec(pool)?.[0];if(!block)throw Error('Missing pool '+id);
 block=block.replace(/^  enemyPrefab:.*$/m,'  enemyPrefab: '+prefab);
 const d='  m_enemyData: '+ref('11400000',data,2),t='  m_poolEnemyType: '+type;
 block=block.includes('  m_enemyData:')?block.replace(/^  m_enemyData:.*$/m,d):block.replace(/^  enemyPrefab:/m,d+'\n  enemyPrefab:');
 block=block.includes('  m_poolEnemyType:')?block.replace(/^  m_poolEnemyType:.*$/m,t):block.replace(/^  initialSize:/m,t+'\n  initialSize:');
 pool=pool.replace(re,()=>block);
}
write(poolPath,pool);
const scenePath='Assets/1. Scenes/3. Stage1.unity';let scene=read(scenePath);
for(const [id,prefab,data] of pools.slice(0,3))for(const [property,value] of [['enemyPrefab',prefab],['m_enemyData',ref('11400000',data,2)]]){
 const re=new RegExp('(- target: \\{fileID: '+id+', guid: 8156ef0932ada0f4493f3267e52967bc, type: 3\\}\\r?\\n      propertyPath: '+property+'\\r?\\n      value: *\\r?\\n      objectReference: )[^\\r\\n]+');
 if(!re.test(scene))throw Error('Missing scene override '+id+' '+property);scene=scene.replace(re,(_,prefix)=>prefix+value);
}
write(scenePath,scene);
fs.mkdirSync(path.join(__dirname,'renders'),{recursive:true});for(const name of fs.readdirSync(path.join(fixture,'cube-previews')))if(name.endsWith('.png'))copy(path.join(fixture,'cube-previews',name),path.join(__dirname,'renders',name));
write('outputs/enemy-3d-cube-connection-v1/installed.json',JSON.stringify({prefab:enemyPath,prefabGuid:enemyGuid,healthFileId:healthId,meshAndMaterials:dest,faceOrder:['Front: SelfHeal','Back: Defense','Left: Action','Right: Mobility','Top: Locked','Bottom: Hourglass'],pools:pools.map(([id,prefab,data,type])=>({id,prefab,data,type}))},null,2)+'\n');
console.log('Installed validated cube prefab, six native materials/mesh, regular wave pools and preserved wave stats. Health fileID: '+healthId);
