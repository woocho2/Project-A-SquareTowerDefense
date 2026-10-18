const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp'),path=require('node:path');
(async()=>{const names=['TowerHero','TowerPaladin','TowerSwordsman','Valkyrie','Brunhild','Loki','CorruptedElf','CorruptedElfCaptain','CorruptedElfChief'];const layers=[];
 for(let i=0;i<names.length;i++){
  const image=await sharp(path.join(__dirname,names[i]+'.png')).extract({left:80,top:130,width:100,height:82}).resize(400,328,{kernel:'nearest'}).png().toBuffer();
  const svg='<svg width="400" height="364" xmlns="http://www.w3.org/2000/svg"><text x="200" y="24" text-anchor="middle" fill="white" font-size="17">'+names[i]+'</text>'+[140,150,160,170,180,190,200,210].map(y=>'<path d="M 0 '+(36+(y-130)*4)+' H 400" stroke="cyan" opacity=".4"/><text x="2" y="'+(47+(y-130)*4)+'" fill="cyan" font-size="14">'+y+'</text>').join('')+'</svg>';
  const left=i%3*400,top=Math.floor(i/3)*364;layers.push({input:image,left,top:top+36},{input:Buffer.from(svg),left,top});
 }
 await sharp({create:{width:1200,height:1092,channels:4,background:'#29313d'}}).composite(layers).png().toFile(path.join(__dirname,'face-inspection.png'));
})();
