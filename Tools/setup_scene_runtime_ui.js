/**
 * ProtoType_LTG.unity — 런타임에 만들던 UI/매니저를 씬에 영구 배치합니다.
 * node Tools/setup_scene_runtime_ui.js
 */
const fs = require("fs");
const path = require("path");

const SCENE = path.join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
const ID = (n) => 880_000_000 + n;

const GUIDS = {
  AccessoryManager: "3971858c64f4c1547a4e2f6593fd1aef",
  AccessoryEffect: "cad624ccc15ecaa44aa66ebf9d391751",
  RewardRollService: "edcb5af5fc41d934d84ecf9f37a0aaca",
  AccessoryInventory: "9efadc5dcc5b85a44bcf4718834d5ab4",
  PotionInventory: "d7d7480d17035654bace4c33693f0512",
  PotionEffect: "b52a8228cc9147347bf6a1fd8a6dd332",
  OverlayPanelEscapeInput: "2dde6902947e0474e9a8206f0fb5739f",
  EndingSequenceController: "a2a6827eee24b0249a6642bee0e859a5",
  MainMenuLeaderboardView: "ec6ce81b64ad58a48a1d355303e36acb",
  Image: "fe87c0e1cc204ed48ad3b37840f39efc",
  Button: "4e29b1a8efbd4b44bb3f3716e73f07ff",
  TMP: "f4688fdb7df044fa926a6407b152d7f3",
  VerticalLayoutGroup: "3245ec927659ee4148005612193963f3",
};

const KOREAN_FONT = "{fileID: 11400000, guid: 4aecfcbbc7d93b541931e843ddf3a997, type: 2}";
const CANVAS_RECT = 461710348;
const MANAGER_GO = 315315527;
const PLAYER_GO = 1680473516;
const GAME_MANAGER_GO = 1789209906;

let text = fs.readFileSync(SCENE, "utf8");

function hasClass(name) {
  return text.includes(`Assembly-CSharp::${name}`);
}

function monoBlock(id, goId, guid, className, extra = "") {
  return `--- !u!114 &${id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::${className}
${extra}`;
}

function canvasRenderer(id, goId) {
  return `--- !u!222 &${id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1`;
}

function rectBlock(id, goId, opts) {
  const children = (opts.children || [])
    .map((c) => `  - {fileID: ${c}}`)
    .join("\r\n");
  return `--- !u!224 &${id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${children || ""}
  m_Father: {fileID: ${opts.father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${opts.anchorMin[0]}, y: ${opts.anchorMin[1]}}
  m_AnchorMax: {x: ${opts.anchorMax[0]}, y: ${opts.anchorMax[1]}}
  m_AnchoredPosition: {x: ${opts.anchoredPosition[0]}, y: ${opts.anchoredPosition[1]}}
  m_SizeDelta: {x: ${opts.sizeDelta[0]}, y: ${opts.sizeDelta[1]}}
  m_Pivot: {x: ${opts.pivot[0]}, y: ${opts.pivot[1]}}`;
}

function goBlock(id, name, components, active = 1) {
  const comps = components.map((c) => `  - component: {fileID: ${c}}`).join("\r\n");
  return `--- !u!1 &${id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${comps}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: ${active}`;
}

function imageMono(id, goId, color) {
  return `--- !u!114 &${id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUIDS.Image}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: ${color[0]}, g: ${color[1]}, b: ${color[2]}, a: ${color[3]}}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1`;
}

function buttonMono(id, goId, targetGraphic) {
  return `--- !u!114 &${id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUIDS.Button}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.85, g: 0.9, b: 1, a: 1}
    m_PressedColor: {r: 0.7, g: 0.78, b: 0.95, a: 1}
    m_SelectedColor: {r: 0.85, g: 0.9, b: 1, a: 1}
    m_DisabledColor: {r: 0.55, g: 0.55, b: 0.55, a: 0.5}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: ${targetGraphic}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []`;
}

function tmpMono(id, goId, content, size, align) {
  return `--- !u!114 &${id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUIDS.TMP}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: ${JSON.stringify(content)}
  m_isRightToLeft: 0
  m_fontAsset: ${KOREAN_FONT}
  m_sharedMaterial: ${KOREAN_FONT}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 1, g: 1, b: 1, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontSize: ${size}
  m_fontSizeBase: ${size}
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: 0
  m_HorizontalAlignment: ${align}
  m_VerticalAlignment: 512
  m_textAlignment: 65536
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_TextWrappingMode: 0
  m_wordWrappingRatios: 0.4
  m_overflowMode: 1
  m_linkedTextComponent: {fileID: 0}
  parentLinkedComponent: {fileID: 0}
  m_enableKerning: 0
  m_ActiveFontFeatures: 6e72656b
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_EmojiFallbackSupport: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {x: 0, y: 0, z: 0, w: 0}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {fileID: 0}
  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}`;
}

