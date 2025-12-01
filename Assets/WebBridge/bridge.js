// Assets/WebBridge/bridge.js
// Minimal Kulino Bridge for Unity WebGL
// - tidak bergantung CDN (mengandung base58 encoder kecil).
// - Menyediakan window.requestClaimFromUnity dan queue → mengirim hasil ke Unity via SendMessage('GameManager','OnClaimResult', json)

(function(){
  const LOG = (...a)=>console.log('[KULINO-BRIDGE]',...a);

  // --- base58 encode (minimal, works for Uint8Array) ---
  // alphabet sama dengan bs58
  const ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
  function base58Encode(bytes){
    // big-integer style encode
    let digits = [0];
    for (let i = 0; i < bytes.length; ++i) {
      let carry = bytes[i];
      for (let j = 0; j < digits.length; ++j) {
        carry += digits[j] << 8;
        digits[j] = carry % 58;
        carry = (carry / 58) | 0;
      }
      while (carry) {
        digits.push(carry % 58);
        carry = (carry / 58) | 0;
      }
    }
    // convert digits to a string
    let str = '';
    // leading zeros
    for (let k = 0; k < bytes.length && bytes[k] === 0; ++k) str += ALPHABET[0];
    for (let q = digits.length - 1; q >= 0; --q) str += ALPHABET[digits[q]];
    return str;
  }

  // queue system to send to Unity when ready
  const queue = [];
  let unityReady = false;

  window.__kulino_unity_ready = function(){
    LOG('Unity reported ready. queue len=', queue.length);
    unityReady = true;
    while(queue.length){
      const item = queue.shift();
      _sendToUnity(item.method, item.payload);
    }
  };

  function _sendToUnity(method, payloadJson){
    try {
      if (window.unityInstance && typeof window.unityInstance.SendMessage === 'function') {
        LOG('SendMessage', method, payloadJson);
        window.unityInstance.SendMessage('GameManager', method, payloadJson);
      } else {
        console.warn('[KULINO-BRIDGE] unityInstance.SendMessage not available, will queue.');
        queue.push({method, payload: payloadJson});
      }
    } catch(e){
      console.error('[KULINO-BRIDGE] sendToUnity exception', e);
    }
  }

  function sendResultToUnity(obj){
    const json = JSON.stringify(obj);
    if (!unityReady) {
      queue.push({method:'OnClaimResult', payload: json});
      LOG('queued OnClaimResult (len=', queue.length,')');
    } else {
      _sendToUnity('OnClaimResult', json);
    }
  }

  // main function called from Unity: messageJson can be JSON string or object
  window.requestClaimFromUnity = async function(messageJson){
    LOG('requestClaimFromUnity called with:', messageJson);
    try {
      const provider = (window.solana && window.solana.isPhantom) ? window.solana :
                       (window.phantom && window.phantom.solana && window.phantom.solana.isPhantom) ? window.phantom.solana : null;
      if (!provider) {
        LOG('Phantom not found');
        sendResultToUnity({ success:false, error:'phantom_not_installed' });
        return;
      }

      let payload = (typeof messageJson === 'string') ? JSON.parse(messageJson) : messageJson;

      // connect if needed
      if (!provider.isConnected) {
        LOG('Connecting to Phantom...');
        await provider.connect();
        LOG('Connected: ', provider.publicKey?.toString?.());
      }

      payload.address = provider.publicKey.toString();
      // canonical order
      const canonical = {
        address: payload.address,
        gameId: payload.gameId,
        amount: payload.amount,
        nonce: payload.nonce,
        ts: payload.ts
      };
      const messageStr = JSON.stringify(canonical);
      LOG('Message to sign:', messageStr);

      // sign
      const encoded = new TextEncoder().encode(messageStr);
      const signed = await provider.signMessage(encoded, 'utf8'); // Phantom prompt
      LOG('signMessage result:', signed);
      // encode sig to base58
      const sigBase58 = base58Encode(signed.signature);

      // POST to server
      const serverUrl = window.__kulino_server_url || 'http://localhost:3000/api/claim';
      LOG('POST to', serverUrl);
      const resp = await fetch(serverUrl, {
        method: 'POST',
        headers: {'Content-Type':'application/json'},
        body: JSON.stringify({ message: messageStr, signature: sigBase58, publicKey: provider.publicKey.toString() })
      });

      let data;
      try { data = await resp.json(); }
      catch(e){
        const txt = await resp.text().catch(()=>'<no-text>');
        LOG('Server not JSON:', txt);
        sendResultToUnity({ success:false, error:'invalid_server_response', raw: txt });
        return;
      }

      LOG('Server response:', data);
      sendResultToUnity(data);
    } catch (err) {
      LOG('requestClaimFromUnity error', err);
      sendResultToUnity({ success:false, error: String(err && err.message ? err.message : err) });
    }
  };

  // helper for console tests
  window.testClaimFromConsole = async function(){
    const payload = { gameId:'unity-demo', amount:1, nonce: crypto.randomUUID(), ts: Date.now() };
    await window.requestClaimFromUnity(JSON.stringify(payload));
  };

  // small info helper
  window.__kulino_bridge_info = function(){ return { unityReady, queueLen: queue.length, providerDetected: !!(window.solana && window.solana.isPhantom) }; };

  LOG('bridge.js loaded');
})();
// Assets/WebBridge/bridge.js
// Minimal Kulino Bridge for Unity WebGL
// - tidak bergantung CDN (mengandung base58 encoder kecil).
// - Menyediakan window.requestClaimFromUnity dan queue → mengirim hasil ke Unity via SendMessage('GameManager','OnClaimResult', json)

