using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance;
    public bool isGameStarted = false;
    private void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;
    }
    void Update()
    {
        if(!IsHost)
            return;
        //if all players are ready, start the game using isReady bool from carController
        //if all players are ready, start the game using isReady bool from carController
        if (!isGameStarted)
        {
        }
    }
    public void StartGame()
    {
        if (!IsHost)
            return;
        isGameStarted = true;
        GameStartedClientRPC();
        Debug.Log("GameStarting");
    }

    [ClientRpc]
    public void GameStartedClientRPC()
    {
        isGameStarted = true;
    }
}
