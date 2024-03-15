using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

public class PlayerTemplate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Button kickButton;
    [SerializeField] private GameObject hostImage;
    public bool isHost = false;
    private Player player;

    private void Awake()
    {
        kickButton.onClick.AddListener(()=>LobbyTest.Instance.KickPlayer(player.Id));
    }

    public void UpdatePlayer(Player player) {
        this.player = player;
        kickButton.gameObject.SetActive(LobbyTest.Instance.IsLobbyHost()&& player.Id!=AuthenticationService.Instance.PlayerId);
        if (LobbyTest.Instance.IsLobbyHost(player.Id))
            hostImage.SetActive(true);
        playerNameText.text = player.Data[LobbyTest.KEY_PLAYER_NAME].Value;
    }
}
