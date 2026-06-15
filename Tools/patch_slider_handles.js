const fs = require('fs');
const path = require('path');
const { writeSceneSafe } = require('./scene_patch_safety');

const scenePath = path.join(__dirname, '..', 'Assets', 'Scenes', 'ProtoType_LTG.unity');
let text = fs.readFileSync(scenePath, 'utf8');
const originalBytes = text.length;

const handleFrom = '  m_SizeDelta: {x: 14, y: 14}\n  m_Pivot: {x: 0.5, y: 0.5}';
const handleTo = '  m_SizeDelta: {x: 20, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}';

let handles = 0;
let idx = 0;
while ((idx = text.indexOf(handleFrom, idx)) !== -1) {
  const blockStart = text.lastIndexOf('--- !u!224', idx);
  const block = text.slice(blockStart, idx + 120);
  if (!block.includes('1611728197') && !block.includes('2024506475')) {
    idx += handleFrom.length;
    continue;
  }
  text = text.slice(0, idx) + handleTo + text.slice(idx + handleFrom.length);
  handles++;
  idx += handleTo.length;
}

writeSceneSafe(scenePath, text, originalBytes);
console.log(`Slider handles restored: ${handles}`);
