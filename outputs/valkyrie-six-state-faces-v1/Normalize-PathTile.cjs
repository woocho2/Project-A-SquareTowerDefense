const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,assetDir='Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/StateFaces_v1';
const write=(p,t)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,t)};
const sha=b=>crypto.createHash('sha256').update(b).digest('hex');
const hash=s=>crypto.createHash('md5').update('ValkyrieSixStateFaces_v1:'+s).digest('hex');
const copyOnce=(a,b)=>{if(fs.existsSync(a)&&!fs.existsSync(b)){fs.mkdirSync(path.dirname(b),{recursive:true});fs.copyFileSync(a,b)}};
const folder=id=>`fileFormatVersion: 2\nguid: ${hash(id)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;
const hueByKey={SelfHeal:351,Defense:46,Mobility:198,Action:141,Locked:0,Hourglass:268};
function hsv(r,g,b){r/=255;g/=255;b/=255;const max=Math.max(r,g,b),min=Math.min(r,g,b),d=max-min;let h=0;if(d)h=max===r?60*((g-b)/d%6):max===g?60*((b-r)/d+2):60*((r-g)/d+4);return[(h+360)%360,max?d/max:0,max];}
function rgb(h,s,v){h=(h%360+360)%360;const c=v*s,x=c*(1-Math.abs(h/60%2-1)),m=v-c;let q=h<60?[c,x,0]:h<120?[x,c,0]:h<180?[0,c,x]:h<240?[0,x,c]:h<300?[x,0,c]:[c,0,x];return q.map(a=>Math.round((a+m)*255));}
async function raw(p){return sharp(p).ensureAlpha().raw().toBuffer({resolveWithObject:true});}
async function png(data,width=512,height=512){return sharp(data,{raw:{width,height,channels:4}}).png().toBuffer();}
function bounds(data,width=512){let x0=width,y0=width,x1=-1,y1=-1;for(let y=0;y<width;y++)for(let x=0;x<width;x++)if(data[(y*width+x)*4+3]){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}return{x:x0,y:y0,width:x1-x0+1,height:y1-y0+1};}
function alphaEqual(a,b){for(let i=3;i<a.length;i+=4)if(a[i]!==b[i])return false;return true;}
const darken=hex=>'#'+hex.slice(1).match(/../g).map(v=>Math.round(parseInt(v,16)*.66).toString(16).padStart(2,'0')).join('');
(async()=>{
 const records=JSON.parse(fs.readFileSync(path.join(out,'generation-manifest.json'),'utf8'));if(records.length!==6)throw Error('Six states required');
 for(const name of ['manifest.json','README.md','overview.png','Enemy_Valkyrie_StateFaces_Atlas_v1.png'])copyOnce(path.join(out,name),path.join(out,'before-path-fit',name));
 for(const r of records)for(const suffix of ['.png','.png.meta']){const name='Enemy_Valkyrie_Face_'+r.key+'_v1'+suffix;copyOnce(path.join(root,assetDir,name),path.join(out,'before-path-fit',name));}
 const masterPath=path.join(root,'outputs/fallen-valkyrie-selfheal-v1/source/Front.png');
 const masterPack=JSON.parse(fs.readFileSync(path.join(root,'outputs/fallen-valkyrie-selfheal-v1/generation/Front.json'),'utf8')).packing;
 const portrait=await sharp(masterPath).extract(masterPack.source).resize(masterPack.width*2,masterPack.height*2).png().toBuffer();
 const master=(await sharp({create:{width:512,height:512,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:portrait,left:masterPack.left*2,top:masterPack.top*2}]).ensureAlpha().raw().toBuffer());
 const outer=(await sharp(path.join(root,'outputs/tower-square-frames-v1/outer-mask256.png')).resize(512,512,{kernel:'nearest'}).ensureAlpha().raw().toBuffer());
 const decoration=(await sharp(path.join(root,'Assets/4. Asset/1. BackGround/Tile_Path_Overlay_Asgard_Atlas.png')).extract({left:512,top:512,width:256,height:256}).resize(512,512,{kernel:'nearest'}).ensureAlpha().raw().toBuffer());
 const pathTile=await sharp(path.join(root,'Assets/4. Asset/1. BackGround/Tile_Floor_Atlas.png')).extract({left:0,top:0,width:256,height:256}).png().toBuffer();
 const equipmentMask=new Uint8Array(512*512);let marked=0;
 for(let y=0;y<512;y++)for(let x=0;x<512;x++){
  const i=(y*512+x)*4,[h,s,v]=hsv(master[i],master[i+1],master[i+2]);
  // Keep all skin, eyes and mouth exactly intact; only source red equipment changes.
  const faceRegion=x>=112&&x<=400&&y>=238&&y<=384;
  if(master[i+3]>0&&!faceRegion&&(h>=335||h<=18)&&s>=.32&&v>=.1){equipmentMask[y*512+x]=1;marked++;}
 }
 const singleMeta=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Emblems_v1/Enemy_Emblem_SelfHeal_White_v1.png.meta'),'utf8');
 const spriteMeta=(id)=>singleMeta.replace(/^guid: .+$/m,'guid: '+hash(id)).replace(/^    spriteID: \w+/m,'    spriteID: '+hash('sprite:'+id)).replace(/spritePixelsToUnits: \d+/,'spritePixelsToUnits: 384');
 const textureMeta=id=>spriteMeta(id).replace(/textureType: 8/,'textureType: 0').replace(/spriteMode: 1/,'spriteMode: 0').replace(/alphaIsTransparency: 1/,'alphaIsTransparency: 0');
 for(const dir of ['Layers','Textures3D'])write(path.join(root,assetDir,dir+'.meta'),folder(dir));
 const tiles=[],opaqueTiles=[],entries=[];let referencePortrait,referenceFrame;
 for(const r of records){
  const character=Buffer.from(master),frame=Buffer.from(decoration),hue=hueByKey[r.key];
  for(let p=0;p<512*512;p++){
   const i=p*4;
   if(equipmentMask[p]&&r.key!=='SelfHeal'){
    const [h,s,v]=hsv(master[i],master[i+1],master[i+2]);const q=rgb(hue+(h>180?h-351:h+9)*.2,r.key==='Locked'?0:s,v);for(let c=0;c<3;c++)character[i+c]=q[c];
   }
   if(frame[i+3]){const[h,s,v]=hsv(frame[i],frame[i+1],frame[i+2]);const q=rgb(hue+(h-45)*.2,r.key==='Locked'?0:s*.95,v);for(let c=0;c<3;c++)frame[i+c]=q[c];}
  }
  if(referencePortrait&&!alphaEqual(referencePortrait,character))throw Error('Portrait geometry differs');
  if(referenceFrame&&!alphaEqual(referenceFrame,frame))throw Error('Decoration geometry differs');
  referencePortrait=character;referenceFrame=frame;
  for(let p=0;p<512*512;p++)if(!equipmentMask[p])for(let c=0;c<4;c++)if(character[p*4+c]!==master[p*4+c])throw Error('Non-equipment portrait pixel changed');
  const characterPNG=await png(character),framePNG=await png(frame);
  const bg=await sharp({create:{width:512,height:512,channels:4,background:r.bg}}).raw().toBuffer();for(let i=3;i<bg.length;i+=4)bg[i]=outer[i];
  const bgPNG=await png(bg);
  for(const [kind,data]of[['Portrait',characterPNG],['Decoration',framePNG],['Background',bgPNG]]){
   const name=kind+'_'+r.key+'.png';write(path.join(out,'layers',name),data);write(path.join(root,assetDir,'Layers',name),data);write(path.join(root,assetDir,'Layers',name+'.meta'),spriteMeta(kind+':'+r.key));
  }
  const icon=await sharp(path.join(out,'symbols',r.key+'.png')).resize(64,64).png().toBuffer();
  const badge=Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="76" height="76"><circle cx="38" cy="38" r="37" fill="${darken(r.color)}" stroke="#ffffff" stroke-opacity=".8" stroke-width="2"/></svg>`);
  const opaque=await sharp({create:{width:512,height:512,channels:4,background:r.bg}}).composite([{input:characterPNG},{input:framePNG},{input:badge,left:218,top:26},{input:icon,left:224,top:32}]).ensureAlpha().raw().toBuffer();
  const clipped=Buffer.from(opaque);for(let i=3;i<clipped.length;i+=4)clipped[i]=outer[i];
  const tile=await png(clipped),opaquePNG=await png(opaque),name='Enemy_Valkyrie_Face_'+r.key+'_v1.png';
  write(path.join(out,'faces',r.key+'.png'),tile);write(path.join(root,assetDir,name),tile);write(path.join(root,assetDir,name+'.meta'),spriteMeta(r.key));
  write(path.join(root,assetDir,'Textures3D',name),opaquePNG);write(path.join(root,assetDir,'Textures3D',name+'.meta'),textureMeta('3D:'+r.key));
  tiles.push(tile);opaqueTiles.push(opaquePNG);
  const actualBounds=bounds(clipped),frameBounds=bounds(frame);
  if(JSON.stringify(actualBounds)!==JSON.stringify({x:14,y:14,width:484,height:484})||JSON.stringify(frameBounds)!==JSON.stringify(actualBounds))throw Error('Path decoration bounds do not match');
  entries.push({key:r.key,label:r.label,color:r.color,background:r.bg,recommendedCubeAxis:r.axis,path:assetDir+'/'+name,guid:hash(r.key),texture3D:assetDir+'/Textures3D/'+name,layers:{portrait:assetDir+'/Layers/Portrait_'+r.key+'.png',decoration:assetDir+'/Layers/Decoration_'+r.key+'.png',background:assetDir+'/Layers/Background_'+r.key+'.png'},bounds:actualBounds,portraitPlacement:{left:masterPack.left*2,top:masterPack.top*2,width:masterPack.width*2,height:masterPack.height*2},badge:{left:218,top:26,size:76},sha256:sha(tile)});
 }
 const atlasName='Enemy_Valkyrie_StateFaces_Atlas_v1.png';
 const atlas=await sharp({create:{width:1536,height:1024,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(tiles.map((input,i)=>({input,left:i%3*512,top:Math.floor(i/3)*512}))).png().toBuffer();
 write(path.join(out,atlasName),atlas);write(path.join(root,assetDir,atlasName),atlas);write(path.join(root,assetDir,atlasName+'.meta'),textureMeta('atlas').replace(/alphaIsTransparency: 0/,'alphaIsTransparency: 1'));
 const overview=[];for(let i=0;i<6;i++){const left=i%3*320,top=Math.floor(i/3)*352;overview.push({input:await sharp(tiles[i]).resize(304,304).png().toBuffer(),left:left+8,top:top+40},{input:Buffer.from(`<svg width="320" height="36"><text x="14" y="27" fill="#e9eff4" font-size="21" font-family="Malgun Gothic,Arial">${entries[i].label}</text></svg>`),left,top});}
 await sharp({create:{width:960,height:704,channels:4,background:'#24303a'}}).composite(overview).png().toFile(path.join(out,'overview.png'));
 const pathWithDecoration=await sharp(pathTile).composite([{input:await sharp(await png(referenceFrame)).resize(256,256).png().toBuffer()}]).png().toBuffer();
 const compare=[pathTile,pathWithDecoration,await sharp(tiles[0]).resize(256,256).png().toBuffer()];
 await sharp({create:{width:768,height:312,channels:4,background:'#24303a'}}).composite(compare.flatMap((input,i)=>[{input,left:i*256,top:40},{input:Buffer.from(`<svg width="256" height="36"><text x="12" y="26" fill="#e9eff4" font-size="17" font-family="Malgun Gothic,Arial">${['실제 패스 타일','같은 크기의 장식','수정된 몬스터 면'][i]}</text></svg>`),left:i*256,top:0}])).png().toFile(path.join(out,'path-fit-preview.png'));
 const images=opaqueTiles.map(b=>'data:image/png;base64,'+b.toString('base64'));
 write(path.join(out,'preview.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>발키리 6면 · 패스 타일 크기</title><style>*{box-sizing:border-box}body{margin:0;background:#111921;color:#e9eff4;font:16px system-ui;padding:28px}main{max-width:1100px;margin:auto}p{color:#bed0da;line-height:1.7}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}article{background:#24303a;border-radius:14px;padding:12px}h2{font-size:18px}article img{width:100%;display:block}.stage{height:410px;display:grid;place-items:center;background:#24303a;border-radius:14px;margin:28px 0;perspective:1100px}.cube{width:220px;height:220px;position:relative;transform-style:preserve-3d;transition:transform .6s}.face{width:220px;height:220px;position:absolute;backface-visibility:hidden}.SelfHeal{transform:translateZ(110px)}.Defense{transform:rotateY(180deg) translateZ(110px)}.Mobility{transform:rotateY(90deg) translateZ(110px)}.Action{transform:rotateY(-90deg) translateZ(110px)}.Locked{transform:rotateX(90deg) translateZ(110px)}.Hourglass{transform:rotateX(-90deg) translateZ(110px)}button{background:#304a5c;color:#e9eff4;border:1px solid #547084;border-radius:8px;font:inherit;margin:4px;padding:10px 14px;cursor:pointer}@media(max-width:650px){.grid{grid-template-columns:repeat(2,1fr)}}</style><main><h1>발키리 6면 · 크기 통일</h1><p>패스 타일 크기를 기준으로 얼굴·장식·심볼의 크기와 위치를 통일했습니다.</p><section class="grid">${entries.map((e,i)=>`<article><h2>${e.label}</h2><img src="${images[i]}" alt="${e.label}"></article>`).join('')}</section><div class="stage"><div class="cube">${entries.map((e,i)=>`<img class="face ${e.key}" src="${images[i]}" alt="${e.label}">`).join('')}</div></div><div id="controls"></div><button id="all">세 면 함께 보기</button><button id="roll">90도 굴리기</button></main><script>const names=${JSON.stringify(entries.map(e=>e.label))},angles=[[0,0],[0,180],[0,-90],[0,90],[-90,0],[90,0]];let x=-20,y=-30;const cube=document.querySelector('.cube');function draw(){cube.style.transform='rotateX('+x+'deg) rotateY('+y+'deg)'}names.forEach((n,i)=>{let b=document.createElement('button');b.textContent=n;b.onclick=()=>{[x,y]=angles[i];draw()};document.querySelector('#controls').append(b)});document.querySelector('#all').onclick=()=>{x=-20;y=-30;draw()};document.querySelector('#roll').onclick=()=>{x-=90;draw()};draw();</script></html>`);
 const manifest={size:512,spritePPU:384,canvasWorldSize:512/384,gridCellSize:1.5,cubeSideRecommendation:512/384,pathReference:{canvas:256,ppu:192},decorationBounds:{x:14,y:14,width:484,height:484},portraitMaster:'outputs/fallen-valkyrie-selfheal-v1/source/Front.png',portraitMasterSHA256:sha(fs.readFileSync(masterPath)),equipmentMaskPixels:marked,geometry:'same master portrait and exact path decoration alpha for all six states',atlas:assetDir+'/'+atlasName,atlasSize:[1536,1024],assets:entries};
 write(path.join(out,'manifest.json'),JSON.stringify(manifest,null,2));
 write(path.join(out,'export-validation.txt'),'PASS six equal 512x512 sprites, 384 PPU, 1.3333333333 world canvas matching 256px / 192 PPU path\nPASS all six outer bounds (14,14,484,484), identical portrait and decoration alpha\nPASS all non-equipment master portrait pixels unchanged (skin, eyes, mouth, gold, hair)\nPASS identical portrait placement, badge size and position; six opaque 3D texture alternatives\n');
 console.log('Normalized six faces and decoration to path-tile canvas; one exact portrait geometry, 384 PPU, separated layers and opaque 3D textures');
})().catch(e=>{console.error(e);process.exitCode=1});
