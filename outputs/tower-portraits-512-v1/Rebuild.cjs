const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const sharp = require('../../_codex_spreadsheet_task/node_modules/sharp');
const root = path.resolve(__dirname, '../..'), old = path.join(root, 'outputs/character-art-audit-v1');
const inventory = require('../character-art-audit-v1/inventory.json');
const config = require('../character-art-audit-v1/layout-config.json');
const layout = require('../character-art-audit-v1/layout-report.json');
const roster = require('../tower-front-portraits-v2/roster.json');
const sha = b => crypto.createHash('sha256').update(b).digest('hex');
const check = (c, m) => { if (!c) throw new Error(m); };
const cache = new Map();
async function load(file) {
  if (!cache.has(file)) cache.set(file, await sharp(path.join(root, file)).ensureAlpha().raw().toBuffer({ resolveWithObject: true }));
  return cache.get(file);
}
// Reproduce the original sheet isolation, including the approved two-dwarf portrait.
function isolate(source, rect, keepCount, lower, trim, ice) {
  const { data, info } = source, w = rect.width, h = rect.height, rgba = Buffer.alloc(w * h * 4);
  for (let y = 0; y < h; y++) data.copy(rgba, y * w * 4, ((rect.top + y) * info.width + rect.left) * 4, ((rect.top + y) * info.width + rect.left + w) * 4);
  if (trim) for (let y = 0; y < h; y++) rgba.fill(0, y * w * 4, (y * w + Math.ceil(w * .06)) * 4);
  if (lower) for (let y = 0; y < Math.min(12, h); y++) {
    let n = 0; for (let x = 0; x < w; x++) if (rgba[(y * w + x) * 4 + 3] >= 8) n++;
    if (n > w * .6) rgba.fill(0, y * w * 4, (y + 1) * w * 4);
  }
  const labels = new Int32Array(w * h), queue = new Int32Array(w * h), counts = [0];
  let label = 0;
  for (let i = 0; i < w * h; i++) if (rgba[i * 4 + 3] >= 8 && !labels[i]) {
    label++; counts.push(0); let start = 0, end = 1; queue[0] = i; labels[i] = label;
    while (start < end) {
      const p = queue[start++], x = p % w, y = Math.floor(p / w); counts[label]++;
      for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
        const xx = x + dx, yy = y + dy; if (xx < 0 || xx >= w || yy < 0 || yy >= h) continue;
        const n = yy * w + xx; if (!labels[n] && rgba[n * 4 + 3] >= 8) { labels[n] = label; queue[end++] = n; }
      }
    }
  }
  const order = counts.map((n, i) => i).slice(1).sort((a, b) => counts[b] - counts[a]);
  check(order.length, 'Empty source cell');
  const selected = new Set(order.slice(0, keepCount).filter((l, i) => !i || counts[l] > counts[order[0]] * .05));
  const mask = new Uint8Array(w * h); let left = w, top = h, right = -1, bottom = -1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (selected.has(labels[y * w + x])) {
    if (ice) { left = Math.min(left, x); top = Math.min(top, y); right = Math.max(right, x); bottom = Math.max(bottom, y); }
    for (let dy = -2; dy <= 2; dy++) for (let dx = -2; dx <= 2; dx++) {
      const xx = x + dx, yy = y + dy; if (xx >= 0 && xx < w && yy >= 0 && yy < h) mask[yy * w + xx] = 1;
    }
  }
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const i = y * w + x;
    if (!mask[i]) rgba.fill(0, i * 4, i * 4 + 4);
    if (!ice && rgba[i * 4 + 3] >= 8) { left = Math.min(left, x); top = Math.min(top, y); right = Math.max(right, x); bottom = Math.max(bottom, y); }
  }
  return { data: rgba, width: w, height: h, bounds: { left, top, width: right - left + 1, height: bottom - top + 1 } };
}
function cubic(x) { x = Math.abs(x); return x <= 1 ? (1.5 * x - 2.5) * x * x + 1 : x < 2 ? ((-.5 * x + 2.5) * x - 4) * x + 2 : 0; }
function sample(s, x, y, dest, offset) {
  const ix = Math.floor(x), iy = Math.floor(y); let a = 0, r = 0, g = 0, b = 0;
  for (let dy = -1; dy <= 2; dy++) {
    const yy = iy + dy; if (yy < 0 || yy >= s.height) continue; const wy = cubic(y - yy);
    for (let dx = -1; dx <= 2; dx++) {
      const xx = ix + dx; if (xx < 0 || xx >= s.width) continue;
      const p = (yy * s.width + xx) * 4, weight = wy * cubic(x - xx) * s.data[p + 3] / 255;
      a += weight; r += s.data[p] * weight; g += s.data[p + 1] * weight; b += s.data[p + 2] * weight;
    }
  }
  const clamp = n => Math.max(0, Math.min(255, Math.round(n)));
  if (a > .001) { dest[offset] = clamp(r / a); dest[offset + 1] = clamp(g / a); dest[offset + 2] = clamp(b / a); dest[offset + 3] = clamp(a * 255); }
}
async function getSource(cell) {
  const id = +cell.id; let file, s, target, inputSize;
  if (id === 52011 || id === 52071) {
    file = id === 52011 ? 'outputs/portrait-refresh-v1/Swordsman_Generated.png' : 'outputs/character-art-audit-v1/Salamander-generated.png';
    const v = await load(file); s = { data: v.data, width: v.info.width, height: v.info.height };
    target = { left: 0, top: 0, width: 256, height: 256 }; s.bounds = { left: 0, top: 0, width: s.width, height: s.height };
    inputSize = [s.width, s.height];
  } else {
    const synergy = id >= 53000, index = synergy ? id - 53001 : id % 10;
    const group = synergy ? roster[14 + Math.floor(index / 4)] : roster[Math.floor((id - 52000) / 10) - 1];
    const localIndex = synergy ? index % 4 : index, ice = id === 52112 || id === 52113;
    file = ice ? 'outputs/tower-front-portraits-v2/edits/ice-creatures-approved/native-approved.png' : `outputs/tower-front-portraits-v2/source/${group.key}.png`;
    const v = await load(file), w = Math.floor(v.info.width / 2), h = ice ? Math.floor(v.info.height * .97) : Math.floor(v.info.height / 2);
    const rect = { left: (ice ? id - 52112 : localIndex % 2) * w, top: ice ? 0 : Math.floor(localIndex / 2) * h, width: w, height: ice ? h : h - Math.max(2, Math.floor(h / 50)) };
    const trim = localIndex === 1 && ['06_Hammer', '18_Synergy_D'].includes(group.key);
    s = isolate(v, rect, group.key === '18_Synergy_D' && localIndex === 0 ? 2 : 1, !ice && localIndex >= 2, trim, ice);
    const scale = Math.min(216 / s.bounds.width, 188 / s.bounds.height), width = Math.round(s.bounds.width * scale), height = Math.round(s.bounds.height * scale);
    target = { left: Math.floor((256 - width) / 2), top: 244 - height, width, height };
    inputSize = [v.info.width, v.info.height];
  }
  return { ...s, target, file, inputSize };
}
// Register the native sheet against the saved pre-audit cell. This also recovers
// the small packing differences in atlases exported before the final pack script.
async function register(source, cell) {
  if (['52011', '52071'].includes(cell.id)) return { dx: 0, dy: 0, fx: 1, fy: 1, error: 0 };
  const reference = await sharp(path.join(old, 'before', cell.atlas)).extract({ left: cell.x, top: inventory.textures.find(t => t.path === cell.atlas).height - cell.y - 256, width: 256, height: 256 }).ensureAlpha().raw().toBuffer();
  const points = [];
  for (let y = 32; y < 256; y += 3) for (let x = 8; x < 248; x += 3) {
    const i = (y * 256 + x) * 4, a = reference[i + 3] / 255;
    points.push([x + .5, y + .5, reference[i] * a, reference[i + 1] * a, reference[i + 2] * a, reference[i + 3]]);
  }
  function loss(p) {
    let total = 0;
    for (const [x, y, r, g, b, a] of points) {
      const qx = 128 + (x - 128) * p.fx + p.dx, qy = 160 + (y - 160) * p.fy + p.dy;
      const sx = source.bounds.left + (qx - source.target.left) * source.bounds.width / source.target.width - .5;
      const sy = source.bounds.top + (qy - source.target.top) * source.bounds.height / source.target.height - .5;
      const ix = Math.floor(sx), iy = Math.floor(sy), xx = sx - ix, yy = sy - iy;
      let rr = 0, gg = 0, bb = 0, aa = 0;
      if (qx >= source.target.left && qx < source.target.left + source.target.width && qy >= source.target.top && qy < source.target.top + source.target.height)
        for (let dy = 0; dy <= 1; dy++) for (let dx = 0; dx <= 1; dx++) {
          const ax = ix + dx, ay = iy + dy; if (ax < 0 || ay < 0 || ax >= source.width || ay >= source.height) continue;
          const i = (ay * source.width + ax) * 4, w = (dx ? xx : 1 - xx) * (dy ? yy : 1 - yy) * source.data[i + 3] / 255;
          rr += source.data[i] * w; gg += source.data[i + 1] * w; bb += source.data[i + 2] * w; aa += 255 * w;
        }
      total += (rr - r) ** 2 + (gg - g) ** 2 + (bb - b) ** 2 + (aa - a) ** 2;
    }
    return total / (points.length * 4);
  }
  let p = { dx: 0, dy: 0, fx: 1, fy: 1 }, best = loss(p);
  for (const step of [4, 2, 1, .5, .25, .125, .0625, .03125]) {
    let changed = true, rounds = 0;
    while (changed && rounds++ < 60) {
      changed = false;
      for (const key of ['dx', 'dy', 'fx', 'fy']) for (const sign of [-1, 1]) {
        const candidate = { ...p, [key]: p[key] + sign * step * (key[0] === 'f' ? .01 : 1) };
        if (Math.abs(candidate.dx) > 16 || Math.abs(candidate.dy) > 16 || candidate.fx < .8 || candidate.fx > 1.2 || candidate.fy < .8 || candidate.fy > 1.2) continue;
        const error = loss(candidate); if (error < best) { p = candidate; best = error; changed = true; }
      }
    }
  }
  check(Math.sqrt(best) < 18, 'Native source does not match previous art ' + cell.id + ': ' + Math.sqrt(best));
  return { ...p, error: Math.sqrt(best) };
}
function normalizeMeta(meta) {
  return meta.replace(/(^  spritePixelsToUnits: )([\d.]+)/m, (_, a, b) => a + Number(b) * 2)
    .replace(/(      rect:\r?\n        serializedVersion: \d+\r?\n        x: )(\d+)(\r?\n        y: )(\d+)(\r?\n        width: )(\d+)(\r?\n        height: )(\d+)/g,
      (_, a, x, b, y, c, w, d, h) => a + x * 2 + b + y * 2 + c + w * 2 + d + h * 2);
}
(async () => {
  const report = { cellSize: 512, pixelsPerUnit: 384, source: 'Native larger originals, sampled directly through previous layout transforms without the 256px intermediate', portraits: [], files: [] };
  const apply = process.argv.includes('--apply'), pending = [];
  fs.mkdirSync(path.join(__dirname, 'cells'), { recursive: true });
  for (const texture of inventory.textures.filter(t => t.spriteCount)) {
    const file = path.join(root, texture.path), current = fs.readFileSync(file), meta = fs.readFileSync(file + '.meta', 'utf8');
    const previous = layout.files.find(t => t.path === texture.path);
    check(sha(current) === previous.sha256, 'Production atlas has changed since layout audit: ' + texture.path);
    check(/^  spritePixelsToUnits: 192$/m.test(meta), 'Unexpected PPU ' + file);
    const width = texture.width * 2, height = texture.height * 2, atlas = Buffer.alloc(width * height * 4);
    for (const cell of inventory.portraits.filter(c => c.atlas === texture.path)) {
      const source = await getSource(cell), registration = await register(source, cell), override = config.artOverrides?.[cell.id];
      const [eyeX, eyeY, distance, chin] = override?.anchors || config.subjects[cell.id];
      const oldRow = layout.portraits.find(p => p.id === cell.id), sourceBottom = oldRow.before.bottom;
      const scale = 39.5 / distance, seam = 160 + (chin - eyeY) * scale, bodyScale = (244 - seam) / (sourceBottom - chin), output = Buffer.alloc(512 * 512 * 4);
      const crownSeam = override ? 160 + (override.crownJoinY - eyeY) * scale : 0;
      for (let y = 0; y < 512; y++) for (let x = 0; x < 512; x++) {
        const px = (x + .5) / 2, py = (y + .5) / 2;
        const localScale = override && py > seam ? scale + (override.bodyXScale - scale) * Math.min(1, (py - seam) / (244 - seam)) : scale;
        const ux = (px - 128) / localScale + eyeX;
        const uy = override && py < crownSeam ? override.crownTop + (py - override.targetTop) * (override.crownJoinY - override.crownTop) / (crownSeam - override.targetTop) : py <= seam ? (py - 160) / scale + eyeY : (py - seam) / bodyScale + chin;
        const qx = 128 + (ux - 128) * registration.fx + registration.dx;
        const qy = 160 + (uy - 160) * registration.fy + registration.dy;
        // Clip the same packed rectangle as the old transparent canvas.
        if (qx < source.target.left || qx >= source.target.left + source.target.width || qy < source.target.top || qy >= source.target.top + source.target.height) continue;
        const sx = source.bounds.left + (qx - source.target.left) / source.target.width * source.bounds.width - .5;
        const sy = source.bounds.top + (qy - source.target.top) / source.target.height * source.bounds.height - .5;
        sample(source, sx, sy, output, (y * 512 + x) * 4);
      }
      let left = 512, top = 512, right = -1, bottom = -1, pixels = 0, edge = 0;
      for (let y = 0; y < 512; y++) for (let x = 0; x < 512; x++) if (output[(y * 512 + x) * 4 + 3] > 128) {
        left = Math.min(left, x); top = Math.min(top, y); right = Math.max(right, x); bottom = Math.max(bottom, y); pixels++;
        if (x === 0 || y === 0 || x === 511 || y === 511) edge++;
      }
      check(pixels > 7000 && !edge, 'Empty/clipped portrait ' + cell.id);
      check(Math.abs(bottom + 1 - 488) <= 3, 'Baseline drift ' + cell.id + ': ' + bottom);
      for (let y = 0; y < 512; y++) output.copy(atlas, (((texture.height - cell.y - 256) * 2 + y) * width + cell.x * 2) * 4, y * 512 * 4, (y + 1) * 512 * 4);
      await sharp(output, { raw: { width: 512, height: 512, channels: 4 } }).png().toFile(path.join(__dirname, 'cells', cell.id + '.png'));
      report.portraits.push({ id: cell.id, name: cell.name, source: source.file, sourceDimensions: source.inputSize, registration, bounds: { left, top, right, bottom }, visiblePixels: pixels, edgePixels: edge });
    }
    const png = await sharp(atlas, { raw: { width, height, channels: 4 } }).png().toBuffer(), nextMeta = normalizeMeta(meta);
    check((nextMeta.match(/        width: 512/g) || []).length === texture.spriteCount, 'Incorrect slice count');
    const staged = path.join(__dirname, 'staged', texture.path); fs.mkdirSync(path.dirname(staged), { recursive: true });
    fs.writeFileSync(staged, png); fs.writeFileSync(staged + '.meta', nextMeta);
    pending.push({ file, current, meta, png, nextMeta });
    report.files.push({ path: texture.path, width, height, beforeSha256: sha(current), sha256: sha(png), metadataBeforeSha256: sha(meta), metadataSha256: sha(nextMeta), spriteCount: texture.spriteCount });
    console.log(path.basename(file) + ': ' + width + 'x' + height);
  }
  check(report.portraits.length === 70 && pending.length === 15, 'Incomplete coverage');
  if (apply) {
    // Validate every source before writing any production file.
    for (const p of pending) check(fs.readFileSync(p.file).equals(p.current) && fs.readFileSync(p.file + '.meta', 'utf8') === p.meta, 'Concurrent edit: ' + p.file);
    for (const p of pending) {
      const backup = path.join(__dirname, 'before', path.relative(root, p.file)); fs.mkdirSync(path.dirname(backup), { recursive: true });
      check(!fs.existsSync(backup), 'Existing backup would be overwritten');
      fs.writeFileSync(backup, p.current); fs.writeFileSync(backup + '.meta', p.meta);
      fs.writeFileSync(p.file, p.png); fs.writeFileSync(p.file + '.meta', p.nextMeta);
    }
  }
  report.applied = apply; fs.writeFileSync(path.join(__dirname, 'rebuild-report.json'), JSON.stringify(report, null, 2));
  console.log((apply ? 'Applied' : 'Staged') + ' all 70 portraits.');
})().catch(e => { console.error(e); process.exitCode = 1; });