(function(){
  const LOG = (...a)=>console.log('[KULINO-BRIDGE]',...a);

  // --- base58 encode (minimal, works for Uint8Array) ---
  // alphabet sama dengan bs58
  const ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
  function base58Encode(bytes){
    // big-integer style encode
    let digits = [0];
    for (let i = 0; i < bytes.length; ++i) {
      let carry = bytes[i];
      for (let j = 0; j < digits.length; ++j) {
        carry += digits[j] << 8;
        digits[j] = carry % 58;
        carry = (carry / 58) | 0;
      }
      while (carry) {
        digits.push(carry % 58);
        carry = (carry / 58) | 0;
      }
    }
    // convert digits to a string
    let str = '';
    // leading zeros
    for (let k = 0; k < bytes.length && bytes[k] === 0; ++k) str += ALPHABET[0];
    for (let q = digits.length - 1; q >= 0; --q) str += ALPHABET[digits[q]];
    return str;
  }

  // queue system to send to Unity when ready
  const queue = [];
  let unityReady = false;

  window.__kulino_unity_ready = function(){
    LOG('Unity reported ready. queue len=', queue.length);
    unityReady = true;
    while(queue.length){
      const item = queue.shift();
      _sendToUnity(item.method, item.payload);
    }
  };

  function _sendToUnity(method, payloadJson){
    try {
      if (window.unityInstance && typeof window.unityInstance.SendMessage === 'function') {
        LOG('SendMessage', method, payloadJson);
        window.unityInstance.SendMessage('GameManager', method, payloadJson);
      } else {
        console.warn('[KULINO-BRIDGE] unityInstance.SendMessage not available, will queue.');
        queue.push({method, payload: payloadJson});
      }
    } catch(e){
      console.error('[KULINO-BRIDGE] sendToUnity exception', e);
    }
  }

  function sendResultToUnity(obj){
    const json = JSON.stringify(obj);
    if (!unityReady) {
      queue.push({method:'OnClaimResult', payload: json});
      LOG('queued OnClaimResult (len=', queue.length,')');
    } else {
      _sendToUnity('OnClaimResult', json);
    }
  }

  // main function called from Unity: messageJson can be JSON string or object
  window.requestClaimFromUnity = async function(messageJson){
    LOG('requestClaimFromUnity called with:', messageJson);
    try {
      const provider = (window.solana && window.solana.isPhantom) ? window.solana :
                       (window.phantom && window.phantom.solana && window.phantom.solana.isPhantom) ? window.phantom.solana : null;
      if (!provider) {
        LOG('Phantom not found');
        sendResultToUnity({ success:false, error:'phantom_not_installed' });
        return;
      }

      let payload = (typeof messageJson === 'string') ? JSON.parse(messageJson) : messageJson;

      // connect if needed
      if (!provider.isConnected) {
        LOG('Connecting to Phantom...');
        await provider.connect();
        LOG('Connected: ', provider.publicKey?.toString?.());
      }

      payload.address = provider.publicKey.toString();
      // canonical order
      const canonical = {
        address: payload.address,
        gameId: payload.gameId,
        amount: payload.amount,
        nonce: payload.nonce,
        ts: payload.ts
      };
      const messageStr = JSON.stringify(canonical);
      LOG('Message to sign:', messageStr);

      // sign
      const encoded = new TextEncoder().encode(messageStr);
      const signed = await provider.signMessage(encoded, 'utf8'); // Phantom prompt
      LOG('signMessage result:', signed);
      // encode sig to base58
      const sigBase58 = base58Encode(signed.signature);

      // POST to server
      const serverUrl = window.__kulino_server_url || 'http://localhost:3000/api/claim';
      LOG('POST to', serverUrl);
      const resp = await fetch(serverUrl, {
        method: 'POST',
        headers: {'Content-Type':'application/json'},
        body: JSON.stringify({ message: messageStr, signature: sigBase58, publicKey: provider.publicKey.toString() })
      });

      let data;
      try { data = await resp.json(); }
      catch(e){
        const txt = await resp.text().catch(()=>'<no-text>');
        LOG('Server not JSON:', txt);
        sendResultToUnity({ success:false, error:'invalid_server_response', raw: txt });
        return;
      }

      LOG('Server response:', data);
      sendResultToUnity(data);
    } catch (err) {
      LOG('requestClaimFromUnity error', err);
      sendResultToUnity({ success:false, error: String(err && err.message ? err.message : err) });
    }
  };

  // helper for console tests
  window.testClaimFromConsole = async function(){
    const payload = { gameId:'unity-demo', amount:1, nonce: crypto.randomUUID(), ts: Date.now() };
    await window.requestClaimFromUnity(JSON.stringify(payload));
  };

  // small info helper
  window.__kulino_bridge_info = function(){ return { unityReady, queueLen: queue.length, providerDetected: !!(window.solana && window.solana.isPhantom) }; };

  LOG('bridge.js loaded');
})();
