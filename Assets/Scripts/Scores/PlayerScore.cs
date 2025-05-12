using UnityEngine;
using System;
using Fusion;

[Serializable]
public struct PlayerScore : INetworkStruct
{
    // Use NetworkString<_16> instead of string
    public NetworkString<_16> Username;
    public int Score;
    
    public string UsernameString 
    {
        get => Username.ToString();
        set => Username = new NetworkString<_16>(value);
    }
    
    // Add these empty implementation methods for PlayerSpawner compatibility
    public void CopyToNetworked() 
    {
        // NetworkString is already networked, nothing to do
    }
    
    public void CopyFromNetworked() 
    {
        // NetworkString is already networked, nothing to do
    }
    
    public override string ToString()
    {
        return $"{UsernameString}: {Score}";
    }
}