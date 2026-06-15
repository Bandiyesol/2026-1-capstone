const fs = require('fs');
const path = require('path');

const scenePath = path.join(__dirname, '..', 'Assets', 'Scenes', 'ProtoType_LTG.unity');
const text = fs.readFileSync(scenePath, 'utf8');

const blockRe = /^--- !u!\d+ &(\d+)/gm;
const blocks = new Set();
let m;
while ((m = blockRe.exec(text))) blocks.add(m[1]);

const refs = [...text.matchAll(/\{fileID: (\d+)/g)].map((x) => x[1]);
const missing = [...new Set(refs)].filter((id) => id !== '0' && !blocks.has(id));

console.log(`File size: ${text.length} bytes`);
console.log(`Blocks: ${blocks.size}`);
console.log(`Missing refs: ${missing.length}`);
if (missing.length) {
  console.log(missing.slice(0, 40).join(', ') + (missing.length > 40 ? '...' : ''));
  process.exit(1);
}
console.log('Scene references OK.');
