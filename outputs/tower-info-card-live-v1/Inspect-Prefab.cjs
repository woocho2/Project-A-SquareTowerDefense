const fs=require('fs');const raw=fs.readFileSync('Assets/2. Prefab/Canvas.prefab','utf8');
const blocks=raw.split(/(?=^--- !u!)/m).filter(s=>s.startsWith('---'));
const map=new Map(blocks.map(b=>[b.match(/^--- !u!\d+ &(\d+)/)[1],b]));
function walk(id,depth=0){const r=map.get(id);if(!r)return;const gid=r.match(/m_GameObject: \{fileID: (\d+)\}/)?.[1],go=map.get(gid)||'';console.log(' '.repeat(depth*2)+(go.match(/m_Name: (.*)/)?.[1]||'?')+' [rect '+id+'] '+(r.match(/m_AnchoredPosition: .*/)?.[0]||'')+' '+(r.match(/m_SizeDelta: .*/)?.[0]||''));const child=r.split('  m_Children:')[1]?.split('  m_Father:')[0]||'';for(const ref of child.matchAll(/fileID: (\d+)/g))walk(ref[1],depth+1);}
walk('8770730461144707938');
