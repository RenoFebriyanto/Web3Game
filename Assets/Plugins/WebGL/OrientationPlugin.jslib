/**
 * OrientationPlugin.jslib
 * JavaScript plugin untuk Unity WebGL - Orientation Detection
 * 
 * LOKASI: Assets/Plugins/WebGL/OrientationPlugin.jslib
 */

mergeInto(LibraryManager.library, {
  
  /**
   * Check apakah device mobile
   */
  IsMobileBrowser: function() {
    var userAgent = navigator.userAgent || navigator.vendor || window.opera;
    var isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(userAgent);
    return isMobile;
  },
  
  /**
   * Check apakah orientation portrait
   */
  IsPortraitMode: function() {
    var isPortrait = window.innerHeight > window.innerWidth;
    return isPortrait;
  },
  
  /**
   * Show rotation prompt overlay
   */
  ShowRotationPromptJS: function() {
    var prompt = document.getElementById('rotation-prompt');
    if (prompt) {
      prompt.classList.add('show');
      console.log('[OrientationPlugin] Rotation prompt shown');
    }
  },
  
  /**
   * Hide rotation prompt overlay
   */
  HideRotationPromptJS: function() {
    var prompt = document.getElementById('rotation-prompt');
    if (prompt) {
      prompt.classList.remove('show');
      console.log('[OrientationPlugin] Rotation prompt hidden');
    }
  },
  
  /**
   * Get current screen dimensions
   */
  GetScreenDimensions: function() {
    var width = window.innerWidth || document.documentElement.clientWidth;
    var height = window.innerHeight || document.documentElement.clientHeight;
    
    // Return as combined value (width in high bits, height in low bits)
    return (width << 16) | height;
  },
  
  /**
   * Force screen refresh (for orientation change)
   */
  RefreshScreen: function() {
    if (window.unityInstance) {
      // Trigger Unity canvas resize
      window.dispatchEvent(new Event('resize'));
      console.log('[OrientationPlugin] Screen refresh triggered');
    }
  }
  
});