const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
(async()=>{
 const folder=path.resolve(__dirname,'../all-tower-assets-connection-v1/UnityValidation/triple-roll-preview'),names=fs.readdirSync(folder).filter(n=>n.endsWith('.png')).sort();
 const frames=[];for(const name of names)frames.push(await sharp(path.join(folder,name)).ensureAlpha().raw().toBuffer());
 await sharp(Buffer.concat(frames),{raw:{width:640,height:360*frames.length,channels:4,pageHeight:360}}).gif({delay:names.map((_,i)=>i%2?20:10),loop:0}).toFile(path.join(__dirname,'triple-roll.gif'));
 const selected=[4,13,19,26,31,42],layers=[];for(let i=0;i<selected.length;i++)layers.push({input:await sharp(path.join(folder,names[selected[i]])).resize(480,270).png().toBuffer(),left:(i%2)*480,top:Math.floor(i/2)*270});
 await sharp({create:{width:960,height:810,channels:4,background:'#1c2630'}}).composite(layers).png().toFile(path.join(__dirname,'overview.png'));
 console.log('Saved actual Unity triple-roll GIF and overview');
})();
