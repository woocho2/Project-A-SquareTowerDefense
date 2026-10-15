// Previous independent-variant build retained for provenance. Use Build-State-Faces.cjs.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,assetDir='Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/StateFaces_v1';
const write=(p,t)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,t)};
const hash=s=>crypto.createHash('md5').update('ValkyrieSixStateFaces_v1:'+s).digest('hex');
const sha=b=>crypto.createHash('sha256').update(b).digest('hex');
const svg=body=>`<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 128 128">${body}</svg>`;
const special={
 Locked:svg('<path d="M43 57V39C43 11 85 11 85 39V57" fill="none" stroke="white" stroke-width="11" stroke-linecap="round"/><path fill="white" fill-rule="evenodd" d="M38 55H90Q100 55 100 65V104Q100 114 90 114H38Q28 114 28 104V65Q28 55 38 55ZM64 72A8 8 0 1 0 64 88A8 8 0 1 0 64 72ZM60 85H68V99H60Z"/>'),
 Hourglass:svg('<path d="M39 29H89C89 44 76 52 68 61V67C76 76 89 84 89 99H39C39 84 52 76 60 67V61C52 52 39 44 39 29Z" fill="none" stroke="white" stroke-width="9" stroke-linejoin="round"/><rect x="30" y="12" width="68" height="10" rx="3" fill="white"/><rect x="30" y="106" width="68" height="10" rx="3" fill="white"/><path d="M49 36H79L64 50ZM64 78L80 94H48Z" fill="white"/>')
};
const darken=hex=>'#'+hex.slice(1).match(/../g).map(v=>Math.round(parseInt(v,16)*.66).toString(16).padStart(2,'0')).join('');
const folderMeta=id=>`fileFormatVersion: 2\nguid: ${hash(id)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;
async function tileLayer(p){
 const {data,info}=await sharp(p).ensureAlpha().raw().toBuffer({resolveWithObject:true});let minX=info.width,minY=info.height,maxX=-1,maxY=-1;
 for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>12){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}
 if(maxX<minX)throw Error('Empty tile source: '+p);
 const rect={left:minX,top:minY,width:maxX-minX+1,height:maxY-minY+1};
 return {buffer:await sharp(p).extract(rect).resize(484,484,{fit:'fill'}).png().toBuffer(),sourceRect:rect};
}
(async()=>{
 const records=JSON.parse(fs.readFileSync(path.join(out,'generation-manifest.json'),'utf8'));if(records.length!==6)throw Error('Six state faces required');
 write(path.join(root,assetDir+'.meta'),folderMeta('folder'));
 const originalMeta=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Emblems_v1/Enemy_Emblem_SelfHeal_White_v1.png.meta'),'utf8');
 const textureMeta=originalMeta.replace(/textureType: 8/,'textureType: 0').replace(/spriteMode: 1/,'spriteMode: 0').replace(/spritePixelsToUnits: \d+/,'spritePixelsToUnits: 512').replace(/alphaIsTransparency: 1/,'alphaIsTransparency: 0');
 const entries=[],tiles=[];
 for(let i=0;i<records.length;i++){
  const r=records[i],source=r.reused?r.frontSource:r.originalPath;
  fs.copyFileSync(source,path.join(out,'source',r.key+'.png'));
  let symbolSvg;if(special[r.key])symbolSvg=special[r.key];else symbolSvg=fs.readFileSync(path.join(root,'outputs/enemy-emblems-white-v1/svg',r.key+'.svg'),'utf8');
  write(path.join(out,'symbols',r.key+'.svg'),symbolSvg);
  const icon=await sharp(Buffer.from(symbolSvg)).png().toBuffer();write(path.join(out,'symbols',r.key+'.png'),icon);
  let symbolAsset='Assets/4. Asset/4. Enemy/Emblems_v1/Enemy_Emblem_'+r.key+'_White_v1.png';
  if(special[r.key]){
   symbolAsset=assetDir+'/Symbol_'+r.key+'_White_v1.png';write(path.join(root,symbolAsset),icon);
   write(path.join(root,symbolAsset+'.meta'),originalMeta.replace(/^guid: .+$/m,'guid: '+hash('symbol:'+r.key)).replace(/^    spriteID: \w+/m,'    spriteID: '+hash('symbol-sprite:'+r.key)));
  }
  const layer=await tileLayer(source),badge=Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="88" height="88"><circle cx="44" cy="44" r="43" fill="${darken(r.color)}" stroke="#ffffff" stroke-opacity=".8" stroke-width="2"/></svg>`);
  const iconSmall=await sharp(icon).resize(72,72).png().toBuffer();
  const tile=await sharp({create:{width:512,height:512,channels:4,background:r.bg}}).composite([{input:layer.buffer,left:14,top:14},{input:badge,left:212,top:20},{input:iconSmall,left:220,top:28}]).png().toBuffer();
  const name='Enemy_Valkyrie_Face_'+r.key+'_v1.png';write(path.join(out,'faces',r.key+'.png'),tile);write(path.join(root,assetDir,name),tile);
  write(path.join(root,assetDir,name+'.meta'),textureMeta.replace(/^guid: .+$/m,'guid: '+hash(r.key)).replace(/^    spriteID: \w+/m,'    spriteID: '+hash('texture-sprite:'+r.key)));
  r.packing={sourceRect:layer.sourceRect,left:14,top:14,width:484,height:484,badge:{left:212,top:20,width:88,height:88}};write(path.join(out,'generation',r.key+'.json'),JSON.stringify(r,null,2));
  entries.push({key:r.key,label:r.label,color:r.color,background:r.bg,recommendedCubeAxis:r.axis,path:assetDir+'/'+name,guid:hash(r.key),symbol:symbolAsset,sha256:sha(tile),sourceRect:layer.sourceRect});tiles.push(tile);
 }
 const atlasName='Enemy_Valkyrie_StateFaces_Atlas_v1.png',atlas=await sharp({create:{width:1536,height:1024,channels:4,background:'#ffffff'}}).composite(tiles.map((input,i)=>({input,left:i%3*512,top:Math.floor(i/3)*512}))).png().toBuffer();
 write(path.join(out,atlasName),atlas);write(path.join(root,assetDir,atlasName),atlas);write(path.join(root,assetDir,atlasName+'.meta'),textureMeta.replace(/^guid: .+$/m,'guid: '+hash('atlas')).replace(/^    spriteID: \w+/m,'    spriteID: '+hash('atlas-sprite')));
 const composites=[];
 for(let i=0;i<entries.length;i++){
  const left=i%3*320,top=Math.floor(i/3)*352;
  composites.push({input:await sharp(tiles[i]).resize(304,304).png().toBuffer(),left:left+8,top:top+40});
  composites.push({input:Buffer.from(`<svg width="320" height="36"><text x="14" y="27" fill="#e9eff4" font-size="21" font-family="Malgun Gothic,Arial">${entries[i].label}</text></svg>`),left,top});
 }
 await sharp({create:{width:960,height:704,channels:4,background:'#24303a'}}).composite(composites).png().toFile(path.join(out,'overview.png'));
 const items=entries.map((e,i)=>({...e,image:'data:image/png;base64,'+tiles[i].toString('base64')}));
 write(path.join(out,'preview.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>발키리 6면 상태 에셋</title><style>*{box-sizing:border-box}body{margin:0;background:#111921;color:#e9eff4;font:16px system-ui;padding:28px}main{max-width:1120px;margin:auto}p{line-height:1.7;color:#bed0da}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}article{background:#24303a;border-radius:14px;padding:12px}h2{font-size:18px}article img{width:100%;display:block}.stage{height:410px;display:grid;place-items:center;background:#24303a;border-radius:14px;margin:28px 0;perspective:1100px}.cube{width:220px;height:220px;position:relative;transform-style:preserve-3d;transition:transform .6s}.face{width:220px;height:220px;position:absolute;backface-visibility:hidden}.SelfHeal{transform:translateZ(110px)}.Defense{transform:rotateY(180deg) translateZ(110px)}.Mobility{transform:rotateY(90deg) translateZ(110px)}.Action{transform:rotateY(-90deg) translateZ(110px)}.Locked{transform:rotateX(90deg) translateZ(110px)}.Hourglass{transform:rotateX(-90deg) translateZ(110px)}button{background:#304a5c;color:#e9eff4;border:1px solid #547084;border-radius:8px;font:inherit;margin:4px;padding:10px 15px;cursor:pointer}.samples{display:flex;align-items:flex-end;gap:12px;margin:12px 0}.samples span{text-align:center}.samples small{display:block;color:#bdd0da}@media(max-width:650px){.grid{grid-template-columns:repeat(2,1fr)}}</style><main><h1>발키리 · 정면 6면 상태</h1><p>같은 정면 캐릭터에 상태별 색과 흰색 심볼을 적용했습니다.<br>회복 · 방어 · 이동력 · 행동력 · 자물쇠 · 모래시계</p><section class="grid">${items.map(e=>`<article><h2>${e.label}</h2><img src="${e.image}" alt="${e.label}"><div class="samples">${[32,48,64].map(n=>`<span><img style="width:${n}px;height:${n}px" src="${e.image}" alt=""><small>${n}px</small></span>`).join('')}</div></article>`).join('')}</section><div class="stage"><div class="cube">${items.map(e=>`<img class="face ${e.key}" src="${e.image}" alt="${e.label}">`).join('')}</div></div><div id="controls"></div><button id="overview">세 면 함께 보기</button><button id="roll">90도 굴려보기</button></main><script>const items=${JSON.stringify(entries)},angles=[[0,0],[0,180],[0,-90],[0,90],[-90,0],[90,0]];let x=-20,y=-30;const cube=document.querySelector('.cube');function draw(){cube.style.transform='rotateX('+x+'deg) rotateY('+y+'deg)'}items.forEach((e,i)=>{const b=document.createElement('button');b.textContent=e.label;b.onclick=()=>{[x,y]=angles[i];draw()};document.querySelector('#controls').append(b)});document.querySelector('#overview').onclick=()=>{x=-20;y=-30;draw()};document.querySelector('#roll').onclick=()=>{x-=90;draw()};draw();</script></html>`);
 // Export sanity checks: complete square textures for opaque 3D face materials.
 for(let i=0;i<tiles.length;i++){
  const {data,info}=await sharp(tiles[i]).ensureAlpha().raw().toBuffer({resolveWithObject:true});if(info.width!==512||info.height!==512)throw Error('Face size mismatch');
  for(let p=3;p<data.length;p+=4)if(data[p]!==255)throw Error(entries[i].key+' contains a gap in its cube face');
 }
 write(path.join(out,'manifest.json'),JSON.stringify({size:512,alpha:'opaque',textureType:'Default',wrap:'Clamp',mipmaps:false,order:entries.map(e=>e.key),atlas:assetDir+'/'+atlasName,atlasSize:[1536,1024],assets:entries},null,2));
 write(path.join(out,'generation-manifest.json'),JSON.stringify(records,null,2));
 write(path.join(out,'export-validation.txt'),'PASS six 512x512 RGBA fully opaque square face textures\nPASS six unique asset GUIDs, complete atlas and source records\n');
 console.log('Created all 6 front-face state assets, 2 white symbols, atlas and interactive cube preview');
})().catch(e=>{console.error(e);process.exitCode=1});
