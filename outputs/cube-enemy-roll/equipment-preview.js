// CPU display of the equipment shader; source PNGs are embedded unchanged.
const equipmentPalettes = [
  {key:'SelfHeal',label:'자가치유형',armor:[.72,.19,.25],gem:[1,.39,.49]},
  {key:'Mobility',label:'이동력형',armor:[.31,.69,.87],gem:[.69,.97,1]},
  {key:'Action',label:'행동력형',armor:[.23,.64,.37],gem:[.62,1,.56]},
  {key:'Defense',label:'방어형',armor:[.85,.68,.19],gem:[1,.94,.46]},
  {key:'GroupHeal',label:'전체회복형',armor:[.89,.92,.96],gem:[.9,1,1]}
];
const coloredFaces = {};
function equipmentMask(face,x,y,r,g,b){
  const box=(x0,y0,x1,y1)=>x>=x0&&x<=x1&&y>=y0&&y<=y1;
  if(face==='Right'){x=256-x;face='Left'}
  let equipment=false,jewel=false;
  switch(face){
    case 'Front':equipment=y<=125;jewel=box(105,32,151,108)||box(16,48,43,112)||box(213,48,240,112);break;
    case 'Left':equipment=y<=130;jewel=box(0,36,35,113)||box(95,38,164,112);break;
    case 'Back':equipment=y<=140;jewel=box(96,36,160,148)||box(16,48,48,116)||box(208,48,240,116);break;
    case 'Top':equipment=box(30,16,226,230);jewel=box(104,192,154,242);break;
    case 'Bottom':equipment=y<=53;jewel=box(109,12,151,48);break;
  }
  const value=Math.max(r,g,b),saturation=(value-Math.min(r,g,b))/Math.max(value,.001);
  return {value,armor:equipment&&saturation>=.18&&value>=.08&&value<=.72&&b>=r+.035&&b>=g+.012&&r-g<=.025,
    gem:jewel&&saturation>=.3&&value>=.12&&r>=g*1.22&&b>=g*1.3};
}
function tintEquipment(image,face,palette){
  const canvas=document.createElement('canvas');canvas.width=256;canvas.height=256;
  const context=canvas.getContext('2d');context.drawImage(image,0,0);
  const pixels=context.getImageData(0,0,256,256),original=pixels.data.slice();
  let armorCount=0,gemCount=0,protectedChanges=0,alphaChanges=0;
  for(let i=0;i<pixels.data.length;i+=4){
    if(!original[i+3])continue;
    const x=(i/4)%256+.5,y=Math.floor(i/4/256)+.5;
    const mask=equipmentMask(face,x,y,original[i]/255,original[i+1]/255,original[i+2]/255);
    let target=null,intensity=0;
    if(mask.armor){armorCount++;target=palette.armor;intensity=Math.max(.12,Math.min(1.18,mask.value/.42))}
    if(mask.gem){gemCount++;target=palette.gem;intensity=Math.max(.25,Math.min(1.2,mask.value/.8))}
    if(target)for(let n=0;n<3;n++)pixels.data[i+n]=Math.round(Math.min(1,target[n]*intensity)*255);
    if(pixels.data[i+3]!==original[i+3])alphaChanges++;
    // Eyes and face are below the equipment on these drawings; gold is warm.
    const protectedFace=face==='Front'&&y>=140||face==='Left'&&y>=150||face==='Right'&&y>=150||face==='Bottom'&&y>=54||face==='Back'&&y>=150;
    const gold=original[i]>original[i+1]&&original[i+1]>original[i+2]&&original[i]-original[i+2]>35;
    if((protectedFace||gold)&&[0,1,2].some(n=>pixels.data[i+n]!==original[i+n]))protectedChanges++;
  }
  if(alphaChanges||protectedChanges)throw Error('보존 영역 색상 검증 실패: '+face);
  context.putImageData(pixels,0,0);
  return {canvas,armorCount,gemCount};
}
async function prepareEquipmentFaces(load){
  const sources={};
  await Promise.all(Object.entries(cubeFaceSources).map(async([face,path])=>sources[face]=await load(path)));
  const counts=[];
  for(const palette of equipmentPalettes){
    coloredFaces[palette.key]={};
    for(const [face,image] of Object.entries(sources)){
      const result=tintEquipment(image,face,palette);
      coloredFaces[palette.key][face]=result.canvas;
      counts.push({palette:palette.key,face,armor:result.armorCount,gems:result.gemCount});
    }
  }
  document.body.dataset.equipmentValidation='passed';
  document.body.dataset.equipmentCounts=JSON.stringify(counts);
  return sources;
}
