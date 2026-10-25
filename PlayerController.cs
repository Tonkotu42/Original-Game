using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody rigidBody;
    private Animator animator;

    [Header("Ultimate Attack Settings")]
    [SerializeField] private GameObject ultimateEffectPrefab;
    [SerializeField] private AudioClip ultimateSound;
    [Tooltip("アルティメット最終段専用の攻撃SE（スラッシュ音/フィニッシュ音）")]
    [SerializeField] private AudioClip ultimateFinishSound;
    [Tooltip("アルティメット最終段専用の着弾ヒットSE（未設定時は通常のultimateHitSoundが鳴ります）")]
    [SerializeField] private AudioClip ultimateFinishHitSound;
    private bool isUltimateAttacking = false;
    public bool IsUltimateAttacking => isUltimateAttacking;
    private bool isUltimateFinishing = false;
    public bool IsUltimateFinishing => isUltimateFinishing;
    private float ultimateDutchAngle = 0f;

    [Header("Ultimate Shake Settings (Camera Shake Only)")]
    [Tooltip("アルティメット通常連打中のカメラシェイク強度")]
    [SerializeField] private float ultimateShakeIntensity = 0.12f;
    [Tooltip("アルティメット最終段（フィニッシュ）のカメラシェイク強度")]
    [SerializeField] private float ultimateFinishShakeIntensity = 0.55f;

    [Header("Adrenaline System")]
    public float currentAdrenaline = 0f;
    public float maxAdrenaline = 100f;
    [SerializeField] private UnityEngine.UI.Slider adrenalineSlider;
    public bool isAdrenalineCounter { get; private set; }
    public bool isMaxAdrenalineCounter { get; private set; }
    [SerializeField] private GameObject adrenalineAuraEffect;
    [SerializeField] private AudioClip adrenalineConsumeSound;
    [SerializeField] private AdrenalineGaugeUI adrenalineGaugeUI;
    [SerializeField] private AudioClip adrenalineMaxSound;
    private bool wasAdrenalineMax = false;

    public bool isSuperArmor { get; private set; }

    [Header("ZZZ Style Post Processing")]
    [SerializeField] private Volume postProcessVolume;
    private ChromaticAberration chromaticAberration;
    private Vignette vignette;
    private LensDistortion lensDistortion;

    [SerializeField] private float parryChromaticIntensity = 1.0f;
    [SerializeField] private float parryVignetteIntensity = 0.45f;
    [SerializeField] private float parryLensDistortionIntensity = -0.3f;

    [Header("Parry Counter Settings")]
    [SerializeField] private float parryCounterWindow = 1.0f;
    private float parryCounterTimer = 0f;
    private bool IsParryCounterAvailable => parryCounterTimer > 0f;

    [Header("Parry Counter Attack")]
    [SerializeField] private float counterForwardForce = 8f;
    [SerializeField] private AudioClip counterAttackSound;
    private bool isCounterAttacking = false;
    public bool IsCounterAttacking => isCounterAttacking;

    [Header("Parry Zoom Settings")]
    [SerializeField] private CinemachineCamera parryTargetCamera;
    [SerializeField] private float zoomFovMultiplier = 0.75f;
    [SerializeField] private float zoomSpeed = 15.0f;

    [Header("ZZZ Style Parry Camera Settings")]
    [SerializeField] private float parryDutchAngle = 3.0f;
    [SerializeField] private float zoomInSpeed = 25.0f;
    [SerializeField] private float zoomOutSpeed = 8.0f;

    [Header("ZZZ Style Dynamic Camera")]
    [SerializeField] private float dashFovMultiplier = 1.15f;
    [SerializeField] private float hitZoomMultiplier = 0.85f;
    private float currentFovMultiplier = 1.0f;

    [Header("Camera Magnetism")]
    [SerializeField] private bool enableMagnetism = true;
    [SerializeField] private float magnetismDelay = 1.0f;
    [SerializeField] private float magnetismSpeed = 3.0f;
    private float noInputTimer = 0f;
    private CinemachineInputAxisController cinemachineInput;

    private float baseFov = -1f;
    private float targetFov = -1f;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 75f;
    [SerializeField] private float dashSpeedMultiplier = 1.8f;
    [SerializeField] private float airMoveMultiplier = 0.5f;
    private bool isDash = false;
    private float inputHorizontal;
    private float inputVertical;
    private Vector3 moveForward;
    private Vector3 cameraForward;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("LockOn Advanced Settings")]
    [Tooltip("ロックオン可能な最大距離")]
    [SerializeField] private float lockOnRange = 16.0f;
    [Tooltip("ロック解除判定距離（ヒステリシス）")]
    [SerializeField] private float lockOnBreakDistance = 20.0f;
    [Tooltip("ロックオン中のストレイフ移動（カニ歩き）を有効にするか")]
    [SerializeField] private bool strafeWhileLockedOn = true;
    [Tooltip("ロックオン中のカメラ自動追従アシスト速度")]
    [SerializeField] private float lockOnCameraTrackingSpeed = 4.0f;
    [Tooltip("障害物（壁）による遮蔽チェックを行うか")]
    [SerializeField] private bool checkLineOfSight = true;
    [Tooltip("遮蔽物や壁のレイヤー")]
    [SerializeField] private LayerMask obstacleLayerMask = ~0;
    [Tooltip("壁に隠れたり画面外に出たときのロスト猶予時間（秒）")]
    [SerializeField] private float lostTargetGracePeriod = 1.2f;
    [Tooltip("ロック中の敵を倒した際、次の敵へ自動切り替えするか")]
    [SerializeField] private bool autoSwitchTargetOnDefeat = true;

    [Header("LockOn Scoring Weights (ターゲット選定の重み)")]
    [Range(0f, 1f)][SerializeField] private float screenCenterWeight = 0.55f;
    [Range(0f, 1f)][SerializeField] private float distanceWeight = 0.25f;
    [Range(0f, 1f)][SerializeField] private float angleWeight = 0.20f;

    [Header("LockOn Switching Settings (ターゲット切替)")]
    [Tooltip("マウス横フリックでのターゲット切替しきい値")]
    [SerializeField] private float mouseSwitchThreshold = 1.8f;
    private float mouseSwitchCooldown = 0.25f;
    private float mouseSwitchTimer = 0f;

    private Transform lockedTarget = null;
    public Transform LockedTarget => lockedTarget;
    private Transform lockOnCenterPoint;
    private Transform originalLookAt = null;
    private bool hasSavedOriginalLookAt = false;
    private float lostTargetTimer = 0f;

    [SerializeField] private RectTransform lockOnMarkerUI;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float markerYOffset = 1.2f;

    [Header("Ground Detection & Jump Assist")]
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded = true;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);
    [SerializeField] private float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;

    [Header("Jump & Wall Jump Settings")]
    [SerializeField] private float jumpPower = 12f;
    [SerializeField] private float extraJumpPower = 10f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2.0f;
    [SerializeField] private int maxExtraJumps = 1;
    private int extraJumpsAvailable;
    [SerializeField] private float wallJumpUpForce = 12f;
    [SerializeField] private float wallJumpKickback = 10f;
    private bool isTouchingWall = false;
    private Vector3 currentWallNormal;
    private float fallAnimationDelayTimer = 0f;
    private bool isJumping = false;

    [Header("Camera Shake Settings")]
    [SerializeField] private float normalShakeMag = 0.05f;
    [SerializeField] private float finisherShakeMag = 0.15f;
    [SerializeField] private float shakeDuration = 0.1f;

    [Header("Status")]
    public int currentHP;
    public int currentMP;
    private float damageCooldown = 0f;

    [Header("Combo Settings")]
    private int comboStep = 0;
    private const int MAX_COMBO = 4;
    private bool hasInputNextCombo = false;
    private bool canTransition = false;
    private float comboFailsafeTimer = 0f;
    private const float COMBO_TIMEOUT = 1.5f;

    [Header("Attack Movement")]
    [SerializeField] private float attackForwardForce = 5f;

    [Header("Auto Target Settings")]
    [SerializeField] private float autoTargetRange = 8.0f;

    [Header("Combat Components")]
    [SerializeField] private Collider weaponCollider;
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject counterEffectPrefab;

    [Header("UI & Texts")]
    public bool statusWindowFlag = false;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private UnityEngine.UI.Slider hpSlider;
    [SerializeField] private PSSO playerStatusSO;
    [SerializeField] private GameObject statusWindow;
    [SerializeField] private GameObject itemBoxManager;
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("External Managers")]
    [SerializeField] private DogManager dogManager;

    [Header("Audio (SE)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip attackSound2;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip ultimateHitSound;
    [SerializeField] private AudioClip lockOnSound;
    [SerializeField] private AudioClip lockSwitchSound;

    [Header("Audio (BGM)")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip normalBGM;
    [SerializeField] private AudioClip battleBGM;
    [SerializeField] private float maxBgmVolume = 0.5f;
    [SerializeField] private float battleDetectRange = 20.0f;
    [SerializeField] private float bgmFadeDuration = 1.0f;
    private bool isBattleState = false;
    private Coroutine bgmFadeCoroutine;
    private AudioLowPassFilter bgmLowPassFilter;
    [SerializeField] private float defaultLowPassCutoff = 22000f;
    [SerializeField] private float parryLowPassCutoff = 800f;

    [Header("Parry Settings")]
    [SerializeField] private float parryWindowDuration = 0.8f;
    private float parryTimer = 0f;
    private bool IsParryWindowActive => parryTimer > 0f;
    [SerializeField] private GameObject parrySuccessEffectPrefab;
    public bool IsComboCancelParry { get; private set; }

    [Header("Parry Backstep Settings")]
    [SerializeField] private float parryBackstepForce = 7f;

    private bool isTalking = false;

    public int CurrentComboStep => comboStep;
    public bool IsAirborne => isJumping || !isGrounded;

    void Start()
    {
        if (adrenalineSlider != null)
        {
            adrenalineSlider.maxValue = maxAdrenaline;
            adrenalineSlider.value = currentAdrenaline;
        }

        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        currentHP = playerStatusSO != null ? playerStatusSO.HP : 100;
        currentMP = playerStatusSO != null ? playerStatusSO.MP : 100;
        UpdateStatusUI();

        if (weaponCollider != null) weaponCollider.enabled = false;
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out chromaticAberration);
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out lensDistortion);
        }

        GameObject centerObj = new GameObject("LockOnCenterPoint");
        lockOnCenterPoint = centerObj.transform;

        if (parryTargetCamera == null)
        {
            parryTargetCamera = FindAnyObjectByType<CinemachineCamera>();
        }
        if (parryTargetCamera != null && !hasSavedOriginalLookAt)
        {
            originalLookAt = parryTargetCamera.LookAt;
            hasSavedOriginalLookAt = true;
            cinemachineInput = parryTargetCamera.GetComponent<CinemachineInputAxisController>();
        }

        if (bgmAudioSource != null && normalBGM != null)
        {
            bgmAudioSource.clip = normalBGM;
            bgmAudioSource.volume = maxBgmVolume;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();

            bgmLowPassFilter = bgmAudioSource.gameObject.GetComponent<AudioLowPassFilter>();
            if (bgmLowPassFilter == null)
            {
                bgmLowPassFilter = bgmAudioSource.gameObject.AddComponent<AudioLowPassFilter>();
            }
            bgmLowPassFilter.cutoffFrequency = defaultLowPassCutoff;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (lockOnMarkerUI != null)
        {
            lockOnMarkerUI.gameObject.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        UpdateLockOnMarkerPosition();
    }

    void Update()
    {
        HandleGlobalInput();
        HandleLockOnInput();
        UpdateCameraZoom();

        if (isTalking || statusWindowFlag || Time.timeScale == 0) return;

        UpdateTimers();

        if (parryCounterTimer > 0)
        {
            parryCounterTimer -= Time.deltaTime;
        }
        HandleUltimateInput();

        if (isUltimateAttacking) return;

        HandleMovementInput();
        HandleDashInput();
        HandleJumpInput();
        HandleComboInput();
        HandleParryInput();

        UpdateAirborneState();
        UpdateBattleState();
    }

    private void UpdateLockOnMarkerPosition()
    {
        if (lockedTarget != null && lockOnMarkerUI != null && mainCamera != null)
        {
            Vector3 targetWorldPos = lockedTarget.position + Vector3.up * markerYOffset;
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(targetWorldPos);
            bool isBehindCamera = viewportPos.z < 0;

            if (!isBehindCamera && viewportPos.x >= -0.1f && viewportPos.x <= 1.1f && viewportPos.y >= -0.1f && viewportPos.y <= 1.1f)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(targetWorldPos);

                Canvas canvas = lockOnMarkerUI.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                    Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    Vector2 localPoint;

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out localPoint))
                    {
                        lockOnMarkerUI.localPosition = Vector2.Lerp(lockOnMarkerUI.localPosition, localPoint, Time.unscaledDeltaTime * 25f);
                    }
                }

                if (!lockOnMarkerUI.gameObject.activeSelf)
                {
                    lockOnMarkerUI.gameObject.SetActive(true);
                }
            }
            else
            {
                if (lockOnMarkerUI.gameObject.activeSelf)
                {
                    lockOnMarkerUI.gameObject.SetActive(false);
                }
            }
        }
        else if (lockOnMarkerUI != null && lockOnMarkerUI.gameObject.activeSelf)
        {
            lockOnMarkerUI.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (isTalking || statusWindowFlag || Time.timeScale == 0) return;
        MovePlayer();
        ApplyActionGravity();
    }

    private void HandleLockOnInput()
    {
        if (mouseSwitchTimer > 0f)
        {
            mouseSwitchTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(2))
        {
            if (lockedTarget == null)
            {
                Transform best = FindBestLockOnTarget();
                if (best != null)
                {
                    LockOnTo(best);
                }
            }
            else
            {
                ReleaseLockOn();
            }
        }

        if (lockedTarget != null)
        {
            EnemyManager enemy = lockedTarget.GetComponent<EnemyManager>();
            float distance = Vector3.Distance(transform.position, lockedTarget.position);

            if (enemy == null || enemy.IsDead)
            {
                if (autoSwitchTargetOnDefeat)
                {
                    Transform nextTarget = FindBestLockOnTarget(excludeTarget: lockedTarget);
                    if (nextTarget != null)
                    {
                        LockOnTo(nextTarget);
                        return;
                    }
                }
                ReleaseLockOn();
                return;
            }

            if (distance > lockOnBreakDistance)
            {
                ReleaseLockOn();
                return;
            }

            bool isVisible = true;
            if (checkLineOfSight && mainCamera != null)
            {
                Vector3 origin = mainCamera.transform.position;
                Vector3 targetCenter = lockedTarget.position + Vector3.up * 1.0f;
                Vector3 dir = targetCenter - origin;

                if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, obstacleLayerMask))
                {
                    if (hit.transform != lockedTarget && !hit.transform.IsChildOf(lockedTarget))
                    {
                        isVisible = false;
                    }
                }
            }

            if (!isVisible)
            {
                lostTargetTimer += Time.deltaTime;
                if (lostTargetTimer >= lostTargetGracePeriod)
                {
                    ReleaseLockOn();
                    return;
                }
            }
            else
            {
                lostTargetTimer = 0f;
            }

            HandleTargetSwitching();
        }
    }

    private void HandleTargetSwitching()
    {
        if (mouseSwitchTimer > 0f || mainCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        bool keyLeft = Input.GetKeyDown(KeyCode.LeftArrow);
        bool keyRight = Input.GetKeyDown(KeyCode.RightArrow);

        int switchDirection = 0;

        if (mouseX > mouseSwitchThreshold || scroll > 0.05f || keyRight)
        {
            switchDirection = 1;
        }
        else if (mouseX < -mouseSwitchThreshold || scroll < -0.05f || keyLeft)
        {
            switchDirection = -1;
        }

        if (switchDirection != 0)
        {
            Transform nextTarget = FindAdjacentTarget(switchDirection);
            if (nextTarget != null && nextTarget != lockedTarget)
            {
                LockOnTo(nextTarget);
                mouseSwitchTimer = mouseSwitchCooldown;

                if (audioSource != null && lockSwitchSound != null)
                {
                    audioSource.PlayOneShot(lockSwitchSound);
                }
            }
        }
    }

    private Transform FindAdjacentTarget(int direction)
    {
        if (mainCamera == null || lockedTarget == null) return null;

        Vector3 currentScreenPos = mainCamera.WorldToViewportPoint(lockedTarget.position);
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        Transform bestAdjacent = null;
        float minDeltaX = Mathf.Infinity;

        foreach (GameObject enemyObj in enemies)
        {
            if (enemyObj.transform == lockedTarget) continue;

            EnemyManager em = enemyObj.GetComponent<EnemyManager>();
            if (em != null && em.IsDead) continue;

            float dist = Vector3.Distance(transform.position, enemyObj.transform.position);
            if (dist > lockOnRange) continue;

            Vector3 vp = mainCamera.WorldToViewportPoint(enemyObj.transform.position);
            if (vp.z <= 0) continue;

            float deltaX = vp.x - currentScreenPos.x;

            if (direction > 0 && deltaX > 0.05f)
            {
                if (deltaX < minDeltaX)
                {
                    minDeltaX = deltaX;
                    bestAdjacent = enemyObj.transform;
                }
            }
            else if (direction < 0 && deltaX < -0.05f)
            {
                float absDelta = Mathf.Abs(deltaX);
                if (absDelta < minDeltaX)
                {
                    minDeltaX = absDelta;
                    bestAdjacent = enemyObj.transform;
                }
            }
        }

        return bestAdjacent;
    }

    private Transform FindBestLockOnTarget(Transform excludeTarget = null)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        if (mainCamera == null) mainCamera = Camera.main;

        Transform bestTarget = null;
        float bestScore = Mathf.Infinity;

        Vector3 camPos = mainCamera != null ? mainCamera.transform.position : transform.position;
        Vector3 camForward = mainCamera != null ? mainCamera.transform.forward : transform.forward;

        foreach (GameObject enemy in enemies)
        {
            if (excludeTarget != null && enemy.transform == excludeTarget) continue;

            EnemyManager manager = enemy.GetComponent<EnemyManager>();
            if (manager != null && manager.IsDead) continue;

            Vector3 enemyPos = enemy.transform.position;
            float distToPlayer = Vector3.Distance(transform.position, enemyPos);

            if (distToPlayer > lockOnRange) continue;

            Vector3 viewportPos = Vector3.zero;
            bool isInViewport = false;
            if (mainCamera != null)
            {
                viewportPos = mainCamera.WorldToViewportPoint(enemyPos + Vector3.up * 1.0f);
                isInViewport = (viewportPos.z > 0 &&
                                viewportPos.x >= 0.05f && viewportPos.x <= 0.95f &&
                                viewportPos.y >= 0.05f && viewportPos.y <= 0.95f);
            }

            if (checkLineOfSight && mainCamera != null)
            {
                Vector3 rayDir = (enemyPos + Vector3.up * 1.0f) - camPos;
                if (Physics.Raycast(camPos, rayDir.normalized, out RaycastHit hit, rayDir.magnitude, obstacleLayerMask))
                {
                    if (hit.transform != enemy.transform && !hit.transform.IsChildOf(enemy.transform))
                    {
                        continue;
                    }
                }
            }

            float screenOffset = 1.0f;
            if (isInViewport)
            {
                Vector2 centerDiff = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);
                screenOffset = centerDiff.magnitude;
            }
            else
            {
                screenOffset = 1.5f;
            }

            float distanceRatio = Mathf.Clamp01(distToPlayer / lockOnRange);

            Vector3 dirFromCam = (enemyPos - camPos).normalized;
            float angleDot = Mathf.Clamp01(Vector3.Dot(camForward, dirFromCam));
            float angleScore = 1.0f - angleDot;

            float finalScore = (screenOffset * screenCenterWeight) +
                               (distanceRatio * distanceWeight) +
                               (angleScore * angleWeight);

            if (finalScore < bestScore)
            {
                bestScore = finalScore;
                bestTarget = enemy.transform;
            }
        }

        return bestTarget;
    }

    private void LockOnTo(Transform target)
    {
        lockedTarget = target;
        lostTargetTimer = 0f;

        if (lockOnMarkerUI != null)
        {
            lockOnMarkerUI.gameObject.SetActive(true);
        }

        if (audioSource != null && lockOnSound != null)
        {
            audioSource.PlayOneShot(lockOnSound);
        }
    }

    private void ReleaseLockOn()
    {
        lockedTarget = null;
        lostTargetTimer = 0f;

        if (parryTargetCamera != null && hasSavedOriginalLookAt)
        {
            parryTargetCamera.LookAt = originalLookAt;
        }

        if (lockOnMarkerUI != null)
        {
            lockOnMarkerUI.gameObject.SetActive(false);
        }
    }

    private void MovePlayer()
    {
        cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
        moveForward = cameraForward * inputVertical + Camera.main.transform.right * inputHorizontal;

        float currentSpeed = speed;
        if (isJumping || !isGrounded)
        {
            currentSpeed = speed * airMoveMultiplier;
        }
        else if (isDash)
        {
            currentSpeed = speed * dashSpeedMultiplier;
        }

        Vector3 currentVelocity = new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z);
        Vector3 targetVelocity = moveForward * currentSpeed;
        Vector3 velocityChange = targetVelocity - currentVelocity;

        rigidBody.AddForce(velocityChange, ForceMode.VelocityChange);

        if (lockedTarget != null && strafeWhileLockedOn)
        {
            Vector3 lookDir = lockedTarget.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
        else if (moveForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void FaceNearestEnemy()
    {
        if (lockedTarget != null)
        {
            Vector3 dir = lockedTarget.position - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
            return;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            EnemyManager manager = enemy.GetComponent<EnemyManager>();
            if (manager != null && manager.IsDead) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance && distance <= autoTargetRange)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        if (closestEnemy != null)
        {
            Vector3 targetDir = closestEnemy.position - transform.position;
            targetDir.y = 0;

            if (targetDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(targetDir);
            }
        }
    }

    private void UpdateCameraZoom()
    {
        if (parryTargetCamera == null)
        {
            parryTargetCamera = FindAnyObjectByType<CinemachineCamera>();
            if (parryTargetCamera != null && !hasSavedOriginalLookAt)
            {
                originalLookAt = parryTargetCamera.LookAt;
                hasSavedOriginalLookAt = true;
            }
            if (parryTargetCamera == null) return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseX) > 0.05f || Mathf.Abs(mouseY) > 0.05f)
        {
            noInputTimer = 0f;
        }
        else
        {
            noInputTimer += Time.unscaledDeltaTime;
        }

        if (lockedTarget != null && lockOnCenterPoint != null)
        {
            Vector3 playerCenter = transform.position + Vector3.up * 1.2f;
            Vector3 enemyCenter = lockedTarget.position + Vector3.up * 1.0f;
            Vector3 midPoint = Vector3.Lerp(playerCenter, enemyCenter, 0.45f);

            lockOnCenterPoint.position = Vector3.Lerp(lockOnCenterPoint.position, midPoint, Time.unscaledDeltaTime * 12f);
            parryTargetCamera.LookAt = lockOnCenterPoint;

            var orbitalFollow = parryTargetCamera.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow != null && lockOnCameraTrackingSpeed > 0f)
            {
                Vector3 toEnemy = lockedTarget.position - transform.position;
                toEnemy.y = 0;
                if (toEnemy != Vector3.zero)
                {
                    float targetYaw = Quaternion.LookRotation(toEnemy).eulerAngles.y;
                    float currentYaw = parryTargetCamera.transform.eulerAngles.y;
                    float angleDiff = Mathf.DeltaAngle(currentYaw, targetYaw);

                    if (noInputTimer > 0.2f && Mathf.Abs(angleDiff) > 1.0f)
                    {
                        orbitalFollow.HorizontalAxis.Value += angleDiff * Time.unscaledDeltaTime * lockOnCameraTrackingSpeed;
                    }
                }
            }
        }
        else
        {
            if (originalLookAt != null && parryTargetCamera.LookAt != originalLookAt)
            {
                parryTargetCamera.LookAt = originalLookAt;
            }

            if (enableMagnetism && noInputTimer >= magnetismDelay && !isUltimateAttacking)
            {
                Transform nearest = FindBestLockOnTarget();
                if (nearest != null)
                {
                    var orbitalFollow = parryTargetCamera.GetComponent<CinemachineOrbitalFollow>();
                    if (orbitalFollow != null)
                    {
                        Vector3 dirToEnemy = nearest.position - parryTargetCamera.transform.position;
                        dirToEnemy.y = 0;

                        if (dirToEnemy != Vector3.zero)
                        {
                            float targetYaw = Quaternion.LookRotation(dirToEnemy).eulerAngles.y;
                            float currentYaw = parryTargetCamera.transform.eulerAngles.y;
                            float angleDiff = Mathf.DeltaAngle(currentYaw, targetYaw);

                            if (Mathf.Abs(angleDiff) > 0.5f)
                            {
                                orbitalFollow.HorizontalAxis.Value += angleDiff * Time.unscaledDeltaTime * magnetismSpeed;
                            }
                        }
                    }
                }
            }
        }

        if (baseFov < 0 && parryTargetCamera != null)
        {
            baseFov = parryTargetCamera.Lens.FieldOfView;
        }

        float targetMultiplier = 1.0f;
        if (IsParryWindowActive || parryTimer > 0)
        {
            targetMultiplier = zoomFovMultiplier;
        }
        else if (isDash && !isUltimateAttacking)
        {
            targetMultiplier = dashFovMultiplier;
        }

        float currentZoomSpeed = (targetMultiplier < currentFovMultiplier) ? zoomInSpeed : zoomOutSpeed;
        currentFovMultiplier = Mathf.Lerp(currentFovMultiplier, targetMultiplier, Time.unscaledDeltaTime * currentZoomSpeed);
        targetFov = baseFov * currentFovMultiplier;

        if (parryTargetCamera != null)
        {
            parryTargetCamera.Lens.FieldOfView = targetFov;
        }

        // アルティメット中は傾かない（シェイクのみ）
        float targetDutch = 0f;
        if (IsParryWindowActive || parryTimer > 0) targetDutch = parryDutchAngle;
        if (isUltimateAttacking) targetDutch = ultimateDutchAngle;

        if (parryTargetCamera != null)
        {
            parryTargetCamera.Lens.Dutch = Mathf.Lerp(
                parryTargetCamera.Lens.Dutch,
                targetDutch,
                Time.unscaledDeltaTime * 10.0f
            );
        }
    }

    private void UpdateBattleState()
    {
        if (bgmAudioSource == null || normalBGM == null || battleBGM == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        bool enemyNearby = false;

        foreach (GameObject enemy in enemies)
        {
            EnemyManager manager = enemy.GetComponent<EnemyManager>();
            if (manager != null && manager.IsDead) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= battleDetectRange)
            {
                enemyNearby = true;
                break;
            }
        }

        if (enemyNearby && !isBattleState)
        {
            isBattleState = true;
            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeBGM(battleBGM));
        }
        else if (!enemyNearby && isBattleState)
        {
            isBattleState = false;
            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeBGM(normalBGM));
        }
    }

    private IEnumerator FadeBGM(AudioClip nextClip)
    {
        float startVol = bgmAudioSource.volume;
        float halfDuration = bgmFadeDuration / 2f;

        for (float t = 0; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            bgmAudioSource.volume = Mathf.Lerp(startVol, 0f, t / halfDuration);
            yield return null;
        }
        bgmAudioSource.volume = 0f;

        bgmAudioSource.clip = nextClip;
        bgmAudioSource.Play();

        for (float t = 0; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            bgmAudioSource.volume = Mathf.Lerp(0f, maxBgmVolume, t / halfDuration);
            yield return null;
        }
        bgmAudioSource.volume = maxBgmVolume;
    }

    private void ApplyActionGravity()
    {
        if (rigidBody == null) return;

        if (rigidBody.linearVelocity.y < 0)
        {
            rigidBody.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rigidBody.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rigidBody.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void UpdateTimers()
    {
        if (damageCooldown > 0) damageCooldown -= Time.deltaTime;

        if (comboStep > 0)
        {
            comboFailsafeTimer -= Time.deltaTime;
            if (comboFailsafeTimer <= 0f)
            {
                ResetCombo();
            }
        }
    }

    private void UpdateStatusUI()
    {
        if (playerStatusSO == null) return;
        string hpStr = $"HP: {currentHP}/{playerStatusSO.HP}";
        string mpStr = $"MP: {currentMP}/{playerStatusSO.MP}";
        if (hpText != null) hpText.text = hpStr;
        if (mpText != null) mpText.text = mpStr;
        if (hpSlider != null)
        {
            hpSlider.maxValue = playerStatusSO.HP;
            hpSlider.value = currentHP;
        }
    }

    private void HandleGlobalInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleStatusWindow();
        }
    }

    private void HandleMovementInput()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal");
        inputVertical = Input.GetAxisRaw("Vertical");

        if (comboStep > 0)
        {
            inputHorizontal = 0f;
            inputVertical = 0f;
        }

        bool isMoving = (inputHorizontal != 0 || inputVertical != 0);

        if (animator != null)
        {
            animator.SetBool("Run", isMoving && !isDash);
            if (comboStep > 0) animator.SetBool("Run", false);
        }
    }

    private void HandleDashInput()
    {
        if (comboStep > 0)
        {
            isDash = false;
            if (animator != null) animator.SetBool("Dash", false);
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift) && (inputHorizontal != 0 || inputVertical != 0))
        {
            isDash = true;
            if (animator != null) animator.SetBool("Dash", true);
        }
        else
        {
            isDash = false;
            if (animator != null) animator.SetBool("Dash", false);
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && comboStep == 0)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            coyoteTimeCounter = 0f;
        }

        if (jumpBufferCounter > 0f && comboStep == 0)
        {
            if (isTouchingWall && !isGrounded)
            {
                PerformWallJump();
                jumpBufferCounter = 0f;
            }
            else if (coyoteTimeCounter > 0f)
            {
                PerformGroundJump();
                jumpBufferCounter = 0f;
            }
            else if (extraJumpsAvailable > 0 && isJumping)
            {
                PerformExtraJump();
                jumpBufferCounter = 0f;
            }
        }
    }

    private void PerformGroundJump()
    {
        rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, jumpPower, rigidBody.linearVelocity.z);
        isJumping = true;
        extraJumpsAvailable = maxExtraJumps;
        coyoteTimeCounter = 0f;

        fallAnimationDelayTimer = 0.2f;

        if (animator != null)
        {
            animator.SetBool("Jump", true);
            animator.SetBool("IsFalling", false);
        }
        PlayJumpSound();
    }

    private void PerformExtraJump()
    {
        rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, extraJumpPower, rigidBody.linearVelocity.z);
        extraJumpsAvailable--;

        fallAnimationDelayTimer = 0.4f;

        if (animator != null)
        {
            animator.SetTrigger("ExtraJump");
            animator.SetBool("IsFalling", false);
        }
        PlayJumpSound();
    }

    private void PerformWallJump()
    {
        Vector3 wallJumpForce = (Vector3.up * wallJumpUpForce) + (currentWallNormal * wallJumpKickback);
        rigidBody.linearVelocity = wallJumpForce;

        if (currentWallNormal != Vector3.zero)
        {
            Vector3 lookDir = new Vector3(currentWallNormal.x, 0, currentWallNormal.z);
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        isTouchingWall = false;
        extraJumpsAvailable = maxExtraJumps;

        fallAnimationDelayTimer = 0.3f;

        if (animator != null)
        {
            animator.SetTrigger("WallJump");
            animator.SetBool("IsFalling", false);
        }
        PlayJumpSound();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, lockOnBreakDistance);
    }

    private void HandleUltimateInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentAdrenaline >= maxAdrenaline)
            {
                ExecuteUltimateAttack();
            }
            else
            {
                Debug.Log("必殺技にはアドレナリンがMAXである必要があります！");
            }
        }
    }

    private void ExecuteUltimateAttack()
    {
        ResetCombo();
        isUltimateAttacking = true;
        isUltimateFinishing = false;
        isSuperArmor = true;

        if (cinemachineInput != null)
        {
            cinemachineInput.enabled = false;
        }

        currentAdrenaline = 0f;
        wasAdrenalineMax = false;
        if (adrenalineGaugeUI != null) adrenalineGaugeUI.OnAdrenalineReset();
        if (adrenalineSlider != null) adrenalineSlider.value = currentAdrenaline;
        UpdateAdrenalineEffect();

        if (lockedTarget == null)
        {
            Transform nearest = FindBestLockOnTarget();
            if (nearest != null)
            {
                LockOnTo(nearest);
            }
            else
            {
                FaceNearestEnemy();
            }
        }
        else
        {
            FaceNearestEnemy();
        }

        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetTrigger("Ultimate");
        }

        if (audioSource != null && ultimateSound != null)
        {
            audioSource.PlayOneShot(ultimateSound);
        }

        if (ultimateEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.0f;
            GameObject effect = Instantiate(ultimateEffectPrefab, spawnPos, transform.rotation);
            Destroy(effect, 3.0f);
        }

        if (ImpactFrameManager.Instance != null)
        {
            ImpactFrameManager.Instance.PlayImpact(new Color(1f, 0.8f, 0f), 0.1f, 0.4f);
        }
        TriggerHitStop(0.2f);

        ShowFloatingText("ULTIMATE!", transform, Color.yellow);

        StartCoroutine(UltimateComboRoutine());
    }

    private IEnumerator UltimateComboRoutine()
    {
        Transform targetEnemy = lockedTarget;

        yield return new WaitForSeconds(0.2f);

        float[,] hitTimings = new float[,]
        {
            { 0.0f, 0.15f },
            { 0.35f, 0.15f },
            { 0.35f, 0.15f },
            { 0.35f, 0.15f },
            { 0.45f, 0.15f },
            { 0.55f, 0.2f  }
        };

        for (int i = 0; i < hitTimings.GetLength(0); i++)
        {
            float delay = hitTimings[i, 0];
            float duration = hitTimings[i, 1];

            if (delay > 0) yield return new WaitForSeconds(delay);

            // 最終段判定
            bool isFinish = (i == 5);
            isUltimateFinishing = isFinish;

            if (targetEnemy != null)
            {
                Vector3 lookDir = targetEnemy.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }

                float stopTime = isFinish ? 0.4f : 0.08f;
                TriggerHitStop(stopTime);
            }
            else
            {
                if (rigidBody != null)
                {
                    rigidBody.linearVelocity = Vector3.zero;
                    float currentForce = isFinish ? attackForwardForce * 2f : attackForwardForce;
                    rigidBody.AddForce(transform.forward * currentForce, ForceMode.VelocityChange);
                }
            }

            // =================================================================
            // ★【カメラシェイクのみ実装】
            // FOVズームやDutchチルト（画面傾き）は行わず、Cinemachine Impulseのみ発動
            // =================================================================
            var impulse = GetComponent<CinemachineImpulseSource>();
            if (impulse != null)
            {
                float shakeIntensity = isFinish ? ultimateFinishShakeIntensity : ultimateShakeIntensity;
                impulse.GenerateImpulseWithForce(shakeIntensity);
            }

            // =================================================================
            // ★【最終段専用SE】
            // 最終段(isFinish)では ultimateFinishSound を最優先再生
            // =================================================================
            if (audioSource != null)
            {
                AudioClip clipToPlay = isFinish
                    ? (ultimateFinishSound != null ? ultimateFinishSound : attackSound2)
                    : attackSound;

                if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
            }

            if (attackEffectPrefab != null)
            {
                Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.0f;
                GameObject effect = Instantiate(attackEffectPrefab, spawnPos, transform.rotation);
                Destroy(effect, 1.0f);
            }

            if (weaponCollider != null) weaponCollider.enabled = true;

            yield return new WaitForSeconds(duration);

            DisableWeaponCollider();
        }

        yield return new WaitForSeconds(1.0f);

        isUltimateAttacking = false;
        isUltimateFinishing = false;
        isSuperArmor = false;

        targetFov = baseFov;
        ultimateDutchAngle = 0f;

        if (cinemachineInput != null)
        {
            cinemachineInput.enabled = true;
        }
    }

    private void UpdateAirborneState()
    {
        if (animator == null || rigidBody == null) return;

        Vector3 origin = transform.position + groundCheckOffset;
        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundLayer);

        if (isGrounded && !isJumping)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (fallAnimationDelayTimer > 0f)
        {
            fallAnimationDelayTimer -= Time.deltaTime;
            animator.SetBool("IsFalling", false);
            return;
        }

        if (!isGrounded && rigidBody.linearVelocity.y < -0.5f)
        {
            isJumping = true;

            if (comboStep == 0)
            {
                animator.SetBool("IsFalling", true);
            }
        }
        else if (isGrounded)
        {
            if (isJumping && rigidBody.linearVelocity.y <= 0f)
            {
                if (comboStep == 0)
                {
                    animator.SetTrigger("Land");
                }
                PlayFootstepSound();
                isJumping = false;
                isTouchingWall = false;
                extraJumpsAvailable = maxExtraJumps;

                animator.SetBool("Jump", false);
                animator.SetBool("IsFalling", false);
            }
        }
    }

    private void HandleParryInput()
    {
        if (Input.GetMouseButtonDown(1) && !isCounterAttacking)
        {
            if (comboStep > 0)
            {
                IsComboCancelParry = true;
                ResetCombo();
            }
            else
            {
                IsComboCancelParry = false;
            }

            parryTimer = parryWindowDuration;
            if (animator != null) animator.SetTrigger("Parry");
            ZoomIn();
        }

        if (parryTimer > 0)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0)
            {
                ZoomOut();
                IsComboCancelParry = false;
            }
        }
    }

    private void HandleComboInput()
    {
        if (statusWindowFlag || Time.timeScale == 0) return;
        if (isCounterAttacking) return;

        bool isNearNPC = dogManager != null && dogManager.IsPlayerNearby;

        if (Input.GetMouseButtonDown(0) && !isNearNPC)
        {
            if (IsParryCounterAvailable)
            {
                ExecuteParryCounterAttack();
                return;
            }

            if (comboStep == 0)
            {
                StartCombo();
            }
            else if (comboStep < MAX_COMBO)
            {
                if (hasInputNextCombo) return;

                hasInputNextCombo = true;

                if (canTransition)
                {
                    ProgressCombo();
                }
            }
        }
    }

    private void ExecuteParryCounterAttack()
    {
        parryCounterTimer = 0f;
        DisableWeaponCollider();
        isCounterAttacking = true;

        if (currentAdrenaline >= maxAdrenaline)
        {
            isMaxAdrenalineCounter = true;
            isAdrenalineCounter = false;
        }
        else if (currentAdrenaline >= 50f)
        {
            isAdrenalineCounter = true;
            isMaxAdrenalineCounter = false;
        }
        else
        {
            isAdrenalineCounter = false;
            isMaxAdrenalineCounter = false;
        }

        Transform targetEnemy = lockedTarget;
        if (targetEnemy == null)
        {
            targetEnemy = FindBestLockOnTarget();
        }

        if (targetEnemy != null)
        {
            Vector3 targetDir = (targetEnemy.position - transform.position);
            targetDir.y = 0;
            targetDir.Normalize();

            if (targetDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(targetDir);
            }

            Vector3 targetPosition;
            Collider enemyCollider = targetEnemy.GetComponentInChildren<Collider>();

            if (enemyCollider != null)
            {
                Vector3 edgePos = enemyCollider.ClosestPoint(transform.position);
                float bufferDistance = 1.5f;
                targetPosition = edgePos - (targetDir * bufferDistance);
            }
            else
            {
                float hitDistance = 2.0f;
                targetPosition = targetEnemy.position - (targetDir * hitDistance);
            }

            targetPosition.y = transform.position.y;

            if (rigidBody != null) rigidBody.position = targetPosition;
            else transform.position = targetPosition;
        }
        else
        {
            FaceNearestEnemy();
            if (rigidBody != null)
            {
                rigidBody.linearVelocity = Vector3.zero;
                rigidBody.AddForce(transform.forward * counterForwardForce, ForceMode.VelocityChange);
            }
        }

        if (counterEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 1.0f + Vector3.up * 1.0f;
            GameObject effect = Instantiate(counterEffectPrefab, spawnPos, transform.rotation);
            Destroy(effect, 1.5f);
        }

        if (ImpactFrameManager.Instance != null)
        {
            ImpactFrameManager.Instance.PlayImpact(Color.black, 0.05f, 0.3f);
        }

        if (animator != null)
        {
            animator.SetTrigger("ParryCounter");
        }

        if (audioSource != null && counterAttackSound != null)
        {
            audioSource.PlayOneShot(counterAttackSound);
        }

        StartCoroutine(CounterAttackCollisionRoutine(0.15f, 0.4f));
        StartCoroutine(ResetCounterAttackStateRoutine(1.2f));
    }

    public void OnParriedByEnemy(Transform enemyTransform)
    {
        ResetCombo();
        StartCoroutine(EnemyParrySlowRoutine());

        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector3.zero;
            Vector3 knockbackDir = transform.position - enemyTransform.position;
            knockbackDir.y = 0;
            rigidBody.AddForce(knockbackDir.normalized * 6f, ForceMode.Impulse);
        }

        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }

        ShowFloatingText("BLOCKED!", transform, Color.gray);
    }

    private IEnumerator EnemyParrySlowRoutine()
    {
        Time.timeScale = 0.2f;

        if (chromaticAberration != null) chromaticAberration.intensity.value = 1.0f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.2f;

        yield return new WaitForSecondsRealtime(0.6f);

        if (!statusWindowFlag)
        {
            Time.timeScale = 1.0f;
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
    }

    private IEnumerator CounterAttackCollisionRoutine(float enableDelay, float disableDelay)
    {
        yield return new WaitForSecondsRealtime(enableDelay);
        if (weaponCollider != null) weaponCollider.enabled = true;
        yield return new WaitForSecondsRealtime(disableDelay - enableDelay);
        DisableWeaponCollider();
    }

    private void PlayJumpSound()
    {
        if (jumpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }

    private void ProgressCombo()
    {
        comboStep++;
        hasInputNextCombo = false;
        canTransition = false;
        ExecuteAttack();
    }

    private void PlayFootstepSound()
    {
        if (footstepSound != null && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(footstepSound);
        }
    }

    private void StartCombo()
    {
        comboStep = 1;
        ExecuteAttack();
    }

    public void OnAnimationEvent_AllowNextCombo()
    {
        canTransition = true;
        if (hasInputNextCombo && comboStep < MAX_COMBO)
        {
            ProgressCombo();
        }
    }

    public void OnAnimationEvent_TryProgressCombo()
    {
        if (!canTransition) return;
        canTransition = false;
        ResetCombo();
    }

    public void ResetCombo()
    {
        comboStep = 0;
        hasInputNextCombo = false;
        canTransition = false;
        isCounterAttacking = false;
        if (animator != null) animator.SetInteger("Combo", 0);
        DisableWeaponCollider();
    }

    private IEnumerator ResetCounterAttackStateRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (isCounterAttacking)
        {
            IsComboCancelParry = false;
            isAdrenalineCounter = false;
            isMaxAdrenalineCounter = false;
            ResetCombo();
        }
    }

    private void ExecuteAttack()
    {
        comboFailsafeTimer = COMBO_TIMEOUT;

        FaceNearestEnemy();

        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector3.zero;
            float currentForce = (comboStep == MAX_COMBO) ? attackForwardForce * 2f : attackForwardForce;
            rigidBody.AddForce(transform.forward * currentForce, ForceMode.VelocityChange);
        }

        if (animator != null) animator.SetInteger("Combo", comboStep);

        PlayAttackSound_Dynamic(comboStep);

        if (comboStep == MAX_COMBO)
        {
            if (ImpactFrameManager.Instance != null)
            {
                ImpactFrameManager.Instance.PlayImpact(new Color(0f, 1f, 1f), 0.04f, 0.25f);
            }
        }

        if (attackEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.0f;
            GameObject effect = Instantiate(attackEffectPrefab, spawnPos, transform.rotation);
            Destroy(effect, 1.0f);
        }

        StartCoroutine(AttackCollisionRoutine(0.15f, 0.35f));
    }

    private IEnumerator AttackCollisionRoutine(float enableDelay, float disableDelay)
    {
        yield return new WaitForSecondsRealtime(enableDelay);
        if (comboStep > 0 && weaponCollider != null)
        {
            weaponCollider.enabled = true;
        }
        yield return new WaitForSecondsRealtime(disableDelay - enableDelay);
        DisableWeaponCollider();
    }

    public void EnableWeaponCollider()
    {
        if (comboStep == 0 && !IsParryCounterAvailable) return;
        if (weaponCollider != null) weaponCollider.enabled = true;
    }

    public void DisableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = false;
    }

    private void ToggleStatusWindow()
    {
        if (statusWindow == null) return;
        statusWindowFlag = !statusWindowFlag;

        if (statusWindowFlag)
        {
            itemBoxManager?.GetComponent<ItemBoxManager>()?.ItemOpen();
            statusWindow.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            statusWindow.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ZoomIn()
    {
        if (baseFov < 0 && parryTargetCamera != null)
        {
            baseFov = parryTargetCamera.Lens.FieldOfView;
        }
        targetFov = baseFov * zoomFovMultiplier;
    }

    private void ZoomOut()
    {
        if (baseFov > 0)
        {
            targetFov = baseFov;
        }
    }

    private IEnumerator DelayedZoomOut(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ZoomOut();
    }

    private IEnumerator ForceDisableWeaponRoutine()
    {
        yield return new WaitForSecondsRealtime(0.05f);
        DisableWeaponCollider();
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "EnemyAttack")
        {
            if (IsParryWindowActive)
            {
                parryTimer = 0f;
                ShowFloatingText("PARRY!", transform, Color.cyan);

                float parryAdrenalineGain = 30f;
                AddAdrenaline(parryAdrenalineGain);

                DisableWeaponCollider();
                TriggerHitStop(0.6f);
                StartCoroutine(TriggerParryBGMFilter(0.6f));
                targetFov = baseFov * (zoomFovMultiplier * 0.9f);
                StartCoroutine(TriggerParryPostProcessEffect());

                if (animator != null) animator.SetTrigger("ParrySuccess");

                parryCounterTimer = parryCounterWindow;

                if (rigidBody != null)
                {
                    rigidBody.linearVelocity = new Vector3(0, rigidBody.linearVelocity.y, 0);
                    rigidBody.AddForce(-transform.forward * parryBackstepForce, ForceMode.VelocityChange);
                }

                if (parrySuccessEffectPrefab != null)
                {
                    Vector3 spawnPos = transform.position + Vector3.up * 1.0f;
                    GameObject effect = Instantiate(parrySuccessEffectPrefab, spawnPos, transform.rotation);
                    Destroy(effect, 1.5f);
                }

                StartCoroutine(DelayedZoomOut(0.4f));

                EnemyManager enemy = collision.transform.root.GetComponent<EnemyManager>();
                if (enemy != null)
                {
                    enemy.OnParried(this.transform);
                }

                StartCoroutine(ForceDisableWeaponRoutine());
                return;
            }
            else
            {
                if (damageCooldown > 0f) return;

                if (isSuperArmor)
                {
                    Vector3 pos = collision.ClosestPoint(transform.position);
                    pos.y += 1.0f;
                    PlayHitEffect(pos);
                    ShowFloatingText("GUARD!", transform, Color.yellow);
                    return;
                }

                int damage = 10;
                if (isJumping) damage = Mathf.RoundToInt(damage * 1.5f);

                currentHP -= damage;
                if (currentHP < 0) currentHP = 0;

                damageCooldown = 0.5f;

                ShowFloatingText($"-{damage}", transform, Color.red);
                UpdateStatusUI();
                TriggerHitStop(0.1f);
                PlayHitSound();

                Vector3 hitPos = collision.ClosestPoint(transform.position);
                hitPos.y += 1.0f;
                PlayHitEffect(hitPos);

                IsComboCancelParry = false;
                isAdrenalineCounter = false;
                isMaxAdrenalineCounter = false;
                ResetCombo();

                if (rigidBody != null)
                {
                    rigidBody.linearVelocity = new Vector3(0, rigidBody.linearVelocity.y, 0);
                }

                if (animator != null)
                {
                    animator.SetTrigger("TakeDamage");
                }

                if (currentHP <= 0)
                {
                    PlayerDie();
                }

                return;
            }
        }

        if (collision.gameObject.tag == "Item")
        {
            Destroy(collision.gameObject);
            var ibm = itemBoxManager.GetComponent<ItemBoxManager>();
            ibm.getItem = collision.gameObject.GetComponent<ItemManager>().ItemNo;
            ibm.ItemGet();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = true;
            if (collision.contacts.Length > 0)
            {
                currentWallNormal = collision.contacts[0].normal;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = false;
        }
    }

    private void PlayerDie()
    {
        if (animator != null) animator.SetBool("IsDead", true);
        DisableWeaponCollider();
        ResetCombo();
        this.enabled = false;
        StartCoroutine(GameOverSequenceRoutine());
    }

    private IEnumerator GameOverSequenceRoutine()
    {
        Time.timeScale = 0.2f;
        if (bgmLowPassFilter != null) bgmLowPassFilter.cutoffFrequency = 400f;
        if (lensDistortion != null) lensDistortion.intensity.value = -0.5f;

        yield return new WaitForSecondsRealtime(2.5f);

        Time.timeScale = 0f;
        if (bgmAudioSource != null) bgmAudioSource.Stop();
        GameOverUIManager.Instance?.ShowGameOverScreen();
    }

    private void PlayAttackSound_Dynamic(int step)
    {
        if (step == 4) audioSource.PlayOneShot(attackSound2);
        else audioSource.PlayOneShot(attackSound);
    }

    public void PlayHitSound()
    {
        if (audioSource != null)
        {
            if (isUltimateAttacking)
            {
                if (isUltimateFinishing && ultimateFinishHitSound != null)
                {
                    audioSource.PlayOneShot(ultimateFinishHitSound);
                }
                else if (ultimateHitSound != null)
                {
                    audioSource.PlayOneShot(ultimateHitSound);
                }
                else if (hitSound != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }
            }
            else if (hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
    }

    public void PlayHitEffect(Vector3 hitPosition)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
            Destroy(effect, 1.0f);
        }
    }

    public void ShowFloatingText(string message, Transform targetTransform, Color textColor)
    {
        if (floatingTextPrefab != null)
        {
            GameObject textObj = Instantiate(floatingTextPrefab, targetTransform.position, Quaternion.identity);
            FloatingText floatingText = textObj.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                Vector3 initialOffset = Vector3.up * 1.5f + Random.insideUnitSphere * 0.5f;
                floatingText.Setup(targetTransform, initialOffset);
            }

            TextMeshPro tmpro = textObj.GetComponent<TextMeshPro>();
            if (tmpro != null)
            {
                tmpro.text = message;
                tmpro.color = textColor;
            }
        }
    }

    public void TriggerHitStop(float stopTime = 0.1f)
    {
        StartCoroutine(HitStopRoutine(stopTime));

        // 通常コンボ時のみここでImpulseを生成（アルティメット中は専用シェイク強度で制御）
        if (!isUltimateAttacking)
        {
            float shakeMag = (comboStep == 4) ? finisherShakeMag : normalShakeMag;
            var impulse = GetComponent<CinemachineImpulseSource>();
            if (impulse != null)
            {
                impulse.GenerateImpulseWithForce(shakeMag);
            }
        }
    }

    private IEnumerator HitStopRoutine(float stopTime)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(stopTime);
        if (!statusWindowFlag)
        {
            Time.timeScale = 1.0f;
        }
    }

    private IEnumerator TriggerParryBGMFilter(float keepDuration)
    {
        if (bgmLowPassFilter == null) yield break;
        bgmLowPassFilter.cutoffFrequency = parryLowPassCutoff;

        yield return new WaitForSecondsRealtime(keepDuration);

        float fadeTime = 0.8f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmLowPassFilter.cutoffFrequency = Mathf.Lerp(parryLowPassCutoff, defaultLowPassCutoff, elapsed / fadeTime);
            yield return null;
        }

        bgmLowPassFilter.cutoffFrequency = defaultLowPassCutoff;
    }

    public void AddAdrenaline(float amount)
    {
        currentAdrenaline = Mathf.Clamp(currentAdrenaline + amount, 0f, maxAdrenaline);
        if (adrenalineSlider != null)
        {
            adrenalineSlider.value = currentAdrenaline;
        }
        UpdateAdrenalineEffect();
        bool isNowMax = currentAdrenaline >= maxAdrenaline;
        if (isNowMax && !wasAdrenalineMax)
        {
            wasAdrenalineMax = true;
            if (audioSource != null && adrenalineMaxSound != null)
            {
                audioSource.PlayOneShot(adrenalineMaxSound);
            }
            if (adrenalineGaugeUI != null)
            {
                adrenalineGaugeUI.OnAdrenalineMax();
            }
            ShowFloatingText("MAX ADRENALINE!", transform, new Color(1f, 0.85f, 0.1f));
        }
        else if (!isNowMax && wasAdrenalineMax)
        {
            wasAdrenalineMax = false;
            if (adrenalineGaugeUI != null)
            {
                adrenalineGaugeUI.OnAdrenalineReset();
            }
        }
    }

    private void UpdateAdrenalineEffect()
    {
        if (adrenalineAuraEffect != null)
        {
            adrenalineAuraEffect.SetActive(currentAdrenaline >= 50f);
        }
    }

    private IEnumerator TriggerParryPostProcessEffect()
    {
        if (chromaticAberration != null) chromaticAberration.intensity.value = parryChromaticIntensity;
        if (vignette != null) vignette.intensity.value = parryVignetteIntensity;
        if (lensDistortion != null) lensDistortion.intensity.value = parryLensDistortionIntensity;

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(parryChromaticIntensity, 0f, t);
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(parryVignetteIntensity, 0.2f, t);
            if (lensDistortion != null)
                lensDistortion.intensity.value = Mathf.Lerp(parryLensDistortionIntensity, 0f, t);

            yield return null;
        }

        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (vignette != null) vignette.intensity.value = 0.2f;
        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
    }
}