using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private Canvas lobbyCanvas, gameCanvas;
    [SerializeField] private Image speedVisualizer, nitroVisualizer;
    [SerializeField] private GameObject startUI, lobbyUI, createLobbyUI, inLobbyUI;
    [SerializeField] private Button playButton,createLobbyUIButton,createLobbyButton,refreshLobbyButton, leaveLobbyButton, startGameButton;
    [SerializeField] private TMP_InputField playerNameInputField,lobbyNameInputField,lobbyMaxPlayerInputField;
    [SerializeField] private Toggle isPrivateToggle;
    [SerializeField] private TextMeshProUGUI speedometerText;
    string lobbyName ="My room";
    int maxPlayers = 5;
    private void Awake()
    {
        GameManager.OnGameStatesChanged += GameManagerOnStateChanged;
        Instance = this;
    }

    private void OnDestroy()
    {
        GameManager.OnGameStatesChanged -= GameManagerOnStateChanged;
    }

    private void GameManagerOnStateChanged(GameManager.GameState gameState)
    {
        startUI.SetActive(gameState == GameManager.GameState.Start);
        lobbyUI.SetActive(gameState == GameManager.GameState.Lobby);
        playButton.interactable=(gameState == GameManager.GameState.Start);
        createLobbyUI.SetActive(gameState == GameManager.GameState.CreateLobby);
        inLobbyUI.SetActive(gameState == GameManager.GameState.InLobby);
    }
    //update speedometer
    public void UpdateSpeedometer(int speed,float normalizedSpeed)
    {
        speedometerText.text = speed + " KM/H";
        speedVisualizer.fillAmount = normalizedSpeed;
        
    }
    //update nitro meter
    public void UpdateNitroVisualizer(float normalizedNitro)
    {
        nitroVisualizer.fillAmount = normalizedNitro;
    }
    void Start()
    {
        if (PlayerPrefs.HasKey("playerName"))
        {
            Debug.Log("Player name found");
            LobbyTest.Instance.playerName = PlayerPrefs.GetString("playerName");
            GameManager.Instance.UpdateGameState(GameManager.GameState.Lobby);
        }
        else
        {
            playButton.onClick.AddListener(() =>
            {
                //try to get the player name from device data
                if (playerNameInputField.text.Length == 0)
                {
                    LobbyTest.Instance.playerName="Guest"+UnityEngine.Random.Range(0, 1000);
                }
                else
                {
                    LobbyTest.Instance.playerName = playerNameInputField.text;
                    PlayerPrefs.SetString("playerName", LobbyTest.Instance.playerName);
                }
                GameManager.Instance.UpdateGameState(GameManager.GameState.Lobby);
            });
        }
       
        
        createLobbyUIButton.onClick.AddListener(() => GameManager.Instance.UpdateGameState(GameManager.GameState.CreateLobby));
        createLobbyButton.onClick.AddListener(() =>
        {
            LobbyTest.Instance.CreateLobby(lobbyName, maxPlayers, 
                isPrivateToggle.isOn);
            GameManager.Instance.UpdateGameState(GameManager.GameState.InLobby);
        });
        
        lobbyNameInputField.onValueChanged.AddListener(delegate {lobbyName=lobbyNameInputField.text; });
        lobbyMaxPlayerInputField.onValueChanged.AddListener(delegate {maxPlayers=int.Parse(lobbyMaxPlayerInputField.text); });
        
        refreshLobbyButton.onClick.AddListener(() => LobbyTest.Instance.ListLobbies());
        leaveLobbyButton.onClick.AddListener((() => LobbyTest.Instance.LeaveLobby()));
        startGameButton.onClick.AddListener(()=>
        {
            lobbyCanvas.enabled = false;
            gameCanvas.enabled = true;
            GameManager.Instance.UpdateGameState(GameManager.GameState.Game);
        });
    }
}
