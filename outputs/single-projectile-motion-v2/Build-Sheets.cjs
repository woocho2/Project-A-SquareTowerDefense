const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,assetDir='Assets/4. Asset/3. Projectile/AnimatedSingle_v2';
const specs=JSON.parse(fs.readFileSync(path.join(out,'prompts.json'),'utf8')).specs;
const write=(p,s)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,s)};
const hash=s=>crypto.createHash('md5').update('AnimatedSingle_v2:'+s).digest('hex');
const median=a=>a.sort((a,b)=>a-b)[Math.floor(a.length/2)];
async function readFrames(spec){
  const source=path.join(out,'source',spec.job+'.png');
  const {data,info}=await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const frames=[];
  for(let i=0;i<8;i++){
    const x0=Math.round(i%4*info.width/4),x1=Math.round((i%4+1)*info.width/4),y0=Math.round(Math.floor(i/4)*info.height/2),y1=Math.round((Math.floor(i/4)+1)*info.height/2);
    let l=x1,t=y1,r=-1,b=-1,mass=0,sx=0,sy=0,edge=0;
    for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++){
      const at=(y*info.width+x)*4,alpha=data[at+3];
      if(alpha>3){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);}
      if(alpha>80&&(x===x0||x===x1-1||y===y0||y===y1-1))edge++;
      if(alpha>190&&(spec.id===2||spec.id===5||x>x0+(x1-x0)*.55)){
        const light=(data[at]+data[at+1]+data[at+2])/765;
        const weight=(alpha/255)**3*(.1+light*light);mass+=weight;sx+=(x-x0)*weight;sy+=(y-y0)*weight;
      }
    }
    if(r<0||edge>2)throw Error(spec.job+' empty/cropped frame '+i+' edges '+edge);
    const unit=256/(x1-x0);
    frames.push({source,x0,y0,l,t,r,b,unit,anchorX:(mass?sx/mass:(l+r)/2-x0)*unit,anchorY:(mass?sy/mass:(t+b)/2-y0)*unit});
  }
  const anchor={x:median(frames.map(f=>f.anchorX)),y:median(frames.map(f=>f.anchorY))};
  return{frames,anchor};
}
async function build(){
  const entries=[],failures=[];
  fs.mkdirSync(path.join(root,assetDir),{recursive:true});
  write(path.join(root,assetDir+'.meta'),`fileFormatVersion: 2\nguid: ${hash('folder')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
  for(const idle of specs.filter(s=>s.state==='Idle')){
    const flight=specs.find(s=>s.id===idle.id&&s.state==='Flight');
    if(!fs.existsSync(path.join(out,'source',idle.job+'.png'))||!fs.existsSync(path.join(out,'source',flight.job+'.png')))continue;
    try{
      const states={Idle:await readFrames(idle),Flight:await readFrames(flight)};
      let scale=1;
      for(const state of Object.values(states))for(const f of state.frames){
        const left=state.anchor.x-(f.l-f.x0)*f.unit,right=(f.r-f.x0)*f.unit-state.anchor.x;
        const top=state.anchor.y-(f.t-f.y0)*f.unit,bottom=(f.b-f.y0)*f.unit-state.anchor.y;
        if(left>0)scale=Math.min(scale,136/left);if(right>0)scale=Math.min(scale,72/right);
        if(top>0)scale=Math.min(scale,104/top);if(bottom>0)scale=Math.min(scale,104/bottom);
      }
      const images=[],packing=[];
      for(const stateName of ['Idle','Flight']){
        const state=states[stateName];
        for(let i=0;i<8;i++){
          const f=state.frames[i],width=Math.max(1,Math.round((f.r-f.l+1)*f.unit*scale)),height=Math.max(1,Math.round((f.b-f.t+1)*f.unit*scale));
          const left=Math.round(160+((f.l-f.x0)*f.unit-state.anchor.x)*scale),top=Math.round(128+((f.t-f.y0)*f.unit-state.anchor.y)*scale);
          const input=await sharp(f.source).extract({left:f.l,top:f.t,width:f.r-f.l+1,height:f.b-f.t+1}).resize(width,height).png().toBuffer();
          const image=await sharp({create:{width:256,height:256,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input,left,top}]).png().toBuffer();
          images.push(image);write(path.join(out,'frames',idle.key,stateName,String(i+1).padStart(2,'0')+'.png'),image);packing.push({state:stateName,frame:i+1,left,top,width,height});
        }
      }
      const name='Bullet_'+idle.key+'_Motion_v2',base=assetDir+'/'+name,target=path.join(root,base),guid=hash(idle.key+':texture'),baseId=21300000+idle.id*100;
      await sharp({create:{width:1024,height:1024,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(images.map((input,i)=>({input,left:i%4*256,top:Math.floor(i/4)*256}))).png().toFile(target+'.png');
      const spriteName=i=>name+'_'+(i<8?'Idle':'Flight')+'_'+String(i%8+1).padStart(2,'0');
      const table=images.map((_,i)=>`  - first:\n      213: ${baseId+i}\n    second: ${spriteName(i)}`).join('\n');
      const rows=images.map((_,i)=>`    - serializedVersion: 2\n      name: ${spriteName(i)}\n      rect:\n        serializedVersion: 2\n        x: ${i%4*256}\n        y: ${(3-Math.floor(i/4))*256}\n        width: 256\n        height: 256\n      alignment: 9\n      pivot: {x: 0.625, y: 0.5}\n      border: {x: 0, y: 0, z: 0, w: 0}\n      customData: \n      outline: []\n      physicsShape: []\n      tessellationDetail: -1\n      bones: []\n      spriteID: ${hash(idle.key+':sprite:'+i)}\n      internalID: ${baseId+i}\n      vertices: []\n      indices: \n      edges: []\n      weights: []`).join('\n');
      const names=images.map((_,i)=>`      ${spriteName(i)}: ${baseId+i}`).join('\n');
      let meta=fs.readFileSync(path.join(root,'Assets/4. Asset/6. Effect/SwordHit_v3/Sword_Target_30_v3.png.meta'),'utf8').replace(/\r\n/g,'\n')
        .replace(/^guid: .+$/m,'guid: '+guid)
        .replace(/  internalIDToNameTable:[\s\S]*?(?=  externalObjects:)/,'  internalIDToNameTable:\n'+table+'\n')
        .replace(/  spriteSheet:[\s\S]*?(?=  mipmapLimitGroupName:)/,`  spriteSheet:\n    serializedVersion: 2\n    sprites:\n${rows}\n    outline: []\n    customData: \n    physicsShape: []\n    bones: []\n    spriteID: \n    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n    spriteCustomMetadata:\n      entries: []\n    nameFileIdTable:\n${names}\n`)
        .replace(/^  alignment: 0/m,'  alignment: 9').replace(/  spritePivot: .+/,'  spritePivot: {x: 0.625, y: 0.5}');
      write(target+'.png.meta',meta);
      entries.push({id:idle.id,key:idle.key,label:idle.name,name,base,textureGuid:guid,baseId,projectileID:200+idle.id,color:idle.color,pivot:{x:.625,y:.5},uniformScale:scale,anchors:{Idle:states.Idle.anchor,Flight:states.Flight.anchor},packing});
      for(const state of ['Idle','Flight']){
        const start=state==='Idle'?0:8,raw=[];
        for(const image of images.slice(start,start+8))raw.push(await sharp(image).flatten({background:'#24303a'}).ensureAlpha().raw().toBuffer());
        fs.mkdirSync(path.join(out,'gif'),{recursive:true});
        await sharp(Buffer.concat(raw),{raw:{width:256,height:2048,channels:4,pageHeight:256}}).gif({delay:[80,90,80,80,90,80,80,90],loop:0,keepDuplicateFrames:true}).toFile(path.join(out,'gif',idle.key+'_'+state+'.gif'));
      }
      await sharp(target+'.png').flatten({background:'#24303a'}).resize({width:768}).png().toFile(path.join(out,idle.key+'-packed-contact.png'));console.log('Built '+idle.key);
    }catch(error){failures.push({key:idle.key,error:String(error)});console.error('FAILED '+idle.key+' '+error.message);}
  }
  write(path.join(out,'asset-manifest.json'),JSON.stringify({fps:12,framesPerState:8,loopDuration:8/12,states:['Idle','Flight'],complete:entries.length===14&&!failures.length,assets:entries,failures},null,2)+'\n');
  if(failures.length)process.exitCode=1;
}
build().catch(e=>{console.error(e);process.exitCode=1});
