/**
 * Scan accessory assets for characters known to be missing from neodgm.ttf.
 * node Tools/scan_neodgm_missing.js
 */
const fs = require("fs");
const path = require("path");

const accDir = path.join(__dirname, "..", "Assets", "Data", "Accessory");
const knownMissing = new Set("끼납낡럭꽃냉".split(""));

function decodeEsc(text) {
  return text.replace(/\\u([0-9A-Fa-f]{4})/g, (_, hex) =>
    String.fromCharCode(parseInt(hex, 16))
  );
}

const issues = [];

for (const file of fs.readdirSync(accDir).filter((f) => f.endsWith(".asset"))) {
  const raw = fs.readFileSync(path.join(accDir, file), "utf8");
  const text = decodeEsc(raw);
  for (const key of ["displayName", "description", "accessoryType"]) {
    const re = new RegExp(`${key}:\\s*"([^"]*)"`, "s");
    const match = text.match(re);
    if (!match) continue;
    const bad = [...match[1]].filter((ch) => knownMissing.has(ch));
    if (bad.length) issues.push({ file, key, bad: [...new Set(bad)], value: match[1] });
  }
}

if (issues.length === 0) {
  console.log("OK: no known neodgm-missing characters in accessory assets.");
} else {
  console.log("Issues found:");
  for (const i of issues) {
    console.log(`  ${i.file} [${i.key}]: ${i.bad.join("")} in "${i.value}"`);
  }
  process.exitCode = 1;
}
