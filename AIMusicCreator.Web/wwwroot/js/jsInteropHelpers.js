// Import functions from audio modules
import { saveFile as audioSaveFile } from './audio.js';
import { saveFile as audioPlayerSaveFile } from './audioPlayer.js';

// Make functions available globally for Blazor JS interop
globalThis.saveFile = function(fileName, dataUrl) {
  // Prefer audioPlayer.js implementation if available, otherwise use audio.js
  return typeof audioPlayerSaveFile === 'function' ? 
    audioPlayerSaveFile(fileName, dataUrl) : 
    audioSaveFile(fileName, dataUrl);
};

// Export for module systems
if (typeof module !== 'undefined') {
  module.exports = { saveFile };
}
