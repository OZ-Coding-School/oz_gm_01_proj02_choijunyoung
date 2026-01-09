using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

public class ConnectionManager : MonoBehaviour
{
    private string _profileName;
    private string _sessionName;
    private int _maxPlayers = 10;
    private ConnectionState _state = ConnectionState.Disconnected;
    private ISession _session;
    private NetworkManager m_NetworkManager;

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

    private void OnGUI()
    {
        if (_state == ConnectionState.Connected)
            return;

        GUI.enabled = _state != ConnectionState.Connecting;

        using (new GUILayout.HorizontalScope(GUILayout.Width(250)))
        {
            GUILayout.Label("Profile Name", GUILayout.Width(100));
            _profileName = GUILayout.TextField(_profileName);
        }

        using (new GUILayout.HorizontalScope(GUILayout.Width(250)))
        {
            GUILayout.Label("Session Name", GUILayout.Width(100));
            _sessionName = GUILayout.TextField(_sessionName);
        }

        GUI.enabled = GUI.enabled && !string.IsNullOrEmpty(_profileName) && !string.IsNullOrEmpty(_sessionName);

        if (GUILayout.Button("Create or Join Session"))
        {
            CreateOrJoinSessionAsync();
        }
    }

    private void OnDestroy()
    {
        _session?.LeaveAsync();
    }

    private async Task CreateOrJoinSessionAsync()
    {
        _state = ConnectionState.Connecting;
        _profileName = FirebaseAuthManager.Instance.UserId;  // 사용자 ID를 프로필 이름으로 설정
        _sessionName = GetRandomId(); // 랜덤 세션 이름 생성

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

    public string GetRandomId(int length = 5)
    {
        // 1. 사용할 문자 집합 정의 (소문자 + 숫자)
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        // 2. 결과물을 담을 문자 배열 생성
        char[] stringChars = new char[length];

        // 3. 길이만큼 반복하며 랜덤 문자 뽑기
        for (int i = 0; i < length; i++)
        {
            // UnityEngine.Random을 사용하여 인덱스 랜덤 선택
            int randomIndex = UnityEngine.Random.Range(0, chars.Length);
            stringChars[i] = chars[randomIndex];
        }

        // 4. 문자 배열을 문자열로 변환하여 반환
        return new string(stringChars);
    }
}