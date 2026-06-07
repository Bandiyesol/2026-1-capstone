const fs = require("fs");
const path = require("path");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
const BAD_GUID = "59f8146938fff824cb5fd7724134e71c";
const GOOD_GUID = "e19747de3f5aca642ab2be37e372fb86";

let text = fs.readFileSync(SCENE, "utf8");
if (!text.includes(BAD_GUID)) {
	console.error("Bad Outline guid not found — already fixed?");
	process.exit(1);
}

const replacement = [
	`m_Script: {fileID: 11500000, guid: ${GOOD_GUID}, type: 3}`,
	"  m_Name: ",
	"  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Outline",
	"  m_EffectColor: {r: 0.12, g: 0.1, b: 0.08, a: 1}",
	"  m_EffectDistance: {x: 1, y: -1}",
	"  m_UseGraphicAlpha: 1",
].join("\n");

text = text.replace(
	new RegExp(
		`m_Script: \\{fileID: 11500000, guid: ${BAD_GUID}, type: 3\\}[\\s\\S]*?m_EditorClassIdentifier: UnityEngine\\.UI::UnityEngine\\.UI\\.Outline`,
		"m"
	),
	replacement
);

if (text.includes(BAD_GUID)) {
	console.error("Replace failed — bad guid still present.");
	process.exit(1);
}

fs.writeFileSync(SCENE, text, "utf8");
console.log("Fixed Stage Icon Outline script reference.");
