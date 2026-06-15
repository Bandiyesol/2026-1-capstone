const fs = require('fs');
const path = require('path');
const { writeSceneSafe } = require('./scene_patch_safety');

const scenePath = path.join(__dirname, '..', 'Assets', 'Scenes', 'ProtoType_LTG.unity');
let text = fs.readFileSync(scenePath, 'utf8');
const originalBytes = text.length;
const centerBlock =
  '  m_Sprite: {fileID: -2847192836012345678, guid: c6e279af717ccb547b0e6736ba2a4728, type: 3}\n  m_Type: 1\n  m_PreserveAspect: 0';
const buttonBlock =
  '  m_Sprite: {fileID: -93586019915418993, guid: c6e279af717ccb547b0e6736ba2a4728, type: 3}\n  m_Type: 0\n  m_PreserveAspect: 1';
const inputBlock =
  '  m_Sprite: {fileID: -8920719777562397111, guid: 51f1c51df498e2041b8d49e109f7019b, type: 3}\n  m_Type: 0\n  m_PreserveAspect: 0';

const authColor = '  m_Color: {r: 1, g: 0.98, b: 0.92, a: 1}';
let buttonCount = 0;
let inputCount = 0;

let searchFrom = 0;
while (true) {
  const spriteIdx = text.indexOf(centerBlock, searchFrom);
  if (spriteIdx === -1) break;

  const blockStart = text.lastIndexOf('--- !u!114', spriteIdx);
  const blockEnd = text.indexOf('--- !u!', spriteIdx + 1);
  const block = text.slice(blockStart, blockEnd === -1 ? undefined : blockEnd);

  if (block.includes(authColor)) {
    text = text.slice(0, spriteIdx) + inputBlock + text.slice(spriteIdx + centerBlock.length);
    inputCount++;
  } else {
    text = text.slice(0, spriteIdx) + buttonBlock + text.slice(spriteIdx + centerBlock.length);
    buttonCount++;
  }

  searchFrom = spriteIdx + buttonBlock.length;
}

// Setting sliders: BgmSlider + SfxSlider fill areas and backgrounds
const sliderPatches = [
  ['m_AnchorMin: {x: 0, y: 0.08}\n  m_AnchorMax: {x: 1, y: 0.92}', 'm_AnchorMin: {x: 0, y: 0.3}\n  m_AnchorMax: {x: 1, y: 0.7}'],
  ['m_AnchorMin: {x: 0.05, y: 0.36}\n  m_AnchorMax: {x: 1, y: 0.64}', 'm_AnchorMin: {x: 0.16, y: 0.445}\n  m_AnchorMax: {x: 0.97, y: 0.555}'],
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

// BgmSlider handle slide area (sizeDelta -20 was default stretch)
const handleFrom =
  '  m_Father: {fileID: 1685352552}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: -20, y: 0}';
const handleTo =
  '  m_Father: {fileID: 1685352552}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.16, y: 0}\n  m_AnchorMax: {x: 0.97, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}';
if (text.includes(handleFrom)) {
  text = text.replace(handleFrom, handleTo);
  sliderCount++;
}

// SfxSlider handle slide area
const sfxHandleFrom =
  '  m_Father: {fileID: 1966794689}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: -20, y: 0}';
const sfxHandleTo =
  '  m_Father: {fileID: 1966794689}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.16, y: 0}\n  m_AnchorMax: {x: 0.97, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}';
if (text.includes(sfxHandleFrom)) {
  text = text.replace(sfxHandleFrom, sfxHandleTo);
  sliderCount++;
}

writeSceneSafe(scenePath, text, originalBytes);
console.log(`Reverted ${buttonCount} menu buttons, ${inputCount} auth inputs, ${sliderCount} slider anchor patches.`);