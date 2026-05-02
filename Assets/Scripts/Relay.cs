using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unity.Collections;
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
using UnityEngine.SceneManagement;

public class Relay : MonoBehaviour
{
    [HideInInspector] public int maxPlayers = 5;
    
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer = 15;
    private string playerName;
    private string KEY_START_GAME = "START_GAME";
    [HideInInspector] public List<string> lobbyIDList = new List<string>();
    [HideInInspector] public List<string> lobbyDetailsList = new List<string>();


    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => { Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);};
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        playerName = "WillDG" + UnityEngine.Random.Range(10, 99);
        Debug.Log(playerName);
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0)
            {
                heartbeatTimer = 15f;

                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                    Debug.Log("Heartbeat sent");
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogError("Heartbeat failed: " + e);
                }
            }
        }
    }


    public bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }


    public async void CreateLobby(string lobbyName)
    {
        try
        {
            CreateLobbyOptions CreateLobbyOptions = new CreateLobbyOptions
            {
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Public, "0") }
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, CreateLobbyOptions);

            hostLobby = lobby;
            joinedLobby = lobby;

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);

            CreateRelay();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByID(string lobbyID)
    {
        try
        {            
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);
            joinedLobby = lobby;


            Debug.Log("Joined Lobby with code " + lobbyID);

            JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
            );
            NetworkManager.Singleton.StartHost();

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                    {
                        {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                    }
            });
        }

        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData
            );
            NetworkManager.Singleton.StartClient();
        }

        catch (RelayServiceException e) { Debug.Log(e); }
    }

    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false,QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            lobbyIDList.Clear();
            lobbyDetailsList.Clear();

            for (int lobbyIndex = 0; lobbyIndex < queryResponse.Results.Count; lobbyIndex++)
            {
                Lobby response = queryResponse.Results[lobbyIndex];
                lobbyIDList.Add(response.Id);
                lobbyDetailsList.Add(response.Name + " " + (response.MaxPlayers - response.AvailableSlots) + "/" + response.MaxPlayers);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
            }
        };
    }

    public async void LeaveGame()
    { 
        try
        {
            string lobbyId = joinedLobby?.Id;

            hostLobby = null;
            joinedLobby = null;

            if (!string.IsNullOrEmpty(lobbyId))
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    lobbyId,
                    AuthenticationService.Instance.PlayerId
                );
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        finally
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

}
