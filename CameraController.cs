using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] private Transform target; //[cite: 6]
    [SerializeField] private float distance = 5.0f; //[cite: 6]
    [SerializeField] private float mouseSensitivity = 2.0f; //[cite: 6]
    private float currentX = 0f;
    private float currentY = 20f;

    [SerializeField] private float minY = -10f; //[cite: 6]
    [SerializeField] private float maxY = 60f; //[cite: 6]
    [SerializeField] private DogManager dogManager; //[cite: 6]

    private PlayerController playerController; //[cite: 6]

    [Header("Dynamic Settings")]
    //[SerializeField] private float smoothSpeed = 15f; // カメラの追従速度
    [SerializeField] private float smoothTime = 0.1f;    //（SmoothDamp用。0.05〜0.15くらいが滑らか）
    [SerializeField] private float dashFov = 70f;
    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float fovTransitionSpeed = 5f;

    private Vector3 currentTargetPos;
    private Vector3 velocity = Vector3.zero; // ★追加：SmoothDampが内部で使う速度参照用の変数
    private Camera mainCamera;

    [Header("Smooth Shake Settings")]
    [SerializeField] private float shakeSpeed = 30f; // ヒット時の揺れスピード
    private float shakeDuration = 0f;
    private float initialShakeDuration = 0f;
    private float shakeMagnitude = 0f;

    void Start()
    {
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>(); //[cite: 6]
        }

        mainCamera = Camera.main;
        if (target != null) currentTargetPos = target.position;

        LockCursor(); //[cite: 6]
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isMenuOpen = (playerController != null && playerController.statusWindowFlag); //[cite: 6]
        bool isTalking = (dogManager != null && dogManager.IsTalking); //[cite: 6]



        if (isTalking || isMenuOpen) //[cite: 6]
        {
            UnlockCursor(); //[cite: 6]
            return; //[cite: 6]
        }
        else
        {
            LockCursor(); //[cite: 6]
        }

        // マウス入力を取得
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity; //[cite: 6]
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity; //[cite: 6]
        currentY = Mathf.Clamp(currentY, minY, maxY); //[cite: 6]

        // ターゲット位置をLerpで滑らかに追従させる
        currentTargetPos = Vector3.SmoothDamp(currentTargetPos, target.position, ref velocity, smoothTime);

        // カメラの位置と回転を計算
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0); //[cite: 6]
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance); //[cite: 6]
        Vector3 position = rotation * negDistance + currentTargetPos;

        // --- 1. 滑らかなカメラシェイクの適用（ヒット時） ---
        if (shakeDuration > 0)
        {
            float damping = shakeDuration / initialShakeDuration; //[cite: 6]
            float currentMag = shakeMagnitude * damping;

            float noiseX = Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) * 2f - 1f; //[cite: 6]
            float noiseY = Mathf.PerlinNoise(Time.time * shakeSpeed, 100f) * 2f - 1f; //[cite: 6]

            // カメラの向き（ローカル座標）を基準に揺らすことで、真上や真下を向いていても自然に揺れる
            position += (transform.right * noiseX + transform.up * noiseY) * currentMag;

            shakeDuration -= Time.deltaTime;
        }

        // 最終的な位置と回転を適用
        transform.rotation = rotation; //[cite: 6]
        transform.position = position; //[cite: 6]

        // ダッシュ判定によるFOVの動的変更
        if (playerController != null && Input.GetKey(KeyCode.LeftShift))
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, dashFov, fovTransitionSpeed * Time.deltaTime);
        }
        else
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, normalFov, fovTransitionSpeed * Time.deltaTime);
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        initialShakeDuration = duration; //[cite: 6]
        shakeMagnitude = magnitude;
    }

    public static void LockCursor() //[cite: 6]
    {
        Cursor.lockState = CursorLockMode.Locked; //[cite: 6]
        Cursor.visible = false; //[cite: 6]
    }

    public static void UnlockCursor() //[cite: 6]
    {
        Cursor.lockState = CursorLockMode.None; //[cite: 6]
        Cursor.visible = true; //[cite: 6]
    }
}