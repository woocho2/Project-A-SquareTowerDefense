const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/ui-common-kit-v1/unity-validation');
const generated='C:/Users/user/.codex/generated_images/01a1347f-21af-71c1-8a9c-3a9cea2aa748';
const specs=[['basic-attack','exec-b5184940-a215-4943-88a5-437b8f03965c.png','Icon_Attack_Basic_Common'],['skill-attack','exec-11b552f7-0797-4def-a451-b3b1a9521af0.png','Icon_Attack_Skill_Common']];
const ui='Assets/4. Asset/5. UI/';
function copy(rel){const dest=path.join(fixture,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,rel),dest);if(fs.existsSync(path.join(root,rel+'.meta')))fs.copyFileSync(path.join(root,rel+'.meta'),dest+'.meta');}
function meta(guid){return `fileFormatVersion: 2
guid: ${guid}
TextureImporter:
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  maxTextureSize: 1024
  textureCompression: 0
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 1024
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: ${crypto.randomBytes(16).toString('hex')}
    internalID: 0
    secondaryTextures: []
    nameFileIdTable: {}
  userData: Generated shared attack category icon - tower-info-shared-ui-v5
  assetBundleName:
  assetBundleVariant:
`;}
(async()=>{
 const report=[];
 for(const [key,file,name] of specs){
  const source=path.join(__dirname,key+'-generated.png');fs.copyFileSync(path.join(generated,file),source);
  const {data,info}=await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let minX=info.width,minY=info.height,maxX=-1,maxY=-1,transparent=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){const a=data[(y*info.width+x)*4+3];if(a<4)transparent++;if(a>=8){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}}
  if(transparent<info.width*info.height*.05||maxX<0)throw Error('Missing real transparency: '+key);
  const cropped=await sharp(source).extract({left:minX,top:minY,width:maxX-minX+1,height:maxY-minY+1}).resize(448,448,{fit:'inside',kernel:'lanczos3'}).png().toBuffer();
  const dim=await sharp(cropped).metadata();const output=await sharp({create:{width:512,height:512,channels:4,background:'#00000000'}}).composite([{input:cropped,left:Math.floor((512-dim.width)/2),top:Math.floor((512-dim.height)/2)}]).png().toBuffer();
  const rel=ui+name+'.png',dest=path.join(root,rel);if(fs.existsSync(dest)&&!fs.existsSync(path.join(__dirname,'verification.json')))throw Error('Refusing unrelated overwrite '+rel);
  fs.writeFileSync(dest,output);fs.writeFileSync(path.join(__dirname,name+'.png'),output);
  if(!fs.existsSync(dest+'.meta'))fs.writeFileSync(dest+'.meta',meta(crypto.randomBytes(16).toString('hex')));
  copy(rel);report.push({name,path:rel,size:[512,512],sourceSize:[info.width,info.height],transparentPixels:transparent,sha256:crypto.createHash('sha256').update(output).digest('hex')});
 }
 for(const rel of [ui+'Shared_UI_Atlas.png',ui+'Stat_Icon_Atlas_v2.png','Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png','Assets/Resources/UITileSkin.asset'])copy(rel);
 const editor=path.join(fixture,'Assets/Editor/SharedCardPreview.cs');fs.copyFileSync(path.join(__dirname,'SharedCardPreview.cs'),editor);
 const fontDest=path.join(fixture,'Assets/PreviewOnlyFont.ttf');fs.copyFileSync('C:/Windows/Fonts/malgunbd.ttf',fontDest);
 fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify({icons:report,previewOnly:true,sharedUiAtlasUnchanged:true},null,2));
 console.log(JSON.stringify(report,null,2));
})();
