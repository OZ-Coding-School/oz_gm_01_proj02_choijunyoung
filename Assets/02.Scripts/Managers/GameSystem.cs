using UnityEngine;

public class GameSystem : MonoBehaviour
{
    [SerializeField] GameObject escPanel;
    bool isOnESC = false;
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnClickESC();
        }
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void OnClickESC()
    {
        isOnESC = !isOnESC;
        escPanel.SetActive(isOnESC);
    }
}
