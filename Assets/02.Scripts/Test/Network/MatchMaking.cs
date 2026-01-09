//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Threading.Tasks;   
//using Unity.Netcode;            
//using Unity.Services.Authentication;
//using Unity.Services.Core;
//using Unity.Services.Multiplayer; 
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class MatchMaking : MonoBehaviour
//{
//    const string gameSceneName = "GameScene";
//    public GameObject playerPrefab;
//    private string _profileName;
//    private string _sessionName = "blank";
//    private int _maxPlayers = 4;

//    private ISession _session;
//    private NetworkManager m_NetworkManager;
//    public CreateSessionByGoogle createSessionByGoogle;

//    private bool _isConnected = false;

//    private enum ConnectionState
//    {
//        Disconnected,
//        Connecting,
//        Connected,
//    }

//    private async void Awake()
//    {
//        m_NetworkManager = GetComponent<NetworkManager>();
//        try
//        {
//            await UnityServices.InitializeAsync();
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//        }

//    }

//    private void Start()
//    {
//        if(m_NetworkManager == null) m_NetworkManager = NetworkManager.Singleton;
        
//        if(m_NetworkManager != null)
//        {
//            m_NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;

//            if (m_NetworkManager.SceneManager != null) m_NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
//        }
        
//    }


//    private void OnClientConnectedCallback(ulong clientId)
//    {
//        if(clientId == m_NetworkManager.LocalClientId)
//        {
//            var status = m_NetworkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
//            if (status != SceneEventProgressStatus.Started)
//            {
//                Debug.Log("씬 로드 실패 : " + status);
//            }
//        }
//        else Debug.Log($"참가자(ID : {clientId})가 접속했습니다."); 

       
        
//    }

//    private void OnLoadEventCompleted(string sceneName, LoadSceneMode LSM, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
//    {
//        if(sceneName == gameSceneName)
//        {
//            foreach(ulong clientId in clientsCompleted)
//            {
//                if (m_NetworkManager.ConnectedClients.ContainsKey(clientId) && m_NetworkManager.ConnectedClients[clientId].PlayerObject == null)
//                {
//                    GameObject inst = Instantiate(playerPrefab);

//                    inst.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
//                }
//            }
//        }
//    }


//    public void OnPlayButtonClicked()
//    {
//        createSessionByGoogle.RequestSessionId((id, count, isCreator) =>
//        {
//            ConnectToSession(id, isCreator);
//        });
//    }


//    private void OnDestroy()
//    {
//        _session?.LeaveAsync();
//    }

//    public async void ConnectToSession(string id, bool isCreator)
//    {
//        _sessionName = id;
//        Debug.Log($"세션 ID: {_sessionName}, 세션에 접속합니다. (생성자: {isCreator})");
        
//        if (isCreator)
//        {
//            await CreateSessionRoutine();
//        }
//        else
//        {
//            await JoinSessionRoutine();
//        }
//    }

//    private async Task JoinSessionRoutine()
//    {
//        if(_isConnected) return;
//        _profileName = FirebaseAuthManager.Instance.UserId;

//        try
//        {
//            if (!AuthenticationService.Instance.IsSignedIn)
//            {
//                AuthenticationService.Instance.SwitchProfile(_profileName);
//                await AuthenticationService.Instance.SignInAnonymouslyAsync();
//            }
//            var options = new SessionOptions()
//            {
//                Name = _sessionName,
//                MaxPlayers = _maxPlayers
//            }.WithDistributedAuthorityNetwork();

//            _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(_sessionName, options);

//            _isConnected = true;
//            Debug.Log("방 참가 완료(방이름 : " + _sessionName + ")");
//        }
//        catch (SessionException e)
//        {
//            Debug.LogWarning($"접속 실패 ({e.Message}). 방장이 아직 방을 안 만들었거나 나갔을 수 있습니다.");
//            _isConnected = false;
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            _isConnected = false;
//        }
//    }

//    private async Task CreateSessionRoutine()
//    {
//        if (_isConnected) return;
//        _profileName = FirebaseAuthManager.Instance.UserId;
//        try
//        {
//            if (!AuthenticationService.Instance.IsSignedIn)
//            {
//                AuthenticationService.Instance.SwitchProfile(_profileName);
//                await AuthenticationService.Instance.SignInAnonymouslyAsync();
//            }
//            var options = new SessionOptions()
//            {
//                Name = _sessionName,
//                MaxPlayers = _maxPlayers
//            }.WithDistributedAuthorityNetwork();

//            _session = await MultiplayerService.Instance.CreateSessionAsync(options);
//            _isConnected = true;
//            Debug.Log($"세션에 접속했습니다: {_sessionName}");
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            _isConnected = false;
//        }
//    }

    
//}
