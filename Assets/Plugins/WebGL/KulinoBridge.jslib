mergeInto(LibraryManager.library, {
  // Called from C# native import: RequestClaim(string json)
  RequestClaim: function(strPtr) {
    try {
      var msg = UTF8ToString(strPtr);
      // Forward ke window.requestClaimFromUnity (yang tadi ada di index.html)
      if (typeof window.requestClaimFromUnity === 'function') {
        window.requestClaimFromUnity(msg);
      } else {
        console.warn('requestClaimFromUnity not defined - forwarding failed');
        // fallback: inform Unity OnClaimResult with error
        if (typeof unityInstance !== 'undefined' && unityInstance.SendMessage) {
          unityInstance.SendMessage('GameManager', 'OnClaimResult', JSON.stringify({ success:false, error:'requestClaim_not_defined' }));
        }
      }
    } catch (e) {
      console.error('KulinoBridge.RequestClaim error', e);
      if (typeof unityInstance !== 'undefined' && unityInstance.SendMessage) {
        unityInstance.SendMessage('GameManager', 'OnClaimResult', JSON.stringify({ success:false, error: String(e) }));
      }
    }
  }
});
