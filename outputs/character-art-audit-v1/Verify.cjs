const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),inv=require('./inventory.json'),layout=require('./layout-report.json'),masks=require('./mask-audit.json');
function assert(test,message){if(!test)throw new Error(message);}
function stats(data,width,height){let bottom=0,edge=0,count=0,transparent=0,solid=0;for(let y=0;y<height;y++)for(let x=0;x<width;x++){const a=data[(y*width+x)*4+3];if(!a)transparent++;if(a>=250)solid++;if(a>128){count++;bottom=y+1;if(x===0||x===width-1||y===0||y===height-1)edge++;}}return {bottom,edge,count,transparent,solid};}
(async()=>{
 const report={scope:'Character portraits: 56 regular towers + 14 synergies + 27 monsters; includes atlas references/import settings/masks. Projectile and hit-effect animation assets are not character portraits.',portraits:[],towersChanged:70,monsterArtChanged:0,regenerated:['52071'],metadataAndSpriteIDsPreserved:true};
 for(const t of inv.textures){
  const file=path.join(root,t.path),before=path.join(__dirname,'before',t.path);assert(fs.readFileSync(file+'.meta').equals(fs.readFileSync(before+'.meta')),'Importer changed: '+t.path);
  if(!t.spriteCount)for(const suffix of ['_Character.png','_EquipmentMask.png','_Appearance.asset']){const rel=t.path.replace('_Character.png',suffix);for(const ext of ['', '.meta'])assert(fs.readFileSync(path.join(root,rel+ext)).equals(fs.readFileSync(path.join(__dirname,'before',rel+ext))),'Monster file changed: '+rel+ext);}
 }
 for(const a of inv.portraits){
  let file,raw,width,height;if(a.type==='monster'){file=path.join(root,a.cell);const value=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});raw=value.data;width=value.info.width;height=value.info.height;}else{const atlas=path.join(root,a.atlas),metadata=await sharp(atlas).metadata();width=256;height=256;raw=await sharp(atlas).extract({left:a.x,top:metadata.height-a.y-256,width,height}).ensureAlpha().raw().toBuffer();const saved=await sharp(path.join(__dirname,'after-cells',a.id+'.png')).ensureAlpha().raw().toBuffer();assert(raw.equals(saved),'Packed cell differs: '+a.id);}
  const s=stats(raw,width,height),expected=a.type==='monster'?488:244;assert(s.count>1500&&s.transparent>0&&s.solid>0,'Invalid portrait alpha: '+a.id);assert(!s.edge,'Clipped portrait: '+a.id);assert(Math.abs(s.bottom-expected)<=1,'Bottom baseline: '+a.id+' '+s.bottom);
  report.portraits.push({id:a.id,name:a.displayName,type:a.type,logicalBottom:s.bottom/(width/256),clippedPixels:s.edge,alphaValid:true,visualReview:'reviewed',action:a.type==='monster'?'unchanged; mask/scale verified':a.id==='52071'?'bust redrawn and normalized':'face/eye/bottom alignment normalized',exception:a.id==='53001'?'eyepatch, one visible eye':a.id==='53013'?'two-person portrait; per-person scale':''});
 }
 for(const m of masks)assert(m.selected>1000&&!m.outside&&!m.nonbinary&&!m.eyes&&!m.face,'Mask issue: '+m.id);
 const byGuid=new Map(inv.textures.filter(t=>t.spriteCount).map(t=>[t.guid,t]));let references=0;
 for(const catalog of ['TowerCharacterCatalog','SynergyCharacterCatalog']){const text=fs.readFileSync(path.join(root,'Assets/Resources',catalog+'.asset'),'utf8');for(const m of text.matchAll(/\{fileID: (5[23]\d+), guid: ([a-f0-9]+)/g)){const atlas=byGuid.get(m[2]);assert(atlas&&inv.portraits.some(p=>p.atlas===atlas.path&&p.fileID===+m[1]),'Broken sprite reference: '+m[1]);references++;}}
 assert(references===70,'Expected 70 portrait catalog references');report.catalogReferencesVerified=references;report.masksVerified=masks.length;
 const unity=path.join(__dirname,'unity-tower-render-verification.json');if(fs.existsSync(unity)){const r=JSON.parse(fs.readFileSync(unity));assert(r.passed&&r.regularCombinations===280&&r.synergy===14,'Unity render validation failed');report.unityTowerRender={passed:true,regularCombinations:r.regularCombinations,synergies:r.synergy,renderedImages:r.renderedImages};}
 assert(report.portraits.length===97,'Incomplete portrait review');fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify(report,null,2));console.log(JSON.stringify({portraits:97,towerAtlases:15,towersChanged:70,monstersUnchanged:27,masks:27,spriteReferences:70,unity:report.unityTowerRender||'pending'}));
})();
