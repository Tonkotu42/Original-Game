using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LockOnMarkerUI : MonoBehaviour
{
    [Header("UI RectTransforms")]
    [Tooltip("回転する外枠リングや四隅ブラケット")]
    [SerializeField] private RectTransform outerRing;
    [Tooltip("中心のレティクル・ダイヤモンド")]
    [SerializeField] private RectTransform centerDot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Idle Animation (待機時アニメーション)")]
    [SerializeField] private float rotationSpeed = 45f;      // 外枠の回転速度
    [SerializeField] private float pulseSpeed = 4.0f;        // 中心の呼吸パルス速度
    [SerializeField] private float pulseAmplitude = 0.08f;   // 呼吸パルスの振幅

    [Header("Lock-In Animation (ロック開始時演出)")]
    [SerializeField] private float lockInStartScale = 2.4f;  // 開始時の巨大サイズ
    [SerializeField] private float lockInDuration = 0.22f;   // 収縮にかかる秒数

    private RectTransform rectTransform;
    private Coroutine lockInCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    // ★重要: PlayerController から SetActive(true) された瞬間に自動でアニメーション開始！
    void OnEnable()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (lockInCoroutine != null) StopCoroutine(lockInCoroutine);
        lockInCoroutine = StartCoroutine(LockInRoutine());
    }

    void Update()
    {
        // 1. 外枠のアイドリング回転
        if (outerRing != null)
        {
            outerRing.Rotate(0f, 0f, -rotationSpeed * Time.unscaledDeltaTime);
        }

        // 2. 中心の呼吸パルス (Sin波)
        if (centerDot != null)
        {
            float scaleOffset = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            centerDot.localScale = Vector3.one * (1.0f + scaleOffset);
        }
    }

    private IEnumerator LockInRoutine()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < lockInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / lockInDuration;
            // イージング収縮
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.one * Mathf.Lerp(lockInStartScale, 0.92f, ease);
            yield return null;
        }
        // わずかに跳ね返って1.0へ
        float bounce = 0f;
        while (bounce < 0.08f)
        {
            bounce += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * 0.92f, Vector3.one, bounce / 0.08f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}