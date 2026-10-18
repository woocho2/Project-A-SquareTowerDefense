const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),enemyRoot=path.join(root,'Assets/4. Asset/4. Enemy');
const config=JSON.parse(fs.readFileSync(path.join(__dirname,'layout-config.json'),'utf8'));
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function saveBefore(file){const rel=path.relative(enemyRoot,file),backup=path.join(__dirname,'before',rel);fs.mkdirSync(path.dirname(backup),{recursive:true});if(!fs.existsSync(backup))fs.copyFileSync(file,backup);return backup;}
function sampleRGBA(data,size,x,y){
 const x0=Math.floor(x),y0=Math.floor(y),fx=x-x0,fy=y-y0;let alpha=0;const rgb=[0,0,0];
 for(let dy=0;dy<=1;dy++)for(let dx=0;dx<=1;dx++){
  const sx=x0+dx,sy=y0+dy;if(sx<0||sy<0||sx>=size||sy>=size)continue;
  const i=(sy*size+sx)*4,w=(dx?fx:1-fx)*(dy?fy:1-fy),a=data[i+3]/255*w;alpha+=a;
  for(let c=0;c<3;c++)rgb[c]+=data[i+c]*a;
 }
 return [alpha?Math.round(rgb[0]/alpha):0,alpha?Math.round(rgb[1]/alpha):0,alpha?Math.round(rgb[2]/alpha):0,Math.round(alpha*255)];
}
(async()=>{
 const reports=[],target=config.target;
 const reportFile=path.join(__dirname,'layout-report.json'),previous=fs.existsSync(reportFile)?JSON.parse(fs.readFileSync(reportFile,'utf8')):[];
 // Never silently restore a snapshot over newer artwork; it needs new landmark calibration first.
 for(const subject of config.subjects){const file=path.join(enemyRoot,subject.folder,subject.name+'_Character.png'),backup=path.join(__dirname,'before',subject.folder,subject.name+'_Character.png');if(fs.existsSync(backup)){const current=hash(fs.readFileSync(file)),old=previous.find(item=>item.name===subject.name);if(current!==hash(fs.readFileSync(backup))&&current!==old?.portraitAfterSHA256)throw new Error('New portrait source requires updated layout calibration: '+subject.name);}}
 for(const subject of config.subjects){
  const portrait=path.join(enemyRoot,subject.folder,subject.name+'_Character.png'),mask=path.join(enemyRoot,subject.folder,subject.name+'_EquipmentMask.png');
  const sourcePath=saveBefore(portrait),maskPath=saveBefore(mask);saveBefore(portrait+'.meta');saveBefore(mask+'.meta');
  const appearance=path.join(enemyRoot,subject.folder,subject.name+'_Appearance.asset');saveBefore(appearance);saveBefore(appearance+'.meta');
  const metaBefore=fs.readFileSync(portrait+'.meta'),maskMetaBefore=fs.readFileSync(mask+'.meta');
  const {data:source,info}=await sharp(sourcePath).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const sourceMask=await sharp(maskPath).removeAlpha().raw().toBuffer();if(info.width!==512||info.height!==512)throw new Error('Unexpected character canvas');
  let sourceBottom=0;for(let y=0;y<512;y++)for(let x=0;x<512;x++)if(source[(y*512+x)*4+3]>32)sourceBottom=Math.max(sourceBottom,(y+1)/2);
  const sx=target.eyeDistance/subject.eyeDistance,faceY=(target.chinY-target.eyeY)/(subject.chinY-subject.eyeY),bodyY=(target.bodyBottomY-target.chinY)/(sourceBottom-subject.chinY);
  const output=Buffer.alloc(512*512*4),maskOutput=Buffer.alloc(512*512*3);
  for(let y=0;y<512;y++)for(let x=0;x<512;x++){
   const logicalX=(x+.5)/2,logicalY=(y+.5)/2;
   const sourceX=((logicalX-target.eyeX)/sx+subject.eyeX)*2-.5;
   let sourceY=logicalY<=target.eyeY?(logicalY-target.eyeY)/sx+subject.eyeY:logicalY<=target.chinY?(logicalY-target.eyeY)/faceY+subject.eyeY:(logicalY-target.chinY)/bodyY+subject.chinY;
   sourceY=sourceY*2-.5;const pixel=sampleRGBA(source,512,sourceX,sourceY),i=y*512+x;
   for(let c=0;c<4;c++)output[i*4+c]=pixel[c];
   const mx=Math.round(sourceX),my=Math.round(sourceY);
   const selected=mx>=0&&mx<512&&my>=0&&my<512&&pixel[3]>128&&sourceMask[(my*512+mx)*3]>=128;
   maskOutput[i*3]=maskOutput[i*3+1]=maskOutput[i*3+2]=selected?255:0;
  }
  let edgePixels=0;for(let i=0;i<512;i++)for(const p of [i,511*512+i,i*512,i*512+511])if(output[p*4+3]>32)edgePixels++;
  if(edgePixels)throw new Error('Layout clips hair/ornaments at canvas edge: '+subject.name);
  await sharp(output,{raw:{width:512,height:512,channels:4}}).png().toFile(portrait);
  await sharp(maskOutput,{raw:{width:512,height:512,channels:3}}).png().toFile(mask);
  if(!metaBefore.equals(fs.readFileSync(portrait+'.meta'))||!maskMetaBefore.equals(fs.readFileSync(mask+'.meta')))throw new Error('Metadata changed');
  reports.push({name:subject.name,folder:subject.folder,source:{eyeX:subject.eyeX,eyeY:subject.eyeY,eyeDistance:subject.eyeDistance,chinY:subject.chinY,bodyBottomY:sourceBottom},target,scale:{x:sx,upperHeadY:sx,lowerFaceY:faceY,bodyY},portraitBeforeSHA256:hash(fs.readFileSync(sourcePath)),portraitAfterSHA256:hash(fs.readFileSync(portrait)),metadataPreserved:true,noClippedEdgePixels:true});
  if(subject.folder==='Alfheim'){
   const manifestFile=path.join(root,'outputs/alfheim-monsters-v1/asset-manifest.json'),manifest=JSON.parse(fs.readFileSync(manifestFile,'utf8'));
   let l=512,t=512,r=0,b=0;for(let y=0;y<512;y++)for(let x=0;x<512;x++)if(output[(y*512+x)*4+3]>32){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
   manifest.bounds[subject.name]={left:l,top:t,width:r-l+1,height:b-t+1,bottom:b+1};manifest.faceLayout={config:'../character-scale-alignment-v1/layout-config.json',logicalCanvas:256,target};fs.writeFileSync(manifestFile,JSON.stringify(manifest,null,2));
  }
 }
 fs.writeFileSync(reportFile,JSON.stringify(reports,null,2));console.log(reports.map(r=>({name:r.name,x:r.scale.x,faceY:r.scale.lowerFaceY})));
})();
