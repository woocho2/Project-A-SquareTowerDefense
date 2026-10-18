const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..');
(async()=>{
 const sword=path.join(root,'Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png');
 const subjects=[];
 for(let i=0;i<4;i++)subjects.push({name:['TowerHero','TowerSwordsman','TowerPaladin','TowerDeathKnight'][i],input:await sharp(sword).extract({left:(i%2)*256,top:Math.floor(i/2)*256,width:256,height:256}).png().toBuffer()});
 for(const [folder,name] of [['Asgard','Valkyrie'],['Asgard','Brunhild'],['Asgard','Loki'],['Alfheim','CorruptedElf'],['Alfheim','CorruptedElfCaptain'],['Alfheim','CorruptedElfChief']])subjects.push({name,input:await sharp(path.join(root,'Assets/4. Asset/4. Enemy',folder,name+'_Character.png')).resize(256,256).png().toBuffer()});
 const layers=[];
 for(let i=0;i<subjects.length;i++){
  const item=subjects[i],left=(i%5)*320,top=Math.floor(i/5)*352;
  layers.push({input:await sharp(item.input).resize(320,320).png().toBuffer(),left,top:top+32});
  const svg='<svg width="320" height="352" xmlns="http://www.w3.org/2000/svg"><text x="160" y="23" text-anchor="middle" fill="white" font-family="sans-serif" font-size="15">'+item.name+'</text>'+Array.from({length:8},(_,n)=>'<path d="M '+n*40+' 32 V 352 M 0 '+(32+n*40)+' H 320" stroke="#68b4d4" opacity=".35"/><text x="2" y="'+(44+n*40)+'" fill="#68b4d4" font-size="11">'+n*32+'</text>').join('')+'</svg>';
  layers.push({input:Buffer.from(svg),left,top});fs.writeFileSync(path.join(__dirname,item.name+'.png'),item.input);
 }
 await sharp({create:{width:1600,height:704,channels:4,background:'#29313d'}}).composite(layers).png().toFile(path.join(__dirname,'landmark-inspection.png'));
})();
