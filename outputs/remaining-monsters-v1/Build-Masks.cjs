const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),plan=require('./production-plan.json'),mp=path.join(__dirname,'asset-manifest.json'),manifest=JSON.parse(fs.readFileSync(mp,'utf8'));
function guid(key){return manifest.assets[key]||(manifest.assets[key]=crypto.randomBytes(16).toString('hex'));}
function cleanup(input){
 const seen=new Uint8Array(input.length);
 for(let start=0;start<input.length;start++){
  if(seen[start])continue;const value=input[start],queue=[start];seen[start]=1;let edge=false;
  for(let k=0;k<queue.length;k++){const p=queue[k],x=p%512,y=Math.floor(p/512);if(x===0||x===511||y===0||y===511)edge=true;
   for(const n of [x>0?p-1:-1,x<511?p+1:-1,y>0?p-512:-1,y<511?p+512:-1])if(n>=0&&!seen[n]&&input[n]===value){seen[n]=1;queue.push(n);}}
  if(value&&queue.length<35)for(const p of queue)input[p]=0;
  if(!value&&!edge&&queue.length<=110)for(const p of queue)input[p]=1;
 }return input;
}
const headGear=new Set(['MiddleSpirit','UpperSpirit','SeaSerpent','GreatSeaSerpent','Jormungandr','FrostGiantChief','CorruptedDwarf','CorruptedDwarfArtisan','Fafnir','FrostWolfAlpha','FireGiantChief','HelQueen']);
(async()=>{
 const base=path.join(root,'Assets/4. Asset/4. Enemy/Asgard'),maskMeta=fs.readFileSync(path.join(base,'Valkyrie_EquipmentMask.png.meta'),'utf8'),appearanceMeta=fs.readFileSync(path.join(base,'Valkyrie_Appearance.asset.meta'),'utf8'),template=fs.readFileSync(path.join(base,'Valkyrie_Appearance.asset'),'utf8');
 const layers=[];manifest.masks={};
 for(let n=0;n<plan.assets.length;n++){
  const a=plan.assets[n],folder=path.join(root,'Assets/4. Asset/4. Enemy',a.realm),color=await sharp(path.join(folder,a.id+'_Character.png')).ensureAlpha().raw().toBuffer();
  const input=await sharp(path.join(__dirname,'source',a.id+'_EquipmentMask.png')).resize(512,512).removeAlpha().raw().toBuffer();
  let selection=new Uint8Array(512*512);for(let i=0;i<selection.length;i++)selection[i]=input[i*3]>128?1:0;selection=cleanup(selection);
  const output=Buffer.alloc(512*512*3),tinted=Buffer.from(color);let count=0;
  for(let y=0;y<512;y++)for(let x=0;x<512;x++){
   const i=y*512+x,o=i*4,r=color[o],g=color[o+1],b=color[o+2];
   const face=((x-256)/68)**2+((y-335)/38)**2<1;
   const colored=Math.max(r,g,b)-Math.min(r,g,b)>43;
   const noHead=!headGear.has(a.id)&&y<375;
   const spiritEars=a.id==='MiddleSpirit'&&y>=310&&y<370;
   const selected=selection[i]&&color[o+3]>128&&!face&&!colored&&!noHead&&!spiritEars;
   if(selected){output[i*3]=output[i*3+1]=output[i*3+2]=255;count++;tinted[o]=255;tinted[o+1]=Math.round(g*.25);tinted[o+2]=255;}
  }
  if(count<1000)throw new Error('Empty/insufficient equipment mask '+a.id+' '+count);
  const file=path.join(folder,a.id+'_EquipmentMask.png');await sharp(output,{raw:{width:512,height:512,channels:3}}).png().toFile(file);
  if(!fs.existsSync(file+'.meta'))fs.writeFileSync(file+'.meta',maskMeta.replace(/^guid: [a-f0-9]+/m,'guid: '+guid(a.id+'_EquipmentMask')));
  const appearance=path.join(folder,a.id+'_Appearance.asset');fs.writeFileSync(appearance,template.replace('m_Name: Valkyrie_Appearance','m_Name: '+a.id+'_Appearance').replace('4c589c7b375143148a6c2ca4aa862c4f',guid(a.id+'_Character')).replace('f6dd47a2f3ee40e0afa808a7900031b1',guid(a.id+'_EquipmentMask')));
  if(!fs.existsSync(appearance+'.meta'))fs.writeFileSync(appearance+'.meta',appearanceMeta.replace(/^guid: [a-f0-9]+/m,'guid: '+guid(a.id+'_Appearance')));
  manifest.masks[a.id]={selectedPixels:count,format:'512x512 opaque binary RGB, linear, uncompressed',naturalColorProtection:'saturation + common face protection + head gear allow list'};
  layers.push({input:await sharp(tinted,{raw:{width:512,height:512,channels:4}}).resize(256,256).png().toBuffer(),left:n%3*256,top:Math.floor(n/3)*256});
 }
 fs.writeFileSync(mp,JSON.stringify(manifest,null,2));await sharp({create:{width:768,height:1792,channels:4,background:'#bcbcbc'}}).composite(layers).png().toFile(path.join(__dirname,'equipment-mask-check.png'));console.log(JSON.stringify(manifest.masks));
})();
