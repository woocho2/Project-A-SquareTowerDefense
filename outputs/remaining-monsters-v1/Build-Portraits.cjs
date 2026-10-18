const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),plan=require('./production-plan.json'),manifestPath=path.join(__dirname,'asset-manifest.json');
const manifest=fs.existsSync(manifestPath)?JSON.parse(fs.readFileSync(manifestPath,'utf8')):{assets:{},portraits:{}};
const chin={LowerSpirit:191,MiddleSpirit:185,UpperSpirit:189,SeaSerpent:202,GreatSeaSerpent:195,Jormungandr:184,FrostGiant:205,FrostGiantChief:190,Ymir:180,CorruptedDwarf:192,CorruptedDwarfArtisan:180,Fafnir:200,FrostWolf:195,FrostWolfAlpha:200,Fenrir:199,FireGiant:200,FireGiantChief:190,Surtr:193,Wight:187,Garm:193,HelQueen:195};
function guid(key){return manifest.assets[key]||(manifest.assets[key]=crypto.randomBytes(16).toString('hex'));}
function meta(file,template,key){if(!fs.existsSync(file+'.meta'))fs.writeFileSync(file+'.meta',template.replace(/^guid: [a-f0-9]+/m,'guid: '+guid(key)));}
function match(r,g,b,type){if(type==='gold')return r>90&&r>g+14&&g>b+22&&g<200;if(type==='blue')return b>75&&b>r+24&&g>r+8&&r<145&&g<200;if(type==='purple')return r>g+18&&b>g+24&&b>65;if(type==='red')return r>90&&r>g+38&&r>b+20&&g<135;if(type==='green')return g>95&&g>r+12&&g>b+18;}
function eyes(data,type){const points=[];for(const [xmin,xmax,expected] of [[145,247,216],[265,367,296]]){
 const eligible=new Set();for(let y=263;y<376;y++)for(let x=xmin;x<xmax;x++){const i=(y*512+x)*4;if(data[i+3]>128&&match(data[i],data[i+1],data[i+2],type))eligible.add(y*512+x);}
 const components=[];while(eligible.size){const queue=[eligible.values().next().value];eligible.delete(queue[0]);for(let i=0;i<queue.length;i++)for(const p of [queue[i]-1,queue[i]+1,queue[i]-512,queue[i]+512])if(eligible.delete(p))queue.push(p);if(queue.length<12)continue;const xs=queue.map(p=>p%512),ys=queue.map(p=>Math.floor(p/512)),width=Math.max(...xs)-Math.min(...xs)+1,height=Math.max(...ys)-Math.min(...ys)+1;if(width>62||height>62||queue.length>1900)continue;const x=xs.reduce((a,b)=>a+b,0)/xs.length,y=ys.reduce((a,b)=>a+b,0)/ys.length;components.push({x,y,pixels:queue.length,score:Math.hypot(x-expected,(y-320)*.75)-Math.sqrt(queue.length)*.5});}
 const found=components.sort((a,b)=>a.score-b.score)[0];if(!found)throw new Error('Missing iris: '+type+' '+xmin);points.push(found);
 }return {x:(points[0].x+points[1].x)/4,y:(points[0].y+points[1].y)/4,distance:(points[1].x-points[0].x)/2,points};}
function sample(data,x,y){const ix=Math.floor(x),iy=Math.floor(y),fx=x-ix,fy=y-iy,rgb=[0,0,0];let a=0;for(let dy=0;dy<=1;dy++)for(let dx=0;dx<=1;dx++){const xx=ix+dx,yy=iy+dy;if(xx<0||xx>=512||yy<0||yy>=512)continue;const i=(yy*512+xx)*4,w=(dx?fx:1-fx)*(dy?fy:1-fy)*data[i+3]/255;a+=w;for(let c=0;c<3;c++)rgb[c]+=data[i+c]*w;}return [...rgb.map(v=>a?Math.round(v/a):0),Math.round(a*255)];}
(async()=>{
 const characterMeta=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Asgard/Valkyrie_Character.png.meta'),'utf8'),folderMeta=fs.readFileSync(path.join(root,'Assets/4. Asset/4. Enemy/Asgard.meta'),'utf8');
 for(const asset of plan.assets){
  const sourceFile=path.join(__dirname,'source',asset.id+'.png');if(!fs.existsSync(sourceFile))throw new Error('Missing portrait source: '+asset.id);
  const data=await sharp(sourceFile).resize(512,512).ensureAlpha().raw().toBuffer();
  const landmarks=eyes(data,asset.eyeColor);if(landmarks.distance<27||landmarks.distance>77)throw new Error('Invalid eye spacing: '+asset.id);
  let bottom=0;for(let y=0;y<500;y++){let count=0;for(let x=20;x<492;x++)if(data[(y*512+x)*4+3]>128)count++;if(count>30)bottom=(y+1)/2;}
  const sx=39.5/landmarks.distance,sy=24/(chin[asset.id]-landmarks.y),body=60/(bottom-chin[asset.id]);
  if(sy<.3||sy>2||body<.4||body>3)throw new Error('Invalid face/body anchors: '+asset.id+' '+JSON.stringify({landmarks,chin:chin[asset.id],bottom,sy,body}));
  const output=Buffer.alloc(512*512*4);let clipped=0;
  for(let y=0;y<512;y++)for(let x=0;x<512;x++){
   const lx=(x+.5)/2,ly=(y+.5)/2,sourceX=((lx-128)/sx+landmarks.x)*2-.5;
   const sourceY=(ly<=160?(ly-160)/sx+landmarks.y:ly<=184?(ly-160)/sy+landmarks.y:(ly-184)/body+chin[asset.id])*2-.5;
   const pixel=sample(data,sourceX,sourceY),i=(y*512+x)*4;for(let c=0;c<4;c++)output[i+c]=pixel[c];
   if((x<2||x>509||y<2||y>509)&&pixel[3]>64)clipped++;
  }
  if(clipped>20)throw new Error('Portrait clipping requires review: '+asset.id+' '+clipped);
  const folder=path.join(root,'Assets/4. Asset/4. Enemy',asset.realm);fs.mkdirSync(folder,{recursive:true});meta(folder,folderMeta,asset.realm);
  const file=path.join(folder,asset.id+'_Character.png');await sharp(output,{raw:{width:512,height:512,channels:4}}).png().toFile(file);meta(file,characterMeta,asset.id+'_Character');
  manifest.portraits[asset.id]={...asset,path:path.relative(root,file),sourceLandmarks:landmarks,sourceChin:chin[asset.id],sourceBottom:bottom,scale:{x:sx,faceY:sy,bodyY:body},target:{eyeX:128,eyeY:160,eyeDistance:39.5,chinY:184,bodyBottomY:244},clippedPixels:clipped};
  console.log(asset.id,JSON.stringify({eyes:[landmarks.x,landmarks.y,landmarks.distance],scale:[sx,sy,body]}));
 }
 fs.writeFileSync(manifestPath,JSON.stringify(manifest,null,2));
})();
