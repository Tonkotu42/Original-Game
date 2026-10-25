using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImpactFrameManager : MonoBehaviour
{
    public static ImpactFrameManager Instance;

    [Header("UI References")]
    [SerializeField] private Image flashImage;
    [SerializeField] private Image speedLinesImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 初期状態は透明にしておく
        SetAlpha(flashImage, 0f);
        SetAlpha(speedLinesImage, 0f);
    }

    /// <summary>
    /// インパクトフレームを再生する
    /// </summary>
    /// <param name="flashColor">フラッシュの色（白、黒、赤など）</param>
    /// <param name="flashDuration">単色塗りつぶしが画面に留まる時間（秒）</param>
    /// <param name="lineDuration">集中線がフェードアウトするまでの時間（秒）</param>
    public void PlayImpact(Color flashColor, float flashDuration = 0.04f, float lineDuration = 0.25f)
    {
        // 既存のコルーチンが走っていれば止めて上書きする
        StopAllCoroutines();
        StartCoroutine(ImpactRoutine(flashColor, flashDuration, lineDuration));
    }

    private IEnumerator ImpactRoutine(Color elementColor, float flashDuration, float lineDuration)
    {
        // 1. 最初は「真っ白」で強烈に発光（アルファ1.0）
        flashImage.color = new Color(1f, 1f, 1f, 1f);
        SetAlpha(speedLinesImage, 1f);

        yield return new WaitForSecondsRealtime(flashDuration);

        // 2. 白から指定された属性色(elementColor)に遷移しながら、素早くフェードアウト
        float colorFadeDuration = 0.15f; // フラッシュ自体が消えるまでの時間
        float elapsed = 0f;

        while (elapsed < lineDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // フラッシュのカラーフェード
            if (elapsed < colorFadeDuration)
            {
                float t = elapsed / colorFadeDuration;
                // 白から属性色へLerpし、同時に透明度を下げていく
                Color currentColor = Color.Lerp(Color.white, elementColor, t);
                currentColor.a = Mathf.Lerp(1f, 0f, t);
                flashImage.color = currentColor;
            }
            else
            {
                SetAlpha(flashImage, 0f);
            }

            // 集中線のフェードアウト
            float lineAlpha = Mathf.Lerp(1f, 0f, elapsed / lineDuration);
            SetAlpha(speedLinesImage, lineAlpha);

            yield return null;
        }

        SetAlpha(flashImage, 0f);
        SetAlpha(speedLinesImage, 0f);
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}