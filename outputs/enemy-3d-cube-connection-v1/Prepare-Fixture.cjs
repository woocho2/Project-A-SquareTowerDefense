const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..'),out=__dirname,fixture=path.join(root,'outputs/all-tower-assets-connection-v1/UnityValidation');
const copy=(a,b)=>{fs.mkdirSync(path.dirname(b),{recursive:true});fs.copyFileSync(a,b)};
for(const name of ['EnemyMoveController.cs','EnemyCubeMeshVisual.cs','EnemyCubeUnlit.shader'])for(const ext of ['', '.meta'])copy(path.join(root,'Assets/3. Scripts/03. Enemy',name+ext),path.join(fixture,'Assets/GameScripts/03. Enemy',name+ext));
for(const p of ['Assets/2. Prefab/3. Enemy/Enemy.prefab','Assets/2. Prefab/3. Enemy/1. Enemy_normal.prefab','Assets/2. Prefab/HPBar.prefab'])for(const ext of ['', '.meta'])copy(path.join(root,p+ext),path.join(fixture,p+ext));
for(const key of ['SelfHeal','Defense','Mobility','Action','Locked','Hourglass'])for(const ext of ['', '.meta']){const name='Enemy_Valkyrie_Face_'+key+'_v1.png';copy(path.join(root,'Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/StateFaces_v1/Textures3D',name+ext),path.join(fixture,'Assets/CubeFaces',name+ext));}
copy(path.join(root,'ProjectSettings/TagManager.asset'),path.join(fixture,'ProjectSettings/TagManager.asset'));
copy(path.join(out,'EnemyCubeBuild.cs'),path.join(fixture,'Assets/Editor/EnemyCubeBuild.cs'));
copy(path.join(out,'EnemyCubeRuntimeCheck.cs'),path.join(fixture,'Assets/EnemyCubeRuntimeCheck.cs'));
fs.mkdirSync(path.join(fixture,'Assets/EnemyCubeGenerated'),{recursive:true});
console.log('Prepared isolated native Unity prefab, current game scripts, six textures and PlayMode validation');
