const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),config=require('./layout-config.json');
(async()=>{
 const reference=await sharp(path.join(root,config.reference)).extract({left:0,top:0,width:256,height:256}).png().toBuffer(),layers=[];
 const names=['기준: 일반 검 타워','발키리','브륀힐드','로키','기준: 일반 검 타워','타락한 엘프','엘프 부족장','엘프 족장'];
 for(let i=0;i<8;i++){
  const subject=i<4?config.subjects[i-1]:config.subjects[i-2],left=i%4*300,top=Math.floor(i/4)*342;
  const input=i%4===0?reference:await sharp(path.join(root,'Assets/4. Asset/4. Enemy',subject.folder,subject.name+'_Character.png')).resize(256,256).png().toBuffer();
  layers.push({input:await sharp(input).resize(300,300).png().toBuffer(),left,top:top+42});
  layers.push({input:Buffer.from('<svg width="300" height="342" xmlns="http://www.w3.org/2000/svg"><text x="150" y="28" text-anchor="middle" fill="#f5f4ea" font-family="Malgun Gothic,sans-serif" font-size="18">'+names[i]+'</text></svg>'),left,top});
 }
 await sharp({create:{width:1200,height:684,channels:4,background:'#29313d'}}).composite(layers).png().toFile(path.join(__dirname,'comparison.png'));
 const beforeAfter=[];
 for(let i=0;i<6;i++){
  const subject=config.subjects[i],left=i%3*360,top=Math.floor(i/3)*396;
  const before=path.join(__dirname,'before',subject.folder,subject.name+'_Character.png'),after=path.join(root,'Assets/4. Asset/4. Enemy',subject.folder,subject.name+'_Character.png');
  beforeAfter.push({input:await sharp(before).resize(180,180).png().toBuffer(),left,top:top+36},{input:await sharp(after).resize(180,180).png().toBuffer(),left:left+180,top:top+36});
  const title=Buffer.from('<svg width="360" height="252" xmlns="http://www.w3.org/2000/svg"><g font-family="Malgun Gothic,sans-serif" fill="white" text-anchor="middle"><text x="180" y="23" font-size="17">'+subject.displayName+'</text><text x="90" y="240" font-size="14">변경 전</text><text x="270" y="240" font-size="14">변경 후</text></g></svg>');
  beforeAfter.push({input:title,left,top});
 }
 await sharp({create:{width:1080,height:720,channels:4,background:'#29313d'}}).composite(beforeAfter).png().toFile(path.join(__dirname,'before-after.png'));
})();
