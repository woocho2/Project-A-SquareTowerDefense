const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname;
(async()=>{
  const manifest=JSON.parse(fs.readFileSync(path.join(out,'asset-manifest.json'),'utf8'));
  assert(manifest.complete&&manifest.connected);assert.equal(manifest.assets.length,28);
  const library=fs.readFileSync(path.join(root,'Assets/2. Prefab/4. Effect/EffectLibrary.asset'),'utf8');
  const before=fs.readFileSync(path.join(out,'before/EffectLibrary.asset'),'utf8');
  const entries=text=>[...text.matchAll(/  - effectID: (\d+)\r?\n    effectPrefab: (\{[^}]+\})/g)].map(m=>({id:Number(m[1]),ref:m[2]}));
  const original=entries(before),current=entries(library),attackIds=new Set(manifest.assets.map(a=>a.effectID));
  assert.equal(current.length,original.length);assert.equal(new Set(current.map(e=>e.id)).size,current.length);
  for(const entry of current)if(!attackIds.has(entry.id))assert.equal(entry.ref,original.find(x=>x.id===entry.id).ref);
  for(const asset of manifest.assets){
    const file=path.join(root,asset.base+'.png'),metadata=await sharp(file).metadata();
    assert.equal(metadata.width,1536);assert.equal(metadata.height,1280);assert(metadata.hasAlpha);
    assert(current.find(e=>e.id===asset.effectID).ref.includes(asset.guids.prefab));
    if(asset.reused)continue;
    const files=fs.readdirSync(path.join(out,'frames',asset.job)).filter(f=>f.endsWith('.png'));
    assert.equal(files.length,30);
    assert(new Set(files.map(f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(out,'frames',asset.job,f))).digest('hex'))).size>=26);
    assert(asset.packing.sourceEdgePixels<=2);
    const {data}=await sharp(file).raw().toBuffer({resolveWithObject:true});
    for(let row=0;row<5;row++)for(let col=0;col<6;col++)for(let n=0;n<256;n++){
      for(const [x,y] of [[col*256,row*256+n],[col*256+255,row*256+n],[col*256+n,row*256],[col*256+n,row*256+255]])assert.equal(data[(y*1536+x)*4+3],0,'Atlas cell edge alpha: '+asset.job);
    }
    const gif=await sharp(path.join(out,'gif',asset.job+'.gif'),{animated:true}).metadata();
    assert.equal(gif.pages,30);assert.equal(gif.delay.reduce((sum,d)=>sum+d,0),2500);
  }
  const report='PASS 28 connected RGBA atlases, 840 sprite cells\nPASS 26 new atlases and GIFs, transparent cell edges, source splits and distinct progression\nPASS 30 GIF frames and 2500ms per effect\nPASS all attack bindings and unchanged non-attack library entries\n';
  fs.writeFileSync(path.join(out,'export-validation.txt'),report);console.log(report);
})().catch(e=>{console.error(e);process.exitCode=1});