function vlgMono(id, goId) {
  return `--- !u!114 &${id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUIDS.VerticalLayoutGroup}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 0
  m_Spacing: 2
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 1
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0`;
}

function addComponentToGo(goId, compId) {
  const re = new RegExp(`(--- !u!1 &${goId}\\r?\\nGameObject:[\\s\\S]*?m_Component:\\r?\\n)([\\s\\S]*?)(\\r?\\n  m_Layer:)`, "m");
  const m = text.match(re);
  if (!m) throw new Error(`GameObject ${goId} not found`);
  if (m[2].includes(`{fileID: ${compId}}`)) return false;
  text = text.replace(re, `$1$2  - component: {fileID: ${compId}}\r\n$3`);
  return true;
}

const inserts = [];

function goHasComponent(goId, className) {
  const re = new RegExp(
    `--- !u!114 &\\d+\\r?\\nMonoBehaviour:[\\s\\S]*?m_GameObject: \\{fileID: ${goId}\\}[\\s\\S]*?Assembly-CSharp::${className}`,
    "m"
  );
  return re.test(text);
}

function addSimpleComponent(goId, compId, guid, className, extra = "") {
  if (goHasComponent(goId, className)) {
    console.log(`= ${className} already on GO ${goId}`);
    return;
  }
  if (addComponentToGo(goId, compId)) {
    inserts.push(monoBlock(compId, goId, guid, className, extra));
    console.log(`+ ${className} on GO ${goId}`);
  }
}

// [ Manager ]
addSimpleComponent(MANAGER_GO, ID(1), GUIDS.AccessoryManager, "AccessoryManager");
addSimpleComponent(MANAGER_GO, ID(2), GUIDS.AccessoryEffect, "AccessoryEffect");
addSimpleComponent(MANAGER_GO, ID(3), GUIDS.RewardRollService, "RewardRollService", "  allAccessories: []\r\n  allRelics: []\r\n");

// Player
addSimpleComponent(PLAYER_GO, ID(4), GUIDS.AccessoryInventory, "AccessoryInventory", "  maxAccessories: 12\r\n");
addSimpleComponent(PLAYER_GO, ID(5), GUIDS.PotionInventory, "PotionInventory", "  maxStacks: 12\r\n");
addSimpleComponent(PLAYER_GO, ID(6), GUIDS.PotionEffect, "PotionEffect");

// GameManager
addSimpleComponent(GAME_MANAGER_GO, ID(7), GUIDS.OverlayPanelEscapeInput, "OverlayPanelEscapeInput");
addSimpleComponent(GAME_MANAGER_GO, ID(8), GUIDS.EndingSequenceController, "EndingSequenceController");

