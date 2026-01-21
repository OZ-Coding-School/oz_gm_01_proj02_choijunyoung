//using DG.Tweening;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;
//using Firebase.Auth;

//public class LogInSystem : MonoBehaviour
//{
//    public TMP_InputField email;
//    public TMP_InputField password;
//    public TMP_InputField email_C;
//    public TMP_InputField password_C;
//    public TMP_InputField re_password_C;
//    public TextMeshProUGUI outputText;
//    public TextMeshProUGUI createErrorText;
//    public GameObject createPanel, loginPanel;
//    public TextMeshProUGUI createSuccessText;
//    public TextMeshProUGUI inLobbyUserIDText;

//    private FirebaseAuth auth;  

//    private void Awake()
//    {
//        auth = FirebaseAuth.DefaultInstance;
//    }

//    private void OnEnable()
//    {
//        if (FirebaseAuthManager.Instance != null)
//        {
//            FirebaseAuthManager.Instance.LoginState += OnChangeState;
//            FirebaseAuthManager.Instance.CreateSuccess += OnChangedPanel;
//        }

//    }

//    private void OnDisable()
//    {
//        if (FirebaseAuthManager.Instance != null)
//        {
//            FirebaseAuthManager.Instance.LoginState -= OnChangeState;
//            FirebaseAuthManager.Instance.CreateSuccess -= OnChangedPanel;
//        }

//    }

//    void Start()
//    {
//        FirebaseAuthManager.Instance.Init();  

//        CheckInitialAuthState();
//    }

//    private void CheckInitialAuthState()
//    {
//        Firebase.Auth.FirebaseUser currentUser = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;

//        bool isSignedIn = currentUser != null;

//        OnChangeState(isSignedIn);  
//    }

//    private void OnChangeState(bool isSignedIn)
//    {

//        if (this == null || gameObject == null || !isActiveAndEnabled)
//            return;

//        if (outputText != null)
//        {
//            outputText.text = isSignedIn ? "Connected :" : "Logout :";
//            outputText.text += "\n" + (FirebaseAuthManager.Instance?.UserId ?? "None");
//        }

//        if (inLobbyUserIDText != null)
//        {
//            inLobbyUserIDText.text = FirebaseAuthManager.Instance?.UserId ?? "";
//        }

//        if (isSignedIn)
//        {
//            CloseCreateAccountPanel();
//            if (loginPanel != null) loginPanel.SetActive(false);

//        }
//        else
//        {

//            if (loginPanel != null) loginPanel.SetActive(true);
//        }
//    }

//    private void OnChangedPanel(bool success)
//    {
//        if (!success) return;

//        Debug.Log("拌沥积己己傍");
//        CloseCreateAccountPanel();
//        ShowSuccessMsg();
//    }

//    public void OnCreateAccountPanel()
//    {
//        if (createPanel != null)
//            createPanel.SetActive(true);
//    }

//    public void CloseCreateAccountPanel()
//    {

//        if (createErrorText != null && createErrorText.gameObject != null)
//            createErrorText.gameObject.SetActive(false);

//        if (createPanel != null)
//            createPanel.SetActive(false);

//        if (loginPanel != null)
//            loginPanel.SetActive(false);
//    }

//    public void Create()
//    {
//        if (createErrorText != null)
//            createErrorText.gameObject.SetActive(false);

//        string e = email_C?.text ?? "";
//        string p = password_C?.text ?? "";

//        if (p == re_password_C?.text)
//        {
//            FirebaseAuthManager.Instance.Create(e, p);
//        }
//        else
//        {
//            if (createErrorText != null)
//            {
//                createErrorText.gameObject.SetActive(true);
//                createErrorText.text = "Password is not matching";
//            }
//        }
//    }

//    public void LogIn()
//    {
//        FirebaseAuthManager.Instance.Login(email?.text ?? "", password?.text ?? "");
//    }

//    public void LogOut()
//    {
//        FirebaseAuthManager.Instance.Logout();
//    }

//    private void ShowSuccessMsg()
//    {
//        if (createSuccessText == null) return;

//        createSuccessText.gameObject.SetActive(true);
//        createSuccessText.alpha = 0f;

//        Sequence seq = DOTween.Sequence();
//        seq.Append(createSuccessText.DOFade(1f, 0.5f));
//        seq.AppendInterval(2f);
//        seq.Append(createSuccessText.DOFade(0f, 0.5f));
//        seq.OnComplete(() =>
//        {
//            if (createSuccessText != null)
//                createSuccessText.gameObject.SetActive(false);
//        });
//    }
//}

using DG.Tweening;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogInSystem : MonoBehaviour
{
    public TMP_InputField email;
    public TMP_InputField password;

    public TMP_InputField email_C;
    public TMP_InputField password_C;
    public TMP_InputField re_password_C;

    public TextMeshProUGUI outputText;
    public TextMeshProUGUI createErrorText;
    public GameObject createPanel, loginPanel;

    public TextMeshProUGUI createSuccessText;

    public TextMeshProUGUI inLobbyUserIDText;


    private void OnEnable()
    {
        if(AuthPersistent.Instance.IsLoggedIn)
        {
            loginPanel.SetActive(false);
            inLobbyUserIDText.text = FirebaseAuthManager.Instance.UserId;
            return;
        }
    }

    void Start()
    {
        if (AuthPersistent.Instance.IsLoggedIn) return;
        FirebaseAuthManager.Instance.LoginState += OnChangeState;
        FirebaseAuthManager.Instance.CreateSuccess += OnChangedPanel;

        FirebaseAuthManager.Instance.Init();
    }

    private void OnChangeState(bool sign)
    {
        outputText.text = sign ? "Conneting :" : "Logout :";
        AuthPersistent.Instance.SetLoginData(true, FirebaseAuthManager.Instance.UserId);
        outputText.text += "\n" + FirebaseAuthManager.Instance.UserId;
        inLobbyUserIDText.text = FirebaseAuthManager.Instance.UserId;
        CloseCreateAccountPanel();
    }

    private void OnChangedPanel(bool success)
    {
        if (!success) return;
        Debug.Log("拌沥积己己傍");
        CloseCreateAccountPanel();
        ShowSuccessMsg();

    }

    public void OnCreateAccountPanel()
    {
        createPanel.SetActive(true);
    }

    public void CloseCreateAccountPanel()
    {
        createErrorText.gameObject.SetActive(false);
        createPanel.SetActive(false);
        loginPanel.SetActive(false);
    }

    public void Create()
    {
        createErrorText.gameObject.SetActive(false);
        string e = email_C.text;
        string p = password_C.text;

        if (p == re_password_C.text)
        {
            FirebaseAuthManager.Instance.Create(e, p);

        }
        else
        {
            createErrorText.gameObject.SetActive(true);
            createErrorText.text = "Password is not matching";
        }

    }
    public void LogIn()
    {
        FirebaseAuthManager.Instance.Login(email.text, password.text);
    }
    public void LogOut()
    {
        FirebaseAuthManager.Instance.Logout();
    }

    private void ShowSuccessMsg()
    {
        if (createSuccessText == null) return;
        createSuccessText.gameObject.SetActive(true);
        createSuccessText.alpha = 0f;

        Sequence seq = DOTween.Sequence();

        seq.Append(createSuccessText.DOFade(1f, 0.5f));
        seq.AppendInterval(2f);
        seq.Append(createSuccessText.DOFade(0f, 0.5f));
        seq.OnComplete(() =>
        {
            createSuccessText.gameObject.SetActive(false);
        });
    }
}
