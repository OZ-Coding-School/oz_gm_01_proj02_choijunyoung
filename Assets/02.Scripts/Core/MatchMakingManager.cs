using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

// [패키지 충돌 방지 구역]
// 충돌이 나는 'Lobby'라는 이름 대신, 'LobbyModel'이라는 별명을 붙여서 사용.
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using LobbyModel = Unity.Services.Lobbies.Models.Lobby;

public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance;

    [Header("Settings")]
    public string gameSceneName = "GameScene";
    public int maxPlayers = 4;
    public string lobbyNamePrefix = "Session_";

    public int minPlayersToStart = 2;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private async void Start()
    {
        // [테스트용] 한 컴퓨터에서 여러 명인 척하기 위해 프로필을 랜덤으로 설정
        // 이렇게 안 하면 4개의 창이 전부 '동일 인물'로 인식되어 로비 접속이 꼬입니다.
        var options = new InitializationOptions();
        options.SetProfile("Player_" + UnityEngine.Random.Range(0, 10000));

        await UnityServices.InitializeAsync(options); // 옵션 넣어서 초기화

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    // [Play 버튼 연결 함수]
    public async void OnPlayButtonClicked()
    {
        Debug.Log("매치메이킹 시작...");

        LobbyModel foundLobby = await FindOpenLobby();

        if (foundLobby != null)
        {
            await JoinExistingLobby(foundLobby);
        }
        else
        {
            await CreateNewLobby();
        }
    }

    // --- [1. 호스트: 방 생성] ---
    private async Task CreateNewLobby()
    {
        try
        {
            // Relay 할당
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 로비 옵션 설정
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = false;
            options.Data = new Dictionary<string, DataObject>()
            {
                { "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
            };

            // 로비 생성 (LobbyModel 사용)
            string lobbyName = lobbyNamePrefix + Guid.NewGuid().ToString().Substring(0, 5);
            LobbyModel lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            // NetworkManager 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // E. 호스트 시작 및 씬 이동
            NetworkManager.Singleton.StartHost();
            Debug.Log($"[Host] 방 생성 완료: {lobbyName}");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;



        }
        catch (Exception e)
        {
            Debug.LogError($"호스트 생성 실패: {e}");
        }
    }

    private void OnClientConnected(ulong clinetId) // 플레이어 접속시 호출되는 메서드
    {
        CheckPlayersAndStart();
    }

    private void CheckPlayersAndStart()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (playerCount >= minPlayersToStart) // 최소 플레이어 수
            {
                Debug.Log("최소 플레이어 수 충족! 게임 시작...");
                // 추가 로직: 게임 시작 알림, 타이머 시작 등
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected; // 중복 호출 방지
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
        }
    }

    // --- [2. 클라이언트: 접속] ---
    // 파라미터도 Lobby 대신 LobbyModel 사용
    private async Task JoinExistingLobby(LobbyModel lobby)
    {
        try
        {
            // A. 로비 접속
            LobbyModel joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joinedLobby.Data["JoinCode"].Value;

            // B. Relay 설정
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            // C. 클라이언트 시작
            NetworkManager.Singleton.StartClient();
            Debug.Log("[Client] 접속 성공! 씬 이동 대기 중...");
        }
        catch (Exception e)
        {
            Debug.LogError($"클라이언트 접속 실패: {e}");
        }
    }

    // --- [3. 빈 방 찾기] ---
    // 반환 타입도 LobbyModel 사용
    private async Task<LobbyModel> FindOpenLobby()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 10;
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            if (response.Results.Count > 0)
            {
                return response.Results[0];
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
        return null;
    }
}