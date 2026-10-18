const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),assetDir=path.join(root,'Assets/4. Asset/4. Enemy/Alfheim'),names=['CorruptedElf','CorruptedElfCaptain','CorruptedElfChief'];
const manifestPath=path.join(__dirname,'asset-manifest.json'),manifest=JSON.parse(fs.readFileSync(manifestPath,'utf8'));
function guid(key){return manifest.assets[key]||(manifest.assets[key]=crypto.randomBytes(16).toString('hex'));}
function inside(x,y,polygon){let result=false;for(let i=0,j=polygon.length-1;i<polygon.length;j=i++){const a=polygon[i],b=polygon[j];if((a[1]>y)!==(b[1]>y)&&x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0])result=!result;}return result;}
function close(mask){const dilated=new Uint8Array(mask.length),closed=new Uint8Array(mask.length);for(let y=1;y<511;y++)for(let x=1;x<511;x++){let any=0;for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)any|=mask[(y+dy)*512+x+dx];dilated[y*512+x]=any;}for(let y=1;y<511;y++)for(let x=1;x<511;x++){let all=1;for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++)all&=dilated[(y+dy)*512+x+dx];closed[y*512+x]=all;}return closed;}
function fillSmallHoles(mask){const seen=new Uint8Array(mask.length);for(let i=0;i<mask.length;i++){if(mask[i]||seen[i])continue;const queue=[i];seen[i]=1;let edge=false;for(let k=0;k<queue.length;k++){const p=queue[k],x=p%512,y=Math.floor(p/512);if(x===0||x===511||y===0||y===511)edge=true;for(const n of [x>0?p-1:-1,x<511?p+1:-1,y>0?p-512:-1,y<511?p+512:-1])if(n>=0&&!seen[n]&&!mask[n]){seen[n]=1;queue.push(n);}}if(!edge&&queue.length<=96)for(const p of queue)mask[p]=1;}return mask;}
const protectedAreas={
 CorruptedElf:[[[87,292],[145,295],[176,300],[200,287],[250,297],[280,287],[340,300],[425,292],[408,316],[359,337],[334,363],[300,378],[260,385],[217,382],[180,365],[151,339],[111,316]],[[240,374],[271,374],[269,396],[255,406],[243,395]]],
 CorruptedElfCaptain:[[[86,311],[142,312],[176,318],[208,309],[243,316],[280,307],[325,318],[405,310],[385,330],[342,347],[323,373],[298,389],[256,397],[215,392],[188,377],[165,352],[127,333]],[[235,385],[264,385],[267,403],[250,419],[237,403]]],
 CorruptedElfChief:[[[106,301],[150,302],[175,306],[204,295],[247,301],[280,295],[330,304],[409,300],[393,322],[346,338],[327,357],[300,375],[258,383],[221,378],[195,365],[175,342],[134,324]],[[245,375],[269,375],[269,392],[256,400],[245,391]]]
};
(async()=>{
 const layers=[],report={};
 const maskMeta=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Asgard/Valkyrie_EquipmentMask.png.meta'),'utf8');
 const appearanceMeta=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Asgard/Valkyrie_Appearance.asset.meta'),'utf8');
 const appearanceTemplate=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Asgard/Valkyrie_Appearance.asset'),'utf8');
 for(let index=0;index<names.length;index++){
  const name=names[index],p=path.join(assetDir,name+'_Character.png');
  const color=await sharp(p).ensureAlpha().raw().toBuffer();
  const input=await sharp(path.join(__dirname,'source',name+'_EquipmentMask.png')).resize(512,512).removeAlpha().raw().toBuffer();
  let selection=new Uint8Array(512*512);
  for(let y=0;y<512;y++)for(let x=0;x<512;x++){
   const sx=name==='CorruptedElfCaptain'?Math.round((x-256)/.92+256):x,sy=name==='CorruptedElfCaptain'?y-37:y;
   if(sx<0||sx>=512||sy<0||sy>=512)continue;
   if(name==='CorruptedElfCaptain'&&sy>=270&&sy<=330)continue; // Generated mask incorrectly included skin ears.
   selection[y*512+x]=input[(sy*512+sx)*3]>128?1:0;
  }
  selection=fillSmallHoles(close(selection));
  let count=0,protectedCount=0;
  const output=Buffer.alloc(512*512*3),tinted=Buffer.from(color);
  for(let y=0;y<512;y++)for(let x=0;x<512;x++){
   const i=y*512+x,o=i*4,r=color[o],g=color[o+1],b=color[o+2];
   const warm=r>b+18&&r>g+5&&g>b+6;
   const coloredHair=(name==='CorruptedElfCaptain'&&g>r+8&&b>r+8)||(name==='CorruptedElfChief'&&b>g+8&&r>g+8);
   const protectedPixel=protectedAreas[name].some(poly=>inside(x+.5,y+.5,poly));
   let selected=selection[i]&&color[o+3]>128&&!warm&&!coloredHair&&!protectedPixel;
   if(protectedPixel)protectedCount++;
   if(selected){output[i*3]=output[i*3+1]=output[i*3+2]=255;count++;tinted[o]=255;tinted[o+1]=Math.round(g*.38);tinted[o+2]=Math.round(b*.38);}
  }
  if(count<3000)throw new Error('Insufficient equipment mask: '+name);
  const target=path.join(assetDir,name+'_EquipmentMask.png');await sharp(output,{raw:{width:512,height:512,channels:3}}).png().toFile(target);
  const maskGuid=guid(name+'_EquipmentMask');if(!fs.existsSync(target+'.meta'))fs.writeFileSync(target+'.meta',maskMeta.replace(/^guid: [a-f0-9]+/m,'guid: '+maskGuid));
  const appearance=path.join(assetDir,name+'_Appearance.asset');
  fs.writeFileSync(appearance,appearanceTemplate.replace('m_Name: Valkyrie_Appearance','m_Name: '+name+'_Appearance').replace('4c589c7b375143148a6c2ca4aa862c4f',guid(name+'_Character')).replace('f6dd47a2f3ee40e0afa808a7900031b1',maskGuid));
  if(!fs.existsSync(appearance+'.meta'))fs.writeFileSync(appearance+'.meta',appearanceMeta.replace(/^guid: [a-f0-9]+/m,'guid: '+guid(name+'_Appearance')));
  report[name]={selectedPixels:count,protectedFaceNeckPixels:protectedCount,maskFormat:'512x512 RGB opaque binary',captainRegistration:name==='CorruptedElfCaptain'?{scaleX:.92,offsetY:37}:null};
  layers.push({input:await sharp(tinted,{raw:{width:512,height:512,channels:4}}).png().toBuffer(),left:index*512,top:0});
 }
 await sharp({create:{width:1536,height:512,channels:4,background:'#29313d'}}).composite(layers).png().toFile(path.join(__dirname,'equipment-mask-check.png'));
 fs.writeFileSync(manifestPath,JSON.stringify(manifest,null,2));fs.writeFileSync(path.join(__dirname,'mask-verification.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})();
