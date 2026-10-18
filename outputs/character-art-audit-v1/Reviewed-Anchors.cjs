const fs=require('node:fs'),path=require('node:path'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const p=require('./inventory.json').portraits.filter(a=>a.type!=='monster').sort((a,b)=>+a.id-+b.id),root=path.resolve(__dirname,'../..'),config=require('./layout-config.json');
(async()=>{for(let page=0;page<4;page++){
 const cells=[];
 for(let i=0;i<20&&page*20+i<p.length;i++){
  const a=p[page*20+i],x=i%4*256,y=Math.floor(i/4)*192;
  let svg='<svg width="256" height="160" xmlns="http://www.w3.org/2000/svg">';
  for(const xx of [80,100,110,120,128,140,150,160,180])svg+='<path d="M'+((xx-64)*2)+' 0V160" stroke="white" opacity=".18"/><text x="'+((xx-64)*2)+'" y="12" font-size="10" fill="white">'+xx+'</text>';
  for(const yy of [140,150,160,170,180])svg+='<path d="M0 '+((yy-120)*2)+'H256" stroke="white" opacity=".2"/><text x="2" y="'+((yy-120)*2)+'" font-size="10" fill="white">'+yy+'</text>';
  const [ex,ey,d]=config.subjects[a.id];
  if(a.id!=='53013')for(const xx of [ex-d/2,ex+d/2])svg+='<circle cx="'+((xx-64)*2)+'" cy="'+((ey-120)*2)+'" r="5" fill="none" stroke="#00ff58" stroke-width="1.5"/>';
  svg+='</svg>';
  cells.push({input:await sharp(path.join(root,a.cell)).extract({left:64,top:120,width:128,height:80}).resize(256,160).composite([{input:Buffer.from(svg)}]).png().toBuffer(),left:x,top:y});
  cells.push({input:Buffer.from('<svg width="256" height="32" xmlns="http://www.w3.org/2000/svg"><text x="128" y="22" text-anchor="middle" fill="white" font-size="14">'+a.id+' '+a.name.replace('Tower_','').replace('Synergy_','').replace('_Front_v2','')+'</text></svg>'),left:x,top:y+160});
 }
 await sharp({create:{width:1024,height:Math.ceil(Math.min(20,p.length-page*20)/4)*192,channels:4,background:'#646a76'}}).composite(cells).png().toFile(path.join(__dirname,'anchors-review-'+(page+1)+'.png'));
}})();
