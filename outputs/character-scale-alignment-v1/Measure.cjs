const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const configs={TowerHero:{roi:[87,140,171,176],color:'gold'},TowerSwordsman:{roi:[85,147,175,179],color:'blue'},TowerPaladin:{roi:[85,136,173,165],color:'blue'},TowerDeathKnight:{roi:[85,136,172,166],color:'purple'},Valkyrie:{roi:[83,139,181,177],color:'magenta'},Brunhild:{roi:[78,122,178,158],color:'blue'},Loki:{roi:[79,122,181,159],color:'green'},CorruptedElf:{roi:[81,141,176,175],color:'purple'},CorruptedElfCaptain:{roi:[83,146,174,179],color:'gold'},CorruptedElfChief:{roi:[84,140,177,177],color:'red'}};
function match(r,g,b,type){if(type==='gold')return r>80&&r>g+14&&g>b+20&&g<185;if(type==='blue')return b>75&&b>r+5&&b>g+3;if(type==='purple')return b>65&&r>g+16&&b>g+25;if(type==='magenta')return r>85&&r>g+40&&b>g+20;if(type==='red')return r>85&&r>g+35&&r>b+15&&g<110&&b<150;if(type==='green')return g>90&&g>r+12&&g>b+30;}
(async()=>{const report={};for(const [name,originalConfig] of Object.entries(configs)){
 const c=process.argv.includes('--after')&&!name.startsWith('Tower')?{...originalConfig,roi:[83,147,176,177]}:originalConfig;
 const data=await sharp(path.join(__dirname,name+'.png')).ensureAlpha().raw().toBuffer(),halves=[];
 for(const [xmin,xmax] of [[c.roi[0],123],[133,c.roi[2]]]){
  const candidates=[];for(let y=c.roi[1];y<c.roi[3];y++)for(let x=xmin;x<xmax;x++){const i=(y*256+x)*4;if(data[i+3]>128&&match(data[i],data[i+1],data[i+2],c.color))candidates.push([x,y]);}
  const eligible=new Set(candidates.map(([x,y])=>y*256+x)),parts=[];
  while(eligible.size){const queue=[eligible.values().next().value];eligible.delete(queue[0]);for(let i=0;i<queue.length;i++)for(const p of [queue[i]-1,queue[i]+1,queue[i]-256,queue[i]+256])if(eligible.delete(p))queue.push(p);parts.push(queue);}
  let part;
  if(process.argv.includes('--after')&&!name.startsWith('Tower')){
   const expectedX=xmin<100?108.25:147.75;
   part=parts.filter(p=>p.length>=8).sort((a,b)=>{
    const score=p=>Math.hypot(p.reduce((s,n)=>s+n%256,0)/p.length-expectedX,p.reduce((s,n)=>s+Math.floor(n/256),0)/p.length-160)-Math.sqrt(p.length)*.1;
    return score(a)-score(b);
   })[0];
  }else part=parts.sort((a,b)=>b.length-a.length)[0];
  if(!part)throw new Error(name+' iris not found');
  halves.push({x:part.reduce((s,p)=>s+p%256,0)/part.length,y:part.reduce((s,p)=>s+Math.floor(p/256),0)/part.length,pixels:part.length,bounds:[Math.min(...part.map(p=>p%256)),Math.min(...part.map(p=>Math.floor(p/256))),Math.max(...part.map(p=>p%256)),Math.max(...part.map(p=>Math.floor(p/256)))]});
 }
 const eyeX=(halves[0].x+halves[1].x)/2,eyeY=(halves[0].y+halves[1].y)/2;
 const skinWidths=[];for(let y=Math.round(eyeY+8);y<Math.min(215,eyeY+45);y++){
  let l=256,r=0;for(let x=Math.round(eyeX-28);x<=Math.round(eyeX+28);x++){const i=(y*256+x)*4;if(data[i]>140&&data[i]>data[i+1]+6&&data[i+1]>data[i+2]+4&&data[i+1]>95){l=Math.min(l,x);r=Math.max(r,x);}}
  skinWidths.push([y,r>=l?r-l+1:0]);
 }
 report[name]={eyes:halves,eyeX,eyeY,eyeDistance:halves[1].x-halves[0].x,skinWidths};
}fs.writeFileSync(path.join(__dirname,process.argv.includes('--after')?'after-measured-landmarks.json':'measured-landmarks.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));})();
