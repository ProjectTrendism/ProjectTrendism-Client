using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuUI : MonoBehaviour
{
    [Header("게임 시작 시 이동할 씬 이름")]
    [SerializeField] private string firstGameSceneName = "villageeScene2";

    [Header("팝업 패널")]
    [SerializeField] private GameObject explainPanel;
    [SerializeField] private GameObject controlPanel;

    private void Start()
    {
        if (explainPanel != null)
            explainPanel.SetActive(false);

        if (controlPanel != null)
            controlPanel.SetActive(false);
    }

    public void OnClickStart()
    {
        SceneManager.LoadScene(firstGameSceneName);
    }

    public void OnClickExplain()
    {
        if (explainPanel != null)
            explainPanel.SetActive(true);

        if (controlPanel != null)
            controlPanel.SetActive(false);
    }

    public void OnClickControl()
    {
        if (controlPanel != null)
            controlPanel.SetActive(true);

        if (explainPanel != null)
            explainPanel.SetActive(false);
    }

    public void OnClickCloseExplain()
    {
        if (explainPanel != null)
            explainPanel.SetActive(false);
    }

    public void OnClickCloseControl()
    {
        if (controlPanel != null)
            controlPanel.SetActive(false);
    }
}