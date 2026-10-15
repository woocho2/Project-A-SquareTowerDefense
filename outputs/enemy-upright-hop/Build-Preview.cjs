const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
(async()=>{
 const folder=path.resolve(__dirname,'../all-tower-assets-connection-v1/UnityValidation/hop-preview'),names=fs.readdirSync(folder).filter(n=>n.endsWith('.png')).sort();
 const frames=[];for(const name of names)frames.push(await sharp(path.join(folder,name)).ensureAlpha().raw().toBuffer());
 await sharp(Buffer.concat(frames),{raw:{width:384,height:384*frames.length,channels:4,pageHeight:384}}).gif({delay:20,loop:0}).toFile(path.join(__dirname,'upright-hop.gif'));
 const selected=[0,13,21,27,34,47],layers=[];for(let i=0;i<selected.length;i++)layers.push({input:await sharp(path.join(folder,names[selected[i]])).resize(256,256).png().toBuffer(),left:(i%3)*256,top:Math.floor(i/3)*256});
 await sharp({create:{width:768,height:512,channels:4,background:'#1c2630'}}).composite(layers).png().toFile(path.join(__dirname,'overview.png'));
 console.log('Saved 48-frame Unity-rendered GIF and pose overview');
})();