// MainMenuLeaderboard
if (!text.includes("m_Name: MainMenuLeaderboard")) {
  const rootGo = ID(100);
  const rootRect = ID(101);
  const rootCr = ID(102);
  const rootImg = ID(103);
  const rootView = ID(104);
  const titleGo = ID(110);
  const titleRect = ID(111);
  const titleCr = ID(112);
  const titleTmp = ID(113);
  const subGo = ID(120);
  const subRect = ID(121);
  const subCr = ID(122);
  const subTmp = ID(123);
  const rowsGo = ID(130);
  const rowsRect = ID(131);
  const rowsVlg = ID(132);

  const rowIds = [];
  const buttonRefs = [];
  const labelRefs = [];
  for (let i = 0; i < 10; i++) {
    const base = ID(200 + i * 10);
    rowIds.push(base + 1);
    buttonRefs.push(base + 4);
    labelRefs.push(base + 8);
  }

  const rowRectIds = [];
  for (let i = 0; i < 10; i++) rowRectIds.push(ID(200 + i * 10 + 1));

  const blocks = [];
  blocks.push(
    goBlock(rootGo, "MainMenuLeaderboard", [rootRect, rootCr, rootImg, rootView]),
    rectBlock(rootRect, rootGo, {
      father: CANVAS_RECT,
      children: [titleRect, subRect, rowsRect],
      anchorMin: [1, 1],
      anchorMax: [1, 1],
      pivot: [1, 1],
      sizeDelta: [340, 440],
      anchoredPosition: [-24, -24],
    }),
    canvasRenderer(rootCr, rootGo),
    imageMono(rootImg, rootGo, [0.08, 0.1, 0.16, 0.82]),
    monoBlock(
      rootView,
      rootGo,
      GUIDS.MainMenuLeaderboardView,
      "MainMenuLeaderboardView",
      `  titleText: {fileID: ${titleTmp}}\r\n  subtitleText: {fileID: ${subTmp}}\r\n  rankButtons:\r\n${buttonRefs.map((b) => `  - {fileID: ${b}}`).join("\r\n")}\r\n  rankLabels:\r\n${labelRefs.map((l) => `  - {fileID: ${l}}`).join("\r\n")}\r\n  koreanFont: ${KOREAN_FONT}\r\n`
    ),
    goBlock(titleGo, "Title", [titleRect, titleCr, titleTmp]),
    rectBlock(titleRect, titleGo, {
      father: rootRect,
      children: [],
      anchorMin: [0, 1],
      anchorMax: [1, 1],
      pivot: [0.5, 1],
      sizeDelta: [0, 32],
      anchoredPosition: [0, -6],
    }),
    canvasRenderer(titleCr, titleGo),
    tmpMono(titleTmp, titleGo, "클리어 랭킹", 22, 2),
    goBlock(subGo, "Subtitle", [subRect, subCr, subTmp]),
    rectBlock(subRect, subGo, {
      father: rootRect,
      children: [],
      anchorMin: [0, 1],
      anchorMax: [1, 1],
      pivot: [0.5, 1],
      sizeDelta: [0, 22],
      anchoredPosition: [0, -36],
    }),
    canvasRenderer(subCr, subGo),
    tmpMono(subTmp, subGo, "닉네임 · 최단 플레이타임 · 탭 상세", 14, 2),
    goBlock(rowsGo, "Rows", [rowsRect, rowsVlg]),
    `--- !u!224 &${rowsRect}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${rowsGo}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${rowRectIds.map((c) => `  - {fileID: ${c}}`).join("\r\n")}
  m_Father: {fileID: ${rootRect}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
  m_OffsetMin: {x: 10, y: 10}
  m_OffsetMax: {x: -10, y: -62}`,
    vlgMono(rowsVlg, rowsGo)
  );

  for (let i = 0; i < 10; i++) {
    const rank = i + 1;
    const go = ID(200 + i * 10);
    const rect = go + 1;
    const cr = go + 2;
    const img = go + 3;
    const btn = go + 4;
    const labelGo = go + 5;
    const labelRect = go + 6;
    const labelCr = go + 7;
    const labelTmp = go + 8;
    blocks.push(
      goBlock(go, `RankRow${rank}`, [rect, cr, img, btn]),
      rectBlock(rect, go, {
        father: rowsRect,
        children: [labelRect],
        anchorMin: [0, 1],
        anchorMax: [1, 1],
        pivot: [0.5, 1],
        sizeDelta: [0, 32],
        anchoredPosition: [0, 0],
      }),
      canvasRenderer(cr, go),
      imageMono(img, go, [0.18, 0.22, 0.3, 0.55]),
      buttonMono(btn, go, img),
      goBlock(labelGo, "Label", [labelRect, labelCr, labelTmp]),
      `--- !u!224 &${labelRect}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${labelGo}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${rect}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
  m_OffsetMin: {x: 8, y: 0}
  m_OffsetMax: {x: -4, y: 0}`,
      canvasRenderer(labelCr, labelGo),
      tmpMono(labelTmp, labelGo, `${rank}. —`, 17, 1)
    );
  }

  if (!text.includes(`  - {fileID: ${rootRect}}`)) {
    const canvasRe = new RegExp(`(--- !u!224 &${CANVAS_RECT}\\r?\\nRectTransform:[\\s\\S]*?m_Children:\\r?\\n)`);
    if (canvasRe.test(text)) {
      text = text.replace(canvasRe, `$1  - {fileID: ${rootRect}}\r\n`);
    }
  }

  inserts.push(...blocks);
  console.log("+ MainMenuLeaderboard UI");
}

// InventoryUI → Player inventories
text = text.replace(
  /(  weaponInventory: \{fileID: 1680473526\}\r?\n)  accessoryInventory: \{fileID: 0\}\r?\n  potionInventory: \{fileID: 0\}/,
  `$1  accessoryInventory: {fileID: ${ID(4)}}\r\n  potionInventory: {fileID: ${ID(5)}}`
);

const anchor = "--- !u!222 &1641493330";
const insertAt = text.indexOf(anchor);
if (insertAt === -1) throw new Error("Scene insert anchor not found");
let end = text.indexOf("\r\n--- !u!", insertAt + 1);
if (end === -1) end = text.indexOf("\n--- !u!", insertAt + 1);
if (end === -1) end = text.length;
text = text.slice(0, end) + "\r\n" + inserts.join("\r\n") + "\r\n" + text.slice(end);

fs.writeFileSync(SCENE, text, "utf8");
console.log("Scene runtime UI setup complete.");
