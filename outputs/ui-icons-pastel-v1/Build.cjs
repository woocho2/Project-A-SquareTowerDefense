const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const out=__dirname,root=path.resolve(out,'../..'),ui=path.join(root,'Assets/4. Asset/5. UI');
const targets=[['UI_Icon_Atlas.png','currency',4,2],['Stat_Icon_Atlas_v2.png','stats',5,4],['Button_Symbol_Atlas.png','buttons',2,2],['Btn_Symbol_GameSpeed.png','speed',1,1]];
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function parse(meta,height){const p=/      name: ([^\r\n]+)\r?\n      rect:\r?\n        serializedVersion: 2\r?\n        x: (\d+)\r?\n        y: (\d+)\r?\n        width: (\d+)\r?\n        height: (\d+)[\s\S]*?      internalID: (-?\d+)/g;return [...meta.matchAll(p)].map(m=>({name:m[1],x:+m[2],y:height-(+m[3])-(+m[5]),width:+m[4],height:+m[5],fileID:m[6]}));}
function clean(data,w,h,minRatio=.006){
 // Alpha cleanup for generated sprite-sheet integration. Remove saturated
 // background fringe/dust and disconnected specks without changing the design.
 for(let p=0;p<data.length;p+=4){const a=data[p+3],lo=Math.min(data[p],data[p+1],data[p+2]),hi=Math.max(data[p],data[p+1],data[p+2]);if(a<12||(hi>210&&lo<35&&hi-lo>190))data[p+3]=0;}
 const seen=new Uint8Array(w*h),components=[];
 for(let n=0;n<seen.length;n++){
  if(seen[n]||data[n*4+3]<32)continue;
  const list=[n];seen[n]=1;
  for(let k=0;k<list.length;k++){const p=list[k],x=p%w,y=Math.floor(p/w);for(const q of [x>0?p-1:-1,x<w-1?p+1:-1,y>0?p-w:-1,y<h-1?p+w:-1])if(q>=0&&!seen[q]&&data[q*4+3]>=32){seen[q]=1;list.push(q);}}
  components.push(list);
 }
 components.sort((a,b)=>b.length-a.length);
 if(!components.length)throw Error('Empty generated sprite');
 const keep=new Uint8Array(w*h),threshold=Math.max(12,components[0].length*minRatio);
 for(const c of components){if(c.length<threshold)continue;for(const n of c){keep[n]=1;const x=n%w,y=Math.floor(n/w);for(const q of [x>0?n-1:-1,x<w-1?n+1:-1,y>0?n-w:-1,y<h-1?n+w:-1])if(q>=0&&data[q*4+3]>0)keep[q]=1;}}
 for(let n=0;n<keep.length;n++)if(!keep[n]){data[n*4]=data[n*4+1]=data[n*4+2]=data[n*4+3]=0;}
 return data;
}
function bounds(data,w,h,threshold=32){let left=w,top=h,right=-1,bottom=-1;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(data[(y*w+x)*4+3]>=threshold){left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x);bottom=Math.max(bottom,y);}if(right<left)throw Error('No icon pixels');return {left,top,width:right-left+1,height:bottom-top+1};}
function groupSheet(data,w,h,cols,rows,key){
 // Generated art can extend slightly across an ideal grid boundary. Associate
 // complete connected shapes with their cell before fitting, so the edge of a
 // coin or arrow cannot be clipped into the adjacent icon.
 for(let p=0;p<data.length;p+=4){const lo=Math.min(data[p],data[p+1],data[p+2]),hi=Math.max(data[p],data[p+1],data[p+2]);if(data[p+3]<12||(hi>210&&lo<35&&hi-lo>190))data[p+3]=0;}
 const seen=new Uint8Array(w*h),groups=Array.from({length:cols*rows},()=>[]);
 for(let n=0;n<seen.length;n++){
  if(seen[n]||data[n*4+3]<32)continue;const list=[n];seen[n]=1;let xx=0,yy=0;
  for(let i=0;i<list.length;i++){const p=list[i],x=p%w,y=Math.floor(p/w);xx+=x;yy+=y;for(const q of[x>0?p-1:-1,x<w-1?p+1:-1,y>0?p-w:-1,y<h-1?p+w:-1])if(q>=0&&!seen[q]&&data[q*4+3]>=32){seen[q]=1;list.push(q);}}
  if(list.length<12)continue;
  const col=Math.min(cols-1,Math.floor(xx/list.length/w*cols)),row=Math.min(rows-1,Math.floor(yy/list.length/h*rows));groups[row*cols+col].push(list);
 }
 return groups.map((components,index)=>{
  components.sort((a,b)=>b.length-a.length);if(!components.length)throw Error('Missing generated cell '+index);
  const threshold=Math.max(12,components[0].length*.006),kept=components.filter(c=>c.length>=threshold).slice(0,key==='currency'?1:key==='speed'?2:32);
  const indices=new Set();for(const c of kept)for(const n of c){indices.add(n);const x=n%w,y=Math.floor(n/w);for(const q of[x>0?n-1:-1,x<w-1?n+1:-1,y>0?n-w:-1,y<h-1?n+w:-1])if(q>=0&&data[q*4+3])indices.add(q);}
  let left=w,top=h,right=-1,bottom=-1;for(const n of indices){const x=n%w,y=Math.floor(n/w);left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x);bottom=Math.max(bottom,y);}
  const width=right-left+1,height=bottom-top+1,cell=Buffer.alloc(width*height*4);
  for(const n of indices){const p=((Math.floor(n/w)-top)*width+n%w-left)*4;data.copy(cell,p,n*4,n*4+4);}
  return {data:cell,width,height};
 });
}
async function png(data,w,h){return sharp(data,{raw:{width:w,height:h,channels:4}}).png().toBuffer();}
(async()=>{
 fs.mkdirSync(path.join(out,'before'),{recursive:true});fs.mkdirSync(path.join(out,'staged'),{recursive:true});fs.mkdirSync(path.join(out,'cells'),{recursive:true});
 const snapshot=path.join(out,'unchanged-files.json');
 if(!fs.existsSync(snapshot)){
  const files=fs.readdirSync(ui).filter(n=>fs.statSync(path.join(ui,n)).isFile()&&!targets.some(t=>t[0]===n));
  const hashes={};for(const n of files)hashes[path.relative(root,path.join(ui,n)).replaceAll('\\','/')]=hash(fs.readFileSync(path.join(ui,n)));
  for(const n of ['Assets/2. Prefab/Canvas.prefab','Assets/2. Prefab/BuffPrefab.prefab','Assets/3. Scripts/05. UI/UIStageThemeController.cs'])hashes[n]=hash(fs.readFileSync(path.join(root,n)));
  fs.writeFileSync(snapshot,JSON.stringify(hashes,null,2));
 }
 const report=[];
 for(const [file,key,cols,rows] of targets){
  for(const n of [file,file+'.meta'])if(!fs.existsSync(path.join(out,'before',n)))fs.copyFileSync(path.join(ui,n),path.join(out,'before',n));
  const original=path.join(out,'before',file),info=await sharp(original).metadata();
  const meta=fs.readFileSync(path.join(ui,file+'.meta'),'utf8');
  if(meta!==fs.readFileSync(path.join(out,'before',file+'.meta'),'utf8'))throw Error('Unrelated metadata change');
  const sprites=/^  spriteMode: 1\r?$/m.test(meta)
   ?[{name:path.basename(file,'.png'),x:0,y:0,width:info.width,height:info.height,fileID:'21300000'}]
   :parse(meta,info.height);
  if(sprites.length!==cols*rows)throw Error('Sprite count mismatch');
  const generated=path.join(out,key+'-generated.png'),genInfo=await sharp(generated).metadata();
  const generatedPixels=await sharp(generated).ensureAlpha().raw().toBuffer();
  const generatedCells=groupSheet(generatedPixels,genInfo.width,genInfo.height,cols,rows,key);
  const result=Buffer.alloc(info.width*info.height*4),entries=[];
  for(const s of sprites){
   const col=Math.round(s.x/(info.width/cols)),row=Math.round(s.y/(info.height/rows));
   const source=generatedCells[row*cols+col],cell=source.data;
   const box=bounds(cell,source.width,source.height);
   const old=await sharp(original).extract({left:s.x,top:s.y,width:s.width,height:s.height}).ensureAlpha().raw().toBuffer();
   const oldBox=bounds(old,s.width,s.height,128);
   // Fit the new art into the original sprite's visible footprint. This keeps
   // existing layout sizes/pivots and relative icon scale stable.
   const scale=Math.min(oldBox.width/box.width,oldBox.height/box.height);
   const w=Math.max(1,Math.round(box.width*scale)),h=Math.max(1,Math.round(box.height*scale));
   const cx=Math.round(oldBox.left+oldBox.width/2-w/2),cy=Math.round(oldBox.top+oldBox.height/2-h/2);
   const icon=await sharp(cell,{raw:{width:source.width,height:source.height,channels:4}}).extract(box).resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();
   const placed=Buffer.alloc(s.width*s.height*4);
   for(let y=0;y<h;y++)for(let x=0;x<w;x++){
    const dx=cx+x,dy=cy+y;if(dx<0||dy<0||dx>=s.width||dy>=s.height)throw Error('Icon overflows sprite');
    icon.copy(placed,(dy*s.width+dx)*4,(y*w+x)*4,(y*w+x+1)*4);
   }
   for(let y=0;y<s.height;y++)placed.copy(result,((s.y+y)*info.width+s.x)*4,y*s.width*4,(y+1)*s.width*4);
   const cellFile=s.name+'.png';fs.writeFileSync(path.join(out,'cells',cellFile),await png(placed,s.width,s.height));
   entries.push({...s,oldBounds:oldBox,newBounds:bounds(placed,s.width,s.height),cellFile});
  }
  const output=await png(result,info.width,info.height);fs.writeFileSync(path.join(out,'staged',file),output);
  report.push({file,key,width:info.width,height:info.height,guid:/^guid: (\w+)/m.exec(meta)[1],metadataSha256:hash(Buffer.from(meta)),imageSha256:hash(output),sprites:entries});
 }
 fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({spriteCount:report.reduce((n,r)=>n+r.sprites.length,0),atlasCount:report.length,metadataUnchanged:true,assets:report},null,2));
 // Compact contact sheet at small display size against actual pastel UI colors.
 const all=report.flatMap(r=>r.sprites.map(s=>({...s,file:r.file})));const ops=[];
 for(let i=0;i<all.length;i++){
  const s=all[i],x=24+(i%11)*112,y=65+Math.floor(i/11)*112;
  const a=await sharp(path.join(out,'before',s.file)).extract({left:s.x,top:s.y,width:s.width,height:s.height}).resize(64,64,{fit:'contain'}).png().toBuffer();
  const b=await sharp(path.join(out,'cells',s.cellFile)).resize(64,64,{fit:'contain'}).png().toBuffer();
  ops.push({input:a,left:x+20,top:y},{input:b,left:x+20,top:y+410});
 }
 const labels=Buffer.from('<svg width="1280" height="820"><style>text{font: bold 24px Arial;fill:#715b4e}</style><text x="28" y="38">BEFORE</text><text x="28" y="448">AFTER — PASTEL</text></svg>');
 ops.push({input:labels,left:0,top:0});
 await sharp({create:{width:1280,height:820,channels:4,background:'#f3e9d8'}}).composite(ops).png().toFile(path.join(out,'comparison.png'));
 if(process.argv.includes('--apply')){
  for(const r of report){const p=path.join(ui,r.file),current=hash(fs.readFileSync(p));if(current!==r.imageSha256&&current!==hash(fs.readFileSync(path.join(out,'before',r.file))))throw Error('Unrelated image change: '+r.file);}
  for(const r of report)fs.copyFileSync(path.join(out,'staged',r.file),path.join(ui,r.file));
  const unchanged=JSON.parse(fs.readFileSync(snapshot));for(const [rel,expected]of Object.entries(unchanged))if(hash(fs.readFileSync(path.join(root,rel)))!==expected)throw Error('Unexpected change: '+rel);
  console.log('APPLIED 33 pastel icons in 4 PNG files. Metadata, UI layout, scripts and other UI assets unchanged.');
 }else console.log('STAGED 33 icons; production files unchanged.');
})().catch(e=>{console.error(e);process.exit(1);});
