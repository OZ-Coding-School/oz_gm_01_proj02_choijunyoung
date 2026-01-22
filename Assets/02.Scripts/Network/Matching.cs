using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Matching : MonoBehaviour
{
    private int _maxPlayers = 4;
    private ConnectionState _state = ConnectionState.Disconnected;
    private ISession _session;
    private NetworkManager m_NetworkManager;
    public CreateSessionByGoogle sessionFetcher;
    private const string gameSceneName = "GameScene";

    [Header("MatchMaking UI Text")]
    [SerializeField] TextMeshProUGUI matchTimer;
    [SerializeField] TextMeshProUGUI matchTxt;

    private bool _isCancelling = false; 
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
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"이미 signed in 상태");
            }
            else
            {
                // signed in 아니면 프로필 스위치 + 로그인
                if (!string.IsNullOrEmpty(_profileName))
                {
                    AuthenticationService.Instance.SwitchProfile(_profileName);  // 또는 await auth.SwitchProfileAsync(profileName);
                    Debug.Log($"프로필 스위치: {_profileName}");
                }

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("익명 로그인 완료");
            }

            //AuthenticationService.Instance.SwitchProfile(_profileName);
            //await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var options = new SessionOptions()
            {
                Name = _sessionName,
                MaxPlayers = _maxPlayers
            }.WithDistributedAuthorityNetwork();

            _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(_sessionName, options);

            _state = ConnectionState.Connected;

            
        }
        catch (AuthenticationException authEx)
        {
            _state = ConnectionState.Disconnected;
            Debug.LogError($"Authentication 에러: {authEx.Message} (코드: {authEx.ErrorCode})");

            // 이미 signed in 관련 에러면 무시하거나 UI 처리
            if (authEx.ErrorCode == AuthenticationErrorCodes.ClientInvalidUserState)
            {
                Debug.LogWarning("이미 로그인 상태 → 그대로 진행 시도");
                // 필요하면 여기서 강제 세션 생성 재시도 or UI 알림
            }
        }
        catch (Exception e)
        {
            _state = ConnectionState.Disconnected;
            Debug.LogException(e);

            _isCancelling = true;

            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
            {
                Debug.LogWarning("NetworkManager 이미 연결됨 → 강제 Shutdown");
                NetworkManager.Singleton.Shutdown();  // 이게 핵심! 이전 연결 끊기
            }

            if (e.Message.Contains("already a member of the lobby"))
            {
                if (_session != null)
                {
                    await _session.LeaveAsync();
                    _session = null;
                }
            }
        }
    }

    public void OnClickPlay()
    {
        float timer = 0;
        
        StartCoroutine( MatchTimer(timer));
        sessionFetcher.FetchSessionId(async (sessionId) =>
        {
            if (sessionId != null)
            {
                //GameManager.SettingManager.SetAmmoPool(FirebaseAuthManager.Instance.UserId);
                GameManager.SettingManager.SetUserData(FirebaseAuthManager.Instance.UserId);
                Debug.Log($"★★★ 성공! 세션 ID: {sessionId} ★★★");
                Debug.Log($"아이디 : {FirebaseAuthManager.Instance.UserId} / 세션 : {sessionId}");
                await CreateOrJoinSessionAsync(FirebaseAuthManager.Instance.UserId, sessionId);
            }
            else
            {
                Debug.LogError("세션 ID 가져오기 실패");
            }
        });
        
    }

    IEnumerator MatchTimer(float timer)
    {
        matchTimer.gameObject.SetActive(true);
        matchTxt.gameObject.SetActive(true);
        float maxWaitTime = 20f;

        while (_state != ConnectionState.Connected && timer < maxWaitTime && !_isCancelling)
        {
            timer += Time.deltaTime;
            string timeStr;
            timeStr = string.Format("{0:D2}:{1:D2}", (int)(timer / 60), (int)(timer % 60));
            matchTimer.text = timeStr;
            yield return null;
        }
        matchTimer.gameObject.SetActive(false);
        matchTxt.gameObject.SetActive(false);

        if (_state != ConnectionState.Connected)
        {
            _state = ConnectionState.Disconnected;
            _isCancelling = false;

            matchTxt.text = "Matching Failed";
            matchTxt.gameObject.SetActive(true);

            if (_session != null)
            {
                _session.LeaveAsync().GetAwaiter().GetResult();
                _session = null;
            }


        }
    }
}
