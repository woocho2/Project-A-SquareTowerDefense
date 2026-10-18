const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,ui='Assets/4. Asset/5. UI';
const atlasRel=ui+'/Shared_UI_Atlas.png',metaRel=atlasRel+'.meta';
const sourceFiles=[atlasRel,metaRel,'Assets/2. Prefab/Canvas.prefab','Assets/2. Prefab/BuffPrefab.prefab'];
const digest=b=>crypto.createHash('sha256').update(b).digest('hex');
function write(rel,data){const p=path.join(root,rel);fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,data);}
function stableGuid(rel){return crypto.createHash('md5').update('square-ui-common-v1/'+rel).digest('hex');}
function ensureMeta(rel){const p=path.join(root,rel+'.meta');if(!fs.existsSync(p))write(rel+'.meta',`fileFormatVersion: 2\nguid: ${stableGuid(rel)}\n`);return /guid: (\w+)/.exec(fs.readFileSync(p,'utf8'))[1];}
function backup(rel){const p=path.join(out,'before',rel);fs.mkdirSync(path.dirname(p),{recursive:true});if(!fs.existsSync(p))fs.copyFileSync(path.join(root,rel),p);return p;}
async function normalized(file,compact=false){
 const {data,info}=await sharp(file).resize(256,256,{fit:'fill'}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 if(compact){
  // Packing cleanup: retain the generated main ring and its antialiasing,
  // discarding detached transparent-background specks.
  const seen=new Uint8Array(256*256);let largest=[];
  for(let n=0;n<seen.length;n++){if(seen[n]||data[n*4+3]<16)continue;const queue=[n];seen[n]=1;
   for(let i=0;i<queue.length;i++){const p=queue[i],x=p%256,y=Math.floor(p/256);for(const q of [x>0?p-1:-1,x<255?p+1:-1,y>0?p-256:-1,y<255?p+256:-1])if(q>=0&&!seen[q]&&data[q*4+3]>=16){seen[q]=1;queue.push(q);}}
   if(queue.length>largest.length)largest=queue;
  }
  const keep=new Uint8Array(seen.length);for(const n of largest)keep[n]=1;
  for(let n=0;n<keep.length;n++)if(!keep[n])data[n*4+3]=0;
 }
 for(let p=0;p<data.length;p+=4){if(!data[p+3]){data[p]=data[p+1]=data[p+2]=0;continue;}const v=Math.max(200,Math.min(255,Math.round((data[p]+data[p+1]+data[p+2])/3)));data[p]=data[p+1]=data[p+2]=v;}
 return sharp(data,{raw:{width:info.width,height:info.height,channels:4}}).png().toBuffer();
}
function spriteBlock(name,id,x,border){return `    - serializedVersion: 2\n      name: ${name}\n      rect:\n        serializedVersion: 2\n        x: ${x}\n        y: 0\n        width: 256\n        height: 256\n      alignment: 0\n      pivot: {x: 0.5, y: 0.5}\n      border: {x: ${border}, y: ${border}, z: ${border}, w: ${border}}\n      customData: \n      outline: []\n      physicsShape: []\n      tessellationDetail: -1\n      bones: []\n      spriteID: ${stableGuid(name)}\n      internalID: ${id}\n      vertices: []\n      indices: \n      edges: []\n      weights: []\n`;}
function migratePrefab(rel,surfaceGuid,skinGuid){
 let source=fs.readFileSync(backup(rel),'utf8');
 const blocks=source.split(/(?=^--- !u!)/m);
 const objects=new Map();for(let i=0;i<blocks.length;i++){const m=/^--- !u!\d+ &(-?\d+)/.exec(blocks[i]);if(m)objects.set(m[1],i);}
 let count=0;const added=[];const used=new Set(objects.keys());
 for(let i=0;i<blocks.length;i++){
  const block=blocks[i];const match=/m_Sprite: \{fileID: (\d+), guid: 8b158e0989f140a88351053d1be429f7, type: 3\}/.exec(block);
  if(!match||/m_Type: 3\b/.test(block))continue;
  const role=+match[1]>=2300?3:Math.floor((+match[1]-2000)/100);
  if(role<0||role>3)throw Error('Unknown legacy UI role');
  const go=/m_GameObject: \{fileID: (-?\d+)\}/.exec(block)[1];
  const color=/m_Color: (\{[^\n]+\})/.exec(block)[1];
  const newId=String(8800100000000000000n+BigInt(count+1));if(used.has(newId))throw Error('Component ID collision');used.add(newId);
  const gi=objects.get(go);if(gi===undefined)throw Error('Missing UI GameObject');
  blocks[gi]=blocks[gi].replace(/  m_Layer:/,`  - component: {fileID: ${newId}}\n  m_Layer:`);
  blocks[i]=blocks[i].replace(/  m_Type: \d+/, '  m_Type: 1');
  added.push(`--- !u!114 &${newId}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${go}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${surfaceGuid}, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::UITileSurface\n  skin: {fileID: 11400000, guid: ${skinGuid}, type: 2}\n  colorRole: ${role}\n  frameStyle: 0\n  borderThickness: 2\n  useStageTheme: ${rel.includes('Canvas')?1:0}\n  customColor: ${color}\n`);
  count++;
 }
 // Preserve existing names, transforms, image colors, raycasts, callbacks,
 // buttons and manual theme overrides; only Image.Type and new components differ.
 for(let i=0;i<blocks.length;i++){
  const original=source.split(/(?=^--- !u!)/m)[i];
  const clean=b=>b.replace(/^  - component: \{fileID: 880010000000000\d+\}\r?\n/gm,'').replace(/^  m_Type: \d+/gm,'  m_Type: [mode]');
  if(clean(original)!==clean(blocks[i]))throw Error('Unexpected prefab mutation at block '+i);
 }
 write(rel,blocks.join('')+added.join(''));
 return {path:rel,images:count};
}
(async()=>{
 const appliedPath=path.join(out,'applied-hashes.json');
 if(fs.existsSync(appliedPath)){
  const applied=JSON.parse(fs.readFileSync(appliedPath));
  for(const rel of sourceFiles){const current=digest(fs.readFileSync(path.join(root,rel))),old=digest(fs.readFileSync(path.join(out,'before',rel)));if(current!==applied[rel]&&current!==old)throw Error('Unrelated changes; refusing to overwrite '+rel);}
 }
 for(const rel of sourceFiles)backup(rel);
 const oldMeta=fs.readFileSync(path.join(out,'before',metaRel),'utf8');
 const atlasGuid=/^guid: (\w+)/m.exec(oldMeta)[1];
 const bg=await normalized(path.join(root,'Assets/4. Asset/0. Common/Board_Background.png'));
 const decorated=await normalized(path.join(root,'outputs/ui-tower-tile-style-v1/frame-source.png'));
 const compact=await normalized(path.join(out,'compact-frame-generated.png'),true);
 const inputs=[bg,decorated,compact];
 for(let i=0;i<3;i++)fs.writeFileSync(path.join(out,['background','decorated','compact'][i]+'.png'),inputs[i]);
 const packed=await sharp({create:{width:1024,height:256,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(inputs.map((input,i)=>({input,left:i*256,top:0}))).raw().toBuffer();
 // Very low alpha can round RGB during premultiplied compositing; enforce the
 // requested range on the final packed, straight-alpha pixels as well.
 for(let p=0;p<packed.length;p+=4){const v=packed[p+3]?Math.max(200,Math.min(255,Math.round((packed[p]+packed[p+1]+packed[p+2])/3))):0;packed[p]=packed[p+1]=packed[p+2]=v;}
 const atlas=await sharp(packed,{raw:{width:1024,height:256,channels:4}}).png().toBuffer();
 write(atlasRel,atlas);
 let meta=oldMeta.replace(/(maxTextureSize:) 4096/g,'$1 1024').replace(/(textureCompression:) 1/g,'$1 0');
 let aliases=0;
 meta=meta.replace(/    - serializedVersion: 2\r?\n      name: Shared_[^\r\n]+[\s\S]*?(?=    - serializedVersion: 2|    outline: \[\])/g,block=>{
  const isFrame=/name: Shared_Frame_/.test(block),x=isFrame?256:0,b=isFrame?96:32;aliases++;
  return block.replace(/        x: \d+/,'        x: '+x).replace(/        y: \d+/,'        y: 0').replace(/        width: \d+/,'        width: 256').replace(/        height: \d+/,'        height: 256').replace(/      border: \{[^\r\n]+\}/,`      border: {x: ${b}, y: ${b}, z: ${b}, w: ${b}}`);
 });
 if(aliases!==60)throw Error('Expected 60 legacy sprite aliases, got '+aliases);
 const names=['Shared_A_Surface','Shared_Frame_Decorated','Shared_Frame_Compact'];
 const core=names.map((n,i)=>spriteBlock(n,2401+i,i*256,i===1?96:32)).join('');
 const at=meta.search(/^    outline: \[\]\r?$/m);if(at<0)throw Error('Missing sprite footer');
 meta=meta.slice(0,at)+core+meta.slice(at);
 meta=meta.replace(/  mipmapLimitGroupName:/,names.map((n,i)=>`      ${n}: ${2401+i}\n`).join('')+'  mipmapLimitGroupName:');
 write(metaRel,meta);
 const guids={};for(const name of ['UITileSkin','UITileSurface','UITilePanel'])guids[name]=ensureMeta('Assets/3. Scripts/05. UI/'+name+'.cs');
 ensureMeta('Assets/Editor/UITilePanelEditor.cs');
 const skinRel='Assets/Resources/UITileSkin.asset',skinGuid=ensureMeta(skinRel);
 write(skinRel,`%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${guids.UITileSkin}, type: 3}\n  m_Name: UITileSkin\n  m_EditorClassIdentifier: Assembly-CSharp::UITileSkin\n  background: {fileID: 2401, guid: ${atlasGuid}, type: 3}\n  decoratedFrame: {fileID: 2402, guid: ${atlasGuid}, type: 3}\n  compactFrame: {fileID: 2403, guid: ${atlasGuid}, type: 3}\n  decoratedStrokePixels: 8\n  compactStrokePixels: 12\n`);
 const prefabs=['Assets/2. Prefab/Canvas.prefab','Assets/2. Prefab/BuffPrefab.prefab'].map(rel=>migratePrefab(rel,guids.UITileSurface,skinGuid));
 const pixels=await sharp(atlas).ensureAlpha().raw().toBuffer();let min=255,max=0;
 for(let p=0;p<pixels.length;p+=4)if(pixels[p+3]){if(pixels[p]!==pixels[p+1]||pixels[p]!==pixels[p+2])throw Error('Nonneutral atlas');min=Math.min(min,pixels[p]);max=Math.max(max,pixels[p]);}
 if(min<200||max>255)throw Error('Atlas outside requested shade range');
 fs.writeFileSync(path.join(out,'asset-report.json'),JSON.stringify({dimensions:[1024,256],oldDimensions:[4096,2048],texelReduction:32,uniqueShapes:3,compatibilityAliases:aliases,canonicalSprites:names,shadeRange:[min,max],atlasGuid,atlasSha256:digest(atlas),guids,skinGuid,prefabs},null,2));
 fs.writeFileSync(appliedPath,JSON.stringify(Object.fromEntries(sourceFiles.map(rel=>[rel,digest(fs.readFileSync(path.join(root,rel)))])),null,2));
 console.log('Built 3-shape atlas with 60 compatibility aliases; migrated '+prefabs.map(p=>p.images).join('+')+' UI Images.');
})().catch(e=>{console.error(e);process.exit(1);});
