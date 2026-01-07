using System;
using System.Collections;      
using System.Threading.Tasks;   
using Unity.Netcode;            
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer; 
using UnityEngine;

public class MatchMaking : MonoBehaviour
{
    const string gameSceneName = "GameScene";
    private string _profileName;
    private string _sessionName = "blank";
    private int _maxPlayers = 4;
    private ConnectionState _state = ConnectionState.Disconnected;
    private ISession _session;
    private NetworkManager m_NetworkManager;
    public CreateSessionByGoogle createSessionByGoogle;

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
            Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s.");
        }
    }

    public void OnPlayButtonClicked()
    {
        createSessionByGoogle.RequestSessionId((id, count) =>
        {
            SetSessionInfo(id, count);
        });
    }

    private void OnDestroy()
    {
        _session?.LeaveAsync();
    }

    public void SetSessionInfo(string id, int currentCount)
    {
        _sessionName = id;

        if (currentCount >= 2)
        {
            Debug.Log($"현재 인원 {currentCount}명. 접속");
            CreateOrJoinSessionAsync();
        }
        else
        {
            Debug.Log($"현재 인원 {currentCount}명. 상대방을 기다립니다.");
            StartCoroutine(WaitForOpponent(id));
        }
    }

    private IEnumerator WaitForOpponent(string id)
    {
        bool isReady = false;

        while (!isReady)
        {
            // 2초마다 확인
            yield return new WaitForSeconds(2.0f);

            // 상태 체크 요청
            createSessionByGoogle.CheckSessionCount(id, (count) =>
            {
                Debug.Log($"대기 중... 현재 인원: {count}");
                if (count >= 2)
                {
                    isReady = true;
                }
            });

            // 콜백 완료 대기
            yield return null;
        }

        Debug.Log("세션에 접속합니다.");
        CreateOrJoinSessionAsync();
    }

    private async Task CreateOrJoinSessionAsync()
    {
        _state = ConnectionState.Connecting;
        _profileName = FirebaseAuthManager.Instance.UserId;
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
            if (m_NetworkManager.IsServer)
            {
                var status = m_NetworkManager.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning($"씬 로드 실패: {status}");
                }
            }

        }
        catch (Exception e)
        {
            _state = ConnectionState.Disconnected;
            Debug.LogException(e);
        }
    }
}
