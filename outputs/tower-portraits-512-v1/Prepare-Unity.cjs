const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/remaining-monsters-v1/unity-validation');
for(const rel of ['Assets/4. Asset/2. Tower/TowerCharacter','Assets/Resources/TowerCharacterCatalog.asset','Assets/Resources/SynergyCharacterCatalog.asset']) {
 const dest=path.join(fixture,rel); fs.mkdirSync(path.dirname(dest),{recursive:true});
 fs.cpSync(path.join(root,rel),dest,{recursive:true});
 if(fs.existsSync(path.join(root,rel+'.meta')))fs.copyFileSync(path.join(root,rel+'.meta'),dest+'.meta');
}
let code=fs.readFileSync(path.join(root,'outputs/character-art-audit-v1/TowerRenderAudit.cs'),'utf8');
code=code.replaceAll('TowerRenderAudit','Tower512Audit')
 .replaceAll('new Vector2(256,256)','new Vector2(512,512)').replaceAll('new Vector2(128,128)','new Vector2(256,256)')
 .replaceAll('s.pixelsPerUnit-192','s.pixelsPerUnit-384')
 .replace('new RenderTexture(256,256','new RenderTexture(512,512').replace('new Texture2D(256,256','new Texture2D(512,512')
 .replace('new Rect(0,0,256,256)','new Rect(0,0,512,512)')
 .replaceAll('../../character-art-audit-v1/unity-rendered-towers','../../tower-portraits-512-v1/unity-rendered-towers')
 .replaceAll('../../character-art-audit-v1/unity-tower-render-','../../tower-portraits-512-v1/unity-render-')
 .replaceAll('TOWER_RENDER_AUDIT_PASS','TOWER_512_AUDIT_PASS');
// The 256/192 world-space canvas is intentionally retained in SetLayer.
fs.writeFileSync(path.join(__dirname,'Tower512Audit.cs'),code);
fs.copyFileSync(path.join(__dirname,'Tower512Audit.cs'),path.join(fixture,'Assets/Editor/Tower512Audit.cs'));
console.log('Copied production 512px textures and generated isolated Unity render audit.');
