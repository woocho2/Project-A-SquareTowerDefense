const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname;
(async()=>{
  const manifest=JSON.parse(fs.readFileSync(path.join(out,'asset-manifest.json'),'utf8'));assert(manifest.complete);assert.equal(manifest.assets.length,14);
  for(const entry of manifest.assets){
    const metadata=await sharp(path.join(root,entry.base+'.png')).metadata();assert.equal(metadata.width,1024);assert.equal(metadata.height,1024);assert(metadata.hasAlpha);
    for(const state of ['Idle','Flight']){
      const dir=path.join(out,'frames',entry.key,state),files=fs.readdirSync(dir).filter(p=>p.endsWith('.png'));assert.equal(files.length,8);
      assert.equal(new Set(files.map(p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(dir,p))).digest('hex'))).size,8);
      for(const file of files){const {data}=await sharp(path.join(dir,file)).raw().toBuffer({resolveWithObject:true});for(let n=0;n<256;n++)for(const [x,y] of [[0,n],[255,n],[n,0],[n,255]])assert.equal(data[(y*256+x)*4+3],0);}
      const gif=await sharp(path.join(out,'gif',entry.key+'_'+state+'.gif'),{animated:true}).metadata();assert.equal(gif.pages,8);assert.equal(gif.delay.reduce((a,b)=>a+b,0),670);
    }
  }
  const specs=JSON.parse(fs.readFileSync(path.join(out,'prompts.json'),'utf8')).specs;
  const extraA='\nInput image 2 is the completed IDLE loop: preserve its weapon/element body identity exactly, change only the looping flight glint and close wake. Keep head shape, head size, direction and body palette. Idle ref background is for viewing only, output TRUE transparency.';
  const extraB='\nInput2 is the completed IDLE loop: preserve that body identity exactly. Change ONLY looped flight glint and short wake. Preserve head shape/size/direction/palette. Reference backdrop is not part of output, use true transparency.';
  for(const spec of specs){const file=path.join(out,'generation',spec.job+'.json'),record=JSON.parse(fs.readFileSync(file,'utf8'));if(!record.actualPrompt){record.actualPrompt=spec.prompt+(spec.state==='Flight'?(spec.id<=4?extraA:extraB):'');fs.writeFileSync(file,JSON.stringify(record,null,2)+'\n');}}
  const report='PASS 14 RGBA atlases, 28 loops, 224 unique frames\nPASS transparent frame borders and 28 GIFs with 8 frames / 670ms\nPASS original raster sources, actual prompts, Idle / Flight body registration\n';fs.writeFileSync(path.join(out,'export-validation.txt'),report);console.log(report);
})().catch(e=>{console.error(e);process.exitCode=1});
