// Slice/pack generated raster art; never redraw effect artwork.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const sharp = require('../../_codex_spreadsheet_task/node_modules/sharp');
const root = path.resolve(__dirname, '../..');
const out = __dirname;
const assetDir = 'Assets/4. Asset/6. Effect/SwordHit_v3';
const size = 256;
const write = (p, content) => { fs.mkdirSync(path.dirname(p), { recursive: true }); fs.writeFileSync(p, content); };
const read = p => fs.readFileSync(path.join(root, p), 'utf8').replace(/\r\n/g, '\n');
const guid = () => crypto.randomBytes(16).toString('hex');
const meta = (p, id, type) => write(p+'.meta', `fileFormatVersion: 2\nguid: ${id}\n${type}:\n  externalObjects: {}\n  ${type === 'NativeFormatImporter' ? 'mainObjectFileID: 7400000\n  ' : ''}userData: \n  assetBundleName: \n  assetBundleVariant: \n`);

async function build() {
  const manifestFile = path.join(out, 'asset-manifest.json');
  const previous = fs.existsSync(manifestFile) ? JSON.parse(fs.readFileSync(manifestFile, 'utf8')) : null;
  const entries = [];
  const sprites = {};
  for (const kind of ['Target', 'Splash']) {
    const name = `Sword_${kind}_30_v3`;
    const ids = previous?.assets.find(x=>x.kind===kind)?.guids || { texture: guid(), clip: guid(), controller: guid(), prefab: guid() };
    const source = path.join(out, 'source', `Sword_${kind}_Source.png`);
    const sourceMeta = await sharp(source).metadata();
    if (sourceMeta.width % 6 || sourceMeta.height % 5 || sourceMeta.width/6 !== sourceMeta.height/5) throw Error('Source must be 6x5 square cells: '+kind);
    const sourceCell = sourceMeta.width / 6;
    // Generated Excalibur has unequal source row spacing. Extract whole frames
    // from their transparent gutters and repack on a fixed impact anchor.
    // Uniform scale preserves artwork; no per-frame stretching or redrawing.
    const rowEdges = kind==='Splash' ? [0,282,529,770,970,1145] : [0,229,458,687,916,1145];
    const impactY = [237,445,684,896,1060];
    const frames = [];
    const bounds = [];
    for (let i=0; i<30; i++) {
      const row=Math.floor(i/6);
      const top=rowEdges[row];
      const region={left:i%6*sourceCell,top,width:sourceCell,height:rowEdges[row+1]-top};
      let frame;
      if(kind==='Splash'){
        const {data,info}=await sharp(source).extract(region).raw().toBuffer({resolveWithObject:true});
        let l=info.width,t=info.height,r=-1,b=-1;
        for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>3){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);}
        if(r<0){frame=await sharp({create:{width:size,height:size,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).png().toBuffer();}
        else{
          const scale=.9;
          const w=Math.max(1,Math.round((r-l+1)*scale)),h=Math.max(1,Math.round((b-t+1)*scale));
          const left=Math.round(128+(l-sourceCell/2)*scale),destTop=Math.round(180+(top+t-impactY[row])*scale);
          if(left<0||destTop<0||left+w>size||destTop+h>size)throw Error('Whole-frame repack overflow '+i+' '+[left,destTop,w,h]);
          const input=await sharp(source).extract({left:region.left+l,top:top+t,width:r-l+1,height:b-t+1}).resize(w,h).png().toBuffer();
          frame=await sharp({create:{width:size,height:size,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input,left,top:destTop}]).png().toBuffer();
        }
      }else{frame=await sharp(source).extract(region).resize(size,size).png().toBuffer();}
      frames.push(frame);
      write(path.join(out,'frames',kind,`${String(i+1).padStart(2,'0')}.png`),frame);
      const {data} = await sharp(frame).ensureAlpha().raw().toBuffer({resolveWithObject:true});
      let minX=size,minY=size,maxX=-1,maxY=-1,edgePixels=0;
      for(let y=0;y<size;y++) for(let x=0;x<size;x++) if(data[(y*size+x)*4+3]>50){
        minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);
        if(x<2||y<2||x>=size-2||y>=size-2)edgePixels++;
      }
      bounds.push({frame:i+1,minX,minY,maxX,maxY,edgePixels});
    }
    if(bounds.some(b=>b.edgePixels>0)) throw Error('Visible effect reaches a cell edge: '+kind+' '+JSON.stringify(bounds.filter(b=>b.edgePixels)));
    sprites[kind] = frames;
    const atlasPath = path.join(root,assetDir,name+'.png');
    await sharp({create:{width:size*6,height:size*5,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(frames.map((input,i)=>({input,left:i%6*size,top:Math.floor(i/6)*size}))).png().toFile(atlasPath);
    const baseId = kind==='Target' ? 2130000 : 2140000;
    const table = frames.map((_,i)=>`  - first:\n      213: ${baseId+i}\n    second: ${name}_${String(i+1).padStart(2,'0')}`).join('\n');
    const sections = frames.map((_,i)=>`    - serializedVersion: 2\n      name: ${name}_${String(i+1).padStart(2,'0')}\n      rect:\n        serializedVersion: 2\n        x: ${i%6*size}\n        y: ${(4-Math.floor(i/6))*size}\n        width: ${size}\n        height: ${size}\n      alignment: 0\n      pivot: {x: 0.5, y: 0.5}\n      border: {x: 0, y: 0, z: 0, w: 0}\n      customData: \n      outline: []\n      physicsShape: []\n      tessellationDetail: -1\n      bones: []\n      spriteID: ${crypto.createHash('md5').update(ids.texture+':'+i).digest('hex')}\n      internalID: ${baseId+i}\n      vertices: []\n      indices: \n      edges: []\n      weights: []`).join('\n');
    const nameTable = frames.map((_,i)=>`      ${name}_${String(i+1).padStart(2,'0')}: ${baseId+i}`).join('\n');
    let textureMeta = read('Assets/4. Asset/6. Effect/Effect_Sword_Target.png.meta')
      .replace(/^guid: .+$/m,'guid: '+ids.texture)
      .replace(/  internalIDToNameTable:[\s\S]*?(?=  externalObjects:)/,'  internalIDToNameTable:\n'+table+'\n')
      .replace(/  spritePixelsToUnits: .+/,'  spritePixelsToUnits: '+size)
      .replace(/  spriteSheet:[\s\S]*?(?=  mipmapLimitGroupName:)/,`  spriteSheet:\n    serializedVersion: 2\n    sprites:\n${sections}\n    outline: []\n    customData: \n    physicsShape: []\n    bones: []\n    spriteID: \n    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n    spriteCustomMetadata:\n      entries: []\n    nameFileIdTable:\n${nameTable}\n`)
      .replace(/textureCompression: \d+/g,'textureCompression: 0').replace(/    filterMode: \d+/,'    filterMode: 1');
    if(kind==='Splash') textureMeta=textureMeta.replace(/alignment: 0/g,'alignment: 9').replace(/\{x: 0\.5, y: 0\.5\}/g,'{x: 0.5, y: 0.296875}');
    write(atlasPath+'.meta',textureMeta);
    const ref = i => `{fileID: ${baseId+i}, guid: ${ids.texture}, type: 3}`;
    // Unity holds the final object-reference key for one frame. Thirty keys
    // at 0..29/12 therefore produce exactly 2.5 seconds (no extra null key).
    const keys = frames.map((_,i)=>`    - time: ${(i/12).toFixed(8)}\n      value: ${ref(i)}`).join('\n');
    const nativeClip=path.join(out,'native-clips',name+'.anim');
    let clip = (fs.existsSync(nativeClip)?fs.readFileSync(nativeClip,'utf8').replace(/\r\n/g,'\n'):read('Assets/10.Animation/Effect/E101.anim')).replace(/m_Name: E101/g,'m_Name: '+name)
      .replace(/    curve:[\s\S]*?(?=    attribute:)/,'    curve:\n'+keys+'\n')
      .replace(/    pptrCurveMapping:[\s\S]*?(?=  m_AnimationClipSettings:)/,'    pptrCurveMapping:\n'+frames.map((_,i)=>'    - '+ref(i)).join('\n')+'\n')
      .replace(/m_StopTime: .+/,'m_StopTime: 2.5').replace(/^  - time: 1\.0$/m,'  - time: 2.5');
    const clipPath = path.join(root,assetDir,name+'.anim'); write(clipPath,clip); meta(clipPath,ids.clip,'NativeFormatImporter');
    let controller = read('Assets/10.Animation/Effect/E101.controller').replace(/m_Name: E101/g,'m_Name: '+name).replace(/m_Speed: 2/,'m_Speed: 1').replace(/m_Motion: \{[^}]+\}/,`m_Motion: {fileID: 7400000, guid: ${ids.clip}, type: 2}`);
    const controllerPath = path.join(root,assetDir,name+'.controller');write(controllerPath,controller);
    write(controllerPath+'.meta',`fileFormatVersion: 2\nguid: ${ids.controller}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 9100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
    let prefab = read('Assets/2. Prefab/4. Effect/E201.prefab').replace(/m_Name: E201/g,'m_Name: '+name).replace(/m_Sprite: \{[^}]+\}/,'m_Sprite: '+ref(0)).replace(/m_Controller: \{[^}]+\}/,`m_Controller: {fileID: 9100000, guid: ${ids.controller}, type: 2}`);
    const prefabPath = path.join(root,assetDir,name+'.prefab');write(prefabPath,prefab);meta(prefabPath,ids.prefab,'PrefabImporter');
    entries.push({kind,name,sourceWidth:sourceMeta.width,sourceHeight:sourceMeta.height,sourceCell,asset:assetDir+'/'+name+'.png',clip:assetDir+'/'+name+'.anim',prefab:assetDir+'/'+name+'.prefab',guids:ids,pivot:kind==='Splash'?{x:0.5,y:0.296875}:{x:0.5,y:0.5},bounds});
  }
  const delays=Array.from({length:30},(_,i)=>i%3===2?90:80); // GIF centiseconds: total precisely2500ms.
  for(const kind of ['Target','Splash']){
    const raw=[];
    for(const frame of sprites[kind]) raw.push(await sharp(frame).flatten({background:'#24303a'}).ensureAlpha().raw().toBuffer());
    await sharp(Buffer.concat(raw),{raw:{width:size,height:size*30,channels:4,pageHeight:size}}).gif({delay:delays,loop:0,dither:0.5,keepDuplicateFrames:true}).toFile(path.join(out,kind+'-preview.gif'));
  }
  const both=[];
  for(let i=0;i<30;i++) both.push(await sharp({create:{width:size*2,height:size,channels:4,background:'#24303a'}}).composite([{input:sprites.Target[i],left:0,top:0},{input:sprites.Splash[i],left:size,top:0}]).raw().toBuffer());
  await sharp(Buffer.concat(both),{raw:{width:size*2,height:size*30,channels:4,pageHeight:size}}).gif({delay:delays,loop:0,dither:0.5,keepDuplicateFrames:true}).toFile(path.join(out,'Sword-both-preview.gif'));
  const library=read('Assets/2. Prefab/4. Effect/EffectLibrary.asset');
  const activeEffectLibraryChanged=entries.every(entry=>library.includes(`guid: ${entry.guids.prefab}, type: 3`));
  write(manifestFile,JSON.stringify({fps:12,uniqueFrames:30,duration:2.5,layout:{columns:6,rows:5,cellSize:size,order:'left-to-right, top-to-bottom'},loop:false,activeEffectLibraryChanged,assets:entries},null,2)+'\n');
  console.log('Built two 30-frame sword effects and previews.');
}
build().catch(e=>{console.error(e);process.exitCode=1;});
