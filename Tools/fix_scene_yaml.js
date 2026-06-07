const fs = require("fs");
const p = require("path").join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
let t = fs.readFileSync(p, "utf8");

t = t.replace(
  "  - component: {fileID: 315315528}  - component: {fileID: 880000001}",
  "  - component: {fileID: 315315528}\r\n  - component: {fileID: 880000001}"
);
t = t.replace(
  "  - component: {fileID: 1680473527}  - component: {fileID: 880000004}",
  "  - component: {fileID: 1680473527}\r\n  - component: {fileID: 880000004}"
);
t = t.replace(
  "  - component: {fileID: 1789209907}  - component: {fileID: 880000007}",
  "  - component: {fileID: 1789209907}\r\n  - component: {fileID: 880000007}"
);
t = t.replace("  accessoryInventory: {fileID: 0}", "  accessoryInventory: {fileID: 880000004}");
t = t.replace("  potionInventory: {fileID: 0}", "  potionInventory: {fileID: 880000005}");

fs.writeFileSync(p, t, "utf8");
console.log("YAML fixes applied.");
