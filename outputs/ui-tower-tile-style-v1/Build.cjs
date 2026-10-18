const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const sharp = require('../../_codex_spreadsheet_task/node_modules/sharp');
const out = __dirname;
const repo = path.resolve(out, '../..');
const ui = path.join(repo, 'Assets/4. Asset/5. UI');
const target = path.join(ui, 'Shared_UI_Atlas.png');
const common = path.join(repo, 'Assets/4. Asset/0. Common');
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
const backup = path.join(out, 'before');
const clamp = n => Math.max(200, Math.min(255, Math.round(n)));
async function raw(file) { return sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true }); }
function sprites(meta, atlasHeight) {
  const pattern = /      name: (Shared_(A|B|C|Frame)_([^\r\n]+))\r?\n      rect:\r?\n        serializedVersion: 2\r?\n        x: (\d+)\r?\n        y: (\d+)\r?\n        width: (\d+)\r?\n        height: (\d+)/g;
  return [...meta.matchAll(pattern)].map(m => ({ name:m[1],layer:m[2],suffix:m[3],x:+m[4],y:atlasHeight-(+m[5])-(+m[7]),width:+m[6],height:+m[7] }));
}
function grayNormalize(buffer) {
  for(let p=0;p<buffer.length;p+=4) {
    if(!buffer[p+3]) {buffer[p]=buffer[p+1]=buffer[p+2]=0;continue;}
    buffer[p]=buffer[p+1]=buffer[p+2]=clamp(.2126*buffer[p]+.7152*buffer[p+1]+.0722*buffer[p+2]);
  }
  return buffer;
}
async function encode(buffer,w,h) { return sharp(buffer,{raw:{width:w,height:h,channels:4}}).png().toBuffer(); }
// Integration into the existing 128px UI geometry: preserve the clean production
// tile alpha mask and use the generated light frame for the new surface tones.
async function sourceFrame() {
  const mask = await raw(path.join(common,'Board_Decoration.png'));
  const generated = await sharp(path.join(out,'generated-frame.png')).resize(mask.info.width,mask.info.height,{fit:'fill'}).ensureAlpha().raw().toBuffer();
  const pixels = Buffer.alloc(mask.data.length);
  for(let p=0;p<pixels.length;p+=4) {
    pixels[p+3]=mask.data[p+3];
    if(!pixels[p+3]) continue;
    const shade = generated[p+3]>=128 ? clamp((generated[p]+generated[p+1]+generated[p+2])/3) : 200;
    pixels[p]=pixels[p+1]=pixels[p+2]=shade;
  }
  const png = await encode(pixels,mask.info.width,mask.info.height);
  await fs.promises.writeFile(path.join(out,'frame-source.png'),png);
  return png;
}
async function slice(source,width,height) {
  const factor=Math.min(width,height)/128;
  const edge=Math.round(48*factor);
  const sx=[0,192,320],sw=[192,128,192],dx=[0,edge,width-edge],dw=[edge,width-2*edge,edge];
  const sy=[0,192,320],sh=[192,128,192],dy=[0,edge,height-edge],dh=[edge,height-2*edge,edge];
  const ops=[];
  for(let y=0;y<3;y++)for(let x=0;x<3;x++) {
    const input=await sharp(source).extract({left:sx[x],top:sy[y],width:sw[x],height:sh[y]}).resize(dw[x],dh[y],{fit:'fill'}).png().toBuffer();
    ops.push({input,left:dx[x],top:dy[y]});
  }
  const pixels=await sharp({create:{width,height,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(ops).raw().toBuffer();
  return grayNormalize(pixels);
}
function tint(buf,color) {
  const rgb=color.match(/\w\w/g).map(x=>parseInt(x,16));
  const result=Buffer.from(buf);
  for(let p=0;p<result.length;p+=4)for(let c=0;c<3;c++)result[p+c]=Math.round(result[p+c]*rgb[c]/255);
  return result;
}
async function preview(before,after,rows,bg,board) {
  const ops=[];
  const labels=[];
  async function place(rawPixels,w,h,x,y,dw,dh) {
    const input=await sharp(rawPixels,{raw:{width:w,height:h,channels:4}}).resize(dw,dh,{fit:'fill'}).png().toBuffer();
    ops.push({input,left:x,top:y});
  }
  async function sample(atlas,suffix,color,x,y,w,h) {
    const row=rows.find(r=>r.layer==='A'&&r.suffix===suffix);
    const frame=rows.find(r=>r.layer==='Frame'&&r.suffix===suffix);
    for(const [s,c] of [[row,color[0]],[frame,color[1]]]) {
      const pixels=await sharp(atlas).extract({left:s.x,top:s.y,width:s.width,height:s.height}).ensureAlpha().raw().toBuffer();
      await place(tint(pixels,c),s.width,s.height,x,y,w,h);
    }
  }
  const colors=['FFFCEC','E4AB2D'];
  await place(tint(bg.data,colors[0]),bg.info.width,bg.info.height,60,90,256,256);
  await place(tint(board.data,colors[1]),board.info.width,board.info.height,60,90,256,256);
  await sample(before,'Btn_256',colors,470,90,256,256);
  await sample(after,'Btn_256',colors,920,90,256,256);
  labels.push([60,60,'TOWER SPAWN TILE'],[470,60,'UI BEFORE'],[920,60,'UI AFTER']);
  await sample(before,'MegaDock_768x128',colors,60,415,550,115);
  await sample(after,'MegaDock_768x128',colors,690,415,550,115);
  labels.push([60,395,'WIDE PANEL BEFORE'],[690,395,'WIDE PANEL AFTER']);
  await sample(after,'CharCard_256x384',colors,60,610,240,360);
  await sample(after,'CharCard_256x384',['F4F8EB','935D2E'],375,610,240,360);
  await sample(after,'CharCard_256x384',['FCECDC','D89C73'],690,610,240,360);
  await sample(after,'Btn_128_B',colors,1040,610,160,160);
  await sample(after,'Capsule_256x128',colors,1000,820,240,120);
  labels.push([60,580,'ASGARD CARD'],[375,580,'ALFHEIM CARD'],[690,580,'BRONZE COLORS'],[1000,580,'SLICED / BUTTON']);
  const svg=`<svg width="1300" height="1010"><style>text{font-family:Arial,sans-serif;font-size:21px;font-weight:bold;fill:#f4f1e7}</style>${labels.map(([x,y,t])=>`<text x="${x}" y="${y}">${t}</text>`).join('')}</svg>`;
  ops.push({input:Buffer.from(svg),left:0,top:0});
  await sharp({create:{width:1300,height:1010,channels:4,background:'#283442'}}).composite(ops).png().toFile(path.join(out,'comparison.png'));
}
(async()=>{
  fs.mkdirSync(backup,{recursive:true});
  for(const name of ['Shared_UI_Atlas.png','Shared_UI_Atlas.png.meta']) {
    const destination=path.join(backup,name);
    if(!fs.existsSync(destination))fs.copyFileSync(path.join(ui,name),destination);
  }
  const inventoryPath=path.join(out,'before-hashes.json');
  if(!fs.existsSync(inventoryPath)) {
    const names=fs.readdirSync(ui).filter(n=>fs.statSync(path.join(ui,n)).isFile());
    const names2=['Assets/2. Prefab/Canvas.prefab','Assets/2. Prefab/BuffPrefab.prefab','Assets/3. Scripts/05. UI/UIStageThemeController.cs'];
    const inventory={};
    for(const name of names) inventory['Assets/4. Asset/5. UI/'+name]=hash(fs.readFileSync(path.join(ui,name)));
    for(const name of names2) inventory[name]=hash(fs.readFileSync(path.join(repo,name)));
    fs.writeFileSync(inventoryPath,JSON.stringify(inventory,null,2));
  }
  const originalPath=path.join(backup,'Shared_UI_Atlas.png');
  const original=await raw(originalPath);
  const meta=fs.readFileSync(target+'.meta','utf8');
  if(meta!==fs.readFileSync(path.join(backup,'Shared_UI_Atlas.png.meta'),'utf8'))throw Error('Sprite metadata changed since backup');
  const rows=sprites(meta,original.info.height);
  if(rows.length!==60)throw Error('Expected 60 existing sprites');
  const frame=await sourceFrame();
  const bg=await raw(path.join(common,'Board_Background.png'));
  const board=await raw(path.join(common,'Board_Decoration.png'));
  const baseBackground=await sharp(path.join(common,'Board_Background.png')).png().toBuffer();
  const result=Buffer.alloc(original.data.length);
  const written=new Uint8Array(original.info.width*original.info.height);
  const cached=new Map();
  const report=[];
  for(const row of rows) {
    const key=(row.layer==='Frame'?'frame':'background')+row.width+'x'+row.height;
    if(!cached.has(key)) {
      const pixels=await slice(row.layer==='Frame'?frame:baseBackground,row.width,row.height);
      if(row.layer!=='Frame') {
        // Keep the source spawn tile's full-height, gentle vertical shading.
        for(let y=0;y<row.height;y++) {
          const sourceY=Math.round(y/(row.height-1)*(bg.info.height-1));
          const shade=clamp(bg.data[(sourceY*bg.info.width+Math.floor(bg.info.width/2))*4]);
          for(let x=0;x<row.width;x++) {const p=(y*row.width+x)*4;if(pixels[p+3])pixels[p]=pixels[p+1]=pixels[p+2]=shade;}
        }
      }
      cached.set(key,pixels);
    }
    const pixels=cached.get(key);
    let count=0,min=255,max=0;
    for(let y=0;y<row.height;y++)for(let x=0;x<row.width;x++) {
      const index=(row.y+y)*original.info.width+row.x+x;
      if(written[index])throw Error('Overlapping sprite rects');written[index]=1;
      const p=(y*row.width+x)*4;
      pixels.copy(result,index*4,p,p+4);
      if(pixels[p+3]) {count++;min=Math.min(min,pixels[p]);max=Math.max(max,pixels[p]);}
    }
    const center=(Math.floor(row.height/2)*row.width+Math.floor(row.width/2))*4;
    if(!count||min<200||max>255)throw Error('Invalid shade range: '+row.name);
    if(row.layer==='Frame'&&pixels[center+3]!==0)throw Error('Frame center not transparent');
    if(row.layer!=='Frame'&&pixels[center+3]<250)throw Error('Background center not opaque');
    report.push({...row,visiblePixels:count,min,max,centerAlpha:pixels[center+3]});
  }
  const candidate=await encode(result,original.info.width,original.info.height);
  const candidatePath=path.join(out,'Shared_UI_Atlas.tile-style.png');
  fs.writeFileSync(candidatePath,candidate);
  await preview(originalPath,candidatePath,rows,bg,board);
  let paddingChanged=0;
  for(let i=0;i<written.length;i++)if(!written[i])for(let c=0;c<4;c++)if(original.data[i*4+c]!==result[i*4+c])paddingChanged++;
  if(paddingChanged)throw Error('Atlas padding changed');
  const verify={spriteCount:rows.length,width:original.info.width,height:original.info.height,visibleRgbRange:[200,255],paddingChanged,metadataUnchanged:true,sourceAlpha:'Production Board_Decoration mask; generated light surface tones',sprites:report};
  fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify(verify,null,2));
  if(process.argv.includes('--apply')) {
    const currentHash=hash(fs.readFileSync(target));
    if(currentHash!==hash(fs.readFileSync(originalPath))&&currentHash!==hash(candidate))throw Error('Target atlas has unrelated changes');
    fs.copyFileSync(candidatePath,target);
    const inventory=JSON.parse(fs.readFileSync(inventoryPath));
    for(const [name,expected] of Object.entries(inventory)) {
      if(name==='Assets/4. Asset/5. UI/Shared_UI_Atlas.png')continue;
      if(hash(fs.readFileSync(path.join(repo,name)))!==expected)throw Error('Unexpected change: '+name);
    }
    console.log('APPLIED: 60 sprites, RGB 200..255, unchanged metadata/prefabs/scripts/icons.');
  } else console.log('PREVIEW READY: 60 sprites; production atlas not yet changed.');
})().catch(e=>{console.error(e);process.exit(1);});
