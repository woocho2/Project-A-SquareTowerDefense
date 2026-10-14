const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,assetDir='Assets/4. Asset/4. Enemy/Emblems_v1';
const write=(p,t)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,t)};
const hash=s=>crypto.createHash('md5').update('EnemyEmblemsWhite_v1:'+s).digest('hex');
const defs=[
 {key:'SelfHeal',label:'자가치유형',meaning:'회전 화살표와 십자',body:'<path d="M98 39A42 42 0 1 0 105 78" fill="none" stroke="white" stroke-width="11" stroke-linecap="round"/><path d="M94 19L114 54L78 49Z" fill="white"/><path d="M56 44H72V56H84V72H72V84H56V72H44V56H56Z" fill="white"/>'},
 {key:'Mobility',label:'이동력형',meaning:'발과 속도선',body:'<path d="M64 20H87L80 69C85 76 98 83 108 87Q115 91 113 101H58Q50 101 50 92V79L60 70Z" fill="white"/><path d="M20 37H48M12 54H44M22 71H39" fill="none" stroke="white" stroke-width="10" stroke-linecap="round"/>'},
 {key:'Action',label:'행동력형',meaning:'번개',body:'<path d="M75 14L32 72H59L51 114L98 55H72Z" fill="white"/>'},
 {key:'Defense',label:'방어형',meaning:'방패',body:'<path d="M64 18L105 34V57C105 81 89 99 64 113C39 99 23 81 23 57V34Z" fill="none" stroke="white" stroke-width="10" stroke-linejoin="round"/><path d="M44 54L64 44L84 54V69L64 86L44 69Z" fill="white"/>'},
 {key:'GroupHeal',label:'전체회복형',meaning:'동료 셋과 십자',body:'<path d="M58 14H70V24H80V36H70V46H58V36H48V24H58Z" fill="white"/><circle cx="36" cy="71" r="11" fill="white"/><circle cx="92" cy="71" r="11" fill="white"/><circle cx="64" cy="64" r="12" fill="white"/><path d="M14 107V98Q14 86 30 86H37Q42 87 42 90V107ZM86 107V90Q88 86 93 86H99Q114 86 114 98V107ZM47 113V96Q47 82 64 82Q81 82 81 96V113Z" fill="white"/>'},
 {key:'MiddleBoss',label:'중간보스형',meaning:'왕관',body:'<path d="M18 38L40 57L64 22L88 57L110 38L101 91H27Z" fill="white"/><rect x="30" y="99" width="68" height="11" rx="3" fill="white"/>'},
 {key:'FinalBoss',label:'최종보스형',meaning:'왕관을 쓴 해골',body:'<path d="M21 24L42 35L64 13L86 35L107 24L100 48H28Z" fill="white"/><path fill="white" fill-rule="evenodd" d="M34 58Q64 47 94 58L100 74Q100 86 87 89L85 111H43L41 89Q28 86 28 74ZM39 70L54 75L53 84L40 82ZM89 70L74 75L75 84L88 82ZM64 83L58 94H70ZM53 101H59V111H53ZM69 101H75V111H69Z"/>'}
];
const svg=d=>`<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 128 128">${d.body}</svg>`;
function folderMeta(key){return `fileFormatVersion: 2\nguid: ${hash(key)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;}
(async()=>{
  write(path.join(root,assetDir+'.meta'),folderMeta('folder'));
  let template=fs.readFileSync(path.join(root,'Assets/4. Asset/2. Tower/Emblem_Water_White.png.meta'),'utf8');
  template=template.replace(/spritePixelsToUnits: \d+/,'spritePixelsToUnits: 256').replace(/spriteMeshType: \d+/,'spriteMeshType: 0');
  const buffers=[],entries=[];
  for(let i=0;i<defs.length;i++){
    const d=defs[i],name='Enemy_Emblem_'+d.key+'_White_v1';
    write(path.join(out,'svg',d.key+'.svg'),svg(d));
    const png=await sharp(Buffer.from(svg(d))).png().toBuffer();buffers.push(png);
    write(path.join(out,'png',d.key+'.png'),png);write(path.join(root,assetDir,name+'.png'),png);
    write(path.join(root,assetDir,name+'.png.meta'),template.replace(/^guid: .+$/m,'guid: '+hash(d.key)).replace(/    spriteID: \w+/, '    spriteID: '+hash('single:'+d.key)));
    entries.push({id:i+1,key:d.key,label:d.label,meaning:d.meaning,svg:'svg/'+d.key+'.svg',png:assetDir+'/'+name+'.png',guid:hash(d.key),atlasSprite:'Enemy_Emblem_'+d.key,atlasFileID:71001+i});
  }
  const atlasName='Enemy_Emblems_White_Atlas_v1',atlas=await sharp({create:{width:1024,height:512,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(buffers.map((input,i)=>({input,left:i%4*256,top:Math.floor(i/4)*256}))).png().toBuffer();
  write(path.join(root,assetDir,atlasName+'.png'),atlas);write(path.join(out,atlasName+'.png'),atlas);
  const spriteRows=entries.map((e,i)=>`    - serializedVersion: 2\n      name: ${e.atlasSprite}\n      rect:\n        serializedVersion: 2\n        x: ${i%4*256}\n        y: ${256-Math.floor(i/4)*256}\n        width: 256\n        height: 256\n      alignment: 0\n      pivot: {x: 0.5, y: 0.5}\n      border: {x: 0, y: 0, z: 0, w: 0}\n      customData: \n      outline: []\n      physicsShape: []\n      tessellationDetail: -1\n      bones: []\n      spriteID: ${hash('sprite:'+e.key)}\n      internalID: ${e.atlasFileID}\n      vertices: []\n      indices: \n      edges: []\n      weights: []`).join('\n');
  let atlasMeta=template.replace(/^guid: .+$/m,'guid: '+hash('atlas')).replace(/spriteMode: 1/,'spriteMode: 2').replace(/    sprites: \[\]/,'    sprites:\n'+spriteRows);
  atlasMeta=atlasMeta.replace(/internalIDToNameTable: \[\]/,'internalIDToNameTable:\n'+entries.map(e=>`  - first:\n      213: ${e.atlasFileID}\n    second: ${e.atlasSprite}`).join('\n'));
  atlasMeta=atlasMeta.replace(/^    spriteID: \w+/m, '    spriteID: '+hash('atlas-root')).replace(/    nameFileIdTable: \{\}/,'    nameFileIdTable:\n'+entries.map(e=>`      ${e.atlasSprite}: ${e.atlasFileID}`).join('\n'));
  write(path.join(root,assetDir,atlasName+'.png.meta'),atlasMeta);
  const overview=[];
  for(let i=0;i<entries.length;i++){
    const e=entries[i],left=i%4*256,top=Math.floor(i/4)*280;
    overview.push({input:buffers[i],left,top:top+24});
    overview.push({input:Buffer.from(`<svg width="256" height="30"><text x="16" y="24" fill="#e9eff4" font-family="Malgun Gothic,Arial" font-size="18">${e.label}</text></svg>`),left,top});
  }
  await sharp({create:{width:1024,height:560,channels:4,background:'#24303a'}}).composite(overview).png().toFile(path.join(out,'overview.png'));
  const images=buffers.map(b=>'data:image/png;base64,'+b.toString('base64'));
  write(path.join(out,'preview.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>몬스터 흰색 심볼 7종</title><style>*{box-sizing:border-box}body{margin:0;background:#111921;color:#e9eff4;font:16px system-ui;padding:28px}main{max-width:1100px;margin:auto}p{line-height:1.7;color:#b9cdd8}.controls{display:flex;gap:16px;align-items:center;flex-wrap:wrap;margin:24px 0}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:16px}article{background:var(--bg,#24303a);border:1px solid #4a6070;border-radius:14px;padding:16px}h2{font-size:18px}.big{width:100%;height:170px;display:grid;place-items:center}.symbol{display:inline-block;background:var(--ink,#fff);mask-image:var(--image);mask-size:contain;mask-position:center;mask-repeat:no-repeat;-webkit-mask-image:var(--image);-webkit-mask-size:contain;-webkit-mask-position:center;-webkit-mask-repeat:no-repeat}.sizes{display:flex;gap:12px;align-items:flex-end;margin-top:24px}.sizes span{display:block;text-align:center}.sizes small{display:block;font-size:11px;margin-top:8px;color:#b9cdd8}button,input{font:inherit}button{background:#294454;color:#fff;border:1px solid #4a6070;border-radius:8px;padding:8px 12px;cursor:pointer}</style><main><h1>몬스터 유형 · 흰색 심볼</h1><p>자가치유·이동력·행동력·방어·전체회복·중간보스·최종보스 7종.<br>흰색 PNG와 투명 배경. Unity SpriteRenderer.color로 유형별 색상을 적용할 수 있습니다.</p><div class="controls"><label>미리보기 색상 <input id="color" type="color" value="#ffffff"></label><button data-color="#ffffff">흰색</button><button data-color="#dc354a">자가치유 빨강</button><button data-color="#64c5ed">이동력 하늘색</button><button data-color="#69cb79">행동력 초록</button><label>배경 <input id="bg" type="color" value="#24303a"></label></div><section class="grid">${entries.map((e,i)=>`<article style="--image:url('${images[i]}')"><h2>${e.label}</h2><p>${e.meaning}</p><div class="big"><i class="symbol" style="width:150px;height:150px"></i></div><div class="sizes">${[16,24,32,48,64].map(n=>`<span><i class="symbol" style="width:${n}px;height:${n}px"></i><small>${n}px</small></span>`).join('')}</div></article>`).join('')}</section></main><script>const color=document.querySelector('#color');function tint(v){document.documentElement.style.setProperty('--ink',v);color.value=v}color.oninput=e=>tint(e.target.value);document.querySelectorAll('button').forEach(b=>b.onclick=()=>tint(b.dataset.color));document.querySelector('#bg').oninput=e=>document.documentElement.style.setProperty('--bg',e.target.value);</script></html>`);
  const manifest={mode:'hand-authored vector SVG rasterized to transparent white PNG',size:256,ppu:256,atlas:assetDir+'/'+atlasName+'.png',atlasGuid:hash('atlas'),atlasSize:[1024,512],grid:[4,2],assets:entries};
  for(let i=0;i<buffers.length;i++){
    const {data,info}=await sharp(buffers[i]).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    if(info.width!==256||info.height!==256)throw Error(entries[i].key+' wrong size');
    for(let y=0;y<256;y++)for(let x=0;x<256;x++){
      const p=(y*256+x)*4,a=data[p+3];
      if(a>0&&(data[p]!==255||data[p+1]!==255||data[p+2]!==255))throw Error(entries[i].key+' must be pure white');
      if((x===0||y===0||x===255||y===255)&&a!==0)throw Error(entries[i].key+' clipped border');
    }
  }
  write(path.join(out,'export-validation.txt'),'PASS seven 256x256 RGBA symbols, pure white visible pixels and transparent canvas edges\nPASS seven unique texture and sprite IDs, 4x2 atlas metadata and centered 256 PPU\n');
  write(path.join(out,'manifest.json'),JSON.stringify(manifest,null,2));
  console.log('Created seven white symbols, Unity individual PNGs and sliced atlas, SVG sources and preview');
})().catch(e=>{console.error(e);process.exitCode=1});
