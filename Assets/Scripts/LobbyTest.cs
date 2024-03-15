using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ParrelSync;
using TMPro;
//using ParrelSync;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
public class LobbyTest : MonoBehaviour
{
    public static LobbyTest Instance { get; private set; }
    public const string KEY_PLAYER_NAME = "PlayerName";
    [SerializeField] private TextMeshProUGUI output;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private Transform lobbyTemplate;
    [SerializeField] private Transform playerContainer;
    [SerializeField] private Transform playerTemplate;
    [SerializeField] private TextMeshProUGUI lobbyInfo;
    public Lobby hostLobby, joinedLobby;
    private float heartbeatTimer,lobbyPollTimer=0;
    public string playerName;

    private async void Start()

    {   
        if (ClonesManager.IsClone())
        {
            playerName = "Clone";
        }
        
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);
        await UnityServices.InitializeAsync(initializationOptions);
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed" + AuthenticationService.Instance.PlayerId);
            
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Awake()
    {
        Instance = this;
    }
    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    private void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0)
            {
                float heartbeatTimerMax=15;
                heartbeatTimer = heartbeatTimerMax;
                LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    private async void HandleLobbyPolling() {
        if (joinedLobby != null) {
            if(GameManager.GameState.InLobby!=GameManager.Instance.gameState) return;
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f) {
                lobbyPollTimer = 1.1f;
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                UpdatePlayerList(joinedLobby);
                // OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            }

            if (!IsPlayerInLobby())
            {
                GameManager.Instance.UpdateGameState(GameManager.GameState.Lobby);
                joinedLobby = null;
            }
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
    {
        try
        {
            Player player = GetPlayer();
            CreateLobbyOptions options = new CreateLobbyOptions {
                Player = player,
                IsPrivate = isPrivate,
            };
            Lobby lobby =await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            output.text = "Lobby created: " + lobbyName + ", " + maxPlayers + " " + lobby.LobbyCode;
            hostLobby = lobby;
            joinedLobby = lobby;
            Allocation allocation =await AllocateRelay();
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            string joinRelayCode= await GetRelayJoinCode(allocation);
            UpdateLobbyOptions updateLobbyOptions = new UpdateLobbyOptions();
            updateLobbyOptions.Data = new Dictionary<string, DataObject>() {{"KEY_RELAY_JOIN_CODE", new DataObject(visibility: DataObject.VisibilityOptions.Member, value: joinRelayCode)}};
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id,updateLobbyOptions);
            NetworkManager.Singleton.StartHost();
            UpdatePlayerList(joinedLobby);
            //OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void ListLobbies()
    {
        QueryLobbiesOptions options = new QueryLobbiesOptions();
        options.Count = 8;
        options.Filters = new List<QueryFilter> {
            new QueryFilter(
                field: QueryFilter.FieldOptions.AvailableSlots,
                op: QueryFilter.OpOptions.GT,
                value: "0")
        };
        options.Order = new List<QueryOrder> {
            new QueryOrder(
                asc: false,
                field: QueryOrder.FieldOptions.Created)
        };
        QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();
        output.text = "Lobby List refreshed.";
        List<Lobby> lobbyList=new List<Lobby>();
        foreach (Lobby lobby in lobbyListQueryResponse.Results)
        {
            lobbyList.Add(lobby);
        }
        UpdateLobbyList(lobbyList);
        //OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
    }

    public async void QuickJoinLobby() {
        try {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            //OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            UpdatePlayerList(joinedLobby);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
    
    public async void JoinLobbyById(string lobbyID)
    {
        try
        {
            Player player = GetPlayer();
            joinedLobby=await Lobbies.Instance.JoinLobbyByIdAsync(lobbyID, new JoinLobbyByIdOptions {
                Player = player});
            Debug.Log(joinedLobby.Data);
            string joinRelayCode=joinedLobby.Data["KEY_RELAY_JOIN_CODE"].Value;
            JoinAllocation joinAllocation = await JoinRelay(joinRelayCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
            //OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            UpdatePlayerList(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void JoinLobbyByCode(string joinCode)
    {
        try
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            joinedLobby=await Lobbies.Instance.JoinLobbyByCodeAsync(joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
                GameManager.Instance.UpdateGameState(GameManager.GameState.Lobby);
                //OnLeftLobby?.Invoke(this, EventArgs.Empty);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }
    
    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    public bool IsLobbyHost(string playerId) {
        return joinedLobby != null && joinedLobby.HostId == playerId;
    }
    
    private bool IsPlayerInLobby() {
        if (joinedLobby != null && joinedLobby.Players != null) {
            foreach (Player player in joinedLobby.Players) {
                if (player.Id == AuthenticationService.Instance.PlayerId) {
                    return true;
                }
            }
        }
        return false;
    }
    
    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public void UpdateLobbyList(List<Lobby> lobbyList)
    {
        foreach (Transform child in lobbyContainer)
        {
            if(child==lobbyTemplate) continue;
            Destroy(child.gameObject);
        }
        foreach (Lobby lobby in lobbyList )
        {
            Transform lobbyTransform = Instantiate(lobbyTemplate,lobbyContainer);
            lobbyTransform.gameObject.SetActive(true);
            lobbyTransform.GetComponent<LobbyTemplate>().SetLobby(lobby);
        }
    }
    
    public void UpdatePlayerList(Lobby lobby)
    {
        foreach (Transform child in playerContainer)
        {
            if(child==playerTemplate) continue;
            Destroy(child.gameObject);
        }
        foreach (Player player in lobby.Players )
        {
            Transform lobbyTransform = Instantiate(playerTemplate,playerContainer);
            lobbyTransform.gameObject.SetActive(true);
            lobbyTransform.GetComponent<PlayerTemplate>().UpdatePlayer(player);
        }
        lobbyInfo.text = joinedLobby.Name +" "+ joinedLobby.Players.Count + "/" + joinedLobby.MaxPlayers;
    }
    
    private Player GetPlayer() {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName)}
        });
    }
    
    private async Task<Allocation> AllocateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(joinedLobby.MaxPlayers);
            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            return await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return default;
        }
        
    }
    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            return await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return default;
        }
        
    }
    
}
