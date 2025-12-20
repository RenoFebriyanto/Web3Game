/**
 * ✅ FINAL v3.0: Minimal jslib - Let HTML do the work
 */

mergeInto(LibraryManager.library, {
    
    IsPortraitMode: function() {
        try {
            if (typeof window.IsPortraitMode === 'function') {
                return window.IsPortraitMode() ? 1 : 0;
            }
            // Fallback
            return (window.innerHeight > window.innerWidth) ? 1 : 0;
        } catch (e) {
            console.error('[jslib] IsPortraitMode error:', e);
            return 0;
        }
    },
    
    IsMobileBrowser: function() {
        try {
            if (typeof window.IsMobileBrowser === 'function') {
                return window.IsMobileBrowser() ? 1 : 0;
            }
            // Fallback
            var ua = navigator.userAgent || '';
            var mobile = /Android.*Mobile|iPhone|iPod/i.test(ua);
            return mobile ? 1 : 0;
        } catch (e) {
            console.error('[jslib] IsMobileBrowser error:', e);
            return 0;
        }
    }
    
});
