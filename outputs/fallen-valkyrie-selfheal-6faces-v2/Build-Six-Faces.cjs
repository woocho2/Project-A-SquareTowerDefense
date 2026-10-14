const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,previous=path.join(root,'outputs/fallen-valkyrie-selfheal-v1');
const write=(p,t)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,t)};
const copy=(a,b)=>{fs.mkdirSync(path.dirname(b),{recursive:true});fs.copyFileSync(a,b)};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
async function fit(p,size){
  const {data,info}=await sharp(p).ensureAlpha().raw().toBuffer({resolveWithObject:true});let minX=info.width,minY=info.height,maxX=-1,maxY=-1;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>12){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}
  if(maxX<minX)throw Error('Empty generated source: '+p);
  const rect={left:minX,top:minY,width:maxX-minX+1,height:maxY-minY+1};
  const buffer=await sharp(p).extract(rect).resize({width:size,height:size,fit:'inside'}).png().toBuffer(),m=await sharp(buffer).metadata();
  const left=Math.round((256-m.width)/2),top=Math.round((256-m.height)/2);
  const result=await sharp({create:{width:256,height:256,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:buffer,left,top}]).png().toBuffer();
  return {result,packing:{source:rect,left,top,width:m.width,height:m.height}};
}
(async()=>{
  const records=JSON.parse(fs.readFileSync(path.join(out,'generation-manifest.json'),'utf8'));
  if(records.length!==2||records[0].key!=='Top'||records[1].key!=='Bottom')throw Error('Top and Bottom sources required');
  const keys=['Front','Left','Right','Back','Top','Bottom'],labels=['정면','왼쪽 측면','오른쪽 측면','뒷면','윗면 · 투구','아랫면 · 자가회복'];
  const frame=fs.readFileSync(path.join(previous,'Tower_Red_Frame.png')),background=fs.readFileSync(path.join(previous,'Tower_Red_Background.png'));
  copy(path.join(previous,'Tower_Red_Frame.png'),path.join(out,'layers/Frame_Red.png'));
  copy(path.join(previous,'Tower_Red_Background.png'),path.join(out,'layers/Background_Red.png'));
  const symbolPath=path.join(root,'outputs/enemy-emblems-white-v1/png/SelfHeal.png'),symbol=await sharp(symbolPath).resize(160,160).png().toBuffer();
  copy(symbolPath,path.join(out,'layers/SelfHeal_White.png'));
  for(const key of keys.slice(0,4)){
    copy(path.join(previous,'faces',key+'.png'),path.join(out,'faces',key+'.png'));
    if(sha(path.join(previous,'faces',key+'.png'))!==sha(path.join(out,'faces',key+'.png')))throw Error('Previous face changed: '+key);
  }
  for(const record of records){
    copy(record.originalPath,path.join(out,'source',record.key+'.png'));
    const fitted=await fit(record.originalPath,record.key==='Top'?232:244);
    write(path.join(out,'layers',record.key+'.png'),fitted.result);
    const overlays=[{input:fitted.result},{input:frame}];if(record.key==='Bottom')overlays.push({input:symbol,left:48,top:48});
    const tile=await sharp(background).composite(overlays).png().toBuffer();write(path.join(out,'faces',record.key+'.png'),tile);
    record.packing=fitted.packing;write(path.join(out,'generation',record.key+'.json'),JSON.stringify(record,null,2));
  }
  fs.writeFileSync(path.join(out,'generation-manifest.json'),JSON.stringify(records,null,2));
  const tiles=keys.map(key=>fs.readFileSync(path.join(out,'faces',key+'.png')));
  await sharp({create:{width:768,height:512,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(tiles.map((input,i)=>({input,left:i%3*256,top:Math.floor(i/3)*256}))).png().toFile(path.join(out,'FallenValkyrie_SelfHeal_6Faces_v2.png'));
  const annotated=tiles.flatMap((input,i)=>[{input,left:i%3*256,top:Math.floor(i/3)*288+32},{input:Buffer.from(`<svg width="256" height="32"><text x="14" y="24" fill="#e9eff4" font-size="17" font-family="Malgun Gothic,Arial">${labels[i]}</text></svg>`),left:i%3*256,top:Math.floor(i/3)*288}]);
  await sharp({create:{width:768,height:576,channels:4,background:'#24303a'}}).composite(annotated).png().toFile(path.join(out,'overview.png'));
  const topBottom=await Promise.all(tiles.slice(4).map(t=>sharp(t).resize(384,384).png().toBuffer()));
  await sharp({create:{width:768,height:420,channels:4,background:'#24303a'}}).composite(topBottom.flatMap((input,i)=>[{input,left:i*384,top:36},{input:Buffer.from(`<svg width="384" height="36"><text x="18" y="26" fill="#e9eff4" font-size="20" font-family="Malgun Gothic,Arial">${i?'아랫면 · 자가회복':'윗면 · 투구 정수리'}</text></svg>`),left:i*384,top:0}])).png().toFile(path.join(out,'top-bottom-preview.png'));
  const images=Object.fromEntries(keys.map((key,i)=>[key,'data:image/png;base64,'+tiles[i].toString('base64')]));
  write(path.join(out,'preview.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>자가치유형 발키리 6면</title><style>*{box-sizing:border-box}body{margin:0;background:#111921;color:#e9eff4;font:16px system-ui;padding:28px}main{max-width:1100px;margin:auto}p{color:#b9cdd8;line-height:1.7}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}article{background:#24303a;padding:12px;border-radius:14px}article img{display:block;width:100%}h2{font-size:17px}.stage{height:420px;display:grid;place-items:center;background:#24303a;border-radius:14px;margin:28px 0;perspective:1100px}.cube{width:220px;height:220px;position:relative;transform-style:preserve-3d;transition:transform .6s}.face{position:absolute;width:220px;height:220px;backface-visibility:hidden}.front{transform:translateZ(110px)}.right{transform:rotateY(90deg) translateZ(110px)}.back{transform:rotateY(180deg) translateZ(110px)}.left{transform:rotateY(-90deg) translateZ(110px)}.top{transform:rotateX(90deg) translateZ(110px)}.bottom{transform:rotateX(-90deg) translateZ(110px)}button{background:#304a5c;color:#e9eff4;border:1px solid #547084;border-radius:8px;font:inherit;margin:4px;padding:10px 15px;cursor:pointer}@media(max-width:650px){.grid{grid-template-columns:repeat(2,1fr)}}</style><main><h1>자가치유형 발키리 · 6면 시안</h1><p>완성된 네 면에 투구 정수리와 자가회복 문양을 추가했습니다.<br>윗면의 이마 보석은 아래쪽 가장자리(정면 방향)에 배치했습니다.</p><div class="grid">${keys.map((key,i)=>`<article><h2>${labels[i]}</h2><img src="${images[key]}" alt="${labels[i]}"></article>`).join('')}</div><div class="stage"><div class="cube">${keys.map(key=>`<img class="face ${key.toLowerCase()}" src="${images[key]}" alt="${key}">`).join('')}</div></div><button data-x="-25" data-y="-30">정면 + 측면 + 위</button><button data-x="25" data-y="-30">정면 + 측면 + 아래</button><button data-x="0" data-y="0">정면</button><button data-x="-90" data-y="0">윗면</button><button data-x="90" data-y="0">아랫면</button><button data-x="0" data-y="180">뒷면</button><button id="roll">한 번 굴리기</button><p>큐브 회전은 면 구성을 확인하는 브라우저 미리보기입니다. 실제 Unity 프리팹과 이동 로직에는 연결하지 않았습니다.</p></main><script>let x=-25,y=-30;const cube=document.querySelector('.cube');function draw(){cube.style.transform='rotateX('+x+'deg) rotateY('+y+'deg)'}document.querySelectorAll('[data-x]').forEach(b=>b.onclick=()=>{x=Number(b.dataset.x);y=Number(b.dataset.y);draw()});document.querySelector('#roll').onclick=()=>{x-=90;draw()};draw();</script></html>`);
  const manifest={size:256,atlasSize:[768,512],order:keys,topFrontEdge:'bottom',whiteEmblem:'layers/SelfHeal_White.png',faces:keys.map(key=>({key,path:'faces/'+key+'.png',sha256:sha(path.join(out,'faces',key+'.png'))})),generationMode:'builtin-imagegen: Top and Bottom; existing exact SVG emblem overlay; four existing faces preserved'};
  write(path.join(out,'manifest.json'),JSON.stringify(manifest,null,2));
  console.log('Created Top/Bottom concept faces and six-face cube preview; existing four faces are byte-identical');
})().catch(e=>{console.error(e);process.exitCode=1});
