using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {



    }

    public void TitleButtonPush(int TitleButton)
    {
        switch (TitleButton)
        {
            case 1:
                // "GameScene" という名前のシーンに遷移
                Time.timeScale = 1f;
                SceneManager.LoadScene("Scene 1");
                break;
            default:
                break;

        }
    }
}
