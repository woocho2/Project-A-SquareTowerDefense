const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/ui-common-kit-v1/unity-validation');
const rel='Assets/4. Asset/5. UI/Card_Background_SwordSigil_v1.png';
const source=path.join(__dirname,'sword-background-generated.png');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function copy(asset){const dest=path.join(fixture,asset);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,asset),dest);if(fs.existsSync(path.join(root,asset+'.meta')))fs.copyFileSync(path.join(root,asset+'.meta'),dest+'.meta');}
(async()=>{
 if(!fs.existsSync(source))fs.copyFileSync('C:/Users/user/.codex/generated_images/01a1347f-21af-71c1-8a9c-3a9cea2aa748/exec-9084b638-4a32-4e39-84aa-0bd03405d083.png',source);
 const {data,info}=await sharp(source).ensureAlpha().extractChannel(3).median(3).threshold(110).raw().toBuffer({resolveWithObject:true});
 const W=info.width,H=info.height,visited=new Uint8Array(W*H),keep=new Uint8Array(W*H),queue=new Int32Array(W*H);let kept=0,removed=0;
 for(let start=0;start<W*H;start++){
  if(visited[start]||data[start]===0)continue;
  let head=0,tail=0;queue[tail++]=start;visited[start]=1;
  while(head<tail){const p=queue[head++],x=p%W,y=Math.floor(p/W);for(const q of [x>0?p-1:-1,x<W-1?p+1:-1,y>0?p-W:-1,y<H-1?p+W:-1])if(q>=0&&!visited[q]&&data[q]){visited[q]=1;queue[tail++]=q;}}
  if(tail>=400){kept++;for(let k=0;k<tail;k++)keep[queue[k]]=255;}else removed++;
 }
 let x0=W,y0=H,x1=-1,y1=-1;
 for(let i=0;i<keep.length;i++)if(keep[i]){x0=Math.min(x0,i%W);x1=Math.max(x1,i%W);y0=Math.min(y0,Math.floor(i/W));y1=Math.max(y1,Math.floor(i/W));}
 if(x1<0||kept<3)throw Error('Missing emblem silhouette');
 const cropped=await sharp(Buffer.from(keep),{raw:{width:W,height:H,channels:1}}).extract({left:x0,top:y0,width:x1-x0+1,height:y1-y0+1}).resize(896,896,{fit:'inside',kernel:'lanczos3'}).toColourspace('b-w').raw().toBuffer({resolveWithObject:true});
 if(cropped.info.channels!==1)throw Error('Alpha resize expanded channels');
 // A white alpha-only sprite lets the existing Image.color choose watermark tint and opacity.
 const rgba=Buffer.alloc(1024*1024*4);const left=Math.floor((1024-cropped.info.width)/2),top=Math.floor((1024-cropped.info.height)/2);
 for(let y=0;y<cropped.info.height;y++)for(let x=0;x<cropped.info.width;x++){const p=((y+top)*1024+x+left)*4;rgba[p]=rgba[p+1]=rgba[p+2]=255;rgba[p+3]=cropped.data[y*cropped.info.width+x];}
 const png=await sharp(rgba,{raw:{width:1024,height:1024,channels:4}}).png().toBuffer();
 const dest=path.join(root,rel),manifest=path.join(__dirname,'verification.json');
 if(fs.existsSync(dest)&&!fs.existsSync(manifest))throw Error('Refusing unrelated overwrite');
 if(fs.existsSync(dest)&&hash(fs.readFileSync(dest))!==JSON.parse(fs.readFileSync(manifest)).sha256)throw Error('Background modified since build');
 fs.writeFileSync(dest,png);fs.writeFileSync(path.join(__dirname,'Card_Background_SwordSigil_v1.png'),png);
 let meta=fs.readFileSync(path.join(root,'Assets/4. Asset/5. UI/Icon_Attack_Basic_Common.png.meta'),'utf8').replace(/^guid: .*$/m,'guid: '+crypto.createHash('md5').update(rel).digest('hex')).replace(/  userData: .*$/m,'  userData: Dedicated monochrome sword background - tower-info-shared-ui-v7');
 fs.writeFileSync(dest+'.meta',meta);copy(rel);
 fs.copyFileSync(path.join(__dirname,'SwordBackgroundPreview.cs'),path.join(fixture,'Assets/Editor/SwordBackgroundPreview.cs'));
 fs.writeFileSync(manifest,JSON.stringify({path:rel,size:[1024,1024],sha256:hash(png),sourceSize:[W,H],keptComponents:kept,removedSpeckleComponents:removed,monochromeWhite:true,previewOnly:true},null,2));
 console.log(JSON.stringify({mask:info,crop:cropped.info,sourceMaskInk:data.reduce((a,b)=>a+(b>128),0),keptInk:keep.reduce((a,b)=>a+(b>128),0),resizedInk:cropped.data.reduce((a,b)=>a+(b>128),0),kept,removed}));
})();
