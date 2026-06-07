const fs = require("fs");
const p = require("path").join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
let text = fs.readFileSync(p, "utf8");

const marker = "--- !u!224 &880000131";
const first = text.indexOf(marker);
if (first === -1) {
  console.log("Marker not found.");
  process.exit(0);
}

const second = text.indexOf(marker, first + marker.length);
if (second === -1) {
  console.log("Only one block — nothing to remove.");
  process.exit(0);
}

// Remove first RectTransform block only (up to next YAML object)
const afterFirst = first + marker.length;
const nextObject = text.indexOf("\n--- !u!", afterFirst);
if (nextObject === -1) {
  console.error("Could not find end of first block.");
  process.exit(1);
}

text = text.slice(0, first) + text.slice(nextObject);

fs.writeFileSync(p, text, "utf8");

const re = /&(\d+)/g;
const c = new Map();
let m;
while ((m = re.exec(text))) c.set(m[1], (c.get(m[1]) || 0) + 1);
const d = [...c.entries()].filter(([, n]) => n > 1);
console.log(d.length ? `Still duplicates: ${d.join(", ")}` : "Removed orphan 880000131 block. All IDs unique.");
