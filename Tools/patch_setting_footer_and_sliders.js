const fs = require('fs');
const path = require('path');
const { writeSceneSafe } = require('./scene_patch_safety');

const scenePath = path.join(__dirname, '..', 'Assets', 'Scenes', 'ProtoType_LTG.unity');
let text = fs.readFileSync(scenePath, 'utf8');
const originalBytes = text.length;

const replacements = [
  // SettingPanel footer button spacing (280px wide buttons, 140px gap between edges)
  [
    '--- !u!224 &1723534091\nRectTransform:',
    null,
  ],
];

function patchRectPosition(blockId, x, y) {
  const start = text.indexOf(`--- !u!224 &${blockId}`);
  if (start === -1) {
    console.warn(`RectTransform ${blockId} not found`);
    return false;
  }

  const posIdx = text.indexOf('  m_AnchoredPosition:', start);
  const end = text.indexOf('\n', posIdx);
  if (posIdx === -1 || end === -1) return false;

  const line = `  m_AnchoredPosition: {x: ${x}, y: ${y}}`;
  text = text.slice(0, posIdx) + line + text.slice(end);
  return true;
}

let patched = 0;
if (patchRectPosition('1723534091', -420, 200)) patched++;
if (patchRectPosition('1525049241', 0, 200)) patched++;
if (patchRectPosition('1949472873', 420, 200)) patched++;

const handleAreaFrom =
  '  m_AnchorMin: {x: 0.07, y: 0}\n  m_AnchorMax: {x: 0.99, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}';
const handleAreaTo =
  '  m_AnchorMin: {x: 0.07, y: 0}\n  m_AnchorMax: {x: 0.93, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}';

let handleAreas = 0;
let idx = 0;
while ((idx = text.indexOf(handleAreaFrom, idx)) !== -1) {
  // Only patch slider handle areas (children of Bgm/Sfx sliders)
  const blockStart = text.lastIndexOf('--- !u!224', idx);
  const fatherLine = text.slice(blockStart, idx + 200);
  if (!fatherLine.includes('1685352552') && !fatherLine.includes('1966794689')) {
    idx += handleAreaFrom.length;
    continue;
  }

  text = text.slice(0, idx) + handleAreaTo + text.slice(idx + handleAreaFrom.length);
  handleAreas++;
  idx += handleAreaTo.length;
}

// Add handle slide area pixel insets for Bgm/Sfx only
for (const fatherId of ['1685352552', '1966794689']) {
  const marker = `  m_Father: {fileID: ${fatherId}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.07, y: 0}\n  m_AnchorMax: {x: 0.93, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}`;
  const inset =
    `  m_Father: {fileID: ${fatherId}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.07, y: 0}\n  m_AnchorMax: {x: 0.93, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: -12, y: 0}`;
  if (text.includes(marker)) {
    text = text.replace(marker, inset);
    handleAreas++;
  }
}

const handleSizeFrom = '  m_SizeDelta: {x: 20, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}';
const handleSizeTo = '  m_SizeDelta: {x: 14, y: 14}\n  m_Pivot: {x: 0.5, y: 0.5}';
let handles = 0;
let hIdx = 0;
while ((hIdx = text.indexOf(handleSizeFrom, hIdx)) !== -1) {
  const blockStart = text.lastIndexOf('--- !u!224', hIdx);
  const block = text.slice(blockStart, hIdx + 120);
  if (!block.includes('1611728197') && !block.includes('2024506475')) {
    hIdx += handleSizeFrom.length;
    continue;
  }
  text = text.slice(0, hIdx) + handleSizeTo + text.slice(hIdx + handleSizeFrom.length);
  handles++;
  hIdx += handleSizeTo.length;
}

writeSceneSafe(scenePath, text, originalBytes);
console.log(`Footer buttons: ${patched}, handle areas: ${handleAreas}, handles: ${handles}`);
