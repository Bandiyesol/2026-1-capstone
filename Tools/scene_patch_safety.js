const fs = require('fs');

const MIN_SCENE_BYTES = 40_000_000;
const MARKERS = ['m_Name: SettingPanel', 'm_Name: BgmSlider', 'm_Name: LoginPanel'];

function assertSceneIntegrity(text, originalBytes) {
  if (text.length < Math.min(originalBytes * 0.95, MIN_SCENE_BYTES)) {
    throw new Error(
      `Scene write blocked: size dropped to ${text.length} bytes (was ${originalBytes}).`
    );
  }

  for (const marker of MARKERS) {
    if (!text.includes(marker)) {
      throw new Error(`Scene write blocked: missing marker "${marker}".`);
    }
  }
}

function writeSceneSafe(scenePath, text, originalBytes) {
  assertSceneIntegrity(text, originalBytes);
  fs.writeFileSync(scenePath, text, 'utf8');
  const written = fs.readFileSync(scenePath, 'utf8');
  assertSceneIntegrity(written, originalBytes);
}

module.exports = { writeSceneSafe, assertSceneIntegrity, MIN_SCENE_BYTES };
