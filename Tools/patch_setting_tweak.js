const fs = require('fs');
const path = require('path');
const { writeSceneSafe } = require('./scene_patch_safety');

const scenePath = path.join(__dirname, '..', 'Assets', 'Scenes', 'ProtoType_LTG.unity');
let text = fs.readFileSync(scenePath, 'utf8');
const originalBytes = text.length;

function patchRectPosition(blockId, x, y) {
  const start = text.indexOf(`--- !u!224 &${blockId}`);
  if (start === -1) return false;
  const posIdx = text.indexOf('  m_AnchoredPosition:', start);
  const end = text.indexOf('\n', posIdx);
  if (posIdx === -1 || end === -1) return false;
  text = text.slice(0, posIdx) + `  m_AnchoredPosition: {x: ${x}, y: ${y}}` + text.slice(end);
  return true;
}

let buttons = 0;
if (patchRectPosition('1723534091', -380, 200)) buttons++;
if (patchRectPosition('1525049241', 0, 200)) buttons++;
if (patchRectPosition('1949472873', 380, 200)) buttons++;

const fillFrom = '  m_AnchorMin: {x: 0.07, y: 0.36}\n  m_AnchorMax: {x: 0.93, y: 0.64}';
const fillTo = '  m_AnchorMin: {x: 0.065, y: 0.28}\n  m_AnchorMax: {x: 0.93, y: 0.72}';
let fills = 0;
let idx = 0;
while ((idx = text.indexOf(fillFrom, idx)) !== -1) {
  text = text.slice(0, idx) + fillTo + text.slice(idx + fillFrom.length);
  fills++;
  idx += fillTo.length;
}

writeSceneSafe(scenePath, text, originalBytes);
console.log(`Footer buttons: ${buttons}, slider fills: ${fills}`);
