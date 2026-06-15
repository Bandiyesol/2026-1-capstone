const fs = require("fs");
const path = require("path");

const scene = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
let text = fs.readFileSync(scene, "utf8");
let n = 0;

const goldBtnC02 =
	"m_Sprite: {fileID: -93586019915418993, guid: c6e279af717ccb547b0e6736ba2a4728, type: 3}";
const goldBtnC01 =
	"m_Sprite: {fileID: 1680684013888878320, guid: 8342a718bfa35a74887d4d61e5e0dbfb, type: 3}";

function patchRect(id, x, y) {
	const marker = `--- !u!224 &${id}`;
	const start = text.indexOf(marker);
	if (start < 0) return false;
	const tail = text.slice(start + marker.length);
	const rel = tail.search(/\r?\n--- !u!/);
	const block = tail.slice(0, rel);
	const updated = block.replace(
		/m_AnchoredPosition: \{x: [^,]+, y: [^}]+\}/,
		`m_AnchoredPosition: {x: ${x}, y: ${y}}`
	);
	if (updated === block) return false;
	text = text.slice(0, start + marker.length) + updated + tail.slice(rel);
	return true;
}

function patchImage(id, spriteLine) {
	const marker = `--- !u!114 &${id}`;
	const start = text.indexOf(marker);
	if (start < 0) return false;
	const tail = text.slice(start + marker.length);
	const rel = tail.search(/\r?\n--- !u!/);
	let block = tail.slice(0, rel);
	const before = block;
	block = block.replace(/m_Sprite: \{fileID: [^}]+\}/, spriteLine);
	block = block.replace(/m_Type: \d+/, "m_Type: 0");
	block = block.replace(/m_Color: \{r: [^}]+\}/, "m_Color: {r: 1, g: 1, b: 1, a: 1}");
	block = block.replace(/m_PreserveAspect: \d+/, "m_PreserveAspect: 0");
	if (block === before) return false;
	text = text.slice(0, start + marker.length) + block + tail.slice(rel);
	return true;
}

for (const [id, x] of [
	["1723534091", -360],
	["1525049241", 0],
	["1949472873", 360],
]) {
	if (patchRect(id, x, 180)) n++;
}

if (patchImage("1726722690", goldBtnC02)) n++;
for (const id of ["203911044", "1368830286"]) {
	if (patchImage(id, goldBtnC01)) n++;
}

fs.writeFileSync(scene, text);
console.log(`patched ${n} scene items`);
