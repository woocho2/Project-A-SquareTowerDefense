const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp'),measure=require('./Measure-Eyes.cjs');
const root=path.resolve(__dirname,'../..'),plan=require('./production-plan.json'),manual=require('./manual-landmarks.json'),backup=path.join(__dirname,'before-final-layout'),mp=path.join(__dirname,'asset-manifest.json'),manifest=JSON.parse(fs.readFileSync(mp,'utf8'));
function sample(data,x,y){const ix=Math.floor(x),iy=Math.floor(y),fx=x-ix,fy=y-iy,rgb=[0,0,0];let a=0;for(let dy=0;dy<=1;dy++)for(let dx=0;dx<=1;dx++){const xx=ix+dx,yy=iy+dy;if(xx<0||xx>=512||yy<0||yy>=512)continue;const i=(yy*512+xx)*4,w=(dx?fx:1-fx)*(dy?fy:1-fy)*data[i+3]/255;a+=w;for(let c=0;c<3;c++)rgb[c]+=data[i+c]*w;}return [...rgb.map(v=>a?Math.round(v/a):0),Math.round(a*255)];}
(async()=>{fs.mkdirSync(backup,{recursive:true});manifest.finalLayout={};for(const a of plan.assets){
 const dir=path.join(root,'Assets/4. Asset/4. Enemy',a.realm),portrait=path.join(dir,a.id+'_Character.png'),mask=path.join(dir,a.id+'_EquipmentMask.png');
 for(const file of [portrait,mask]){const destination=path.join(backup,path.basename(file));if(!fs.existsSync(destination)||process.argv.includes('--capture'))fs.copyFileSync(file,destination);}
 const color=await sharp(path.join(backup,a.id+'_Character.png')).ensureAlpha().raw().toBuffer(),selection=await sharp(path.join(backup,a.id+'_EquipmentMask.png')).removeAlpha().raw().toBuffer(),annotation=manual[a.id],eyes=annotation?{x:(annotation.left[0]+annotation.right[0])/4,y:(annotation.left[1]+annotation.right[1])/4,distance:(annotation.right[0]-annotation.left[0])/2,method:'visually reviewed pupil centers'}:measure(color,a.eyeColor);
 let bottom=0;for(let i=0;i<512*512;i++)if(color[i*4+3]>128)bottom=Math.max(bottom,Math.floor(i/512)+1);
 const sx=39.5/eyes.distance,sy=48/(368-eyes.y*2),body=120/(bottom-368),out=Buffer.alloc(512*512*4),maskOut=Buffer.alloc(512*512*3);
 for(let y=0;y<512;y++)for(let x=0;x<512;x++){
  const px=x+.5,py=y+.5,xx=(px-256)/sx+eyes.x*2-.5,yy=(py<=320?(py-320)/sx+eyes.y*2:py<=368?(py-320)/sy+eyes.y*2:(py-368)/body+368)-.5;
  const pixel=sample(color,xx,yy),i=y*512+x;for(let c=0;c<4;c++)out[i*4+c]=pixel[c];
  const ix=Math.round(xx),iy=Math.round(yy);if(ix>=0&&ix<512&&iy>=0&&iy<512&&pixel[3]>128&&selection[(iy*512+ix)*3])maskOut[i*3]=maskOut[i*3+1]=maskOut[i*3+2]=255;
 }
 await sharp(out,{raw:{width:512,height:512,channels:4}}).png().toFile(portrait);await sharp(maskOut,{raw:{width:512,height:512,channels:3}}).png().toFile(mask);
 manifest.finalLayout[a.id]={before:{eyes,bottom},scale:{x:sx,faceY:sy,bodyY:body},target:{eyeX:128,eyeY:160,eyeDistance:39.5,chinY:184,bodyBottomY:244}};
 }fs.writeFileSync(mp,JSON.stringify(manifest,null,2));console.log('Final calibrated 21 portrait/mask pairs.');})();
