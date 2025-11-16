// kulino-payment.js - Phantom Payment Integration
(function() {
    const LOG = (...args) => console.log('[KULINO-PAY]', ...args);
    
    // ⚠️ GANTI DENGAN WALLET KULINO TREASURY YANG SEBENARNYA
    const KULINO_TREASURY = "REAL_WALLET_ADDRESS_HERE";
    const KULINO_COIN_MINT = "2tWC4JAqL4AxEFJxGKjPqPkz8z7w3p7ujd4hRcnHTWfA";

    // ✅ Main function yang dipanggil dari Unity
    window.requestKulinoCoinPayment = async function(payloadJson) {
        LOG('📥 Payment request received:', payloadJson);

        // ✅ Mobile detection
    const isMobile = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    if (isMobile) {
        LOG('📱 Mobile device detected');
        // TODO: Implement Phantom mobile deep link
        // window.location.href = `https://phantom.app/ul/v1/signTransaction?...`;
        return;
    }
        
        try {
            const payload = JSON.parse(payloadJson);
            const { amount, itemId, itemName, nonce, timestamp } = payload;
            
            LOG(`💰 Processing payment: ${amount} KC for ${itemName}`);
            
            // Check Phantom wallet
            const provider = window.solana?.isPhantom ? window.solana : null;
            if (!provider) {
                LOG('❌ Phantom wallet not found');
                sendResultToUnity(false, 'phantom_not_installed', null);
                return;
            }
            
            // Connect wallet jika belum
            if (!provider.isConnected) {
                LOG('🔌 Connecting to Phantom...');
                await provider.connect();
            }
            
            const playerWallet = provider.publicKey.toString();
            LOG(`✓ Player wallet: ${playerWallet.substring(0, 8)}...`);
            
            // Build SPL Token transfer
            LOG('🔨 Building transaction...');
            const transaction = await buildSPLTokenTransfer(
                provider,
                playerWallet,
                KULINO_TREASURY,
                amount,
                KULINO_COIN_MINT
            );
            
            if (!transaction) {
                throw new Error('Failed to build transaction');
            }
            
            // Request signature
            LOG('✍️ Requesting signature from player...');
            const { signature } = await provider.signAndSendTransaction(transaction);
            LOG(`📝 Transaction sent! Signature: ${signature}`);
            
            // Wait for confirmation
            LOG('⏳ Waiting for confirmation...');
            await waitForConfirmation(signature);
            
            LOG('✅✅✅ PAYMENT CONFIRMED!');
            
            // Send success ke Unity
            sendResultToUnity(true, null, signature);
            
        } catch (error) {
            LOG('❌ Payment failed:', error);
            sendResultToUnity(false, error.message, null);
        }
    };
    
    // Build SPL Token transfer transaction
    async function buildSPLTokenTransfer(provider, fromWallet, toWallet, amount, mintAddress) {
        try {
            if (!window.solanaWeb3 || !window.splToken) {
                throw new Error('Solana libraries not loaded');
            }
            
            const { Connection, PublicKey, Transaction } = window.solanaWeb3;
            const { getAssociatedTokenAddress, createTransferInstruction } = window.splToken;
            
            const connection = new Connection('https://api.mainnet-beta.solana.com', 'confirmed');
            
            const fromPubkey = new PublicKey(fromWallet);
            const toPubkey = new PublicKey(toWallet);
            const mintPubkey = new PublicKey(mintAddress);
            
            // Get token accounts
            const fromTokenAccount = await getAssociatedTokenAddress(mintPubkey, fromPubkey);
            const toTokenAccount = await getAssociatedTokenAddress(mintPubkey, toPubkey);
            
            // Convert amount ke smallest unit (6 decimals)
            const amountSmallest = Math.floor(amount * 1_000_000);
            LOG(`Amount: ${amount} KC = ${amountSmallest} smallest units`);
            
            // Build transaction
            const transaction = new Transaction().add(
                createTransferInstruction(
                    fromTokenAccount,
                    toTokenAccount,
                    fromPubkey,
                    amountSmallest
                )
            );
            
            // Get recent blockhash
            const { blockhash } = await connection.getRecentBlockhash();
            transaction.recentBlockhash = blockhash;
            transaction.feePayer = fromPubkey;
            
            LOG('✓ Transaction built successfully');
            return transaction;
            
        } catch (error) {
            LOG('❌ Error building transaction:', error);
            return null;
        }
    }
    
    // Wait for transaction confirmation
    async function waitForConfirmation(signature, maxRetries = 30) {
        const { Connection } = window.solanaWeb3;
        const connection = new Connection('https://api.mainnet-beta.solana.com', 'confirmed');
        
        for (let i = 0; i < maxRetries; i++) {
            try {
                const status = await connection.getSignatureStatus(signature);
                
                if (status?.value?.confirmationStatus === 'confirmed' || 
                    status?.value?.confirmationStatus === 'finalized') {
                    LOG('✓ Transaction confirmed!');
                    return true;
                }
                
                LOG(`⏳ Waiting for confirmation... (${i + 1}/${maxRetries})`);
                await new Promise(resolve => setTimeout(resolve, 1000));
                
            } catch (error) {
                LOG('⚠️ Error checking status:', error);
            }
        }
        
        throw new Error('Transaction confirmation timeout');
    }
    
    // Send result back to Unity
    function sendResultToUnity(success, error, txHash) {
        const result = {
            success: success,
            error: error,
            txHash: txHash,
            timestamp: Date.now()
        };
        
        const json = JSON.stringify(result);
        LOG('📤 Sending result to Unity:', json);
        
        if (window.unityInstance && typeof window.unityInstance.SendMessage === 'function') {
            window.unityInstance.SendMessage('GameManager', 'OnPhantomPaymentResult', json);
            LOG('✓ Result sent to Unity');
        } else {
            LOG('⚠️ Unity instance not available');
        }
    }
    
    LOG('✅ Kulino Payment module loaded');
})();