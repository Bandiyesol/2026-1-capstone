const fs = require("fs");
const path = require("path");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
let text = fs.readFileSync(SCENE, "utf8");

const broken = `  m_Layer: 5
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 364532940}`;

const fixed = `  m_Layer: 5
  m_Name: ShopHudButton
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &364532941
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 364532940}`;

if (!text.includes(broken)) {
	console.error("Broken ShopHudButton block not found.");
	process.exit(1);
}

text = text.replace(broken, fixed);
fs.writeFileSync(SCENE, text, "utf8");
console.log("Restored and disabled ShopHudButton.");
