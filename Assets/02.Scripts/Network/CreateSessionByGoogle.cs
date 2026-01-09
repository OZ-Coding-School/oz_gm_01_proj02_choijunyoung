using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[Serializable]
public class SessionResponse
{
    public bool success;
    public string session_id;
    public int count;
    public string message; // ���� �� ������ (�ɼ�)
}

public class CreateSessionByGoogle : MonoBehaviour
{

    private const string GET_URL = "https://script.google.com/macros/s/AKfycbyGm2jVx8_A4R7YxW2RlbKk_YHZ58QGIQlT_Z0HnB-LtL-APfFMuOqzGO95pvzAMEXu/exec";

    
    public void FetchSessionId(Action<string> onComplete)
    {
        StartCoroutine(FetchRoutine(onComplete));
    }

    private IEnumerator FetchRoutine(Action<string> onComplete)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(GET_URL))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Session ID 요청 실패: " + www.error);
                onComplete?.Invoke(null);
                yield break;
            }

            string sessionId = www.downloadHandler.text.Trim();

            if (string.IsNullOrEmpty(sessionId) || sessionId == "error" || sessionId == "busy")
            {
                Debug.LogError("유효하지 않은 session_id: " + sessionId);
                onComplete?.Invoke(null);
            }
            else
            {
                Debug.Log("Session ID 획득 성공: " + sessionId);
                onComplete?.Invoke(sessionId);
            }
        }
    }
}