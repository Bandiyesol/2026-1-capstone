/**
 * Re-home manager/player MonoBehaviours from 880000001-008 to Unity-native fileIDs
 * placed next to their GameObjects (880xxx IDs fail to load in Unity).
 * node Tools/fix_scene_manager_components.js
 */
const fs = require("fs");
const path = require("path");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");

const GUIDS = {
  AccessoryManager: "3971858c64f4c1547a4e2f6593fd1aef",
  AccessoryEffect: "cad624ccc15ecaa44aa66ebf9d391751",
  RewardRollService: "edcb5af5fc41d934d84ecf9f37a0aaca",
  AccessoryInventory: "9efadc5dcc5b85a44bcf4718834d5ab4",
  PotionInventory: "d7d7480d17035654bace4c33693f0512",
  PotionEffect: "b52a8228cc9147347bf6a1fd8a6dd332",
  OverlayPanelEscapeInput: "2dde6902947e0474e9a8206f0fb5739f",
  EndingSequenceController: "a2a6827eee24b0249a6642bee0e859a5",
};

const MOVES = [
  { oldId: 880000001, newId: 315315529, go: 315315527, guid: GUIDS.AccessoryManager, className: "AccessoryManager", extra: "" },
  { oldId: 880000002, newId: 315315530, go: 315315527, guid: GUIDS.AccessoryEffect, className: "AccessoryEffect", extra: "" },
  {
    oldId: 880000003,
    newId: 315315531,
    go: 315315527,
    guid: GUIDS.RewardRollService,
    className: "RewardRollService",
    extra: "  allAccessories: []\r\n  allRelics: []\r\n",
  },
  {
    oldId: 880000004,
    newId: 1680473528,
    go: 1680473516,
    guid: GUIDS.AccessoryInventory,
    className: "AccessoryInventory",
    extra: "  maxAccessories: 12\r\n",
  },
  {
    oldId: 880000005,
    newId: 1680473529,
    go: 1680473516,
    guid: GUIDS.PotionInventory,
    className: "PotionInventory",
    extra: "  maxStacks: 12\r\n",
  },
  { oldId: 880000006, newId: 1680473530, go: 1680473516, guid: GUIDS.PotionEffect, className: "PotionEffect", extra: "" },
  { oldId: 880000007, newId: 1789209909, go: 1789209906, guid: GUIDS.OverlayPanelEscapeInput, className: "OverlayPanelEscapeInput", extra: "" },
  { oldId: 880000008, newId: 1789209910, go: 1789209906, guid: GUIDS.EndingSequenceController, className: "EndingSequenceController", extra: "" },
];

const GROUPS = [
  {
    anchor: 315315528,
    anchorIsTransform: true,
    moves: MOVES.filter((m) => m.go === 315315527),
  },
  {
    anchor: 1680473527,
    anchorIsTransform: false,
    moves: MOVES.filter((m) => m.go === 1680473516),
  },
  {
    anchor: 1789209908,
    anchorIsTransform: true,
    moves: MOVES.filter((m) => m.go === 1789209906),
  },
];

function monoBlock(id, goId, guid, className, extra = "") {
  return `--- !u!114 &${id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::${className}
${extra}`;
}

function stripBlock(text, id) {
  const marker = `--- !u!114 &${id}`;
  const start = text.indexOf(marker);
  if (start === -1) return { text, removed: false };
  const end = text.indexOf("\r\n--- !u!", start + marker.length);
  if (end === -1) return { text, removed: false };
  return { text: text.slice(0, start) + text.slice(end), removed: true };
}

function replaceId(text, oldId, newId) {
  return text.replace(new RegExp(`\\{fileID: ${oldId}\\}`, "g"), `{fileID: ${newId}}`);
}

let text = fs.readFileSync(SCENE, "utf8");

for (const move of MOVES) {
  ({ text } = stripBlock(text, move.oldId));
  ({ text } = stripBlock(text, move.newId));
}

for (const move of MOVES) {
  text = replaceId(text, move.oldId, move.newId);
}

for (const group of GROUPS) {
  const blocks = group.moves.map((move) =>
    monoBlock(move.newId, move.go, move.guid, move.className, move.extra)
  );
  const anchorId = group.anchor;
  const anchor = group.anchorIsTransform
    ? `--- !u!4 &${anchorId}\r\nTransform:`
    : `--- !u!114 &${anchorId}\r\nMonoBehaviour:`;
  const idx = text.indexOf(anchor);
  if (idx === -1) throw new Error(`Anchor ${anchorId} not found`);
  const end = text.indexOf("\r\n--- !u!", idx + 10);
  if (end === -1) throw new Error(`End after anchor ${anchorId} not found`);
  text = text.slice(0, end) + "\r\n" + blocks.join("\r\n") + text.slice(end);
}

fs.writeFileSync(SCENE, text, "utf8");

const blockRe = /--- !u!\d+ &(\d+)\r?\n/g;
const counts = new Map();
let m;
while ((m = blockRe.exec(text))) counts.set(m[1], (counts.get(m[1]) || 0) + 1);
const dups = [...counts.entries()].filter(([, n]) => n > 1);
console.log("Moved 8 manager/player components to native fileIDs.");
console.log("Duplicates:", dups.length ? dups.map(([id, n]) => `${id}x${n}`).join(", ") : "none");
for (const move of MOVES) {
  const ok = text.includes(`--- !u!114 &${move.newId}`) && text.includes(`Assembly-CSharp::${move.className}`);
  console.log(`  ${move.className}: ${ok ? "OK" : "MISSING"} (${move.newId})`);
}
