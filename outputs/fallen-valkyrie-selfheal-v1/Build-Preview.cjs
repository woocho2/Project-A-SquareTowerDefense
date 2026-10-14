const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname;
const write=(p,t)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,t)};
const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128"><circle cx="64" cy="64" r="51" fill="#fff6f7" fill-opacity=".96"/><path d="M97 40 A39 39 0 1 0 102 82" fill="none" stroke="#d92746" stroke-width="12" stroke-linecap="round"/><path d="M102 21 L108 54 L76 46Z" fill="#d92746"/><path d="M53 43H75V55H87V77H75V89H53V77H41V55H53Z" fill="#d92746"/></svg>`;
(async()=>{
  const specs=JSON.parse(fs.readFileSync(path.join(out,'generation-manifest.json'),'utf8'));
  if(specs.length!==4||specs.map(s=>s.key).join()!=='Front,Left,Right,Back')throw Error('Exactly front, left, right and back required');
  const frame=await sharp(path.join(root,'Assets/4. Asset/2. Tower/TierLayers/Tower_Decorations_Atlas_v1.png')).extract({left:512,top:256,width:256,height:256}).png().toBuffer();
  const background=await sharp(path.join(root,'Assets/4. Asset/2. Tower/TierLayers/Tower_Backgrounds_Atlas_v1.png')).extract({left:512,top:256,width:256,height:256}).png().toBuffer();
  write(path.join(out,'Tower_Red_Frame.png'),frame);write(path.join(out,'Tower_Red_Background.png'),background);
  write(path.join(out,'SelfHeal_Red.svg'),svg);const symbol=await sharp(Buffer.from(svg)).resize(38,38).png().toBuffer();
  await sharp(Buffer.from(svg)).png().toFile(path.join(out,'SelfHeal_Red.png'));
  const tiles=[];
  for(const s of specs){
    fs.copyFileSync(s.originalPath,path.join(out,'source',s.key+'.png'));
    const {data,info}=await sharp(s.originalPath).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    let minX=info.width,minY=info.height,maxX=-1,maxY=-1;
    for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>12){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}
    if(maxX<minX)throw Error(s.key+' missing image');
    const rect={left:minX,top:minY,width:maxX-minX+1,height:maxY-minY+1};
    const cropped=await sharp(s.originalPath).extract(rect).resize({width:236,height:230,fit:'inside'}).png().toBuffer();
    const m=await sharp(cropped).metadata(),left=Math.round((256-m.width)/2),top=246-m.height;
    const portrait=await sharp({create:{width:256,height:256,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:cropped,left,top}]).png().toBuffer();
    write(path.join(out,'portraits',s.key+'.png'),portrait);
    const tile=await sharp(background).composite([{input:portrait},{input:frame},{input:symbol,left:109,top:13}]).png().toBuffer();
    write(path.join(out,'faces',s.key+'.png'),tile);tiles.push({key:s.key,tile,portrait});
    s.packing={source:rect,left,top,width:m.width,height:m.height};write(path.join(out,'generation',s.key+'.json'),JSON.stringify(s,null,2));
  }
  const atlas=await sharp({create:{width:512,height:512,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(tiles.map((t,i)=>({input:t.tile,left:i%2*256,top:Math.floor(i/2)*256}))).png().toBuffer();
  write(path.join(out,'FallenValkyrie_SelfHeal_4Faces_v1.png'),atlas);
  const labels=['정면','왼쪽 측면','오른쪽 측면','뒷면'];
  await sharp({create:{width:1024,height:304,channels:4,background:'#24303a'}}).composite(tiles.flatMap((t,i)=>[{input:t.tile,left:i*256,top:40},{input:Buffer.from(`<svg width="256" height="32"><text x="16" y="25" fill="#e9eff4" font-family="Malgun Gothic,Arial" font-size="18">${labels[i]}</text></svg>`),left:i*256,top:0}])).png().toFile(path.join(out,'overview.png'));
  write(path.join(out,'generation-manifest.json'),JSON.stringify(specs,null,2));
  const images=Object.fromEntries(tiles.map(t=>[t.key,'data:image/png;base64,'+t.tile.toString('base64')]));
  write(path.join(out,'preview.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>자가치유형 발키리 4면</title><style>*{box-sizing:border-box}body{margin:0;background:#111921;color:#e9eff4;font:16px system-ui;padding:28px}main{max-width:1080px;margin:auto}p{color:#bfd0da;line-height:1.7}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}.grid img{width:100%;background:#24303a;border-radius:12px}.grid h2{font-size:17px}.stage{height:350px;display:grid;place-items:center;perspective:1000px;margin:28px 0;background:#24303a;border-radius:14px}.cube{width:220px;height:220px;position:relative;transform-style:preserve-3d;transition:transform .55s}.face{position:absolute;width:220px;height:220px;backface-visibility:hidden}.front{transform:translateZ(110px)}.right{transform:rotateY(90deg) translateZ(110px)}.back{transform:rotateY(180deg) translateZ(110px)}.left{transform:rotateY(-90deg) translateZ(110px)}button{background:#304a5c;color:#e9eff4;border:1px solid #547084;border-radius:8px;padding:10px 16px;font:inherit;margin:4px;cursor:pointer}@media(max-width:600px){.grid{grid-template-columns:repeat(2,1fr)}}</style><main><h1>자가치유형 · 타락한 발키리</h1><p>기존 타워의 배경·빨간 테두리 + 타워 스타일의 캐릭터 초상 + 상단 빨간 회복 심볼.<br>정면·좌측·우측·뒷면 4장. 위·아래 그림은 이번에 제작하지 않았습니다.</p><section class="grid">${tiles.map((t,i)=>`<article><h2>${labels[i]}</h2><img src="${images[t.key]}" alt="${labels[i]}"></article>`).join('')}</section><div class="stage"><div class="cube">${['Front','Right','Back','Left'].map(k=>`<img class="face ${k.toLowerCase()}" src="${images[k]}" alt="${k}">`).join('')}</div></div><button data-angle="-25">정면 + 측면</button><button data-angle="0">정면</button><button data-angle="90">왼쪽 측면</button><button data-angle="-90">오른쪽 측면</button><button data-angle="180">뒷면</button><p>아래 큐브는 4면 부착 형태를 확인하는 브라우저 시안입니다. Unity 프리팹과 굴림 동작의 교체는 별도 작업입니다.</p></main><script>document.querySelectorAll('button').forEach(b=>b.onclick=()=>document.querySelector('.cube').style.transform='rotateY('+b.dataset.angle+'deg)');document.querySelector('.cube').style.transform='rotateY(-25deg)';</script></html>`);
  console.log('Saved 4 portrait sprites, 4 composited faces, exact tower red layers, self-heal icon and cube preview');
})().catch(e=>{console.error(e);process.exitCode=1});
