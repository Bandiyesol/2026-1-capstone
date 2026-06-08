/**
 * Strip all 880000xxx scene objects (corrupted merge artifacts) and rebuild cleanly.
 * node Tools/repair_scene_880.js
 */
const fs = require("fs");
const path = require("path");
const { execSync } = require("child_process");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
const MIN_ID = 880_000_000;
const MAX_ID = 880_999_999;

function inRange(id) {
  const n = Number(id);
  return n >= MIN_ID && n <= MAX_ID;
}

function strip880Blocks(text) {
  const re = /--- !u!\d+ &(\d+)\r?\n/g;
  const blocks = [];
  let m;
  while ((m = re.exec(text))) {
    blocks.push({ id: m[1], start: m.index, headerEnd: m.index + m[0].length });
  }

  let removed = 0;
  for (let i = blocks.length - 1; i >= 0; i--) {
    if (!inRange(blocks[i].id)) continue;
    const start = blocks[i].start;
    const end = i + 1 < blocks.length ? blocks[i + 1].start : text.length;
    text = text.slice(0, start) + text.slice(end);
    removed++;
  }
  return { text, removed };
}

function cleanRefs(text) {
  text = text.replace(/^  - component: \{fileID: 880\d+\}\r?\n/gm, "");
  text = text.replace(/^  - \{fileID: 880\d+\}\r?\n/gm, "");
  text = text.replace(/^  accessoryInventory: \{fileID: 880\d+\}\r?\n/gm, "  accessoryInventory: {fileID: 0}\r\n");
  text = text.replace(/^  potionInventory: \{fileID: 880\d+\}\r?\n/gm, "  potionInventory: {fileID: 0}\r\n");
  return text;
}

function validate(text) {
  const blockRe = /--- !u!\d+ &(\d+)\r?\n/g;
  const counts = new Map();
  let m;
  while ((m = blockRe.exec(text))) counts.set(m[1], (counts.get(m[1]) || 0) + 1);
  const dups = [...counts.entries()].filter(([, n]) => n > 1);
  const blocks = new Set(counts.keys());
  const refs = [...text.matchAll(/\{fileID: (\d+)\}/g)].map((x) => Number(x[1]));
  const missing = [...new Set(refs)].filter(
    (id) => id > 0 && !blocks.has(String(id)) && id >= MIN_ID && id <= MAX_ID
  );
  return { dups, missing };
}

let text = fs.readFileSync(SCENE, "utf8");
const before = (text.match(/--- !u!\d+ &880/g) || []).length;

({ text, removed } = strip880Blocks(text));
text = cleanRefs(text);

const v1 = validate(text);
if (v1.dups.length) {
  console.error("Duplicates after strip:", v1.dups);
  process.exit(1);
}

fs.writeFileSync(SCENE, text, "utf8");
console.log(`Stripped ${removed} blocks (880xxx markers before: ~${before}).`);

execSync("node " + path.join(__dirname, "setup_scene_runtime_ui.js"), { stdio: "inherit" });

text = fs.readFileSync(SCENE, "utf8");
const v2 = validate(text);
console.log("After rebuild — duplicates:", v2.dups.length ? v2.dups : "none");
console.log("After rebuild — missing 880 refs:", v2.missing.length ? v2.missing.join(", ") : "none");
