const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),target=path.join(root,'Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
(async()=>{
 const folder=path.join(__dirname,'atlas-apply');fs.mkdirSync(folder,{recursive:true});
 const before=fs.readFileSync(target),meta=fs.readFileSync(target+'.meta');
 const backup=path.join(folder,'Tower_Sword_Front_Atlas_v2.before.png');if(!fs.existsSync(backup))fs.writeFileSync(backup,before);
 const {data:original,info}=await sharp(before).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const {data:portrait,info:p}=await sharp(path.join(__dirname,'Tower_Sword_Swordsman_Front_v3.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 if(info.width!==512||info.height!==512||p.width!==256||p.height!==256)throw new Error('Unexpected atlas/portrait dimensions');
 const updated=Buffer.from(original);
 for(let y=0;y<256;y++)portrait.copy(updated,(y*512+256)*4,y*256*4,(y+1)*256*4);
 const png=await sharp(updated,{raw:{width:512,height:512,channels:4}}).png().toBuffer();
 const verify=await sharp(png).ensureAlpha().raw().toBuffer();
 for(let y=0;y<512;y++)for(let x=0;x<512;x++){
  const targetCell=x>=256&&y<256,offset=(y*512+x)*4,expected=targetCell?portrait:original,index=targetCell?(y*256+x-256)*4:offset;
  for(let c=0;c<4;c++)if(verify[offset+c]!==expected[index+c])throw new Error('Pixel mismatch at '+x+','+y);
 }
 fs.writeFileSync(target,png);
 if(!meta.equals(fs.readFileSync(target+'.meta')))throw new Error('Sprite metadata changed');
 const report={target:path.relative(root,target),replacedCell:'top-right 256x256 (Unity rect x=256,y=256)',otherThreePortraitsPixelIdentical:true,replacementPixelsIdentical:true,metadataByteIdentical:true,beforeSha256:hash(before),afterSha256:hash(png),metaSha256:hash(meta)};
 fs.writeFileSync(path.join(folder,'verification.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
})();
