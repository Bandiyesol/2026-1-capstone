const fs = require("fs");
const scene = require("path").join(__dirname, "..", "Assets/Scenes/ProtoType_LTG.unity");
let text = fs.readFileSync(scene, "utf8");
const before = (text.match(/m_PreserveAspect: 1/g) || []).length;
text = text.replace(
	/(guid: 67cb8c04fdf330347b988243cd05a5c6, type: 3\}\r?\n  m_Type: 0\r?\n  )m_PreserveAspect: 1/g,
	"$1m_PreserveAspect: 0"
);
fs.writeFileSync(scene, text);
const after = (text.match(/m_PreserveAspect: 1/g) || []).length;
console.log(`preserveAspect 1: ${before} -> ${after}`);
