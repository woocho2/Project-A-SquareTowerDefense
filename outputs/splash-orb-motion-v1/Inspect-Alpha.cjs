const fs=require('node:fs'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
(async()=>{for(const a of JSON.parse(fs.readFileSync('outputs/splash-orb-motion-v1/asset-manifest.json','utf8')).assets){
const {data,info}=await sharp(a.texture).ensureAlpha().raw().toBuffer({resolveWithObject:true});let count=0,max=0,solid=0;
for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(x===0||y===0||x===info.width-1||y===info.height-1){const v=data[(y*info.width+x)*4+3];if(v)count++;max=Math.max(max,v);if(v>128)solid++;}
console.log(a.key,info.width,info.height,'nonzero edge pixels',count,'maximum alpha',max,'solid edge pixels',solid);
}})().catch(e=>{console.error(e);process.exitCode=1});
