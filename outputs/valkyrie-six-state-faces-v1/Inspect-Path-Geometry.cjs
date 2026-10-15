const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
async function bounds(p,rect){const {data,info}=await sharp(p).extract(rect).ensureAlpha().raw().toBuffer({resolveWithObject:true});let minX=info.width,minY=info.height,maxX=-1,maxY=-1;for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}return {x:minX,y:minY,width:maxX-minX+1,height:maxY-minY+1};}
(async()=>{for(const [key,p,r] of [
 ['path','Assets/4. Asset/1. BackGround/Tile_Floor_Atlas.png',{left:0,top:0,width:256,height:256}],
 ['decoration','Assets/4. Asset/1. BackGround/Tile_Path_Overlay_Asgard_Atlas.png',{left:512,top:512,width:256,height:256}],
 ['tower','Assets/4. Asset/2. Tower/TierLayers/Tower_Decorations_Atlas_v1.png',{left:512,top:256,width:256,height:256}]
])console.log(key,await bounds(p,r));})().catch(e=>{console.error(e);process.exitCode=1});
