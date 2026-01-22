using UnityEngine;
using Firebase.Auth;

public class AuthPersistent : MonoBehaviour
{
    public static AuthPersistent Instance { get; private set; }

    [SerializeField] private bool _isLoggedIn = false;
    [SerializeField] private string _userId = "";
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set => _isLoggedIn = value;
    }

    public string UserId
    {
        get => _userId;
        private set => _userId = value;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }
    public void SetLoginData(bool loggedin, string userid)
    {
        if (IsLoggedIn) return;
        IsLoggedIn = loggedin;
        UserId = userid;
    }

}