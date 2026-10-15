const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
(async()=>{
 const names=['Front','Back','Left','Right','Top','Bottom','Roll_25','Roll_50'],labels=['Self heal / Front','Defense / Back','Action / Left','Mobility / Right','Locked / Top','Hourglass / Bottom','Rolling 25%','Rolling 50%'];
 const layers=[];for(let i=0;i<names.length;i++){const x=(i%4)*300,y=Math.floor(i/4)*340;layers.push({input:await sharp(path.join(__dirname,'renders',names[i]+'.png')).resize(300,300).png().toBuffer(),left:x,top:y});layers.push({input:Buffer.from(`<svg width="300" height="40"><text x="150" y="25" fill="white" text-anchor="middle" font-family="Arial" font-size="17">${labels[i]}</text></svg>`),left:x,top:y+300});}
 await sharp({create:{width:1200,height:680,channels:4,background:'#24303a'}}).composite(layers).png().toFile(path.join(__dirname,'overview.png'));console.log('Saved actual Unity render overview');
})();
