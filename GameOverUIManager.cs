using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUIManager : MonoBehaviour
{
    // 他のスクリプト（PlayerControllerなど）から簡単に呼び出すためのSingleton
    public static GameOverUIManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private CanvasGroup gameOverCanvasGroup; // 透明度を操作するコンポーネント

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gameOverJingle; // 自作の専用ジングルをここにセット

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 2.0f; // フェードインにかかる秒数

    void Awake()
    {
        // Singletonの初期化
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 最初はゲームオーバー画面を見えない状態（透明・操作不可）にしておく
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }
    }

    // PlayerControllerから呼ばれるメソッド
    public void ShowGameOverScreen()
    {
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        // ゲームオーバージングルを再生
        if (audioSource != null && gameOverJingle != null)
        {
            audioSource.PlayOneShot(gameOverJingle);
        }

        // 徐々に不透明（alpha = 1）にしていく
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Time.timeScale = 0 の影響を受けないように unscaled を使う
            gameOverCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        gameOverCanvasGroup.alpha = 1f;

        // フェード完了後にボタンを押せるようにする
        gameOverCanvasGroup.interactable = true;
        gameOverCanvasGroup.blocksRaycasts = true;

        // マウスカーソルを表示してロックを解除
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // リトライボタンが押された時に呼ばれるメソッド
    public void RetryGame()
    {
        // 時間の流れを元に戻す
        Time.timeScale = 1.0f;

        // 現在のシーンを最初から読み込み直す
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}