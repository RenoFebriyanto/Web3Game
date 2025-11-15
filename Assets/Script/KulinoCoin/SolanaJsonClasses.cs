using System;
using UnityEngine;

/// <summary>
/// JSON Helper Classes untuk Solana RPC Response
/// Sesuai dengan response format dari getTokenAccountsByOwner
/// </summary>

[Serializable]
public class SolanaRpcResponse
{
    public string jsonrpc;
    public SolanaResult result;
    public int id;
}

[Serializable]
public class SolanaResult
{
    public SolanaContext context;
    public TokenAccountValue[] value;
}

[Serializable]
public class SolanaContext
{
    public int slot;
}

[Serializable]
public class TokenAccountValue
{
    public string pubkey;
    public TokenAccountData account;
}

[Serializable]
public class TokenAccountData
{
    public ParsedData data;
    public bool executable;
    public long lamports;
    public string owner;
    public long rentEpoch;
}

[Serializable]
public class ParsedData
{
    public string program;
    public ParsedInfo parsed;
    public int space;
}

[Serializable]
public class ParsedInfo
{
    public TokenInfo info;
    public string type;
}

[Serializable]
public class TokenInfo
{
    public bool isNative;
    public string mint;
    public string owner;
    public string state;
    public TokenAmount tokenAmount;
}

[Serializable]
public class TokenAmount
{
    public string amount;          // Raw amount (string)
    public int decimals;            // Token decimals
    public string uiAmount;         // Human-readable amount (null jika 0)
    public double uiAmountString;   // Human-readable amount (string)
}