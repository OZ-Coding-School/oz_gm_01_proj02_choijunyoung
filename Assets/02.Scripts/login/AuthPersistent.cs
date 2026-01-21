using UnityEngine;
using Firebase.Auth;

public class AuthPersistent : MonoBehaviour
{
    public static AuthPersistent Instance { get; private set; }

    public bool IsLoggedIn { get; private set; } = false;
    public string UserId { get; private set; } = "";

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
        if (!IsLoggedIn) return;
        IsLoggedIn = loggedin;
        UserId = userid;
    }

}