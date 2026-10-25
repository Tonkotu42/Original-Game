using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdrenalineGaugeUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("アドレナリンSliderのFill画像")]
    [SerializeField] private Image fillImage;
    [Tooltip("MAX時に点滅させるテキスト (例: 'ULTIMATE READY!')")]
    [SerializeField] private TextMeshProUGUI maxNoticeText;
    [Tooltip("脈動させるゲージのRectTransform (未設定時は自分自身)")]
    [SerializeField] private RectTransform gaugeRect;
    [Tooltip("画面全体のフラッシュ画像 (任意)")]
    [SerializeField] private Image flashImage;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.7f, 1f);     // 通常水色
    [SerializeField] private Color maxColor = new Color(1f, 0.85f, 0.1f);       // 黄金色
    [SerializeField] private Color maxGlowColor = new Color(1f, 0.4f, 0.1f);    // 発光オレンジ

    [Header("Pulse Settings")]
    [SerializeField] private float pulseSpeed = 6.0f;       // 脈動速度
    [SerializeField] private float pulseScaleAmount = 0.08f; // 脈動の大きさ

    [Header("Audio (効果音)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip maxChargeSound;      // チャージ完了音

    private bool isMaxState = false;
    private Vector3 originalScale;

    void Awake()
    {
        if (gaugeRect == null) gaugeRect = GetComponent<RectTransform>();
        if (gaugeRect != null) originalScale = gaugeRect.localScale;

        if (maxNoticeText != null) maxNoticeText.gameObject.SetActive(false);
        if (fillImage != null) fillImage.color = normalColor;
        if (flashImage != null) flashImage.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isMaxState) return;

        // 1. ゲージ全体の脈動アニメーション
        if (gaugeRect != null)
        {
            float scaleOffset = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseScaleAmount;
            gaugeRect.localScale = originalScale * (1.0f + scaleOffset);
        }

        // 2. 黄金カラーループ
        if (fillImage != null)
        {
            float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1.0f) * 0.5f;
            fillImage.color = Color.Lerp(maxColor, maxGlowColor, t);
        }

        // 3. テキスト点滅
        if (maxNoticeText != null)
        {
            float alpha = Mathf.PingPong(Time.unscaledTime * 3.5f, 1.0f);
            maxNoticeText.alpha = Mathf.Clamp(alpha, 0.35f, 1.0f);
        }
    }

    /// <summary>
    /// 100%到達時に呼ばれる
    /// </summary>
    public void OnAdrenalineMax()
    {
        if (isMaxState) return;
        isMaxState = true;

        if (maxNoticeText != null)
        {
            maxNoticeText.gameObject.SetActive(true);
            maxNoticeText.text = "ULTIMATE READY!";
        }

        if (audioSource != null && maxChargeSound != null)
        {
            audioSource.PlayOneShot(maxChargeSound);
        }
    }

    /// <summary>
    /// 必殺技使用時や消費時に呼ばれる
    /// </summary>
    public void OnAdrenalineReset()
    {
        isMaxState = false;
        if (maxNoticeText != null) maxNoticeText.gameObject.SetActive(false);
        if (gaugeRect != null) gaugeRect.localScale = originalScale;
        if (fillImage != null) fillImage.color = normalColor;
    }
}