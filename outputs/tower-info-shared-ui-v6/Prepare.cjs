const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/ui-common-kit-v1/unity-validation');
const defs=JSON.parse(fs.readFileSync(path.join(__dirname,'icons.json'),'utf8'));
const sources=JSON.parse(fs.readFileSync(path.join(__dirname,'sources.json'),'utf8'));
const atlasRel='Assets/4. Asset/5. UI/Tower_Card_Icon_Atlas_v1.png';
const hash=buffer=>crypto.createHash('sha256').update(buffer).digest('hex');
function copy(rel){const dest=path.join(fixture,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,rel),dest);if(fs.existsSync(path.join(root,rel+'.meta')))fs.copyFileSync(path.join(root,rel+'.meta'),dest+'.meta');}
(async()=>{
 const sheets=[],report=[];
 for(let i=0;i<defs.length;i++){
  const d=defs[i],source=path.join(__dirname,d.key+'-generated.png');
  if(!fs.existsSync(source))fs.copyFileSync(sources[d.key],source);
  const {data,info}=await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let x0=info.width,y0=info.height,x1=-1,y1=-1,transparent=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){const a=data[(y*info.width+x)*4+3];if(a<4)transparent++;if(a>=8){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}}
  if(x1<0||transparent<info.width*info.height*.04)throw Error('Missing real transparency '+d.key);
  const ink=await sharp(source).extract({left:x0,top:y0,width:x1-x0+1,height:y1-y0+1}).resize(448,448,{fit:'inside',kernel:'lanczos3'}).png().toBuffer();
  const size=await sharp(ink).metadata();
  const png=await sharp({create:{width:512,height:512,channels:4,background:'#00000000'}}).composite([{input:ink,left:Math.floor((512-size.width)/2),top:Math.floor((512-size.height)/2)}]).png().toBuffer();
  fs.writeFileSync(path.join(__dirname,d.name+'.png'),png);
  sheets.push({input:png,left:(i%3)*512,top:Math.floor(i/3)*512});
  report.push({...d,id:21300001+i,x:(i%3)*512,y:(2-Math.floor(i/3))*512,size:[512,512],sourceSize:[info.width,info.height],transparentPixels:transparent,sha256:hash(png)});
 }
 const atlas=await sharp({create:{width:1536,height:1536,channels:4,background:'#00000000'}}).composite(sheets).png().toBuffer();
 const destination=path.join(root,atlasRel),manifest=path.join(__dirname,'verification.json');
 if(fs.existsSync(destination)&&!fs.existsSync(manifest))throw Error('Unrelated atlas exists');
 if(fs.existsSync(manifest)&&fs.existsSync(destination)&&hash(fs.readFileSync(destination))!==JSON.parse(fs.readFileSync(manifest)).atlasSha256)throw Error('Atlas changed since previous build');
 fs.writeFileSync(destination,atlas);fs.writeFileSync(path.join(__dirname,'Tower_Card_Icon_Atlas_v1.png'),atlas);
 const guid=crypto.createHash('md5').update('SquareTowerDefense/Tower_Card_Icon_Atlas_v1').digest('hex');
 let meta=fs.readFileSync(path.join(root,'Assets/4. Asset/5. UI/Icon_Attack_Basic_Common.png.meta'),'utf8').replace(/^guid: .*$/m,'guid: '+guid).replace('  spriteMode: 1','  spriteMode: 2').replace(/maxTextureSize: 1024/g,'maxTextureSize: 2048').replace(/  userData: .*$/m,'  userData: Generated card icon atlas - tower-info-shared-ui-v6');
 const blocks=report.map(d=>`    - serializedVersion: 2
      name: ${d.name}
      rect:
        serializedVersion: 2
        x: ${d.x}
        y: ${d.y}
        width: 512
        height: 512
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: ${crypto.createHash('md5').update(d.name).digest('hex')}
      internalID: ${d.id}
      vertices: []
      indices:
      edges: []
      weights: []`).join('\n');
 meta=meta.replace('    sprites: []','    sprites:\n'+blocks).replace('    nameFileIdTable: {}','    nameFileIdTable:\n'+report.map(d=>'      '+d.name+': '+d.id).join('\n'));
 if(fs.existsSync(destination+'.meta')){const old=fs.readFileSync(destination+'.meta','utf8').match(/^guid: (.*)$/m)?.[1];if(old!==guid)throw Error('Unexpected existing GUID');}
 fs.writeFileSync(destination+'.meta',meta);copy(atlasRel);
 for(const rel of ['Assets/4. Asset/5. UI/Shared_UI_Atlas.png','Assets/Resources/UITileSkin.asset','Assets/4. Asset/5. UI/Icon_Attack_Basic_Common.png','Assets/4. Asset/5. UI/Icon_Attack_Skill_Common.png','Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png'])copy(rel);
 fs.copyFileSync(path.join(__dirname,'CardIconPreview.cs'),path.join(fixture,'Assets/Editor/CardIconPreview.cs'));
 fs.writeFileSync(manifest,JSON.stringify({atlasPath:atlasRel,atlasGuid:guid,atlasSize:[1536,1536],atlasSha256:hash(atlas),icons:report,previewOnly:true},null,2));
 console.log('Prepared 9 icons in 1536x1536 atlas and isolated Unity card preview.');
})();
