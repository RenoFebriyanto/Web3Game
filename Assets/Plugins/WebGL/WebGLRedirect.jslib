/**
 * WebGL JavaScript Plugin untuk redirect ke URL
 * 
 * INSTALASI:
 * 1. Buat folder "Assets/Plugins/WebGL" jika belum ada
 * 2. Save file ini sebagai "WebGLRedirect.jslib" di folder tersebut
 * 3. Unity akan otomatis compile file ini saat build WebGL
 */

mergeInto(LibraryManager.library, {
    
    /**
     * Redirect browser ke URL tertentu
     * Dipanggil dari C# menggunakan [DllImport("__Internal")]
     * 
     * @param {string} url - URL tujuan redirect
     */
    RedirectToURL: function(url) {
        // Convert Unity string pointer ke JavaScript string
        var urlString = UTF8ToString(url);
        
        console.log('[WebGLRedirect] Redirecting to: ' + urlString);
        
        try {
            // Method 1: Direct window.location (recommended)
            window.location.href = urlString;
            
            // Alternative method jika method 1 gagal:
            // window.location.replace(urlString);
            
            // Alternative method untuk open new tab (optional):
            // window.open(urlString, '_self');
            
        } catch (e) {
            console.error('[WebGLRedirect] Failed to redirect:', e);
            
            // Fallback: try to open in new tab
            try {
                window.open(urlString, '_blank');
            } catch (e2) {
                console.error('[WebGLRedirect] All redirect methods failed:', e2);
            }
        }
    },
    
    /**
     * Open URL in new tab (alternative method)
     * 
     * @param {string} url - URL to open
     */
    OpenURLNewTab: function(url) {
        var urlString = UTF8ToString(url);
        console.log('[WebGLRedirect] Opening in new tab: ' + urlString);
        window.open(urlString, '_blank');
    },
    
    /**
     * Get current page URL (useful for debugging)
     * 
     * @returns {string} Current page URL
     */
    GetCurrentURL: function() {
        var currentURL = window.location.href;
        var bufferSize = lengthBytesUTF8(currentURL) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(currentURL, buffer, bufferSize);
        return buffer;
    },
    
    /**
     * Show browser confirmation dialog before exit
     * 
     * @param {string} message - Confirmation message
     * @returns {boolean} True if user confirmed, false if cancelled
     */
    ConfirmExit: function(message) {
        var messageString = UTF8ToString(message);
        return window.confirm(messageString) ? 1 : 0;
    }
    
});