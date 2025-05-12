using UnityEngine;
using Fusion;

[System.Serializable]
public struct PlayerScore : INetworkStruct
{
    public string Username;
    public int Score;

    public NetworkString<_16> NetworkUsername;

    public void CopyFromNetworked()
    {
        Username = NetworkUsername.ToString();
    }
    public void CopyToNetworked()
    {
        NetworkUsername = new NetworkString<_16>(Username);
    }
}