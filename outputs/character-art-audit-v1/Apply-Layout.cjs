const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),inventory=require('./inventory.json'),config=require('./layout-config.json'),reportPath=path.join(__dirname,'layout-report.json');
const previous=fs.existsSync(reportPath)?JSON.parse(fs.readFileSync(reportPath,'utf8')):null;
const hash=buffer=>crypto.createHash('sha256').update(buffer).digest('hex');
function sample(data,x,y){const ix=Math.floor(x),iy=Math.floor(y),fx=x-ix,fy=y-iy,rgb=[0,0,0];let alpha=0;for(let dy=0;dy<=1;dy++)for(let dx=0;dx<=1;dx++){const xx=ix+dx,yy=iy+dy;if(xx<0||xx>=256||yy<0||yy>=256)continue;const i=(yy*256+xx)*4,w=(dx?fx:1-fx)*(dy?fy:1-fy)*data[i+3]/255;alpha+=w;for(let c=0;c<3;c++)rgb[c]+=data[i+c]*w;}return [...rgb.map(v=>alpha?Math.round(v/alpha):0),Math.round(alpha*255)];}
(async()=>{const report={target:config.target,files:[],portraits:[],metadataChanged:false,catalogReferencesChanged:false},pending=[];fs.mkdirSync(path.join(__dirname,'after-cells'),{recursive:true});
 for(const texture of inventory.textures.filter(t=>t.spriteCount)){
  const file=path.join(root,texture.path),before=path.join(__dirname,'before',texture.path),original=fs.readFileSync(before),current=fs.readFileSync(file),currentHash=hash(current),old=previous?.files.find(f=>f.path===texture.path);
  const {data,info}=await sharp(original).ensureAlpha().raw().toBuffer({resolveWithObject:true}),atlas=Buffer.from(data);
  for(const cell of inventory.portraits.filter(p=>p.atlas===texture.path)){
   const override=config.artOverrides?.[cell.id],source=override?await sharp(path.join(__dirname,override.source)).resize(256,256).ensureAlpha().raw().toBuffer():await sharp(original).extract({left:cell.x,top:info.height-cell.y-cell.height,width:256,height:256}).ensureAlpha().raw().toBuffer();
   let sourceBottom=cell.bounds.bottom;if(override){sourceBottom=0;for(let i=0;i<256*256;i++)if(source[i*4+3]>128)sourceBottom=Math.max(sourceBottom,Math.floor(i/256)+1);}
   const [eyeX,eyeY,distance,chin]=override?.anchors||config.subjects[cell.id],scale=config.target.eyeDistance/distance,seam=config.target.eyeY+(chin-eyeY)*scale,bodyScale=(config.target.bottom-seam)/(sourceBottom-chin);
   if(bodyScale<.5||bodyScale>2.5)throw new Error('Body anchor needs review '+cell.id);
   const output=Buffer.alloc(256*256*4);let edgePixels=0;
   for(let y=0;y<256;y++)for(let x=0;x<256;x++){
    const px=x+.5,py=y+.5,crownSeam=override?config.target.eyeY+(override.crownJoinY-eyeY)*scale:0;
    const localXScale=override&&py>seam?scale+(override.bodyXScale-scale)*Math.min(1,(py-seam)/(244-seam)):scale;
    const sx=(px-config.target.eyeX)/localXScale+eyeX-.5,sy=(override&&py<crownSeam?override.crownTop+(py-override.targetTop)*(override.crownJoinY-override.crownTop)/(crownSeam-override.targetTop):py<=seam?(py-config.target.eyeY)/scale+eyeY:(py-seam)/bodyScale+chin)-.5,pixel=sample(source,sx,sy),i=(y*256+x)*4;
    for(let c=0;c<4;c++)output[i+c]=pixel[c];if((x===0||y===0||x===255||y===255)&&pixel[3]>128)edgePixels++;
   }
   if(edgePixels)throw new Error('Portrait touches cell edge '+cell.id);
   for(let y=0;y<256;y++)output.copy(atlas,((info.height-cell.y-256+y)*info.width+cell.x)*4,y*256*4,(y+1)*256*4);
   await sharp(output,{raw:{width:256,height:256,channels:4}}).png().toFile(path.join(__dirname,'after-cells',cell.id+'.png'));
   report.portraits.push({id:cell.id,name:cell.name,type:cell.type,before:{eyeX,eyeY,eyeDistance:distance,bottom:sourceBottom},after:{eyeX:128,eyeY:160,eyeDistance:39.5,bottom:244},headScale:scale,bodyScale,edgePixels,artRegenerated:!!override,exception:cell.id==='53001'?'eyepatch retained; hidden-eye spacing inferred':cell.id==='53013'?'two heads; average individual eye spacing':''});
  }
  const png=await sharp(atlas,{raw:{width:info.width,height:info.height,channels:4}}).png().toBuffer();
  if(currentHash!==hash(original)&&currentHash!==old?.sha256&&currentHash!==hash(png))throw new Error('Portrait atlas changed since review: '+texture.path);
  pending.push({file,png});
  if(!fs.readFileSync(file+'.meta').equals(fs.readFileSync(before+'.meta')))throw new Error('Atlas importer or sprite IDs changed '+texture.path);
  report.files.push({path:texture.path,sha256:hash(png),beforeSha256:hash(original),spriteGUIDAndFileIDsPreserved:true});
 }
 for(const {file,png} of pending)fs.writeFileSync(file,png);
 fs.writeFileSync(reportPath,JSON.stringify(report,null,2));console.log('Normalized '+report.portraits.length+' tower portraits in '+report.files.length+' atlases; metadata preserved.');
})();
