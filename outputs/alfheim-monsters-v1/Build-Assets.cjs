const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),assetDir=path.join(root,'Assets/4. Asset/4. Enemy/Alfheim');
const names=['CorruptedElf','CorruptedElfCaptain','CorruptedElfChief'];
const manifestPath=path.join(__dirname,'asset-manifest.json');
const manifest=fs.existsSync(manifestPath)?JSON.parse(fs.readFileSync(manifestPath,'utf8')):{assets:{},bounds:{}};
function guid(key){return manifest.assets[key]||(manifest.assets[key]=crypto.randomBytes(16).toString('hex'));}
function meta(p,template,key){if(!fs.existsSync(p+'.meta'))fs.writeFileSync(p+'.meta',template.replace(/^guid: [a-f0-9]+/m,'guid: '+guid(key)));}
async function bounds(file){const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});let l=info.width,t=info.height,r=0,b=0;for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>32){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}return {left:l,top:t,width:r-l+1,height:b-t+1};}
(async()=>{
 fs.mkdirSync(assetDir,{recursive:true});
 const folderMeta=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Asgard.meta'),'utf8');meta(assetDir,folderMeta,'Alfheim');
 const characterMeta=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Asgard/Valkyrie_Character.png.meta'),'utf8');
 for(const name of names){
  const source=path.join(__dirname,'source',name+'.png'),b=await bounds(source),scale=Math.min(400/b.width,378/b.height);
  const width=Math.round(b.width*scale),height=Math.round(b.height*scale),left=Math.round((512-width)/2),top=496-height;
  const content=await sharp(source).extract(b).resize(width,height).png().toBuffer(),target=path.join(assetDir,name+'_Character.png');
  await sharp({create:{width:512,height:512,channels:4,background:'#00000000'}}).composite([{input:content,left,top}]).png().toFile(target);
  meta(target,characterMeta,name+'_Character');manifest.bounds[name]={left,top,width,height,bottom:496};
 }
 fs.writeFileSync(manifestPath,JSON.stringify(manifest,null,2));console.log(JSON.stringify(manifest.bounds));
})();
