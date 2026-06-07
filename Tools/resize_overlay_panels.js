/**
 * BoxPanel 1520x960, Status/Title/Portrait 레이아웃·폰트 보정
 * node Tools/resize_overlay_panels.js
 */
const fs = require("fs");
const path = require("path");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
const FRAME = { w: 1520, h: 960 };

const frameIds = ["801907361", "1678042295", "1015870993", "413751409"];

const rectPatches = {
	1966274695: { pos: { x: -300, y: -48 }, size: { x: 520, y: 820 } },
	292555129: { pos: { x: 100, y: -48 }, size: { x: 400, y: 820 } },
	1913832362: {
		anchorMin: { x: 0.5, y: 1 },
		anchorMax: { x: 0.5, y: 1 },
		pivot: { x: 0.5, y: 1 },
		pos: { x: 0, y: -96 },
		size: { x: 640, y: 72 },
	},
	1976461822: {
		anchorMin: { x: 1, y: 0.5 },
		anchorMax: { x: 1, y: 0.5 },
		pivot: { x: 0.5, y: 0.5 },
		pos: { x: -320, y: -20 },
		size: { x: 360, y: 520 },
	},
};

function patchRect(text, id, patchFn) {
	const marker = `--- !u!224 &${id}`;
	const start = text.indexOf(marker);
	if (start === -1) return { text, ok: false };
	const tail = text.slice(start + marker.length);
	const rel = tail.search(/\r?\n--- !u!/);
	if (rel === -1) return { text, ok: false };
	const block = tail.slice(0, rel);
	const updated = patchFn(block);
	if (updated === block) return { text, ok: false };
	return { text: text.slice(0, start + marker.length) + updated + tail.slice(rel), ok: true };
}

function applyRectPatch(block, patch) {
	let b = block;
	if (patch.anchorMin)
		b = b.replace(/m_AnchorMin: \{x: [^,]+, y: [^}]+\}/, `m_AnchorMin: {x: ${patch.anchorMin.x}, y: ${patch.anchorMin.y}}`);
	if (patch.anchorMax)
		b = b.replace(/m_AnchorMax: \{x: [^,]+, y: [^}]+\}/, `m_AnchorMax: {x: ${patch.anchorMax.x}, y: ${patch.anchorMax.y}}`);
	if (patch.pivot)
		b = b.replace(/m_Pivot: \{x: [^,]+, y: [^}]+\}/, `m_Pivot: {x: ${patch.pivot.x}, y: ${patch.pivot.y}}`);
	if (patch.pos)
		b = b.replace(/m_AnchoredPosition: \{x: [^,]+, y: [^}]+\}/, `m_AnchoredPosition: {x: ${patch.pos.x}, y: ${patch.pos.y}}`);
	if (patch.size)
		b = b.replace(/m_SizeDelta: \{x: [^,]+, y: [^}]+\}/, `m_SizeDelta: {x: ${patch.size.x}, y: ${patch.size.y}}`);
	return b;
}

let text = fs.readFileSync(SCENE, "utf8");
let frames = 0;
let rects = 0;

for (const id of frameIds) {
	let ok;
	({ text, ok } = patchRect(text, id, (b) =>
		b.replace(/m_SizeDelta: \{x: [^,]+, y: [^}]+\}/, `m_SizeDelta: {x: ${FRAME.w}, y: ${FRAME.h}}`)
	));
	if (ok) frames++;
}

for (const [id, patch] of Object.entries(rectPatches)) {
	let ok;
	({ text, ok } = patchRect(text, id, (b) => applyRectPatch(b, patch)));
	if (ok) rects++;
}

text = text.replace(
	/(m_GameObject: \{fileID: 1966274694\}[\s\S]*?m_fontSize: )26(\r?\n  m_fontSizeBase: )26/,
	"$128$228"
);
text = text.replace(
	/(m_GameObject: \{fileID: 292555128\}[\s\S]*?m_fontSize: )22(\r?\n  m_fontSizeBase: )22/,
	"$128$228"
);

fs.writeFileSync(SCENE, text, "utf8");
console.log(`Patched ${frames} BoxPanels, ${rects} child rects`);
