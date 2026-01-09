using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Matching : MonoBehaviour
{
    private int _maxPlayers = 10;
    private ConnectionState _state = ConnectionState.Disconnected;
    private ISession _session;
    private NetworkManager m_NetworkManager;
    public CreateSessionByGoogle sessionFetcher;
    private const string gameSceneName = "GameScene";

    private enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    private async void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        m_NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        m_NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        await UnityServices.InitializeAsync();
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (m_NetworkManager.LocalClient.IsSessionOwner)
        {
            Debug.Log($"Client-{m_NetworkManager.LocalClientId} is the session owner!");
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (m_NetworkManager.LocalClientId == clientId)
        {
            var status = m_NetworkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.Log("씬 로드 실패 : " + status);
            }
        }
        else Debug.Log($"참가자(ID : {clientId})가 접속했습니다.");
       
    }

    private void OnDestroy()
    {
        _session?.LeaveAsync();
    }

    private async Task CreateOrJoinSessionAsync(string _profileName, string _sessionName)
    {
        _state = ConnectionState.Connecting;
        try
        {
            AuthenticationService.Instance.SwitchProfile(_profileName);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var options = new SessionOptions()
            {
                Name = _sessionName,
                MaxPlayers = _maxPlayers
            }.WithDistributedAuthorityNetwork();

            _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(_sessionName, options);

            _state = ConnectionState.Connected;
        }
        catch (Exception e)
        {
            _state = ConnectionState.Disconnected;
            Debug.LogException(e);
        }
    }

    public void OnClickPlay()
    {
        sessionFetcher.FetchSessionId((sessionId) =>
        {
            if (sessionId != null)
            {
                Debug.Log($"★★★ 성공! 세션 ID: {sessionId} ★★★");
                Debug.Log($"아이디 : {FirebaseAuthManager.Instance.UserId} / 세션 : {sessionId}"); 
                CreateOrJoinSessionAsync(FirebaseAuthManager.Instance.UserId, sessionId);
            }
            else
            {
                Debug.LogError("세션 ID 가져오기 실패");
            }
        });
        
    }
}
