const fs = require("fs");
const path = require("path");

const scene = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
let text = fs.readFileSync(scene, "utf8");
let n = 0;

const goldBtnCenter =
	"m_Sprite: {fileID: -2847192836012345678, guid: c6e279af717ccb547b0e6736ba2a4728, type: 3}";

function patchRectAnchors(id, minX, minY, maxX, maxY) {
	const marker = `--- !u!224 &${id}`;
	const start = text.indexOf(marker);
	if (start < 0) return false;
	const tail = text.slice(start + marker.length);
	const rel = tail.search(/\r?\n--- !u!/);
	const block = tail.slice(0, rel);
	const updated = block
		.replace(/m_AnchorMin: \{x: [^,]+, y: [^}]+\}/, `m_AnchorMin: {x: ${minX}, y: ${minY}}`)
		.replace(/m_AnchorMax: \{x: [^,]+, y: [^}]+\}/, `m_AnchorMax: {x: ${maxX}, y: ${maxY}}`);
	if (updated === block) return false;
	text = text.slice(0, start + marker.length) + updated + tail.slice(rel);
	return true;
}

function patchMenuButtonImage(id) {
	const marker = `--- !u!114 &${id}`;
	const start = text.indexOf(marker);
	if (start < 0) return false;
	const tail = text.slice(start + marker.length);
	const rel = tail.search(/\r?\n--- !u!/);
	let block = tail.slice(0, rel);
	const before = block;
	block = block.replace(/m_Sprite: \{fileID: [^}]+\}/, goldBtnCenter);
	block = block.replace(/m_Type: \d+/, "m_Type: 1");
	block = block.replace(/m_PreserveAspect: \d+/, "m_PreserveAspect: 0");
	if (block === before) return false;
	text = text.slice(0, start + marker.length) + block + tail.slice(rel);
	return true;
}

// Slider backgrounds — taller gold frame
for (const id of ["203911043", "1368830285"]) {
	if (patchRectAnchors(id, 0, 0.08, 1, 0.92)) n++;
}

// Slider fill areas — thinner green bar inside frame
for (const id of ["1897722323", "126036354"]) {
	if (patchRectAnchors(id, 0.05, 0.36, 1, 0.64)) n++;
}

// Setting + record confirm buttons — 9-slice gold
for (const id of ["1723534093", "1525049243", "1949472875", "1726722690"]) {
	if (patchMenuButtonImage(id)) n++;
}

fs.writeFileSync(scene, text);
console.log(`patched ${n} items`);
