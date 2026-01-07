using Firebase;
using Firebase.Auth;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class FirebaseAuthManager
{
    private static FirebaseAuthManager instance = null;

    public static FirebaseAuthManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new FirebaseAuthManager();
            }
            return instance;
        }
    }

    private FirebaseAuth auth;
    private FirebaseUser user;

    public string UserId => user != null ? user.UserId : "Unknown";

    public Action<bool> LoginState;
    public Action<bool> CreateSuccess;

    public void Init()
    {
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                // 상태가 'Available'일 때만 인스턴스를 가져와야 함
                auth = FirebaseAuth.DefaultInstance;
                
                if (auth.CurrentUser != null)
                {
                    Logout();
                }
                auth.StateChanged += OnChanged;

                OnChanged(this, null);

                Debug.Log("Firebase 초기화 성공");
            }
            else
            {
                // 실패 시 에러 로그
                Debug.LogError($"Firebase 초기화 실패: {dependencyStatus}");
            }
        });

        
    }

    private void OnChanged(object sender, System.EventArgs e)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("로그아웃 됨");
                LoginState?.Invoke(false);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("로그인 됨: " + user.Email);
                LoginState?.Invoke(true);
            }
        }
    }


    public void Create(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task => 
        {
            if (task.IsCanceled)
            {
                Debug.Log("회원가입 취소");
                CreateSuccess?.Invoke(false);
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("회원가입 실패");
                CreateSuccess?.Invoke(false);
                return;
            }

            var newUser = task.Result;
            CreateSuccess?.Invoke(true);
            Debug.Log("회원가입 성공");
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
    }
    public void Login(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("로그인 취소");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("로그인 실패");
                return;
            }

            var newUser = task.Result;
            Debug.Log("로그인 성공");

        });
    }
    public void Logout()
    {
        auth.SignOut();
        user = null;
        Debug.Log("User logged out.");
    }

}
