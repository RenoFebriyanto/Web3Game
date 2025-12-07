/**
 * ✅ FIXED: Mobile Orientation Detection Plugin for Unity WebGL
 * Removed NotifyOrientationToJS to fix WebAssembly error
 */

mergeInto(LibraryManager.library, {
    
    /**
     * Check if device is in portrait mode
     * @returns {number} 1 if portrait, 0 if landscape
     */
    IsPortraitMode: function() {
        try {
            if (typeof window.IsPortraitMode === 'function') {
                return window.IsPortraitMode() ? 1 : 0;
            }
            
            // Fallback
            var isPortrait = window.innerHeight > window.innerWidth;
            console.log('[MobileOrientation] Fallback portrait check:', isPortrait);
            return isPortrait ? 1 : 0;
            
        } catch (e) {
            console.error('[MobileOrientation] IsPortraitMode error:', e);
            return 0;
        }
    },
    
    /**
     * Check if browser is on mobile device
     * @returns {number} 1 if mobile, 0 if desktop
     */
    IsMobileBrowser: function() {
        try {
            if (typeof window.IsMobileBrowser === 'function') {
                return window.IsMobileBrowser() ? 1 : 0;
            }
            
            // Fallback
            var userAgent = navigator.userAgent || navigator.vendor || window.opera;
            var mobileKeywords = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i;
            var isMobile = mobileKeywords.test(userAgent);
            
            console.log('[MobileOrientation] Fallback mobile check:', isMobile);
            return isMobile ? 1 : 0;
            
        } catch (e) {
            console.error('[MobileOrientation] IsMobileBrowser error:', e);
            return 0;
        }
    }
    
});