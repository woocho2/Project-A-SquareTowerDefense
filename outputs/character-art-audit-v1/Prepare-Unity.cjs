const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/remaining-monsters-v1/unity-validation');
function copy(rel){const target=path.join(fixture,rel);fs.mkdirSync(path.dirname(target),{recursive:true});fs.copyFileSync(path.join(root,rel),target);if(fs.existsSync(path.join(root,rel)+'.meta'))fs.copyFileSync(path.join(root,rel)+'.meta',target+'.meta');}
fs.cpSync(path.join(root,'Assets/4. Asset/2. Tower'),path.join(fixture,'Assets/4. Asset/2. Tower'),{recursive:true});
for(const file of ['TowerCharacterCatalog.cs','SynergyCharacterCatalog.cs'])copy('Assets/3. Scripts/01. Tower/'+file);
for(const file of ['TowerCharacterCatalog.asset','SynergyCharacterCatalog.asset'])copy('Assets/Resources/'+file);
fs.copyFileSync(path.join(__dirname,'TowerImportAudit.cs'),path.join(fixture,'Assets/Editor/TowerImportAudit.cs'));
fs.copyFileSync(path.join(__dirname,'TowerRenderAudit.cs'),path.join(fixture,'Assets/Editor/TowerRenderAudit.cs'));
console.log('Tower import audit fixture prepared.');
