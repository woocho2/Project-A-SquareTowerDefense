const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
(async()=>{
 const folder=path.resolve(__dirname,'../all-tower-assets-connection-v1/UnityValidation/spawn-drop-preview'),names=fs.readdirSync(folder).filter(n=>n.endsWith('.png')).sort();
 const frames=[];for(const name of names)frames.push(await sharp(path.join(folder,name)).ensureAlpha().raw().toBuffer());
 await sharp(Buffer.concat(frames),{raw:{width:800,height:480*frames.length,channels:4,pageHeight:480}}).gif({delay:names.map((_,i)=>i%4===0?10:20),loop:0}).toFile(path.join(__dirname,'spawn-drop.gif'));
 const selected=[0,10,17,22,29,43],layers=[];for(let i=0;i<selected.length;i++)layers.push({input:await sharp(path.join(folder,names[selected[i]])).resize(480,288).png().toBuffer(),left:(i%2)*480,top:Math.floor(i/2)*288});
 await sharp({create:{width:960,height:864,channels:4,background:'#1c2630'}}).composite(layers).png().toFile(path.join(__dirname,'overview.png'));
 console.log('Saved Unity spawn-drop GIF and overview');
})();
