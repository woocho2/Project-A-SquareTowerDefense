const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../..'),fixture=path.join(__dirname,'unity-validation');
function write(rel,value){const dest=path.join(fixture,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,value);}
function copy(rel){const dest=path.join(fixture,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,rel),dest);if(fs.existsSync(path.join(root,rel+'.meta')))fs.copyFileSync(path.join(root,rel+'.meta'),dest+'.meta');}
for(const name of ['UITileSkin','UITileSurface','UITilePanel','UIStageThemeController'])copy('Assets/3. Scripts/05. UI/'+name+'.cs');
for(const name of ['UIAtlasNameConnector','UITilePanelEditor','UIThemeManualColorTracker'])copy('Assets/Editor/'+name+'.cs');
for(const rel of ['Assets/4. Asset/5. UI/Shared_UI_Atlas.png','Assets/Resources/UITileSkin.asset'])copy(rel);
fs.mkdirSync(path.join(fixture,'Assets/Editor'),{recursive:true});
fs.copyFileSync(path.join(__dirname,'UICommonKitCheck.cs'),path.join(fixture,'Assets/Editor/UICommonKitCheck.cs'));
write('Packages/manifest.json',JSON.stringify({dependencies:{'com.unity.ugui':'file:'+path.join(root,'Library/PackageCache/com.unity.ugui@bb329a87fcdc').replaceAll('\\','/'),'com.unity.modules.imageconversion':'1.0.0','com.unity.modules.imgui':'1.0.0','com.unity.modules.jsonserialize':'1.0.0'}},null,2));
copy('ProjectSettings/ProjectVersion.txt');
console.log('Prepared isolated Unity UI validation project.');
