const fs = require('fs'), path = require('path'), sharp = require('../../_codex_spreadsheet_task/node_modules/sharp');
const inv = require('../character-art-audit-v1/inventory.json'), report = require('./rebuild-report.json');
function label(s, width, h = 32) { return Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${h}"><text x="${width / 2}" y="22" fill="#403a36" text-anchor="middle" font-family="Arial" font-size="17">${s.replaceAll('&', '&amp;')}</text></svg>`); }
(async () => {
 const rows = [...report.portraits].sort((a,b) => +a.id - b.id), layers = [];
 for (let i=0; i<rows.length; i++) {
  const r=rows[i], left=i%10*256, top=Math.floor(i/10)*288;
  layers.push({input:await sharp(path.join(__dirname,'cells',r.id+'.png')).resize(256,256).png().toBuffer(),left,top});
  layers.push({input:label(r.id+' '+r.name.replace(/^Tower_|^Synergy_|_Front_v2$/g,''),256),left,top:top+256});
 }
 await sharp({create:{width:2560,height:2016,channels:4,background:'#eee8e1'}}).composite(layers).png().toFile(path.join(__dirname,'all70-overview.png'));
 const sample=['52010','52011','52071','52112','52131','53001','53013','53014'];
 for(let page=0;page<2;page++) {
  const overlay=[];
  for(let j=0;j<4;j++) {
   const id=sample[page*4+j], row=inv.portraits.find(p=>p.id===id), top=j*548+36;
   overlay.push({input:await sharp(path.resolve(__dirname,'../character-art-audit-v1/after-cells',id+'.png')).resize(512,512,{kernel:'linear'}).png().toBuffer(),left:0,top});
   overlay.push({input:path.join(__dirname,'cells',id+'.png'),left:512,top});
   overlay.push({input:label(id+' | 256px enlarged',512),left:0,top:top-36});
   overlay.push({input:label('512px from larger source',512),left:512,top:top-36});
  }
  await sharp({create:{width:1024,height:2192,channels:4,background:'#eee8e1'}}).composite(overlay).png().toFile(path.join(__dirname,'comparison-'+(page+1)+'.png'));
 }
 console.log('Created 70-portrait contact sheet and eight before/after comparisons.');
})().catch(e=>{console.error(e);process.exitCode=1;});
