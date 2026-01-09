using UnityEngine;

public class SessionTest : MonoBehaviour
{
    public CreateSessionByGoogle sessionFetcher; // Inspector에서 드래그

    // UI 버튼에 연결하거나, Start에서 자동 호출
    public void OnClick_TestFetch()
    {
        Debug.Log("세션 ID 요청 시작...");
        sessionFetcher.FetchSessionId((sessionId) =>
        {
            if (sessionId != null)
            {
                Debug.Log($"★★★ 성공! 세션 ID: {sessionId} ★★★");
                // 여기서 실제 게임 시작 로직 연결 (예: NetworkManager 연결)
            }
            else
            {
                Debug.LogError("세션 ID 가져오기 실패");
            }
        });
    }

    // 자동 테스트 원하면 Start에 넣기
    private void Start()
    {
        // 주석 풀면 에디터 실행과 동시에 자동 테스트
        // OnClick_TestFetch();
    }
}