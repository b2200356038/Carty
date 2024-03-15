using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState gameState;
    public static event Action<GameState> OnGameStatesChanged; 

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateGameState(GameState.Start);
        DontDestroyOnLoad(this);
        // Set the frame rate to max frame rate
        
    }

    public void UpdateGameState(GameState newState)
    {
        gameState = newState;
        switch (newState)
        { case GameState.Start:
                break;
            case GameState.CreateLobby:
               break;
           case GameState.InLobby:
               break;
           case GameState.Lobby:
               break;
           case GameState.Game:
               break;
        }
        OnGameStatesChanged?.Invoke(newState);
    }
    public enum GameState
    {
     Start,
     Lobby,
     CreateLobby,
     InLobby,
     Game
    }
}
