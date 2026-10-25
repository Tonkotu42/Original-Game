using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{

    // 次の難易度のシーンへ進むメソッド
    public void GoToNextStage()
    {
        // 登録したScene2の名前を指定する
        SceneManager.LoadScene("Scene2");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
