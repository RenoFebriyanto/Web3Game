/**
 * ✅ Phantom Payment Bridge for Unity WebGL
 * Handle Kulino Coin payment transactions
 * 
 * USAGE:
 * 1. Include this script in your WebGL template
 * 2. Unity calls: requestKulinoCoinPayment(jsonPayload)
 * 3. Result sent back via: unityInstance.SendMessage('GameManager', 'OnPhantomPaymentResult', jsonResult)
 */

(function() {
  const LOG = (...args) => console.log('[PHANTOM-PAYMENT]', ...args);
  const ERROR = (...args) => console.error('[PHANTOM-PAYMENT]', ...args);

  // ==================== CONFIGURATION ====================
  const KULINO_MINT = "E5chNtjGFvCMVYoTwcP9DtrdMdctRCGdGahAAhnHbHc1";
  const COMPANY_WALLET = "9QM8aSCHFp76RacWXgXFQQxUKXt5Vf2zLzSkdMdQuByk";

  // Check if Solana Web3 is loaded
  if (typeof solanaWeb3 === 'undefined') {
    ERROR('Solana Web3.js not loaded! Please include it in your HTML.');
  }

  // ==================== MAIN FUNCTION ====================
  
  /**
   * Request Kulino Coin payment via Phantom
   * @param {string} payloadJson - JSON string containing payment details
   */
  window.requestKulinoCoinPayment = async function(payloadJson) {
    LOG('=== PAYMENT REQUEST ===');
    LOG('Payload:', payloadJson);

    try {
      // Parse payload
      const payload = typeof payloadJson === 'string' 
        ? JSON.parse(payloadJson) 
        : payloadJson;

      LOG('Parsed payload:', payload);

      // Validate payload
      if (!payload.recipientAddress || !payload.amount) {
        throw new Error('Invalid payload: missing recipientAddress or amount');
      }

      // Get Phantom provider
      const provider = getPhantomProvider();
      if (!provider) {
        sendResultToUnity({
          success: false,
          error: 'phantom_not_found',
          message: 'Phantom wallet not detected'
        });
        return;
      }

      // Connect if needed
      if (!provider.isConnected) {
        LOG('Connecting to Phantom...');
        await provider.connect();
      }

      const senderPublicKey = provider.publicKey;
      LOG('Sender:', senderPublicKey.toString());
      LOG('Recipient:', payload.recipientAddress);
      LOG('Amount:', payload.amount, 'KC');

      // Create connection
      const connection = new solanaWeb3.Connection(
        'https://api.mainnet-beta.solana.com',
        'confirmed'
      );

      // Convert amount to raw units (6 decimals for Kulino Coin)
      const decimals = 6;
      const amountRaw = Math.floor(payload.amount * Math.pow(10, decimals));
      LOG('Amount (raw):', amountRaw);

      // Get mint and token accounts
      const mintPubkey = new solanaWeb3.PublicKey(payload.mintAddress || KULINO_MINT);
      const recipientPubkey = new solanaWeb3.PublicKey(payload.recipientAddress);

      // Get associated token addresses
      const senderATA = await getAssociatedTokenAddress(
        mintPubkey,
        senderPublicKey
      );

      const recipientATA = await getAssociatedTokenAddress(
        mintPubkey,
        recipientPubkey
      );

      LOG('Sender ATA:', senderATA.toString());
      LOG('Recipient ATA:', recipientATA.toString());

      // Build transaction
      const transaction = new solanaWeb3.Transaction();

      // Check if recipient ATA exists
      const recipientAccount = await connection.getAccountInfo(recipientATA);
      if (!recipientAccount) {
        LOG('Creating recipient ATA...');
        
        // Add create ATA instruction
        const createATAIx = createAssociatedTokenAccountInstruction(
          senderPublicKey,
          recipientATA,
          recipientPubkey,
          mintPubkey
        );
        transaction.add(createATAIx);
      }

      // Add transfer instruction
      const transferIx = createTransferCheckedInstruction(
        senderATA,
        mintPubkey,
        recipientATA,
        senderPublicKey,
        amountRaw,
        decimals
      );
      transaction.add(transferIx);

      // Get recent blockhash
      const { blockhash } = await connection.getLatestBlockhash();
      transaction.recentBlockhash = blockhash;
      transaction.feePayer = senderPublicKey;

      LOG('Transaction built, requesting signature...');

      // Sign and send via Phantom
      const signedTx = await provider.signAndSendTransaction(transaction);
      LOG('Transaction sent:', signedTx.signature);

      // Wait for confirmation
      LOG('Waiting for confirmation...');
      const confirmation = await connection.confirmTransaction(
        signedTx.signature,
        'confirmed'
      );

      if (confirmation.value.err) {
        throw new Error('Transaction failed: ' + JSON.stringify(confirmation.value.err));
      }

      LOG('✅ Transaction confirmed!');
      LOG('Signature:', signedTx.signature);

      // Send success to Unity
      sendResultToUnity({
        success: true,
        txHash: signedTx.signature,
        amount: payload.amount,
        recipient: payload.recipientAddress,
        itemId: payload.itemId,
        itemName: payload.itemName,
        rewardAmount: payload.rewardAmount
      });

    } catch (error) {
      ERROR('Payment error:', error);
      
      sendResultToUnity({
        success: false,
        error: error.code || 'transaction_failed',
        message: error.message || String(error)
      });
    }
  };

  // ==================== HELPER FUNCTIONS ====================

  function getPhantomProvider() {
    if (window.solana && window.solana.isPhantom) {
      return window.solana;
    }
    
    if (window.phantom && window.phantom.solana && window.phantom.solana.isPhantom) {
      return window.phantom.solana;
    }
    
    return null;
  }

  function sendResultToUnity(result) {
    const json = JSON.stringify(result);
    LOG('Sending result to Unity:', json);

    try {
      if (window.unityInstance && typeof window.unityInstance.SendMessage === 'function') {
        window.unityInstance.SendMessage('GameManager', 'OnPhantomPaymentResult', json);
        LOG('✓ Result sent to Unity');
      } else {
        ERROR('Unity instance not available');
      }
    } catch (e) {
      ERROR('Failed to send to Unity:', e);
    }
  }

  // ==================== SPL TOKEN HELPERS ====================

  async function getAssociatedTokenAddress(mint, owner) {
    const [address] = await solanaWeb3.PublicKey.findProgramAddress(
      [
        owner.toBuffer(),
        TOKEN_PROGRAM_ID.toBuffer(),
        mint.toBuffer(),
      ],
      ASSOCIATED_TOKEN_PROGRAM_ID
    );
    return address;
  }

  function createAssociatedTokenAccountInstruction(
    payer,
    associatedToken,
    owner,
    mint
  ) {
    const keys = [
      { pubkey: payer, isSigner: true, isWritable: true },
      { pubkey: associatedToken, isSigner: false, isWritable: true },
      { pubkey: owner, isSigner: false, isWritable: false },
      { pubkey: mint, isSigner: false, isWritable: false },
      { pubkey: solanaWeb3.SystemProgram.programId, isSigner: false, isWritable: false },
      { pubkey: TOKEN_PROGRAM_ID, isSigner: false, isWritable: false },
    ];

    return new solanaWeb3.TransactionInstruction({
      keys,
      programId: ASSOCIATED_TOKEN_PROGRAM_ID,
      data: Buffer.alloc(0),
    });
  }

  function createTransferCheckedInstruction(
    source,
    mint,
    destination,
    owner,
    amount,
    decimals
  ) {
    const dataLayout = BufferLayout.struct([
      BufferLayout.u8('instruction'),
      BufferLayout.blob(8, 'amount'),
      BufferLayout.u8('decimals'),
    ]);

    const data = Buffer.alloc(dataLayout.span);
    dataLayout.encode(
      {
        instruction: 12, // TransferChecked instruction
        amount: BigInt(amount),
        decimals,
      },
      data
    );

    const keys = [
      { pubkey: source, isSigner: false, isWritable: true },
      { pubkey: mint, isSigner: false, isWritable: false },
      { pubkey: destination, isSigner: false, isWritable: true },
      { pubkey: owner, isSigner: true, isWritable: false },
    ];

    return new solanaWeb3.TransactionInstruction({
      keys,
      programId: TOKEN_PROGRAM_ID,
      data,
    });
  }

  // Program IDs
  const TOKEN_PROGRAM_ID = new solanaWeb3.PublicKey(
    'TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA'
  );

  const ASSOCIATED_TOKEN_PROGRAM_ID = new solanaWeb3.PublicKey(
    'ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL'
  );

  // ==================== TEST FUNCTION ====================

  /**
   * Test payment function (untuk console testing)
   */
  window.testKulinoCoinPayment = async function() {
    const testPayload = {
      recipientAddress: COMPANY_WALLET,
      amount: 0.01, // 0.01 KC for testing
      mintAddress: KULINO_MINT,
      itemId: 'shard_100',
      itemName: 'Test Shard',
      rewardAmount: 100,
      rewardType: 'shard',
      nonce: Date.now().toString(),
      timestamp: Math.floor(Date.now() / 1000)
    };

    await window.requestKulinoCoinPayment(JSON.stringify(testPayload));
  };

  LOG('✅ Phantom Payment Bridge loaded');
  LOG('Company Wallet:', COMPANY_WALLET);
  LOG('Mint Address:', KULINO_MINT);
  LOG('Test function: window.testKulinoCoinPayment()');

})();