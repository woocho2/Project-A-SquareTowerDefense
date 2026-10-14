// Pack generated raster frames and author Unity references; no VFX redraw.
const fs=require('node:fs'), path=require('node:path'), crypto=require('node:crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,assetDir='Assets/4. Asset/6. Effect/TowerHits_v4',size=256;
const specs=JSON.parse(fs.readFileSync(path.join(out,'prompts.json'),'utf8')).specs;
const write=(p,s)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,s)};
const read=p=>fs.readFileSync(path.join(root,p),'utf8').replace(/\r\n/g,'\n');
const hash=s=>crypto.createHash('md5').update('TowerHits_v4:'+s).digest('hex');
const median=a=>a.sort((a,b)=>a-b)[Math.floor(a.length/2)];
function cuts(projection,count){
  const length=projection.length,cell=length/count,result=[0];
  for(let n=1;n<count;n++){
    const expected=n*cell,lo=Math.max(result.at(-1)+Math.floor(cell*.35),Math.floor(expected-cell*.43)),hi=Math.min(length-1,Math.ceil(expected+cell*.43));
    let runs=[],start=-1;
    for(let p=lo;p<=hi+1;p++){
      const zero=p<=hi&&projection[p]===0;
      if(zero&&start<0)start=p;
      if(!zero&&start>=0){runs.push([start,p-1]);start=-1;}
    }
    if(runs.length){runs.sort((a,b)=>Math.abs((a[0]+a[1])/2-expected)-Math.abs((b[0]+b[1])/2-expected));result.push(Math.round((runs[0][0]+runs[0][1])/2));}
    else{
      let best=lo,score=Infinity;
      for(let p=lo;p<=hi;p++){let weight=0;for(let k=-3;k<=3;k++)weight+=projection[Math.max(0,Math.min(length-1,p+k))];weight+=Math.abs(p-expected)*.01;if(weight<score){score=weight;best=p;}}
      result.push(best);
    }
  }
  result.push(length);return result;
}
async function pack(s){
  const source=path.join(out,'source',s.job+'.png');
  const {data,info}=await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const xp=Array(info.width).fill(0),yp=Array(info.height).fill(0);
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>24){xp[x]++;yp[y]++;}
  const xs=cuts(xp,6),ys=cuts(yp,5),regions=[];
  const perColumnRows=Array.from({length:6},(_,col)=>{
    if(s.key!=='Dark'||s.kind!=='Target')return ys;
    const projection=Array(info.height).fill(0);
    for(let y=0;y<info.height;y++)for(let x=xs[col];x<xs[col+1];x++)if(data[(y*info.width+x)*4+3]>24)projection[y]++;
    return cuts(projection,5);
  });
  for(let i=0;i<30;i++){
    const col=i%6,row=Math.floor(i/6),x0=xs[col],y0=perColumnRows[col][row],x1=xs[col+1],y1=perColumnRows[col][row+1];
    let l=x1,t=y1,r=-1,b=-1,edgePixels=0,mass=0;
    for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++){
      const alpha=data[(y*info.width+x)*4+3];
      if(alpha>3){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);mass+=alpha;}
      if(alpha>64&&(x===x0||x===x1-1||y===y0||y===y1-1))edgePixels++;
    }
    regions.push({frame:i+1,col,row,x0,y0,x1,y1,l,t,r,b,edgePixels,mass,cx:r<0?(x0+x1)/2:(l+r)/2,cy:r<0?(y0+y1)/2:(t+b)/2});
  }
  if(regions.some(r=>r.edgePixels>2))throw Error(s.job+' has visible source art crossing split: '+JSON.stringify(regions.filter(r=>r.edgePixels>2).map(r=>({frame:r.frame,edges:r.edgePixels}))));
  const columnAnchors=Array.from({length:6},(_,c)=>median(regions.filter(r=>r.col===c&&r.r>=0).map(r=>r.cx)));
  const rowAnchors=Array.from({length:5},(_,row)=>median(regions.filter(r=>r.row===row&&r.r>=0).map(r=>r.cy)));
  for(let c=0;c<6;c++)if(columnAnchors[c]===undefined)columnAnchors[c]=(xs[c]+xs[c+1])/2;
  for(let r=0;r<5;r++)if(rowAnchors[r]===undefined)rowAnchors[r]=(ys[r]+ys[r+1])/2;
  let extent=1;
  for(const r of regions.filter(r=>r.r>=0))extent=Math.max(extent,Math.abs(r.l-columnAnchors[r.col]),Math.abs(r.r-columnAnchors[r.col]),Math.abs(r.t-rowAnchors[r.row]),Math.abs(r.b-rowAnchors[r.row]));
  const scale=104/extent,frames=[],bounds=[];
  for(const r of regions){
    let frame;
    if(r.r<0)frame=await sharp({create:{width:size,height:size,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).png().toBuffer();
    else{
      const width=Math.max(1,Math.round((r.r-r.l+1)*scale)),height=Math.max(1,Math.round((r.b-r.t+1)*scale));
      const left=Math.round(128+(r.l-columnAnchors[r.col])*scale),top=Math.round(128+(r.t-rowAnchors[r.row])*scale);
      const input=await sharp(source).extract({left:r.l,top:r.t,width:r.r-r.l+1,height:r.b-r.t+1}).resize(width,height).png().toBuffer();
      frame=await sharp({create:{width:size,height:size,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input,left,top}]).png().toBuffer();
      bounds.push({frame:r.frame,left,top,width,height});
    }
    frames.push(frame);
    write(path.join(out,'frames',s.job,String(r.frame).padStart(2,'0')+'.png'),frame);
  }
  // Register the bright impact core, rather than the center of a falling
  // weapon's full silhouette. Dark voids keep the geometric center.
  const coreX=[],coreY=[];
  for(const frame of frames.slice(6,9)){
    const rgba=await sharp(frame).raw().toBuffer();let mass=0,xsum=0,ysum=0;
    for(let y=0;y<256;y++)for(let x=0;x<256;x++){
      const at=(y*256+x)*4,bright=Math.min(rgba[at],rgba[at+1],rgba[at+2])-205;
      if(bright>0&&rgba[at+3]>200){const weight=bright*bright;mass+=weight;xsum+=x*weight;ysum+=y*weight;}
    }
    if(mass>1000){coreX.push(xsum/mass);coreY.push(ysum/mass);}
  }
  const corePx=s.key==='Spear'?180:coreX.length?Math.max(64,Math.min(192,Math.round(median(coreX)))):128;
  const corePy=coreY.length?Math.max(64,Math.min(192,Math.round(median(coreY)))):128;
  const pivot={x:corePx/256,y:(256-corePy)/256};
  return{frames,pivot,packing:{sourceWidth:info.width,sourceHeight:info.height,columnCuts:xs,rowCuts:ys,rowCutsPerColumn:perColumnRows,columnAnchors,rowAnchors,uniformScale:scale,bounds,sourceEdgePixels:regions.reduce((a,r)=>a+r.edgePixels,0),impactCore:{x:corePx,y:corePy}}};
}
function textureMeta(s,name,ids,pivot){
  const baseId=21300000+s.effectID*100;
  const internal=Array.from({length:30},(_,i)=>`  - first:\n      213: ${baseId+i}\n    second: ${name}_${String(i+1).padStart(2,'0')}`).join('\n');
  const rows=Array.from({length:30},(_,i)=>`    - serializedVersion: 2\n      name: ${name}_${String(i+1).padStart(2,'0')}\n      rect:\n        serializedVersion: 2\n        x: ${i%6*size}\n        y: ${(4-Math.floor(i/6))*size}\n        width: 256\n        height: 256\n      alignment: 0\n      pivot: {x: 0.5, y: 0.5}\n      border: {x: 0, y: 0, z: 0, w: 0}\n      customData: \n      outline: []\n      physicsShape: []\n      tessellationDetail: -1\n      bones: []\n      spriteID: ${hash(s.job+':sprite:'+i)}\n      internalID: ${baseId+i}\n      vertices: []\n      indices: \n      edges: []\n      weights: []`).join('\n');
  const names=Array.from({length:30},(_,i)=>`      ${name}_${String(i+1).padStart(2,'0')}: ${baseId+i}`).join('\n');
  let text=read('Assets/4. Asset/6. Effect/SwordHit_v3/Sword_Target_30_v3.png.meta')
    .replace(/^guid: .+$/m,'guid: '+ids.texture)
    .replace(/  internalIDToNameTable:[\s\S]*?(?=  externalObjects:)/,'  internalIDToNameTable:\n'+internal+'\n')
    .replace(/  spriteSheet:[\s\S]*?(?=  mipmapLimitGroupName:)/,`  spriteSheet:\n    serializedVersion: 2\n    sprites:\n${rows}\n    outline: []\n    customData: \n    physicsShape: []\n    bones: []\n    spriteID: \n    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n    spriteCustomMetadata:\n      entries: []\n    nameFileIdTable:\n${names}\n`);
  text=text.replace(/alignment: 0/g,'alignment: 9').replace(/\{x: 0\.5, y: 0\.5\}/g,`{x: ${pivot.x}, y: ${pivot.y}}`);
  return{text,baseId};
}
async function build(){
  fs.mkdirSync(path.join(root,assetDir),{recursive:true});
  write(path.join(root,assetDir+'.meta'),`fileFormatVersion: 2\nguid: ${hash('folder')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
  const previousPath=path.join(out,'asset-manifest.json');
  const previous=fs.existsSync(previousPath)?JSON.parse(fs.readFileSync(previousPath,'utf8')).assets:[];
  const assets=[],failures=[];
  for(const s of specs){
    if(!fs.existsSync(path.join(out,'source',s.job+'.png')))continue;
    try{
      const sourceHash=crypto.createHash('sha256').update(fs.readFileSync(path.join(out,'source',s.job+'.png'))).digest('hex');
      const cached=previous.find(a=>a.job===s.job&&a.packing?.revision===3&&a.packing.sourceHash===sourceHash);
      if(cached&&fs.existsSync(path.join(root,cached.base+'.png'))){assets.push(cached);console.log('Cached '+s.job);continue;}
      const name=`Hit_${s.key}_${s.kind}_30_v4`,ids={texture:hash(s.job+':texture'),clip:hash(s.job+':clip'),controller:hash(s.job+':controller'),prefab:hash(s.job+':prefab')};
      const {frames,packing,pivot}=await pack(s),{text,baseId}=textureMeta(s,name,ids,pivot);
      const target=path.join(root,assetDir,name);
      await sharp({create:{width:1536,height:1280,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(frames.map((input,i)=>({input,left:i%6*256,top:Math.floor(i/6)*256}))).png().toFile(target+'.png');
      write(target+'.png.meta',text);
      const reference=i=>`{fileID: ${baseId+i}, guid: ${ids.texture}, type: 3}`;
      let clip=read('outputs/sword-hit-v3/native-clips/Sword_Target_30_v3.anim')
        .replace(/m_Name: Sword_Target_30_v3/,'m_Name: '+name)
        .replace(/    curve:[\s\S]*?(?=    attribute:)/,'    curve:\n'+frames.map((_,i)=>`    - time: ${(i/12).toFixed(8)}\n      value: ${reference(i)}`).join('\n')+'\n')
        .replace(/    pptrCurveMapping:[\s\S]*?(?=  m_AnimationClipSettings:)/,'    pptrCurveMapping:\n'+frames.map((_,i)=>'    - '+reference(i)).join('\n')+'\n');
      write(target+'.anim',clip);
      write(target+'.anim.meta',`fileFormatVersion: 2\nguid: ${ids.clip}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 7400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
      const controller=read('Assets/4. Asset/6. Effect/SwordHit_v3/Sword_Target_30_v3.controller').replace(/m_Name: Sword_Target_30_v3/g,'m_Name: '+name).replace(/m_Motion: \{[^}]+\}/,`m_Motion: {fileID: 7400000, guid: ${ids.clip}, type: 2}`);
      write(target+'.controller',controller);write(target+'.controller.meta',`fileFormatVersion: 2\nguid: ${ids.controller}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 9100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
      const prefab=read('Assets/4. Asset/6. Effect/SwordHit_v3/Sword_Target_30_v3.prefab').replace(/m_Name: Sword_Target_30_v3/g,'m_Name: '+name).replace(/m_Sprite: \{[^}]+\}/,'m_Sprite: '+reference(0)).replace(/m_Controller: \{[^}]+\}/,`m_Controller: {fileID: 9100000, guid: ${ids.controller}, type: 2}`);
      write(target+'.prefab',prefab);write(target+'.prefab.meta',`fileFormatVersion: 2\nguid: ${ids.prefab}\nPrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
      const raw=[];for(const f of frames)raw.push(await sharp(f).flatten({background:'#24303a'}).ensureAlpha().raw().toBuffer());
      fs.mkdirSync(path.join(out,'gif'),{recursive:true});
      await sharp(Buffer.concat(raw),{raw:{width:256,height:256*30,channels:4,pageHeight:256}}).gif({delay:Array.from({length:30},(_,i)=>i%3===2?90:80),loop:0,keepDuplicateFrames:true,dither:.5}).toFile(path.join(out,'gif',s.job+'.gif'));
      await sharp(target+'.png').flatten({background:'#24303a'}).resize({width:960}).png().toFile(path.join(out,s.job+'-contact.png'));
      packing.sourceHash=sourceHash;packing.revision=3;
      assets.push({...s,prompt:undefined,ref:undefined,name,base:assetDir+'/'+name,guids:ids,pivot,packing});console.log('Built '+s.job);
    }catch(e){failures.push({job:s.job,error:String(e)});console.error('FAILED '+s.job+' '+e.message);}
  }
  const sword=JSON.parse(read('outputs/sword-hit-v3/asset-manifest.json')).assets.map(a=>({id:1,key:'Sword',name:a.name,emblemName:'검',kind:a.kind,effectID:a.kind==='Splash'?101:201,base:a.asset.replace(/\.png$/,''),guids:a.guids,reused:true,pivot:a.pivot}));
  write(path.join(out,'asset-manifest.json'),JSON.stringify({fps:12,frames:30,duration:2.5,assets:[...sword,...assets],failures,complete:assets.length===26&&!failures.length},null,2)+'\n');
  if(failures.length)process.exitCode=1;
}
build().catch(e=>{console.error(e);process.exitCode=1});
