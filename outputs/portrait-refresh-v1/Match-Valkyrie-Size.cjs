const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
async function bounds(p){const {data,info}=await sharp(p).ensureAlpha().raw().toBuffer({resolveWithObject:true});let l=info.width,t=info.height,r=0,b=0;for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>32){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}return {left:l,top:t,width:r-l+1,height:b-t+1,canvasWidth:info.width,canvasHeight:info.height};}
(async()=>{
 const source=path.join(__dirname,'Valkyrie_Size_Generated.png'),sword=path.join(__dirname,'Tower_Sword_Swordsman_Front_v3.png');
 const ref=await bounds(sword),content=await bounds(source);
 const width=Math.round(ref.width/ref.canvasWidth*512),height=Math.round(content.height*width/content.width);
 const bottom=Math.round((ref.top+ref.height)/ref.canvasHeight*512),left=Math.round((512-width)/2),top=bottom-height;
 const portrait=await sharp(source).extract({left:content.left,top:content.top,width:content.width,height:content.height}).resize(width,height).png().toBuffer();
 const target=path.join(__dirname,'Valkyrie_Character_v3.png');
 await sharp({create:{width:512,height:512,channels:4,background:'#00000000'}}).composite([{input:portrait,left,top}]).png().toFile(target);
 const layers=[{input:await sharp(sword).resize(384,384).png().toBuffer(),left:8,top:8},{input:await sharp(target).resize(384,384).png().toBuffer(),left:408,top:8}];
 await sharp({create:{width:800,height:400,channels:4,background:'#29313d'}}).composite(layers).png().toFile(path.join(__dirname,'Swordsman_Valkyrie_Size_Matched.png'));
 const actual=await bounds(target),stats=await sharp(target).stats();
 if(Math.abs(actual.width/512-ref.width/ref.canvasWidth)>.005||Math.abs((actual.top+actual.height)/512-(ref.top+ref.height)/ref.canvasHeight)>.005||stats.channels[3].min!==0)throw new Error('Portrait sizing/transparency mismatch');
 const report={reference:ref,matched:actual,widthFraction:actual.width/512,bottomFraction:(actual.top+actual.height)/512,existingUnityAssetsModified:false};
 fs.writeFileSync(path.join(__dirname,'valkyrie-size-verification.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})();
