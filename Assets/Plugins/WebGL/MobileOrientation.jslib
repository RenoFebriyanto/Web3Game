/**
 * ✅ Mobile Orientation Detection Plugin for Unity WebGL
 * Provides JavaScript functions for orientation and mobile detection
 * 
 * INSTALLATION:
 * Save this file as: Assets/Plugins/WebGL/MobileOrientation.jslib
 */

mergeInto(LibraryManager.library, {
    
    /**
     * Check if device is in portrait mode
     * @returns {number} 1 if portrait, 0 if landscape
     */
    IsPortraitMode: function() {
        try {
            // Method 1: window.orientation (deprecated but still works)
            if (typeof window.orientation !== 'undefined') {
                // 0 or 180 = portrait, 90 or -90 = landscape
                var isPortrait = (window.orientation === 0 || window.orientation === 180);
                console.log('[MobileOrientation] window.orientation:', window.orientation, '= portrait:', isPortrait);
                return isPortrait ? 1 : 0;
            }
            
            // Method 2: screen.orientation API
            if (window.screen && window.screen.orientation) {
                var type = window.screen.orientation.type;
                var isPortrait = type.includes('portrait');
                console.log('[MobileOrientation] screen.orientation.type:', type, '= portrait:', isPortrait);
                return isPortrait ? 1 : 0;
            }
            
            // Method 3: Aspect ratio fallback
            var isPortrait = window.innerHeight > window.innerWidth;
            console.log('[MobileOrientation] Aspect ratio:', window.innerWidth, 'x', window.innerHeight, '= portrait:', isPortrait);
            return isPortrait ? 1 : 0;
            
        } catch (e) {
            console.error('[MobileOrientation] IsPortraitMode error:', e);
            // Default to landscape (safer for games)
            return 0;
        }
    },
    
    /**
     * Check if browser is on mobile device
     * @returns {number} 1 if mobile, 0 if desktop
     */
    IsMobileBrowser: function() {
        try {
            var userAgent = navigator.userAgent || navigator.vendor || window.opera;
            
            // Check 1: Mobile keywords in user agent
            var mobileKeywords = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i;
            var isMobileUA = mobileKeywords.test(userAgent);
            
            // Check 2: Touch support
            var isTouchDevice = ('ontouchstart' in window) || 
                               (navigator.maxTouchPoints > 0) ||
                               (navigator.msMaxTouchPoints > 0);
            
            // Check 3: Screen size
            var isSmallScreen = window.innerWidth < 768;
            
            // Combine checks
            var isMobile = isMobileUA || (isTouchDevice && isSmallScreen);
            
            console.log('[MobileOrientation] Mobile detection:', {
                userAgent: isMobileUA,
                touch: isTouchDevice,
                smallScreen: isSmallScreen,
                result: isMobile
            });
            
            return isMobile ? 1 : 0;
            
        } catch (e) {
            console.error('[MobileOrientation] IsMobileBrowser error:', e);
            return 0;
        }
    },
    
    /**
     * Notify JavaScript about orientation changes from Unity
     * Can be used to trigger custom JS events
     * @param {number} isPortrait - 1 if portrait, 0 if landscape
     */
    NotifyOrientationToJS: function(isPortrait) {
        try {
            var orientationName = isPortrait ? 'portrait' : 'landscape';
            console.log('[MobileOrientation] Unity notified JS of orientation:', orientationName);
            
            // Dispatch custom event that can be listened to in JS
            var event = new CustomEvent('unityOrientationChanged', {
                detail: {
                    isPortrait: isPortrait === 1,
                    isLandscape: isPortrait === 0,
                    orientation: orientationName,
                    timestamp: Date.now()
                }
            });
            
            window.dispatchEvent(event);
            
            // Optional: Call global handler if defined
            if (typeof window.onUnityOrientationChanged === 'function') {
                window.onUnityOrientationChanged(isPortrait === 1);
            }
            
        } catch (e) {
            console.error('[MobileOrientation] NotifyOrientationToJS error:', e);
        }
    },
    
    /**
     * BONUS: Get current screen dimensions
     * @returns {string} JSON string with width and height
     */
    GetScreenDimensions: function() {
        try {
            var dimensions = {
                width: window.innerWidth,
                height: window.innerHeight,
                availWidth: window.screen.availWidth,
                availHeight: window.screen.availHeight,
                devicePixelRatio: window.devicePixelRatio || 1
            };
            
            var json = JSON.stringify(dimensions);
            var bufferSize = lengthBytesUTF8(json) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(json, buffer, bufferSize);
            return buffer;
            
        } catch (e) {
            console.error('[MobileOrientation] GetScreenDimensions error:', e);
            return 0;
        }
    }
    
});