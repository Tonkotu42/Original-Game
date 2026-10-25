using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private DogManager dogManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Escキーが押されたか検知
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }

        
    }

    public void clickMenubutton(int button)
    {
        switch (button)
        {
            case 1:
                Time.timeScale = 1f;
                SceneManager.LoadScene("Scene Title");
                break;
        }
    }

    private void ToggleMenu()
    {
        if (menuPanel != null)
        {
            // 現在の状態の真逆にする（アクティブなら非アクティブ、非アクティブならアクティブ）
            bool isActive = !menuPanel.activeSelf;
            menuPanel.SetActive(isActive);

            // ゲームを一時停止
            Time.timeScale = isActive ? 0f : 1f;

            // ★カーソルの状態に応じた切り替え分岐
            if (isActive)
            {
                // メニューが開いた時：カーソルを自由に動かせるようにして表示
                CameraController.UnlockCursor();
            }
            else
            {
                // メニューを閉じた時：
                // もし犬との会話中であればカーソルはそのまま（UI操作のためロックしない）[cite: 5]
                if (dogManager != null && dogManager.IsTalking)
                {
                    CameraController.UnlockCursor();
                }
                else
                {
                    // 通常時なら中央に固定して非表示にする
                    CameraController.LockCursor();
                }
            }
        }
        
    }
}
