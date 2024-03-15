using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTemplate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyName;
    private Lobby lobby;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            LobbyTest.Instance.JoinLobbyById(lobby.Id);
            GameManager.Instance.UpdateGameState(GameManager.GameState.InLobby);
        });
    }

    public void SetLobby(Lobby lobby)
    {
        this.lobby = lobby;
        lobbyName.text = lobby.Name;
    }
}
