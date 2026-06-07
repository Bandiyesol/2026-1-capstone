/** Duplicate WeaponSelectUI hierarchy as RewardSelectUI (children only, no external refs). */
const fs = require("fs");
const path = require("path");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
const ROOT_GO = 1641493326;
const ROOT_WEAPON_UI = 1641493328;
const CONTENT_PANEL_GO = 623864430;
const WEAPON_SELECT_SCRIPT = "03e83d1d368f31242acf5af7aaca38a8";
const REWARD_SELECT_SCRIPT = "ae269f8f74d068f43ba8a13c038916c2";
const ID_OFFSET = 900_000_000;

const BTN_IDS = [879163206, 1972672413, 853437880];
const ICON_IDS = [246519792, 713906292, 853125401];
const TITLE_IDS = [1910446549, 1965494919, 2116330943];
const DETAIL_IDS = [2144105048, 2014686754, 1024911500];

let text = fs.readFileSync(SCENE, "utf8");
if (text.includes("m_Name: RewardSelectUI")) {
  console.log("RewardSelectUI already exists — skipping.");
  process.exit(0);
}

const blockRe = /--- !u!(\d+) &(\d+)\r?\n([\s\S]*?)(?=\r?\n--- !u!|$)/g;
const blocks = new Map();
const order = [];
let m;
while ((m = blockRe.exec(text)) !== null) {
  blocks.set(Number(m[2]), { type: m[1], body: m[3] });
  order.push(Number(m[2]));
}

function parseComponents(goBody) {
  const ids = [];
  const re = /- component: \{fileID: (\d+)\}/g;
  let r;
  while ((r = re.exec(goBody)) !== null) ids.push(Number(r[1]));
  return ids;
}

function parseChildren(rectBody) {
  const ids = [];
  const re = /- \{fileID: (\d+)\}/g;
  let r;
  const start = rectBody.indexOf("m_Children:");
  if (start === -1) return ids;
  const section = rectBody.slice(start);
  const end = section.indexOf("m_Father:");
  const slice = end === -1 ? section : section.slice(0, end);
  while ((r = re.exec(slice)) !== null) ids.push(Number(r[1]));
  return ids;
}

function gameObjectOf(body) {
  const r = /m_GameObject: \{fileID: (\d+)\}/.exec(body);
  return r ? Number(r[1]) : null;
}

function rectOfGo(goId) {
  const go = blocks.get(goId);
  if (!go) return null;
  for (const cid of parseComponents(go.body)) {
    const c = blocks.get(cid);
    if (c && c.type === "224") return cid;
  }
  return null;
}

const seen = new Set();
const queue = [ROOT_GO];
while (queue.length) {
  const goId = queue.shift();
  if (seen.has(goId) || !blocks.has(goId)) continue;
  seen.add(goId);

  const go = blocks.get(goId);
  for (const cid of parseComponents(go.body)) {
    seen.add(cid);
    const comp = blocks.get(cid);
    if (!comp || comp.type !== "224") continue;
    for (const childRect of parseChildren(comp.body)) {
      const childGo = gameObjectOf(blocks.get(childRect)?.body || "");
      if (childGo) queue.push(childGo);
    }
  }
}

console.log(`Hierarchy objects: ${seen.size}`);

const idMap = new Map([...seen].map((old) => [old, old + ID_OFFSET]));

function remapBody(body) {
  let out = body.replace(/\{fileID: (\d+)\}/g, (_, id) => {
    const n = Number(id);
    return idMap.has(n) ? `{fileID: ${idMap.get(n)}}` : `{fileID: ${id}}`;
  });
  if (out.includes("OnPickWeapon")) {
    out = out.replace(
      /  m_OnClick:\r?\n    m_PersistentCalls:\r?\n      m_Calls:\r?\n      - m_Target:[\s\S]*?        m_CallState: 2\r?\n/g,
      "  m_OnClick:\r\n    m_PersistentCalls:\r\n      m_Calls: []\r\n"
    );
  }
  return out;
}

function buildRewardUiBody(body) {
  const idx = body.indexOf("  m_EditorClassIdentifier:");
  if (idx === -1) return body;
  const head = body.slice(0, idx).trimEnd();
  const panel = idMap.get(CONTENT_PANEL_GO);
  return [
    head,
    "  m_EditorClassIdentifier: Assembly-CSharp::RewardSelectUI",
    `  panel: {fileID: ${panel}}`,
    "  slotButtons:",
    ...BTN_IDS.map((id) => `  - {fileID: ${idMap.get(id)}}`),
    "  slotIcons:",
    ...ICON_IDS.map((id) => `  - {fileID: ${idMap.get(id)}}`),
    "  slotTitles:",
    ...TITLE_IDS.map((id) => `  - {fileID: ${idMap.get(id)}}`),
    "  slotDetails:",
    ...DETAIL_IDS.map((id) => `  - {fileID: ${idMap.get(id)}}`),
    "",
  ].join("\r\n");
}

const duplicated = [];
for (const fid of order) {
  if (!seen.has(fid)) continue;
  const { type, body } = blocks.get(fid);
  const newId = idMap.get(fid);
  let newBody = remapBody(body);
  if (fid === ROOT_GO) newBody = newBody.replace("m_Name: WeaponSelectUI", "m_Name: RewardSelectUI");
  if (fid === ROOT_WEAPON_UI) {
    newBody = buildRewardUiBody(newBody.replace(WEAPON_SELECT_SCRIPT, REWARD_SELECT_SCRIPT));
  }
  duplicated.push(`--- !u!${type} &${newId}\r\n${newBody}`);
}

const newRootRect = idMap.get(rectOfGo(ROOT_GO));
const canvasNeedle = `  - {fileID: ${rectOfGo(ROOT_GO)}}`;
if (!text.includes(`  - {fileID: ${newRootRect}}`)) {
  const idx = text.indexOf(canvasNeedle);
  if (idx === -1) throw new Error("Canvas child anchor not found");
  const lineEnd = text.indexOf("\n", idx);
  text = text.slice(0, lineEnd + 1) + `  - {fileID: ${newRootRect}}\r\n` + text.slice(lineEnd + 1);
}

const anchor = "--- !u!222 &1641493330";
const insertAt = text.indexOf(anchor);
if (insertAt === -1) throw new Error("Insert anchor not found");
let end = text.indexOf("\r\n--- !u!", insertAt + 1);
if (end === -1) end = text.indexOf("\n--- !u!", insertAt + 1);
if (end === -1) end = text.length;
text = text.slice(0, end) + "\r\n" + duplicated.join("\r\n") + "\r\n" + text.slice(end);

fs.writeFileSync(SCENE, text, "utf8");
console.log(`Inserted RewardSelectUI (${seen.size} blocks). Root rect: ${newRootRect}`);
