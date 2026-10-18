const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),names=['CorruptedElf','CorruptedElfCaptain','CorruptedElfChief'];
(async()=>{
 const layers=[];
 for(let index=0;index<names.length;index++){
  const name=names[index],portrait=path.join(root,'Assets/4. Asset/4. Enemy/Alfheim',name+'_Character.png');
  const {data:c}=await sharp(portrait).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const {data:m}=await sharp(path.join(__dirname,'source',name+'_EquipmentMask.png')).resize(512,512).removeAlpha().raw().toBuffer({resolveWithObject:true});
  const colored=Buffer.from(c);for(let i=0;i<512*512;i++)if(m[i*3]>128){colored[i*4]=255;colored[i*4+1]=Math.round(c[i*4+1]*.38);colored[i*4+2]=Math.round(c[i*4+2]*.38);}
  layers.push({input:await sharp(colored,{raw:{width:512,height:512,channels:4}}).png().toBuffer(),left:index*512,top:0});
 }
 await sharp({create:{width:1536,height:512,channels:4,background:'#29313d'}}).composite(layers).png().toFile(path.join(__dirname,'mask-registration-before.png'));
})();
