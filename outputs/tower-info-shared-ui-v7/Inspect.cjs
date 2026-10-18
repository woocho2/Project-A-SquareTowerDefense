const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp'),path=require('path');
(async()=>{
const p=path.join(__dirname,'sword-background-generated.png');
const original=await sharp(p).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const mask=await sharp(p).ensureAlpha().extractChannel(3).median(3).threshold(110).raw().toBuffer({resolveWithObject:true});
const resize=await sharp(mask.data,{raw:{width:mask.info.width,height:mask.info.height,channels:mask.info.channels}}).resize(896,896).raw().toBuffer({resolveWithObject:true});
console.log(JSON.stringify({original:original.info,mask:mask.info,resize:resize.info,samples:[0,Math.floor(original.info.width/2),Math.floor(original.info.width*original.info.height/2)].map(i=>Array.from(original.data.slice(i*4,i*4+4)))}));
const final=await sharp(path.join(__dirname,'Card_Background_SwordSigil_v1.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});let zero=0,opaque=0;for(let i=3;i<final.data.length;i+=4){if(final.data[i]===0)zero++;if(final.data[i]>128)opaque++;}console.log(JSON.stringify({final:final.info,zero,opaque}));
await sharp(path.join(__dirname,'Card_Background_SwordSigil_v1.png')).flatten({background:'#8a775d'}).png().toFile(path.join(__dirname,'emblem-visibility-check.png'));
})();
