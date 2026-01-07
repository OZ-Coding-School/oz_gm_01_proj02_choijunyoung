using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SessionResponse
{
    public string id;
    public int count;
}

public class CreateSessionByGoogle : MonoBehaviour
{
    const string URL = "https://script.google.com/macros/s/AKfycbwIfCXIm_1nF5EXzmwCKpqpqVmALhbTdYDN-H6XhZYCQLMo_4y-D-_eJYURwN3i9u55/exec";
    public MatchMaking matchMaking;


    public void RequestSessionId(Action<string, int> onResult)
    {
        StartCoroutine(GetSessionId(onResult));
    }

    // 특정 세션의 인원수만 확인하는 함수
    public void CheckSessionCount(string sessionId, Action<int> onResult)
    {
        StartCoroutine(GetSessionStatusRoutine(sessionId, onResult));
    }

    private IEnumerator GetSessionStatusRoutine(string sessionId, Action<int> onResult)
    {
        WWWForm form = new WWWForm();
        form.AddField("session_id", sessionId);
        form.AddField("mode", "check"); // 구글시트쪽의 CASE 0 실행

        using (UnityWebRequest www = UnityWebRequest.Post(URL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                if (int.TryParse(www.downloadHandler.text, out int count))
                {
                    onResult(count);
                }
                else
                {
                    onResult(0);
                }
            }
            else
            {
                onResult(0);
            }
        }
    }

    public IEnumerator GetSessionId(Action<string, int> onResult)
    {
        // 빈 방이 있는지 구글 시트에 물어보기
        WWWForm formCheck = new WWWForm();

        using (UnityWebRequest www = UnityWebRequest.Post(URL, formCheck))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("웹 통신 에러: " + www.error);
                yield break;
            }

            string response = www.downloadHandler.text;
            Debug.Log("서버 응답: " + response);

            // 응답 분석 "false"가 왔다는 건 -> 방이 없거나 꽉 찼다는 뜻 -> 새로 만들어야 함
            if (response == "false")
            {
                Debug.Log("빈 방이 없습니다. 새로운 방을 생성합니다...");

                // 새 ID 생성 및 등록 요청
                string newId = GetRandomId(5);
                yield return StartCoroutine(RegisterNewSession(newId));

                // 새로 만든 ID 리턴
                onResult(newId,1);
            }
            else
            {
                try
                {
                    SessionResponse data = JsonUtility.FromJson<SessionResponse>(response);
                    onResult(data.id, data.count);
                }
                catch(Exception e)
                {
                    Debug.LogError("응답 파싱 오류: " + e.Message);
                }
                // "false"가 아니면 -> Session_ID가 돌아온 것임 -> 그대로 사용
                Debug.Log("빈 방을 찾았습니다. 해당 방으로 입장합니다.");
            }
        }
    }

    // 새로운 방을 구글 시트에 등록하는 서브 루틴
    IEnumerator RegisterNewSession(string newId)
    {
        WWWForm formCreate = new WWWForm();
        formCreate.AddField("session_id", newId); // session_id를 보내면 생성 모드로 작동

        using (UnityWebRequest www = UnityWebRequest.Post(URL, formCreate))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("새 방 등록 완료: " + newId);
            }
            else
            {
                Debug.LogError("방 등록 실패: " + www.error);
            }
        }
    }

    // 랜덤 ID 생성 메서드
    public string GetRandomId(int length = 5)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        char[] stringChars = new char[length];

        for (int i = 0; i < length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, chars.Length);
            stringChars[i] = chars[randomIndex];
        }
        return new string(stringChars);
    }
}