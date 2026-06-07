const fs = require("fs");
const path = require("path");

function decodeEsc(text) {
  return text.replace(/\\u([0-9A-Fa-f]{4})/g, (_, hex) =>
    String.fromCharCode(parseInt(hex, 16))
  );
}

const accDir = path.join(__dirname, "..", "Assets", "Data", "Accessory");
const sdfPath = path.join(__dirname, "..", "Assets", "Arts", "UI", "Fonts", "neodgm SDF.asset");
const fallbackPath = path.join(__dirname, "..", "Assets", "Arts", "UI", "Fonts", "neodgm Korean Fallback SDF.asset");

const chars = new Set();
for (const file of fs.readdirSync(accDir).filter((f) => f.endsWith(".asset"))) {
  const raw = decodeEsc(fs.readFileSync(path.join(accDir, file), "utf8"));
  for (const key of ["displayName", "description", "accessoryType"]) {
    const re = new RegExp(`${key}:\\s*"([^"]*)"`, "s");
    const match = raw.match(re);
    if (!match) continue;
    for (const ch of match[1]) if (ch.charCodeAt(0) > 127) chars.add(ch);
  }
}

function loadUnicodeSet(assetPath) {
  const sdf = fs.readFileSync(assetPath, "utf8");
  const set = new Set();
  for (const m of sdf.matchAll(/m_Unicode: (\d+)/g)) set.add(Number(m[1]));
  return set;
}

const primary = loadUnicodeSet(sdfPath);
const fallback = fs.existsSync(fallbackPath) ? loadUnicodeSet(fallbackPath) : new Set();

const missingPrimary = [...chars].filter((ch) => !primary.has(ch.charCodeAt(0)));
const missingBoth = missingPrimary.filter((ch) => !fallback.has(ch.charCodeAt(0)));

console.log("Accessory unique chars:", chars.size);
console.log("Primary SDF entries:", primary.size);
console.log("Fallback SDF entries:", fallback.size);
console.log("Missing from primary:", missingPrimary.length, missingPrimary.join(""));
console.log("Missing from both:", missingBoth.length, missingBoth.join(""));
