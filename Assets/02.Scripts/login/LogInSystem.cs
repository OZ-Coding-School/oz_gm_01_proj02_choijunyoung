using DG.Tweening;
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

    void Start()
    {
        FirebaseAuthManager.Instance.LoginState += OnChangeState;
        FirebaseAuthManager.Instance.CreateSuccess += OnChangedPanel;

        FirebaseAuthManager.Instance.Init();
    }

    private void OnChangeState(bool sign)
    {
        outputText.text = sign? "Conneting :" : "Logout :";
        outputText.text += "\n"+FirebaseAuthManager.Instance.UserId;
        inLobbyUserIDText.text = FirebaseAuthManager.Instance.UserId;
        CloseCreateAccountPanel();
    }

    private void OnChangedPanel(bool success)
    {
        if(!success) return;
        Debug.Log("계정생성성공");
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

        if(p == re_password_C.text) 
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
        if(createSuccessText ==null) return;
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
