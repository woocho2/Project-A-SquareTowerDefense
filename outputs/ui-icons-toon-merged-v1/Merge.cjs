const fs=require('fs'),path=require('path'),crypto=require('crypto'),cp=require('child_process');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,ui='Assets/4. Asset/5. UI/';
const atlasRel=ui+'UI_Icon_Atlas.png',buttonRel=ui+'Button_Symbol_Atlas.png';
const oldGuid='a59a7d01000100010001000100010099',guid='b201a001000100010001000100010010';
const CELL=512,WIDTH=CELL*4,HEIGHT=CELL*2,INK=CELL*.875;
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function backup(rel){const dest=path.join(out,'before',rel);fs.mkdirSync(path.dirname(dest),{recursive:true});if(!fs.existsSync(dest))fs.copyFileSync(path.join(root,rel),dest);return dest;}
function write(rel,data){const dest=path.join(root,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,data);}
function activeRefs(search){try{return cp.execFileSync('rg',['-l','-F',search,'Assets','--glob','*.{prefab,unity,asset,mat,controller,anim,overrideController,playable}'],{cwd:root,encoding:'utf8'}).trim().split(/\r?\n/).filter(Boolean).map(p=>p.replaceAll('\\','/'));}catch(e){if(e.status===1)return[];throw e;}}
function blocks(meta){return [...meta.matchAll(/^    - serializedVersion: 2\r?\n      name: [\s\S]*?(?=^    - serializedVersion: 2|^    outline: \[\])/gm)].map(m=>({block:m[0],name:/      name: ([^\r\n]+)/.exec(m[0])[1],id:/      internalID: (-?\d+)/.exec(m[0])[1]}));}
function getBounds(data,w,h){let l=w,t=h,r=-1,b=-1;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(data[(y*w+x)*4+3]>=32){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}if(r<l)throw Error('Empty cell');return{left:l,top:t,width:r-l+1,height:b-t+1};}
function clean(data,w,h){
 for(let p=0;p<data.length;p+=4){const hi=Math.max(data[p],data[p+1],data[p+2]),lo=Math.min(data[p],data[p+1],data[p+2]);if(data[p+3]<12||(data[p+3]<128&&hi>210&&lo<30&&hi-lo>190))data[p+3]=0;}
 const seen=new Uint8Array(w*h),parts=[];
 for(let n=0;n<seen.length;n++){if(seen[n]||data[n*4+3]<32)continue;const list=[n];seen[n]=1;for(let k=0;k<list.length;k++){const p=list[k],x=p%w,y=Math.floor(p/w);for(const q of[x>0?p-1:-1,x<w-1?p+1:-1,y>0?p-w:-1,y<h-1?p+w:-1])if(q>=0&&!seen[q]&&data[q*4+3]>=32){seen[q]=1;list.push(q);}}parts.push(list);}
 parts.sort((a,b)=>b.length-a.length);if(!parts.length)throw Error('Missing icon');
 const keep=new Uint8Array(w*h);for(const part of parts)if(part.length>=Math.max(12,parts[0].length*.01))for(const n of part){keep[n]=1;const x=n%w,y=Math.floor(n/w);for(const q of[x>0?n-1:-1,x<w-1?n+1:-1,y>0?n-w:-1,y<h-1?n+w:-1])if(q>=0&&data[q*4+3]>0)keep[q]=1;}
 for(let n=0;n<keep.length;n++)if(!keep[n])data[n*4]=data[n*4+1]=data[n*4+2]=data[n*4+3]=0;
 return data;
}
function hue(r,g,b){const max=Math.max(r,g,b),min=Math.min(r,g,b),d=max-min;if(!d)return-1;let h=max===r?60*((g-b)/d):max===g?60*((b-r)/d+2):60*((r-g)/d+4);return(h+360)%360;}
async function png(p,w,h){return sharp(p,{raw:{width:w,height:h,channels:4}}).png().toBuffer();}
(async()=>{
 fs.mkdirSync(path.join(out,'staged'),{recursive:true});
 for(const rel of[atlasRel,atlasRel+'.meta',buttonRel,buttonRel+'.meta'])backup(rel);
 const oldMeta=fs.readFileSync(path.join(out,'before',atlasRel+'.meta'),'utf8');
 const oldButtons=fs.readFileSync(path.join(out,'before',buttonRel+'.meta'),'utf8');
 const original=blocks(oldMeta),buttons=blocks(oldButtons);
 const names=['Icon_Gold','Icon_Gem','Icon_Heart','Icon_Heart_White','Symbol_CreateTower','Symbol_TierUpgrade','Symbol_ColorUpgrade','Symbol_MiddleBoss'];
 const selected=names.map(name=>[...original,...buttons].find(s=>s.name===name));if(selected.some(s=>!s))throw Error('Missing original sprite metadata');
 const sourceFiles=['resources-hires-generated.png','buttons-hires-generated.png'];
 const sourceInfo=await Promise.all(sourceFiles.map(file=>sharp(path.join(out,file)).metadata()));
 const cells=[];
 for(let n=0;n<8;n++){
  const sheet=n<4?0:1,local=n%4,info=sourceInfo[sheet];
  const col=local%2,row=Math.floor(local/2),x=Math.round(col*info.width/2),y=Math.round(row*info.height/2),w=Math.round((col+1)*info.width/2)-x,h=Math.round((row+1)*info.height/2)-y;
  if(Math.min(w,h)<CELL)throw Error('Source cell is smaller than target sprite');
  const data=clean(await sharp(path.join(out,sourceFiles[sheet])).extract({left:x,top:y,width:w,height:h}).ensureAlpha().raw().toBuffer(),w,h);
  const box=getBounds(data,w,h),scale=INK/Math.max(box.width,box.height),nw=Math.round(box.width*scale),nh=Math.round(box.height*scale);
  const pixels=await sharp(data,{raw:{width:w,height:h,channels:4}}).extract(box).resize(nw,nh,{fit:'fill'}).ensureAlpha().raw().toBuffer();
  const cell=Buffer.alloc(CELL*CELL*4),left=Math.round((CELL-nw)/2),top=Math.round((CELL-nh)/2);
  for(let iy=0;iy<nh;iy++)pixels.copy(cell,((top+iy)*CELL+left)*4,iy*nw*4,(iy+1)*nw*4);
  cells.push(cell);
 }
 // The protective white heart overlays the red heart. Share the exact same
 // alpha, outline and placement, using the generated white-heart cel palette.
 const red=cells[2],white=Buffer.from(red),paint=cells[3];
 for(let p=0;p<red.length;p+=4){
  if(!red[p+3]){white[p]=white[p+1]=white[p+2]=white[p+3]=0;continue;}
  const h=hue(red[p],red[p+1],red[p+2]);
  if(h>=0&&(h<16||h>344)&&red[p]>100){
   const light=.2126*red[p]+.7152*red[p+1]+.0722*red[p+2];
   const sourceLight=.2126*paint[p]+.7152*paint[p+1]+.0722*paint[p+2];
   const color=paint[p+3]>128&&sourceLight>185?[paint[p],paint[p+1],paint[p+2]]:light>150?[250,244,233]:[216,202,184];
   [white[p],white[p+1],white[p+2]]=color;
  }
 }
 cells[3]=white;
 let alphaMismatch=0;for(let p=3;p<red.length;p+=4)if(red[p]!==white[p])alphaMismatch++;if(alphaMismatch)throw Error('Heart masks differ');
 const packed=Buffer.alloc(WIDTH*HEIGHT*4);
 for(let n=0;n<8;n++)for(let y=0;y<CELL;y++)cells[n].copy(packed,((Math.floor(n/4)*CELL+y)*WIDTH+n%4*CELL)*4,y*CELL*4,(y+1)*CELL*4);
 const atlas=await png(packed,WIDTH,HEIGHT);fs.writeFileSync(path.join(out,'staged','UI_Icon_Atlas.png'),atlas);
 const newBlocks=selected.map((s,n)=>s.block.replace(/        x: \d+/,'        x: '+n%4*CELL).replace(/        y: \d+/,'        y: '+(Math.floor(n/4)===0?CELL:0)).replace(/        width: \d+/,'        width: '+CELL).replace(/        height: \d+/,'        height: '+CELL)).join('');
 const start=oldMeta.indexOf('    - serializedVersion: 2'),end=oldMeta.search(/^    outline: \[\]\r?$/m);if(start<0||end<start)throw Error('Bad sprite metadata');
 let meta=oldMeta.slice(0,start)+newBlocks+oldMeta.slice(end);
 meta=meta.replace(/^    nameFileIdTable:[\s\S]*?(?=^  mipmapLimitGroupName:)/m,'    nameFileIdTable:\n'+selected.map(s=>'      '+s.name+': '+s.id+'\n').join(''));
 meta=meta.replace(/(textureCompression:) 1/g,'$1 0').replace(/(maxTextureSize:) 2048/g,'$1 '+WIDTH).replace(/filterMode: 0/,'filterMode: 1');
 fs.writeFileSync(path.join(out,'staged','UI_Icon_Atlas.png.meta'),meta);
 const retired=original.filter(s=>!names.includes(s.name));
 const affected=new Set([...activeRefs(oldGuid),...activeRefs(guid)]),migrations=[];
 for(const rel of affected){
  const before=fs.readFileSync(backup(rel),'utf8');let changed=before,relinked=0,disabled=0;
  changed=changed.replace(new RegExp('fileID: (10010[1-4]), guid: '+oldGuid,'g'),(match,id)=>{relinked++;return'fileID: '+id+', guid: '+guid;});
  const documents=changed.split(/(?=^--- !u!)/m);
  for(let n=0;n<documents.length;n++)for(const old of retired){
   const needle='m_Sprite: {fileID: '+old.id+', guid: '+guid+', type: 3}';
   if(documents[n].includes(needle)){
    if(!documents[n].includes('guid: fe87c0e1cc204ed48ad3b37840f39efc'))throw Error('Retired icon in unsupported component: '+rel);
    documents[n]=documents[n].replace(needle,'m_Sprite: {fileID: 0}').replace(/^  m_Enabled: 1/m,'  m_Enabled: 0');disabled++;
   }
  }
  changed=documents.join('');
  if(changed.includes(oldGuid))throw Error('Unsupported old atlas reference remains: '+rel);
  if(changed!==before){const dest=path.join(out,'staged',rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,changed);migrations.push({path:rel,relinked,disabledRetiredImages:disabled,beforeHash:hash(Buffer.from(before)),afterHash:hash(Buffer.from(changed))});}
 }
 for(let n=0;n<8;n++)fs.writeFileSync(path.join(out,names[n]+'.png'),await png(cells[n],CELL,CELL));
 const labels=Buffer.from('<svg width="1100" height="710"><style>text{font-family:Arial;font-weight:bold;fill:#745e50}</style><text x="40" y="42" font-size="28">TOON UI — ONE ATLAS</text><text x="40" y="620" font-size="20">HEART OVERLAY: red / half white / white</text></svg>');
 const ops=[{input:await sharp(atlas).resize(1024,512).png().toBuffer(),left:38,top:65}];
 for(let n=0;n<3;n++){
  const a=Buffer.from(red);if(n>0)for(let y=0;y<CELL;y++)for(let x=0;x<CELL;x++)if(n===2||x<CELL/2){const p=(y*CELL+x)*4;white.copy(a,p,p,p+4);}
  ops.push({input:await sharp(await png(a,CELL,CELL)).resize(64,64).png().toBuffer(),left:50+n*100,top:635});
 }
 ops.push({input:labels,left:0,top:0});await sharp({create:{width:1100,height:710,channels:4,background:'#f3e9d8'}}).composite(ops).png().toFile(path.join(out,'preview.png'));
 const report={icons:8,atlasGuid:guid,oldButtonGuid:oldGuid,size:[WIDTH,HEIGHT],cellSize:CELL,sourceSheets:sourceInfo.map((s,i)=>({file:sourceFiles[i],width:s.width,height:s.height,nativeCell:Math.min(s.width,s.height)/2})),oldCombinedTexels:1024*512+2048*2048,newTexels:WIDTH*HEIGHT,texelReduction:(1024*512+2048*2048)/(WIDTH*HEIGHT),heartAlphaMismatch:alphaMismatch,retired:retired.map(s=>({name:s.name,fileID:s.id})),sprites:selected.map((s,n)=>({name:s.name,fileID:s.id,x:n%4*CELL,y:Math.floor(n/4)===0?CELL:0,width:CELL,height:CELL})),migrations,atlasSha256:hash(atlas)};
 fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify(report,null,2));
 if(process.argv.includes('--apply')){
  const manifestPath=path.join(out,'applied-hashes.json'),applied=fs.existsSync(manifestPath)?JSON.parse(fs.readFileSync(manifestPath)):{};
  const changes=[{path:atlasRel,source:path.join(out,'staged','UI_Icon_Atlas.png')},{path:atlasRel+'.meta',source:path.join(out,'staged','UI_Icon_Atlas.png.meta')},...migrations.map(m=>({path:m.path,source:path.join(out,'staged',m.path)}))];
  for(const c of changes){const current=hash(fs.readFileSync(path.join(root,c.path))),old=hash(fs.readFileSync(path.join(out,'before',c.path)));if(current!==old&&current!==applied[c.path]&&current!==hash(fs.readFileSync(c.source)))throw Error('Unrelated changes: '+c.path);}
  for(const c of changes)write(c.path,fs.readFileSync(c.source));
  fs.writeFileSync(manifestPath,JSON.stringify(Object.fromEntries(changes.map(c=>[c.path,hash(fs.readFileSync(path.join(root,c.path)))])),null,2));
  if(activeRefs(oldGuid).length)throw Error('Old button atlas still referenced');
  for(const item of retired)for(const rel of activeRefs(guid))if(fs.readFileSync(path.join(root,rel),'utf8').includes('fileID: '+item.id+', guid: '+guid))throw Error('Retired sprite still referenced');
  console.log('APPLIED 8 toon icons, exact shared heart mask, button references migrated. Old button texture ready to retire.');
 }else console.log('STAGED 8 icons and migrations. No game files changed.');
})().catch(e=>{console.error(e);process.exit(1);});
