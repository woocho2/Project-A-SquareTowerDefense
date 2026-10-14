const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,dir='Assets/4. Asset/3. Projectile/AnimatedSplash_v1';
const fixture=path.join(root,'outputs/all-tower-assets-connection-v1/UnityValidation');
const specs=JSON.parse(fs.readFileSync(path.join(out,'prompts.json'),'utf8'));
const single=JSON.parse(fs.readFileSync(path.join(root,'outputs/single-projectile-motion-v2/asset-manifest.json'),'utf8')).assets;
const hash=s=>crypto.createHash('md5').update('AnimatedSplash_v1:'+s).digest('hex');
const write=(p,t)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,t)};
const copy=(a,b)=>{fs.mkdirSync(path.dirname(b),{recursive:true});fs.copyFileSync(a,b)};
const poses={Idle:{x:[1,1.023,1.039,1.026,1.002,.977,.962,.974],y:[1,1.023,1.039,1.026,1.002,.977,.962,.974],alpha:[1,.98,.96,.975,.995,.98,.95,.97],angle:[0,1.7,2.4,1.6,.1,-1.7,-2.4,-1.6]},Flight:{x:[1.1,1.122,1.14,1.125,1.104,1.083,1.067,1.081],y:[.96,.947,.936,.945,.958,.973,.985,.976],alpha:[1,.99,.97,.98,.995,.98,.96,.99],angle:Array(8).fill(0)}};
(async()=>{
  if(specs.length!==6)throw Error('Six completed weapon images required');
  write(path.join(root,dir+'.meta'),`fileFormatVersion: 2\nguid: ${hash('folder')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
  const assets=[];
  for(const a of single){
    const original=`Assets/4. Asset/3. Projectile/Cartoon/Splash/Splash_${a.key}.png`;
    let texture=original;
    if(a.id<=6){
      const s=specs.find(s=>s.key===a.key);copy(s.originalPath,path.join(out,'source',`Splash_${a.key}_Source.png`));
      texture=dir+`/Splash_${a.key}_Orb_v2.png`;copy(s.originalPath,path.join(root,texture));
      const info=await sharp(s.originalPath).metadata();if(!info.hasAlpha||info.width!==info.height)throw Error(a.key+' must be square RGBA');
      let meta=fs.readFileSync(path.join(root,original+'.meta'),'utf8').replace(/^guid: .+$/m,'guid: '+hash(a.key)).replace(/spritePixelsToUnits: \d+/,`spritePixelsToUnits: ${info.width}`);
      write(path.join(root,texture+'.meta'),meta);
      write(path.join(out,'generation',a.key+'.json'),JSON.stringify(s,null,2));
    }
    const fixtureTexture=`Assets/OrbMotion_v1/Splash_${a.key}.png`;
    copy(path.join(root,texture),path.join(fixture,fixtureTexture));copy(path.join(root,texture+'.meta'),path.join(fixture,fixtureTexture+'.meta'));
    copy(path.join(root,`Assets/Resources/Projectiles/${100+a.id}.prefab`),path.join(out,'before',`${100+a.id}.prefab`));
    for(const suffix of ['.prefab','.prefab.meta'])copy(path.join(root,`Assets/Resources/Projectiles/${100+a.id}${suffix}`),path.join(fixture,`Assets/Resources/Projectiles/${100+a.id}${suffix}`));
    assets.push({id:a.id,key:a.key,label:a.label,texture,fixtureTexture,color:a.color,redrawn:a.id<=6,prefab:`Assets/Resources/Projectiles/${100+a.id}.prefab`});
  }
  const manifest={mode:'builtin-imagegen for six weapon redraws; native Unity curves for motion',fps:12,posesPerState:8,duration:8/12,poses,assets};
  write(path.join(out,'asset-manifest.json'),JSON.stringify(manifest,null,2));
  write(path.join(fixture,'orb-motion-manifest.json'),JSON.stringify({assets:assets.map(a=>({...a,texture:a.fixtureTexture}))},null,2));
  copy(path.join(out,'OrbMotionBuild.cs'),path.join(fixture,'Assets/Editor/OrbMotionBuild.cs'));
  copy(path.join(root,'Assets/3. Scripts/02. Projectile/ProjectileHit2D.cs'),path.join(fixture,'Assets/GameScripts/02. Projectile/ProjectileHit2D.cs'));
  const sheets=[];
  for(let i=0;i<assets.length;i++){
    const a=assets[i],left=i%4*256,top=Math.floor(i/4)*288;
    sheets.push({input:await sharp(path.join(root,a.texture)).resize(256,256).png().toBuffer(),left,top:top+32});
    sheets.push({input:Buffer.from(`<svg width="256" height="32"><text x="12" y="24" fill="#e6eef4" font-size="17" font-family="Malgun Gothic,Arial">${a.label}${a.redrawn?' · 수정':' · 유지'}</text></svg>`),left,top});
  }
  await sharp({create:{width:1024,height:1152,channels:4,background:'#24303a'}}).composite(sheets).png().toFile(path.join(out,'overview.png'));
  const items=assets.map(a=>({...a,image:'data:image/png;base64,'+fs.readFileSync(path.join(root,a.texture)).toString('base64'),before:a.redrawn?'data:image/png;base64,'+fs.readFileSync(path.join(root,`Assets/4. Asset/3. Projectile/Cartoon/Splash/Splash_${a.key}.png`)).toString('base64'):null}));
  write(path.join(out,'preview.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>스플래쉬 오브 모션</title><style>body{margin:0;background:#111921;color:#e9eff4;font:16px system-ui;padding:28px}main{max-width:960px;margin:auto}nav,.controls{display:flex;flex-wrap:wrap;gap:8px;margin:24px 0}button,select{background:#294454;color:#e9eff4;border:1px solid #577084;border-radius:8px;font:inherit;padding:10px 15px;cursor:pointer}button.on{background:#996a38}section{display:grid;grid-template-columns:1fr 1fr;gap:20px}article{border:1px solid #486174;border-radius:12px;overflow:hidden}h2{margin:16px}canvas{width:100%;background:var(--board,#24303a);display:block}input[type=range]{width:100%}p{line-height:1.7;color:#b9cbd5}@media(max-width:600px){section{grid-template-columns:1fr}}</style><main><h1>스플래쉬 오브 · 대기 / 비행</h1><p>무기군 6종 수정 · 원소군 8종 유지 · 각 모션 8단계 / 12FPS / 약 0.67초 반복</p><nav></nav><section><article><h2>Idle · 대기 맥동</h2><canvas width="440" height="440"></canvas></article><article><h2>Flight · 비행 수축과 팽창</h2><canvas width="440" height="440"></canvas></article></section><div class="controls"><button id="play">일시정지</button><button id="reset">처음부터</button><button id="compare">기존 무기 오브 보기</button><select id="bg"><option value="#24303a">어두운 배경</option><option value="#cdd8d0">밝은 배경</option></select><output></output></div><input id="seek" type="range" min="0" max="7" value="0"><p>이 모션은 기존 오브 그림의 크기·각도·투명도를 8단계로 반복합니다. 새 스프라이트 시트 교체 애니메이션은 아닙니다. Unity에서는 그림 자식만 움직이므로 총알 이동과 전역 배율은 유지됩니다.</p></main><script>const items=${JSON.stringify(items)},poses=${JSON.stringify(poses)};let selected=0,image=new Image,before=new Image,ready=false,time=0,last=0,playing=true,compare=false;const canvases=[...document.querySelectorAll('canvas')],ctxs=canvases.map(c=>c.getContext('2d')),nav=document.querySelector('nav'),seek=document.querySelector('#seek'),play=document.querySelector('#play');items.forEach((a,i)=>{let b=document.createElement('button');b.textContent=a.label;b.onclick=()=>choose(i);nav.append(b)});async function choose(i){ready=false;selected=i;time=0;compare=false;image=new Image;image.src=items[i].image;before=new Image;if(items[i].before)before.src=items[i].before;await image.decode();if(items[i].before)await before.decode();ready=true;document.querySelector('#compare').disabled=!items[i].redrawn;[...nav.children].forEach((b,k)=>b.classList.toggle('on',k===i));document.querySelector('#compare').textContent='기존 무기 오브 보기'}function render(){let f=Math.floor(time*12)%8;ctxs.forEach((ctx,i)=>{ctx.clearRect(0,0,440,440);if(!ready)return;let p=poses[i?'Flight':'Idle'];ctx.save();ctx.translate(220,220);ctx.rotate(-p.angle[f]*Math.PI/180);ctx.scale(p.x[f],p.y[f]);ctx.globalAlpha=p.alpha[f];ctx.drawImage(compare?before:image,-180,-180,360,360);ctx.restore()});seek.value=f;document.querySelector('output').textContent=(f+1)+' / 8'}function tick(t){if(last&&playing&&ready)time+=(t-last)/1000;last=t;render();requestAnimationFrame(tick)}play.onclick=()=>{playing=!playing;play.textContent=playing?'일시정지':'재생'};document.querySelector('#reset').onclick=()=>{time=0;playing=true;play.textContent='일시정지'};seek.oninput=()=>{playing=false;time=Number(seek.value)/12;play.textContent='재생'};document.querySelector('#bg').onchange=e=>canvases.forEach(c=>c.style.setProperty('--board',e.target.value));document.querySelector('#compare').onclick=e=>{compare=!compare;e.target.textContent=compare?'수정된 오브 보기':'기존 무기 오브 보기'};choose(0);requestAnimationFrame(tick);</script></html>`);
  console.log('Prepared 6 weapon orb redraws, 8 unchanged elements and 14 native animation prefab inputs');
})().catch(e=>{console.error(e);process.exitCode=1});
