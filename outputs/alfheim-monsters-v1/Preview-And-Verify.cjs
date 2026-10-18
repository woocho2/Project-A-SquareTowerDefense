const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),assetDir=path.join(root,'Assets/4. Asset/4. Enemy/Alfheim'),names=['CorruptedElf','CorruptedElfCaptain','CorruptedElfChief'];
const korean=['타락한 엘프','타락한 엘프 부족장','타락한 엘프 족장'];
const yaml=fs.readFileSync(path.join(root,'Assets/Resources/SharedBoardVisuals.asset'),'utf8').split('  m_enemyStates:')[1].split('  m_enemies:')[0];
const colors=[...yaml.matchAll(/- state: (\d+)[\s\S]*?background: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}[\s\S]*?decoration: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}[\s\S]*?equipment: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}/g)].map(m=>({state:+m[1],background:m.slice(2,6).map(Number),decoration:m.slice(6,10).map(Number),equipment:m.slice(10,14).map(Number)}));
function over(bottom,top){const out=Buffer.alloc(bottom.length);for(let i=0;i<bottom.length;i+=4){const ta=top[i+3]/255,ba=bottom[i+3]/255,a=ta+ba*(1-ta);for(let c=0;c<3;c++)out[i+c]=a?Math.round((top[i+c]*ta+bottom[i+c]*ba*(1-ta))/a):0;out[i+3]=Math.round(a*255);}return out;}
function tint(data,color){const out=Buffer.from(data);for(let i=0;i<out.length;i+=4)for(let c=0;c<4;c++)out[i+c]=Math.round(data[i+c]*color[c]);return out;}
async function rgba(p){return sharp(p).ensureAlpha().raw().toBuffer();}
(async()=>{
 if(colors.length!==6)throw new Error('Could not read six existing face colors');
 const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'asset-manifest.json'),'utf8'));
 const background=await rgba(path.join(root,'Assets/4. Asset/0. Common/Board_Background.png')),decoration=await rgba(path.join(root,'Assets/4. Asset/0. Common/Board_Decoration.png'));
 const columns=[],states=[],checks=[];
 const title=Buffer.from('<svg width="1200" height="68" xmlns="http://www.w3.org/2000/svg"><style>text{font-family:Malgun Gothic,sans-serif;fill:#f2f2e9;font-size:23px;text-anchor:middle}</style>'+korean.map((name,i)=>'<text x="'+(200+i*400)+'" y="42">'+name+'</text>').join('')+'</svg>');
 for(let row=0;row<names.length;row++){
  const name=names[row],portraitPath=path.join(assetDir,name+'_Character.png'),maskPath=path.join(assetDir,name+'_EquipmentMask.png');
  const metadata=await sharp(portraitPath).metadata(),portrait=await rgba(portraitPath),mask=await sharp(maskPath).removeAlpha().raw().toBuffer();
  if(metadata.width!==512||metadata.height!==512||!metadata.hasAlpha)throw new Error('Invalid portrait format');
  const alpha=await sharp(portraitPath).stats();if(alpha.channels[3].min!==0||alpha.channels[3].max!==255)throw new Error('Missing usable alpha');
  let selected=0;for(let i=0;i<512*512;i++){if(mask[i*3]!==0&&mask[i*3]!==255)throw new Error('Nonbinary mask');if(mask[i*3]!==mask[i*3+1]||mask[i*3]!==mask[i*3+2])throw new Error('Colored data mask');if(mask[i*3]){selected++;if(portrait[i*4+3]<=128)throw new Error('Mask outside visible character');}}
  const appearance=fs.readFileSync(path.join(assetDir,name+'_Appearance.asset'),'utf8');
  for(const suffix of ['Character','EquipmentMask']){const meta=fs.readFileSync(path.join(assetDir,name+'_'+suffix+'.png.meta'),'utf8');if(!meta.includes(manifest.assets[name+'_'+suffix])||!appearance.includes(manifest.assets[name+'_'+suffix]))throw new Error('Broken appearance texture reference');}
  const maskMeta=fs.readFileSync(maskPath+'.meta','utf8');if(!maskMeta.includes('sRGBTexture: 0')||!maskMeta.includes('textureCompression: 0'))throw new Error('Mask importer incorrect');
  columns.push({input:await sharp(portraitPath).resize(400,400).png().toBuffer(),left:row*400,top:68});
  for(const color of colors){
   const character=Buffer.from(portrait);for(let i=0;i<512*512;i++)if(mask[i*3])for(let c=0;c<3;c++)character[i*4+c]=Math.round(character[i*4+c]*color.equipment[c]);
   let combined=over(tint(background,color.background),character);combined=over(combined,tint(decoration,color.decoration));
   // Reuse the shared 3x2 state-badge atlas and the current material slot order.
   const cell=color.state;
   const badge=await sharp(path.join(root,'Assets/4. Asset/4. Enemy/Enemy_State_Badges_Atlas.png')).extract({left:(cell%3)*128,top:Math.floor(cell/3)*128,width:128,height:128}).ensureAlpha().raw().toBuffer();
   const badgeLayer=Buffer.alloc(512*512*4);for(let y=0;y<128;y++)for(let x=0;x<128;x++){const a=(y*128+x)*4,b=(y*512+x+192)*4;for(let c=0;c<4;c++)badgeLayer[b+c]=badge[a+c];}
   combined=over(combined,badgeLayer);
   states.push({input:await sharp(combined,{raw:{width:512,height:512,channels:4}}).resize(256,256).png().toBuffer(),left:color.state*256,top:row*256});
  }
  checks.push({name,korean:korean[row],portrait:'512x512 RGBA',mask:'512x512 RGB binary linear uncompressed',maskSelectedPixels:selected,referencesValid:true,bounds:manifest.bounds[name]});
 }
 await sharp({create:{width:1200,height:468,channels:4,background:'#29313d'}}).composite([{input:title,left:0,top:0},...columns]).png().toFile(path.join(__dirname,'overview.png'));
 await sharp({create:{width:1536,height:768,channels:4,background:'#29313d'}}).composite(states).png().toFile(path.join(__dirname,'six-state-preview.png'));
 const guids=Object.values(manifest.assets);if(new Set(guids).size!==guids.length)throw new Error('Duplicate new GUID');
 fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify({assets:checks,existingGameFilesModified:false,preview:'CPU composition from current shared textures/palette, not Unity runtime render',unityImportOrPlayTested:false},null,2));console.log(JSON.stringify(checks));
})();
