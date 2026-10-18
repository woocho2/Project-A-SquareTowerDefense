const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),config=require('./layout-config.json'),measured=require('./after-measured-landmarks.json');
(async()=>{
 const rows=[];
 for(const subject of config.subjects){
  const folder=path.join(root,'Assets/4. Asset/4. Enemy',subject.folder),portrait=path.join(folder,subject.name+'_Character.png'),mask=path.join(folder,subject.name+'_EquipmentMask.png');
  const [metadata,maskMetadata]=await Promise.all([sharp(portrait).metadata(),sharp(mask).metadata()]);if(metadata.width!==512||metadata.height!==512||!metadata.hasAlpha||maskMetadata.width!==512||maskMetadata.height!==512)throw new Error('Unexpected asset dimensions');
  const pixels=await sharp(portrait).ensureAlpha().raw().toBuffer(),maskPixels=await sharp(mask).removeAlpha().raw().toBuffer();let bottom=0,selected=0;
  for(let i=0;i<512*512;i++){
   if(pixels[i*4+3]>32)bottom=Math.max(bottom,Math.floor(i/512)+1);
   const value=maskPixels[i*3];if(value!==0&&value!==255||value!==maskPixels[i*3+1]||value!==maskPixels[i*3+2])throw new Error('Mask must stay binary');
   if(value){selected++;if(pixels[i*4+3]<=128)throw new Error('Mask outside visible portrait');}
  }
  const landmarks=measured[subject.name],eyeCenterError=Math.hypot(landmarks.eyeX-config.target.eyeX,landmarks.eyeY-config.target.eyeY),spacingError=Math.abs(landmarks.eyeDistance-config.target.eyeDistance);
  if(eyeCenterError>2.25||spacingError>2.25)throw new Error('Visible iris landmarks did not align: '+subject.name+' '+eyeCenterError+' '+spacingError);
  if(Math.abs(bottom-488)>1)throw new Error('Body baseline mismatch');
  for(const suffix of ['_Character.png.meta','_EquipmentMask.png.meta','_Appearance.asset','_Appearance.asset.meta']){
   const current=fs.readFileSync(path.join(folder,subject.name+suffix)),before=fs.readFileSync(path.join(__dirname,'before',subject.folder,subject.name+suffix));
   if(!current.equals(before))throw new Error('Metadata/appearance references changed');
  }
  rows.push({name:subject.name,eyeCenter:[landmarks.eyeX,landmarks.eyeY],eyeDistance:landmarks.eyeDistance,eyeCenterError,spacingError,bodyBottom:bottom,maskSelectedPixels:selected,metadataAndReferencesPreserved:true});
 }
 fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify({reference:config.reference,target:config.target,assets:rows,unityRuntimeTested:false},null,2));console.log(JSON.stringify(rows,null,2));
})();
