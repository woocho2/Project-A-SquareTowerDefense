const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),fixture=path.join(__dirname,'unity-validation');
fs.mkdirSync(fixture,{recursive:true});
function copy(relative){const source=path.join(root,relative),target=path.join(fixture,relative);fs.mkdirSync(path.dirname(target),{recursive:true});fs.copyFileSync(source,target);if(fs.existsSync(source+'.meta'))fs.copyFileSync(source+'.meta',target+'.meta');}
for(const name of ['EnemyCubeAppearance.cs','EnemyCubeMeshVisual.cs','EnemyCubeOrientation.cs','EnemyCubeRollGeometry.cs','EnemyCubeUnlit.shader','EnemyCubeShadow.shader'])copy('Assets/3. Scripts/03. Enemy/'+name);
copy('Assets/3. Scripts/08.Common/SharedBoardVisuals.cs');
fs.cpSync(path.join(root,'Assets/4. Asset/4. Enemy'),path.join(fixture,'Assets/4. Asset/4. Enemy'),{recursive:true});
for(const file of ['Board_Background.png','Board_Decoration.png','Board_Arrow.png','Enemy_State_Heal.mat','Enemy_State_Defense.mat','Enemy_State_Action.mat','Enemy_State_Mobility.mat','Enemy_State_Locked.mat','Enemy_State_Hourglass.mat'])copy('Assets/4. Asset/0. Common/'+file);
copy('Assets/Resources/SharedBoardVisuals.asset');
const ui=fs.readFileSync(path.join(root,'Assets/3. Scripts/05. UI/UIStageThemeController.cs'),'utf8');
fs.writeFileSync(path.join(fixture,'Assets/StageRealm.cs'),ui.match(/public enum StageRealm\s*\{[^}]+\}/)[0]);
fs.mkdirSync(path.join(fixture,'Packages'),{recursive:true});fs.writeFileSync(path.join(fixture,'Packages/manifest.json'),JSON.stringify({dependencies:{'com.unity.modules.imageconversion':'1.0.0','com.unity.modules.tilemap':'1.0.0','com.unity.modules.imgui':'1.0.0','com.unity.modules.jsonserialize':'1.0.0'}},null,2));
fs.mkdirSync(path.join(fixture,'ProjectSettings'),{recursive:true});copy('ProjectSettings/ProjectVersion.txt');
fs.writeFileSync(path.join(fixture,'.gitignore'),'/Library/\n/Temp/\n/Logs/\n/obj/\n/UserSettings/\n/MemoryCaptures/\n');
fs.mkdirSync(path.join(fixture,'Assets/Validation'),{recursive:true});
for(const file of ['MonsterAssetValidation.cs','MonsterAssetRuntimeValidation.cs']){
 const old=path.join(fixture,'Assets/Editor',file);for(const stale of [old,old+'.meta'])if(fs.existsSync(stale))fs.unlinkSync(stale);
 fs.writeFileSync(path.join(fixture,'Assets/Validation',file),'#if UNITY_EDITOR\n'+fs.readFileSync(path.join(__dirname,file),'utf8')+'\n#endif\n');
}
const files=[];function walk(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){const file=path.join(dir,entry.name);if(entry.isDirectory())walk(file);else if(!file.endsWith('.meta'))files.push(file);}}walk(path.join(fixture,'Assets/4. Asset/4. Enemy'));
fs.writeFileSync(path.join(__dirname,'unity-validation-inputs.json'),JSON.stringify({unityVersion:'6000.3.9f1',scope:'Isolated project, production portrait/mask/appearance/mesh/material/shader and cube visual source copies; no stage scene or wave-spawn integration.',files:files.map(file=>({path:path.relative(fixture,file).replaceAll('\\','/'),sha256:crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')}))},null,2));
console.log(fixture);
