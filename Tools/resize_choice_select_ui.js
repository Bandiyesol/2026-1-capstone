/**
 * Persist ChoiceSelectUILayout sizes into ProtoType_LTG.unity YAML.
 * node Tools/resize_choice_select_ui.js
 */
const fs = require("fs");
const path = require("path");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
let text = fs.readFileSync(SCENE, "utf8");

const panelIds = ["1174408552", "2074408552"];
const PANEL = { w: 1520, h: 960 };
const CARD = { w: 405, h: 500 };
const btnPatches = [
  ["879163209", -418],
  ["1972672416", 0],
  ["853437879", 418],
  ["1779163209", -418],
  ["2872672416", 0],
  ["1753437879", 418],
];

function patchRect(id, patchFn) {
  const marker = `--- !u!224 &${id}`;
  const start = text.indexOf(marker);
  if (start === -1) return false;
  const tail = text.slice(start + marker.length);
  const rel = tail.search(/\r?\n--- !u!/);
  if (rel === -1) return false;
  const block = tail.slice(0, rel);
  const updated = patchFn(block);
  if (updated === block) return false;
  text = text.slice(0, start + marker.length) + updated + tail.slice(rel);
  return true;
}

let n = 0;
for (const id of panelIds) {
  if (
    patchRect(id, (b) =>
      b.replace(/m_SizeDelta: \{x: [^,]+, y: [^}]+\}/, `m_SizeDelta: {x: ${PANEL.w}, y: ${PANEL.h}}`)
    )
  )
    n++;
}

for (const [id, x] of btnPatches) {
  if (
    patchRect(id, (b) =>
      b
        .replace(/m_AnchoredPosition: \{x: [^,]+, y: [^}]+\}/, `m_AnchoredPosition: {x: ${x}, y: 0}`)
        .replace(/m_SizeDelta: \{x: [^,]+, y: [^}]+\}/, `m_SizeDelta: {x: ${CARD.w}, y: ${CARD.h}}`)
    )
  )
    n++;
}

const detailPatterns = [
  /  m_AnchorMin: \{x: 0\.5, y: 0\.5\}\r?\n  m_AnchorMax: \{x: 0\.5, y: 0\.5\}\r?\n  m_AnchoredPosition: \{x: 0, y: -135\}\r?\n  m_SizeDelta: \{x: 350, y: 585\}/g,
  /  m_AnchorMin: \{x: 0\.08, y: 0\.06\}\r?\n  m_AnchorMax: \{x: 0\.92, y: 0\.38\}\r?\n  m_AnchoredPosition: \{x: 0, y: 0\}\r?\n  m_SizeDelta: \{x: 0, y: 0\}/g,
  /  m_AnchorMin: \{x: 0\.12, y: 0\.10\}\r?\n  m_AnchorMax: \{x: 0\.88, y: 0\.34\}\r?\n  m_AnchoredPosition: \{x: 0, y: 0\}\r?\n  m_SizeDelta: \{x: 0, y: 0\}/g,
];
const detailNew =
  "  m_AnchorMin: {x: 0.12, y: 0.10}\r\n  m_AnchorMax: {x: 0.88, y: 0.34}\r\n  m_AnchoredPosition: {x: 0, y: 0}\r\n  m_SizeDelta: {x: 0, y: 0}";
let detailCount = 0;
for (const p of detailPatterns) {
  const c = (text.match(p) || []).length;
  detailCount += c;
  text = text.replace(p, detailNew);
}

fs.writeFileSync(SCENE, text, "utf8");
console.log(`Patched panels/buttons (${n} rects), Detail blocks: ${detailCount}`);
