// WebGLPayment.jslib
// Place this in: Assets/Plugins/WebGL/WebGLPayment.jslib

mergeInto(LibraryManager.library, {
    // ✅ Function untuk request Rupiah payment
    requestRupiahPayment: function(jsonPtr) {
        const jsonStr = UTF8ToString(jsonPtr);
        const payload = JSON.parse(jsonStr);
        
        console.log('[WebGL] 💰 Rupiah Payment Request:', payload);
        console.log('  Destination:', payload.destinationWallet);
        console.log('  KC Amount:', payload.kcAmount);
        console.log('  Rupiah Value:', payload.rupiahAmount);
        
        // ✅ Request payment via Phantom Wallet
        if (window.solana && window.solana.isPhantom) {
            (async () => {
                try {
                    // Connect wallet if needed
                    if (!window.solana.isConnected) {
                        await window.solana.connect();
                    }
                    
                    const walletPublicKey = window.solana.publicKey.toString();
                    console.log('[WebGL] Wallet:', walletPublicKey);
                    
                    // Create transaction
                    const connection = new solanaWeb3.Connection(
                        solanaWeb3.clusterApiUrl('mainnet-beta'),
                        'confirmed'
                    );
                    
                    const transaction = new solanaWeb3.Transaction();
                    
                    // Add transfer instruction
                    transaction.add(
                        solanaWeb3.SystemProgram.transfer({
                            fromPubkey: window.solana.publicKey,
                            toPubkey: new solanaWeb3.PublicKey(payload.destinationWallet),
                            lamports: Math.floor(payload.kcAmount * 1000000000) // Convert to lamports
                        })
                    );
                    
                    // Get recent blockhash
                    const { blockhash } = await connection.getLatestBlockhash();
                    transaction.recentBlockhash = blockhash;
                    transaction.feePayer = window.solana.publicKey;
                    
                    // Request signature from Phantom
                    const signed = await window.solana.signAndSendTransaction(transaction);
                    
                    console.log('[WebGL] ✅ Transaction sent:', signed.signature);
                    
                    // Wait for confirmation
                    await connection.confirmTransaction(signed.signature);
                    
                    console.log('[WebGL] ✅ Transaction confirmed!');
                    
                    // Send result back to Unity
                    const result = {
                        success: true,
                        txHash: signed.signature,
                        kcAmount: payload.kcAmount,
                        rupiahAmount: payload.rupiahAmount
                    };
                    
                    gameInstance.SendMessage('GameManager', 'OnRupiahPaymentResult', JSON.stringify(result));
                    
                } catch (error) {
                    console.error('[WebGL] ❌ Payment failed:', error);
                    
                    const result = {
                        success: false,
                        error: error.message || 'Payment failed',
                        txHash: null
                    };
                    
                    gameInstance.SendMessage('GameManager', 'OnRupiahPaymentResult', JSON.stringify(result));
                }
            })();
        } else {
            console.error('[WebGL] ❌ Phantom Wallet not found!');
            
            const result = {
                success: false,
                error: 'Phantom Wallet not installed',
                txHash: null
            };
            
            gameInstance.SendMessage('GameManager', 'OnRupiahPaymentResult', JSON.stringify(result));
        }
    }
});