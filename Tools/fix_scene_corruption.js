const fs = require("fs");
const p = require("path").join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
let text = fs.readFileSync(p, "utf8");

const titleGoBlock = `--- !u!1 &880000110
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 880000111}
  - component: {fileID: 880000112}
  - component: {fileID: 880000113}
  m_Layer: 5
  m_Name: Title
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`;

if (!text.includes("--- !u!1 &880000110")) {
  const anchor = "--- !u!224 &880000111";
  const idx = text.indexOf(anchor);
  if (idx === -1) {
    console.error("Title RectTransform anchor not found.");
    process.exit(1);
  }
  text = text.slice(0, idx) + titleGoBlock + text.slice(idx);
  console.log("Inserted missing GameObject 880000110 (Title).");
} else {
  console.log("GameObject 880000110 already exists.");
}

// Remove duplicate 880000131 blocks if any
while (true) {
  const marker = "--- !u!224 &880000131";
  const first = text.indexOf(marker);
  const second = text.indexOf(marker, first + marker.length);
  if (second === -1) break;
  const end = text.indexOf("\n--- !u!", first + marker.length);
  text = text.slice(0, first) + text.slice(end);
  console.log("Removed duplicate 880000131 block.");
}

// Add row padding if missing on Rows rect
if (!text.includes("m_OffsetMin: {x: 10, y: 10}")) {
  text = text.replace(
    /(--- !u!224 &880000131\r?\nRectTransform:[\s\S]*?m_Pivot: \{x: 0\.5, y: 0\.5\})(\r?\n--- !u!)/,
    "$1\r\n  m_OffsetMin: {x: 10, y: 10}\r\n  m_OffsetMax: {x: -10, y: -62}$2"
  );
  console.log("Added Rows rect offsets.");
}

fs.writeFileSync(p, text, "utf8");

// Validate
const blockRe = /--- !u!(\d+) &(\d+)\r?\n/g;
const counts = new Map();
let m;
while ((m = blockRe.exec(text))) counts.set(m[2], (counts.get(m[2]) || 0) + 1);
const dups = [...counts.entries()].filter(([, n]) => n > 1);
const blocks = new Set(counts.keys());
const refs = [...text.matchAll(/\{fileID: (\d+)\}/g)].map((x) => Number(x[1]));
const missing = [...new Set(refs)].filter((id) => id > 0 && !blocks.has(String(id)) && id >= 880000000 && id < 881000000);

console.log("Duplicates:", dups.length ? dups.map(([id, n]) => `${id}x${n}`).join(", ") : "none");
console.log("Missing 880 refs:", missing.length ? missing.join(", ") : "none");
