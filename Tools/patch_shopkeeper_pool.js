const fs = require('fs');
const path = require('path');
const { writeSceneSafe } = require('./scene_patch_safety');

const scenePath = path.join(__dirname, '..', 'Assets', 'Scenes', 'ProtoType_LTG.unity');
const shopkeeperGuid = '10a7c40f93706774597a53e02352e838';
const shopkeeperEntry =
  `  - {fileID: 5151787703965304345, guid: ${shopkeeperGuid}, type: 3}`;

let text = fs.readFileSync(scenePath, 'utf8');
const originalBytes = text.length;

if (text.includes(shopkeeperGuid)) {
  console.log('ShopkeeperNpc already registered in scene.');
  process.exit(0);
}

const portalLine =
  '  - {fileID: 4486845286367034275, guid: 0d28e9b4f2d3110499cb2787284b949b, type: 3}';
const anchor = `${portalLine}\n  coinPrefabs:`;

if (!text.includes(anchor)) {
  throw new Error('gimmickPrefabs anchor not found — scene layout may have changed.');
}

text = text.replace(anchor, `${portalLine}\n${shopkeeperEntry}\n  coinPrefabs:`);
writeSceneSafe(scenePath, text, originalBytes);
console.log('ShopkeeperNpc added to PoolManager.gimmickPrefabs.');
