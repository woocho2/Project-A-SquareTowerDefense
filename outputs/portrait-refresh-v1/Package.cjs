const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const out=__dirname,root=path.resolve(out,'../..');
(async()=>{
 const sword=path.join(out,'Swordsman_Generated.png'),valkyrie=path.join(out,'Valkyrie_Generated.png');
 await sharp(sword).resize(256,256).png().toFile(path.join(out,'Tower_Sword_Swordsman_Front_v3.png'));
 await sharp(valkyrie).resize(512,512).png().toFile(path.join(out,'Valkyrie_Character_v2.png'));
 const atlas=await sharp(path.join(root,'Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png')).ensureAlpha().raw().toBuffer();
 const replacement=await sharp(path.join(out,'Tower_Sword_Swordsman_Front_v3.png')).ensureAlpha().raw().toBuffer();
 for(let y=0;y<256;y++)replacement.copy(atlas,(y*512+256)*4,y*256*4,(y+1)*256*4);
 await sharp(atlas,{raw:{width:512,height:512,channels:4}}).png().toFile(path.join(out,'Tower_Sword_Front_Atlas_v3.png'));
 const references=[
  path.join(out,'Swordsman_Reference.png'),path.join(out,'Tower_Sword_Swordsman_Front_v3.png'),
  path.join(root,'Assets/4. Asset/4. Enemy/Asgard/Valkyrie_Character.png'),path.join(out,'Valkyrie_Character_v2.png')
 ];
 const composites=[];for(let i=0;i<4;i++)composites.push({input:await sharp(references[i]).resize(384,384).png().toBuffer(),left:(i%2)*400+8,top:Math.floor(i/2)*400+8});
 await sharp({create:{width:800,height:800,channels:4,background:'#29313d'}}).composite(composites).png().toFile(path.join(out,'Comparison_Before_After.png'));
 const report=[];
 for(const name of ['Tower_Sword_Swordsman_Front_v3.png','Valkyrie_Character_v2.png','Tower_Sword_Front_Atlas_v3.png']){
  const p=path.join(out,name),metadata=await sharp(p).metadata(),stats=await sharp(p).stats();
  if(!metadata.hasAlpha||stats.channels[3].min!==0||stats.channels[3].max!==255)throw new Error('Missing alpha: '+name);
  report.push({name,width:metadata.width,height:metadata.height,transparent:true});
 }
 const original=await sharp(path.join(root,'Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png')).ensureAlpha().raw().toBuffer();
 let unchanged=true;for(let y=0;y<512;y++)for(let x=0;x<512;x++)if(x<256||y>=256)for(let c=0;c<4;c++)if(atlas[(y*512+x)*4+c]!==original[(y*512+x)*4+c])unchanged=false;
 if(!unchanged)throw new Error('Untargeted atlas cells changed');
 fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({assets:report,otherThreeAtlasCellsUnchanged:true,existingUnityAssetsModified:false},null,2));
 console.log(JSON.stringify(report));
})();
