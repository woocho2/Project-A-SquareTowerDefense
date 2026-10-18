const path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..');
(async()=>{
 const atlas=path.join(root,'Assets/4. Asset/2. Tower/TowerCharacter/Synergy_All14_Front_Atlas_v2.png');
 for(const [name,left,top] of [['Hel',768,0],['Surtr',0,256],['Ymir',256,512]])await sharp(atlas).extract({left,top,width:256,height:256}).png().toFile(path.join(__dirname,'references',name+'.png'));
 const items=['Hel','Surtr','Ymir'],layers=[];for(let i=0;i<3;i++)layers.push({input:await sharp(path.join(__dirname,'references',items[i]+'.png')).resize(384,384).png().toBuffer(),left:i*384,top:0});
 await sharp({create:{width:1152,height:384,channels:4,background:'#29313d'}}).composite(layers).png().toFile(path.join(__dirname,'references','boss-reference.png'));
})();
