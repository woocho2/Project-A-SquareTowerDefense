const fs=require('fs'),path=require('path');const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/ui-common-kit-v1/unity-validation');
function copy(rel){const dest=path.join(fixture,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,rel),dest);if(fs.existsSync(path.join(root,rel+'.meta')))fs.copyFileSync(path.join(root,rel+'.meta'),dest+'.meta');}
for(const rel of ['Assets/3. Scripts/05. UI/TowerInfoCardView.cs','Assets/3. Scripts/05. UI/TowerTierBadgeGraphic.cs','Assets/5. Font/NotoSansKR-Black SDF.asset','Assets/5. Font/NotoSansKR-Black.ttf','Assets/4. Asset/5. UI/Shared_UI_Atlas.png','Assets/4. Asset/5. UI/Stat_Icon_Atlas_v2.png','Assets/4. Asset/5. UI/Tower_Card_Icon_Atlas_v1.png','Assets/4. Asset/5. UI/Card_Background_SwordSigil_v1.png','Assets/4. Asset/5. UI/UI_Icon_Atlas.png','Assets/4. Asset/5. UI/Icon_Attack_Basic_Common.png','Assets/4. Asset/5. UI/Icon_Attack_Skill_Common.png','Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png','Assets/Resources/UITileSkin.asset'])copy(rel);
fs.cpSync(path.join(root,'Assets/8. TextMesh Pro'),path.join(fixture,'Assets/8. TextMesh Pro'),{recursive:true});
fs.copyFileSync(path.join(__dirname,'BuildCard.cs'),path.join(fixture,'Assets/Editor/BuildCard.cs'));
fs.copyFileSync(path.join(__dirname,'TowerCardHost.cs'),path.join(fixture,'Assets/TowerCardHost.cs'));
fs.copyFileSync(path.join(__dirname,'CardPlayTest.cs'),path.join(fixture,'Assets/CardPlayTest.cs'));
fs.copyFileSync(path.join(root,'outputs/ui-common-kit-v1/Compile-Project.ps1'),path.join(__dirname,'Compile-Project.ps1'));
fs.mkdirSync(path.join(__dirname,'before'),{recursive:true});
for(const rel of ['Assets/2. Prefab/Canvas.prefab','Assets/3. Scripts/05. UI/TowerInfoPanel.cs','Assets/3. Scripts/01. Tower/TowerVisual.cs']){
 const dest=path.join(__dirname,'before',rel);if(fs.existsSync(dest))continue;fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,rel),dest);
}
console.log('Prepared project font, real UI art and independent card view in isolated Unity project.');
