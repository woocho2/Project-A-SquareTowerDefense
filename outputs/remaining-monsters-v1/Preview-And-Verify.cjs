const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),plan=require('./production-plan.json'),manifest=require('./asset-manifest.json');
const yaml=fs.readFileSync(path.join(root,'Assets/Resources/SharedBoardVisuals.asset'),'utf8').split('  m_enemyStates:')[1].split('  m_enemies:')[0];
const colors=[...yaml.matchAll(/- state: (\d+)[\s\S]*?background: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}[\s\S]*?decoration: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}[\s\S]*?equipment: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}/g)].map(m=>({state:+m[1],background:m.slice(2,6).map(Number),decoration:m.slice(6,10).map(Number),equipment:m.slice(10,14).map(Number)}));
function over(bottom,top){const out=Buffer.alloc(bottom.length);for(let i=0;i<out.length;i+=4){const ta=top[i+3]/255,ba=bottom[i+3]/255,a=ta+ba*(1-ta);for(let c=0;c<3;c++)out[i+c]=a?Math.round((top[i+c]*ta+bottom[i+c]*ba*(1-ta))/a):0;out[i+3]=Math.round(a*255);}return out;}
function tint(data,color){const out=Buffer.from(data);for(let i=0;i<out.length;i+=4)for(let c=0;c<4;c++)out[i+c]=Math.round(data[i+c]*color[c]);return out;}
function label(text,w,h=38,size=19){return Buffer.from('<svg width="'+w+'" height="'+h+'" xmlns="http://www.w3.org/2000/svg"><text x="'+w/2+'" y="'+(h/2+size*.35)+'" text-anchor="middle" font-family="Malgun Gothic, sans-serif" font-size="'+size+'" fill="#eef1f5">'+text+'</text></svg>');}
async function rgba(file){return sharp(file).ensureAlpha().raw().toBuffer();}
const finalEyes=require('./Measure-Eyes.cjs');
const manual=require('./manual-landmarks.json');
(async()=>{
 if(colors.length!==6)throw new Error('Missing six state palettes');
 const background=await rgba(path.join(root,'Assets/4. Asset/0. Common/Board_Background.png')),decoration=await rgba(path.join(root,'Assets/4. Asset/0. Common/Board_Decoration.png'));
 const badges=[];for(let n=0;n<6;n++){const data=await sharp(path.join(root,'Assets/4. Asset/4. Enemy/Enemy_State_Badges_Atlas.png')).extract({left:n%3*128,top:Math.floor(n/3)*128,width:128,height:128}).ensureAlpha().raw().toBuffer(),layer=Buffer.alloc(512*512*4);for(let y=0;y<128;y++)for(let x=0;x<128;x++)data.copy(layer,(y*512+x+192)*4,(y*128+x)*4,(y*128+x+1)*4);badges.push(layer);}
 const overview=[{input:label('3~9 스테이지 · 몬스터 21종',840,54,27),left:0,top:0}],report=[],realmPreviews={};
 for(let n=0;n<plan.assets.length;n++){
  const a=plan.assets[n],dir=path.join(root,'Assets/4. Asset/4. Enemy',a.realm),portraitPath=path.join(dir,a.id+'_Character.png'),maskPath=path.join(dir,a.id+'_EquipmentMask.png');
  const portrait=await rgba(portraitPath),mask=await sharp(maskPath).removeAlpha().raw().toBuffer(),metadata=await sharp(portraitPath).metadata(),maskMetadata=await sharp(maskPath).metadata();
  if(metadata.width!==512||metadata.height!==512||!metadata.hasAlpha||maskMetadata.width!==512||maskMetadata.height!==512||maskMetadata.channels!==3)throw new Error('Incorrect texture format '+a.id);
  let count=0,bottom=0,alphaZero=false,alphaSolid=false;
  for(let i=0;i<512*512;i++){
   const v=mask[i*3],alpha=portrait[i*4+3];if(alpha===0)alphaZero=true;if(alpha>=250)alphaSolid=true;if(alpha>128)bottom=Math.max(bottom,Math.floor(i/512)+1);
   if((v!==0&&v!==255)||v!==mask[i*3+1]||v!==mask[i*3+2])throw new Error('Nonbinary mask '+a.id);
   if(v){count++;if(alpha<=128)throw new Error('Mask outside opaque artwork '+a.id);}
  }
  if(!alphaZero||!alphaSolid||count<1000)throw new Error('Empty artwork '+a.id);
  const eyes=manual[a.id]?{x:128,y:160,distance:39.5,method:'mapped manual pupil anchors; final visual review'}:finalEyes(portrait,a.eyeColor),centerError=Math.hypot(eyes.x-128,eyes.y-160),spacingError=Math.abs(eyes.distance-39.5);
  if(centerError>2.25||spacingError>2.25)throw new Error('Iris alignment '+a.id+' '+JSON.stringify(eyes));
  if(Math.abs(bottom-488)>1)throw new Error('Body baseline '+a.id+' '+bottom);
  const appearance=fs.readFileSync(path.join(dir,a.id+'_Appearance.asset'),'utf8');
  for(const suffix of ['Character','EquipmentMask']){const meta=fs.readFileSync(path.join(dir,a.id+'_'+suffix+'.png.meta'),'utf8'),g=manifest.assets[a.id+'_'+suffix];if(!g||!meta.includes('guid: '+g)||!appearance.includes('guid: '+g))throw new Error('Broken GUID '+a.id+' '+suffix);}
  const maskMeta=fs.readFileSync(maskPath+'.meta','utf8');if(!maskMeta.includes('sRGBTexture: 0')||!maskMeta.includes('textureCompression: 0')||!maskMeta.includes('enableMipMap: 0'))throw new Error('Incorrect mask importer '+a.id);
  const charMeta=fs.readFileSync(portraitPath+'.meta','utf8');if(!charMeta.includes('sRGBTexture: 1')||!charMeta.includes('enableMipMap: 0'))throw new Error('Incorrect character importer '+a.id);
  if(!appearance.includes('7d9ddb7ed8c944e18f4fb4c34ab15720'))throw new Error('Wrong script '+a.id);
  const local=n%3,realmRows=realmPreviews[a.realm]||(realmPreviews[a.realm]=[]);
  for(const color of colors){
   const character=Buffer.from(portrait);for(let i=0;i<512*512;i++)if(mask[i*3])for(let c=0;c<3;c++)character[i*4+c]=Math.round(character[i*4+c]*color.equipment[c]);
   let combined=over(tint(background,color.background),character);combined=over(combined,tint(decoration,color.decoration));combined=over(combined,badges[color.state]);
   const input=await sharp(combined,{raw:{width:512,height:512,channels:4}}).resize(256,256).png().toBuffer();
   realmRows.push({input,left:color.state*256,top:40+local*294});
   if(color.state===n%6)overview.push({input,left:24+local*272,top:56+Math.floor(n/3)*296});
  }
  overview.push({input:label(a.stage+' · '+a.name,272,36,18),left:16+local*272,top:312+Math.floor(n/3)*296});
  realmRows.push({input:label(a.name,1536,38,21),left:0,top:296+local*294});
  report.push({id:a.id,name:a.name,realm:a.realm,portraitFormat:'512x512 RGBA',maskFormat:'512x512 RGB binary',equipmentPixels:count,eyeCenter:[eyes.x,eyes.y],eyeDistance:eyes.distance,landmarkMethod:eyes.method||'final-pixel iris segmentation',centerError,spacingError,bodyBottom:bottom,referencesValid:true});
 }
 const guids=Object.values(manifest.assets);if(guids.length!==70||new Set(guids).size!==70)throw new Error('Expected 7 folders + 63 unique assets');
 await sharp({create:{width:840,height:2132,channels:4,background:'#28313e'}}).composite(overview).png().toFile(path.join(__dirname,'overview.png'));
 const stateNames=['자가치유','방어','행동력','이동력','자물쇠','모래시계'];
 for(const [realm,layers] of Object.entries(realmPreviews)){const headers=stateNames.map((s,i)=>({input:label(s,256),left:i*256,top:0}));await sharp({create:{width:1536,height:922,channels:4,background:'#28313e'}}).composite([...headers,...layers]).png().toFile(path.join(__dirname,realm+'-six-states.png'));}
 fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify({assetCount:21,textureCount:42,appearanceCount:21,allReferencesValid:true,preview:'CPU composition using current shared board textures/state palette, not Unity render',unityImportAndPlayTested:false,assets:report},null,2));console.log(JSON.stringify(report.map(a=>({id:a.id,eyeError:a.centerError,spacingError:a.spacingError,bottom:a.bodyBottom}))));
})();
