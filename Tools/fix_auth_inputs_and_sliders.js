const fs = require('fs');
const path = require('path');
const { writeSceneSafe } = require('./scene_patch_safety');

const scenePath = path.join(__dirname, '..', 'Assets', 'Scenes', 'ProtoType_LTG.unity');
let text = fs.readFileSync(scenePath, 'utf8');
const originalBytes = text.length;
const authColor = '  m_Color: {r: 1, g: 0.98, b: 0.92, a: 1}';
const panelsBlock =
  '  m_Sprite: {fileID: -8920719777562397111, guid: 51f1c51df498e2041b8d49e109f7019b, type: 3}\n  m_Type: 0\n  m_PreserveAspect: 0';
const inputBlock =
  '  m_Sprite: {fileID: -2847192836012345678, guid: c6e279af717ccb547b0e6736ba2a4728, type: 3}\n  m_Type: 1\n  m_PreserveAspect: 0';

let inputCount = 0;
let searchFrom = 0;
while (true) {
  const colorIdx = text.indexOf(authColor, searchFrom);
  if (colorIdx === -1) break;

  const spriteIdx = text.indexOf(panelsBlock, colorIdx);
  if (spriteIdx === -1 || spriteIdx - colorIdx > 400) {
    searchFrom = colorIdx + authColor.length;
    continue;
  }

  text = text.slice(0, spriteIdx) + inputBlock + text.slice(spriteIdx + panelsBlock.length);
  inputCount++;
  searchFrom = spriteIdx + inputBlock.length;
}

const sliderPatches = [
  ['m_SizeDelta: {x: 400, y: 50}', 'm_SizeDelta: {x: 400, y: 64}'],
  ['m_AnchorMin: {x: 0, y: 0.3}\n  m_AnchorMax: {x: 1, y: 0.7}', 'm_AnchorMin: {x: 0, y: 0.18}\n  m_AnchorMax: {x: 1, y: 0.82}'],
  ['m_AnchorMin: {x: 0.16, y: 0.445}\n  m_AnchorMax: {x: 0.97, y: 0.555}', 'm_AnchorMin: {x: 0.07, y: 0.36}\n  m_AnchorMax: {x: 0.99, y: 0.64}'],
  ['m_AnchorMin: {x: 0.16, y: 0}\n  m_AnchorMax: {x: 0.97, y: 1}', 'm_AnchorMin: {x: 0.07, y: 0}\n  m_AnchorMax: {x: 0.99, y: 1}'],
];

let sliderCount = 0;
for (const [from, to] of sliderPatches) {
  let idx = 0;
  while ((idx = text.indexOf(from, idx)) !== -1) {
    text = text.slice(0, idx) + to + text.slice(idx + from.length);
    sliderCount++;
    idx += to.length;
  }
}

// Fix Fill rect pivot so green bar starts flush left inside fill area
const fillPivotFrom = '  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 10, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}';
const fillPivotTo = '  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0, y: 0.5}';
let fillCount = 0;
let fillIdx = 0;
while ((fillIdx = text.indexOf(fillPivotFrom, fillIdx)) !== -1) {
  text = text.slice(0, fillIdx) + fillPivotTo + text.slice(fillIdx + fillPivotFrom.length);
  fillCount++;
  fillIdx += fillPivotTo.length;
}

writeSceneSafe(scenePath, text, originalBytes);
console.log(`Auth inputs: ${inputCount}, slider patches: ${sliderCount}, fill pivots: ${fillCount}`);