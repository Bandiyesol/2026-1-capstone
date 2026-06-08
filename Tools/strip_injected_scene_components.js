/**
 * Remove all manually YAML-injected components that Unity cannot load.
 * After this, open Unity and run: Tools → Setup ProtoType Scene UI
 * Play mode still works via RewardSystemBootstrap runtime AddComponent.
 * node Tools/strip_injected_scene_components.js
 */
const fs = require("fs");
const path = require("path");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");

const COMPONENT_IDS = [
  315315529, 315315530, 315315531,
  1680473528, 1680473529, 1680473530,
  1789209909, 1789209910,
];

const MIN_LEADERBOARD_ID = 880_000_100;
const MAX_LEADERBOARD_ID = 880_000_999;

function stripBlock(text, id, typePrefix = "!u!114") {
  const marker = `--- !u!${typePrefix.replace("!u!", "")} &${id}`;
  // support any type
  for (const t of ["114", "1", "224", "222", "4"]) {
    const m = `--- !u!${t} &${id}`;
    const start = text.indexOf(m);
    if (start === -1) continue;
    const end = text.indexOf("\r\n--- !u!", start + m.length);
    if (end === -1) return { text, removed: false };
    return { text: text.slice(0, start) + text.slice(end), removed: true };
  }
  return { text, removed: false };
}

function stripAnyBlock(text, id) {
  const re = new RegExp(`--- !u!\\d+ &${id}(?:\\r\\n|\\n)`);
  const m = text.match(re);
  if (!m) return { text, removed: false };
  const start = text.indexOf(m[0]);
  const tail = text.slice(start + m[0].length);
  const rel = tail.search(/\r?\n--- !u!/);
  if (rel === -1) return { text, removed: false };
  return { text: text.slice(0, start) + tail.slice(rel), removed: true };
}

function strip880Leaderboard(text) {
  const re = /--- !u!\d+ &(\d+)(?:\r\n|\n)/g;
  const blocks = [];
  let m;
  while ((m = re.exec(text))) {
    const id = Number(m[1]);
    if (id >= MIN_LEADERBOARD_ID && id <= MAX_LEADERBOARD_ID)
      blocks.push({ id: m[1], start: m.index });
  }
  let removed = 0;
  for (let i = blocks.length - 1; i >= 0; i--) {
    const r = stripAnyBlock(text, blocks[i].id);
    if (r.removed) {
      text = r.text;
      removed++;
    }
  }
  return { text, removed };
}

let text = fs.readFileSync(SCENE, "utf8");

let compRemoved = 0;
for (const id of COMPONENT_IDS) {
  let r = stripAnyBlock(text, id);
  while (r.removed) {
    text = r.text;
    compRemoved++;
    r = stripAnyBlock(text, id);
  }
}

({ text, removed: lbRemoved } = strip880Leaderboard(text));

for (const id of COMPONENT_IDS) {
  text = text.replace(new RegExp(`^  - component: \\{fileID: ${id}\\}(?:\\r\\n|\\n)`, "gm"), "");
}

text = text.replace(/^  - \{fileID: 880000101\}(?:\r\n|\n)/gm, "");
text = text.replace(/^  accessoryInventory: \{fileID: 1680473528\}(?:\r\n|\n)/gm, "  accessoryInventory: {fileID: 0}\n");
text = text.replace(/^  potionInventory: \{fileID: 1680473529\}(?:\r\n|\n)/gm, "  potionInventory: {fileID: 0}\n");
text = text.replace(/\{fileID: 880000\d+\}/g, "{fileID: 0}");

fs.writeFileSync(SCENE, text, "utf8");

const blockRe = /--- !u!\d+ &(\d+)\r?\n/g;
const counts = new Map();
while ((m = blockRe.exec(text))) counts.set(m[1], (counts.get(m[1]) || 0) + 1);
const dups = [...counts.entries()].filter(([, n]) => n > 1);
const blocks = new Set(counts.keys());
const refs = [...text.matchAll(/\{fileID: (\d+)\}/g)].map((x) => Number(x[1]));
const missing = [...new Set(refs)].filter(
  (id) => id > 0 && !blocks.has(String(id)) && (COMPONENT_IDS.includes(id) || (id >= MIN_LEADERBOARD_ID && id <= MAX_LEADERBOARD_ID))
);

console.log(`Removed ${compRemoved} injected MonoBehaviour blocks.`);
console.log(`Removed ${lbRemoved} MainMenuLeaderboard (880xxx) blocks.`);
console.log("Stripped component refs from Manager / Player / GameManager.");
console.log("Duplicates:", dups.length ? dups : "none");
console.log("Missing injected refs:", missing.length ? missing.join(", ") : "none");
console.log("\nNext: Close Unity → reopen scene → Tools → Setup ProtoType Scene UI");
