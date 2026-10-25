using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
public class EnemyManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider hpSlider;

    [Header("UI Settings")]
    [SerializeField] private Transform floatingTextSpawnPoint;

    [SerializeField] Transform target;
    [SerializeField] ESSO enemyStatusSO;
    [SerializeField] PSSO playerStatusSO;
    private NavMeshAgent agent;
    private Animator animator;
    private int currentHP;
    private int maxHP; // ★追加：最大HP
    private float baseAttackCooldown; // ★追加：初期の攻撃クールダウン時間
    private bool isDead = false;
    public bool IsDead => isDead;
    private bool isPendingDeath = false; // ★追加：死亡待機フラグ
    private Rigidbody rb;
    private bool isKnockedBack = false;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockbackDuration = 0.5f;

    [Header("Parry Signal Settings")]
    [SerializeField] private GameObject parrySignalEffect;
    [SerializeField] private AudioClip parrySignalSound;
    [SerializeField] private AudioClip parrySuccessSound;

    [SerializeField] private float attackWindupDelay = 0.5f;
    [SerializeField] private float parryWindowDuration = 0.3f;

    private bool isParryable = false;
    private AudioSource audioSource;
    private Coroutine parryCoroutine;

    [Header("Distance Management")]
    [SerializeField] private float safeDistance = 1.5f;
    [SerializeField] private float rotationSpeed = 5.0f;

    public enum EnemyAttackType
    {
        Normal,
        Heavy,
        Combo,
        Guard   // 追加：パリィ（ガード）待機状態
    }

    [Header("Enemy Parry / Guard Settings")]
    [SerializeField] private float guardDuration = 1.5f; // ガードを構えている時間
    private bool isEnemyParrying = false;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 3.0f;
    [SerializeField] private float attackAnimationTime = 1.0f;
    private float attackTimer = 0f;
    private bool isAttacking = false;
    private Coroutine attackCoroutine;

    [SerializeField] private float normalAttackWindup = 0.5f;
    [SerializeField] private float normalAttackTime = 1.0f;
    [SerializeField] private float heavyAttackWindup = 1.0f;
    [SerializeField] private float heavyAttackTime = 1.5f;
    [SerializeField] private float comboAttackWindup = 0.3f;
    [SerializeField] private float comboAttackTime = 2.0f;

    [Header("Weapon Settings")]
    [SerializeField] private Collider weaponCollider;
    private float damageCooldown = 0f;
    private int lastHitComboStep = 0;
    // ★追加：被弾カウンター（スーパーアーマー反撃用）
    [Header("Revenge Counter")]
    [SerializeField] private int revengeThreshold = 4; // 4発殴られたら強制反撃
    private int currentHitCount = 0;
    void Start()
    {
        
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (rb != null) rb.isKinematic = true;

        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animatorが見つかりません！");
        }

        if (enemyStatusSO != null && enemyStatusSO.enemyStatusList != null && enemyStatusSO.enemyStatusList.Count > 0)
        {
            maxHP = enemyStatusSO.enemyStatusList[0].HP; // ★変更：maxHPに代入
        }
        else
        {
            maxHP = 100; // ★変更
        }
        currentHP = maxHP; // ★追加
        baseAttackCooldown = attackCooldown; // ★追加

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
        }
    }

    void Update()
    {
        if (isDead || isKnockedBack) return;

        if (damageCooldown > 0f)
        {
            damageCooldown -= Time.deltaTime;
        }

        PlayerController player = target != null ? target.GetComponent<PlayerController>() : null;
        if (player == null || player.CurrentComboStep == 0)
        {
            lastHitComboStep = 0;
        }

        if (isPendingDeath)
        {
            if (player != null && player.CurrentComboStep == 0 && !player.IsUltimateAttacking)
            {
                Die();
            }
            return; // 死亡待機中は通常のAI行動（移動・攻撃）を無効化する
        }

        if (damageCooldown > 0f)
        {
            damageCooldown -= Time.deltaTime;
        }

        attackTimer += Time.deltaTime;

        if (target != null && agent != null && agent.enabled)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= attackRange)
            {
                // ★追加：体力割合（1.0 ～ 0.0）を計算
                float hpRatio = maxHP > 0 ? (float)currentHP / maxHP : 1f;

                // ★追加：HPが100%ならbaseAttackCooldown、0%に近づくとその0.4倍までクールダウンが短縮される
                float currentAttackCooldown = Mathf.Lerp(baseAttackCooldown * 0.4f, baseAttackCooldown, hpRatio);

                // ★変更：attackCooldown の代わりに currentAttackCooldown を使用
                if (attackTimer >= currentAttackCooldown && !isAttacking)
                {
                    agent.updateRotation = true;
                    agent.isStopped = true;

                    // ★変更：引数に hpRatio を渡すように修正
                    EnemyAttackType nextAttack = ChooseAttackType(distance, hpRatio);
                    attackCoroutine = StartCoroutine(AttackRoutine(nextAttack));
                }

                if (attackTimer >= attackCooldown && !isAttacking)
                {
                    agent.updateRotation = true;
                    agent.isStopped = true;

                    EnemyAttackType nextAttack = ChooseAttackType(distance);
                    attackCoroutine = StartCoroutine(AttackRoutine(nextAttack));
                }
                else if (!isAttacking)
                {
                    agent.updateRotation = false;

                    Vector3 dirToTarget = (target.position - transform.position).normalized;
                    dirToTarget.y = 0;
                    if (dirToTarget != Vector3.zero)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
                    }

                    if (distance < safeDistance)
                    {
                        agent.isStopped = true;
                        Vector3 retreatDir = -dirToTarget;
                        agent.Move(retreatDir * (agent.speed * 0.5f) * Time.deltaTime);
                    }
                    else
                    {
                        agent.isStopped = true;
                    }
                }
            }
            else if (!isAttacking)
            {
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
    }

    // ★引数に float hpRatio を追加
    private EnemyAttackType ChooseAttackType(float distance, float hpRatio)
    {
        float closeRange = attackRange * 0.4f;
        float midRange = attackRange * 0.7f;
        float rand = Random.value;

        // ★追加：HP低下に応じてガード（敵パリィ）確率を上げる（最大40%、HP最大時15%）
        float guardProb = Mathf.Lerp(0.40f, 0.15f, hpRatio);
        float threshold = 1.0f - guardProb;

        // ランダム値が閾値以上ならガードを選択
        if (rand >= threshold)
        {
            return EnemyAttackType.Guard;
        }

        // ガード以外の攻撃を、残りの乱数範囲(0 ～ threshold)で割り振るために数値を正規化
        float normRand = rand / threshold;

        if (distance > midRange)
        {
            if (normRand < 0.5f) return EnemyAttackType.Heavy;
            else return EnemyAttackType.Normal;
        }
        else if (distance <= closeRange)
        {
            if (normRand < 0.4f) return EnemyAttackType.Normal;
            else return EnemyAttackType.Combo;
        }
        else
        {
            if (normRand < 0.3f) return EnemyAttackType.Normal;
            else if (normRand < 0.6f) return EnemyAttackType.Combo;
            else return EnemyAttackType.Heavy;
        }
    }

    private EnemyAttackType ChooseAttackType(float distance)
    {
        float closeRange = attackRange * 0.4f;
        float midRange = attackRange * 0.7f;
        float rand = Random.value;

        if (distance > midRange)
        {
            if (rand < 0.6f) return EnemyAttackType.Heavy;
            else if (rand < 0.8f) return EnemyAttackType.Normal;
            else return EnemyAttackType.Guard; // 20%の確率でガード
        }
        else if (distance <= closeRange)
        {
            if (rand < 0.5f) return EnemyAttackType.Normal;
            else if (rand < 0.8f) return EnemyAttackType.Combo;
            else return EnemyAttackType.Guard; // 20%でガード
        }
        else
        {
            if (rand < 0.3f) return EnemyAttackType.Normal;
            else if (rand < 0.6f) return EnemyAttackType.Combo;
            else if (rand < 0.8f) return EnemyAttackType.Heavy;
            else return EnemyAttackType.Guard; // 20%でガード
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (isDead) return;

        if (col.gameObject.CompareTag("Weapon"))
        {
            PlayerController player = col.transform.root.GetComponent<PlayerController>();

            if (isEnemyParrying)
            {
                // ★追加：プレイヤーがULT攻撃中なら、敵のガードを無効化（ガードブレイク）する
                if (player != null && player.IsUltimateAttacking)
                {
                    Debug.Log("ガードブレイク！ULT攻撃が敵のガードを粉砕しました！");
                    isEnemyParrying = false;

                    // （※必要であれば、ここにガラスが割れるようなガードブレイク音を入れるとさらに爽快です）

                    // リターンせずに、そのまま下のダメージ処理（ULTの5倍ダメージ等）へ進ませる
                }
                else
                {
                    // 通常攻撃の場合は、これまで通り敵がプレイヤーを弾き返す
                    Debug.Log("敵がプレイヤーの攻撃をパリィしました！即座に反撃します！");

                    if (audioSource != null && parrySuccessSound != null)
                    {
                        audioSource.PlayOneShot(parrySuccessSound);
                    }

                    if (player != null)
                    {
                        player.OnParriedByEnemy(transform); // プレイヤーをひるませる
                    }

                    isEnemyParrying = false;
                    CancelAttack();
                    attackTimer = attackCooldown; // 即座に反撃可能にする

                    return; // ダメージを受けないのでここで処理を終了
                }
            }

            int currentCombo = (player != null) ? player.CurrentComboStep : 1;

            if (currentCombo != lastHitComboStep)
            {
                damageCooldown = 0f;
            }

            float requiredCooldown = (currentCombo == 4) ? 0.1f : 0.3f;

            if (damageCooldown > 0f) return;

            if (playerStatusSO != null && enemyStatusSO != null && enemyStatusSO.enemyStatusList.Count > 0)
            {
                int damage = (int)(playerStatusSO.Attack / 2 - enemyStatusSO.enemyStatusList[0].Defence / 4);

                // ★追加：プレイヤーがULT攻撃中なら、ダメージを大幅に増加（例: 5倍）
                if (player != null && player.IsUltimateAttacking)
                {
                    damage *= 30; // ※倍率はゲームバランスに合わせて調整してください
                    Debug.Log("ULT攻撃ヒット！ 30倍ダメージ！");

                    // ULT攻撃中は反撃の怯みカウンター(currentHitCount)を増やさない、または強制的に怯ませる等の調整も可能です
                }

                if (player != null && player.IsCounterAttacking)
                {
                    float multiplier = player.IsAirborne ? 6f : 3f; // 基本の倍率（空中6倍、地上3倍）

                    // ★追加：コンボキャンセルからのパリィ反撃なら、さらに1.5倍のボーナス倍率を加える
                    if (player.IsComboCancelParry)
                    {
                        multiplier *= 1.5f; // 地上なら4.5倍、空中なら9倍になる
                    }

                    if (player.isAdrenalineCounter)
                    {
                        multiplier *= 2.0f;
                        Debug.Log("【アドレナリン強化反撃】 究極ダメージ！！");

                        // もしあれば、ここで極太のヒットストップなどを追加するとさらに爽快です
                         player.TriggerHitStop(0.3f); 
                    }

                    if (player.isMaxAdrenalineCounter)
                    {
                        multiplier *= 10.0f;
                        Debug.Log($"【アドレナリンMAX反撃】 {multiplier}倍の超絶ダメージ！！");
                    }
                    else if (player.isAdrenalineCounter)
                    {
                        multiplier *= 2.0f;
                        Debug.Log($"【アドレナリン強化反撃】 {multiplier}倍のダメージ！");
                    }

                    damage = Mathf.RoundToInt(damage * multiplier);

                    // ログ出力で倍率を確認
                    if (player.IsComboCancelParry)
                    {
                        Debug.Log($"コンボキャンセルパリィ反撃！ {multiplier}倍の特大ダメージ！");
                    }
                    else
                    {
                        Debug.Log($"通常のパリィ反撃！ {multiplier}倍ダメージ！");
                    }
                }

                if (damage > 0)
                {
                    currentHP -= damage;
                    Debug.Log($"{currentCombo}段目がヒット！ 敵の残りHP: " + currentHP);

                    if (hpSlider != null)
                    {
                        hpSlider.value = currentHP;
                    }

                    if (player != null && !player.IsCounterAttacking)
                    {
                        // コンボの段数が上がるほど獲得量を増やす（例：1段目は10、4段目は40）
                        float adrenalineGain = currentCombo * 5f;
                        player.AddAdrenaline(adrenalineGain);
                    }

                    if (player != null)
                    {
                        Transform textTarget = floatingTextSpawnPoint != null ? floatingTextSpawnPoint : transform;
                        player.ShowFloatingText(damage.ToString(), textTarget, new Color(1.0f, 0.5f, 0.0f));
                    }

                    lastHitComboStep = currentCombo;
                    damageCooldown = requiredCooldown;
                    currentHitCount++;


                    // ▼ ここにあった CancelAttack() を下部に移動し、エフェクト等は常に再生させる
                    if (player != null)
                    {
                        player.TriggerHitStop(0.05f);
                        player.PlayHitSound();
                        Vector3 hitPos = col.ClosestPoint(transform.position);
                        hitPos.y += (1.0f * transform.localScale.y);
                        player.PlayHitEffect(hitPos);
                    }

                    if (currentHP <= 0)
                    {
                        currentHP = 0;

                        bool isPlayerAttacking = player != null && (player.CurrentComboStep > 0 || player.IsUltimateAttacking);

                        if (isPlayerAttacking)
                        {
                            // 攻撃中なら死亡待機状態にし、サンドバッグとして怯ませる
                            isPendingDeath = true;
                            CancelAttack();

                            if (animator != null) animator.SetTrigger("GetHit");

                            // 必要に応じてノックバックも実行させる
                            if (currentCombo != 4)
                            {
                                StartCoroutine(KnockbackRoutine(col.transform.root));
                            }
                        }
                        else
                        {
                            Die(); // 攻撃中でなければ即座に死亡
                        }
                    }
                    else
                    {
                        // ★修正：被弾カウンターが閾値に達したか判定
                        if (currentHitCount >= revengeThreshold)
                        {
                            Debug.Log("敵の怒りが爆発！怯み無効で強制反撃！");
                            currentHitCount = 0; // カウンターをリセット

                            // 攻撃中でなければ、即座に強攻撃などで反撃を開始
                            if (!isAttacking)
                            {
                                agent.updateRotation = true;
                                agent.isStopped = true;

                                // 強制的にヘビーアタック（またはコンボ）を繰り出す
                                attackCoroutine = StartCoroutine(AttackRoutine(EnemyAttackType.Heavy));
                            }
                            // ※すでに攻撃中の場合は「怯まない（スーパーアーマー維持）」状態になる
                        }
                        else
                        {
                            // カウンターが溜まりきっていない場合は、通常通り怯む
                            if (!isAttacking)
                            {
                                CancelAttack();
                                if (animator != null) animator.SetTrigger("GetHit");

                                if (currentCombo != 4)
                                {
                                    StartCoroutine(KnockbackRoutine(col.transform.root));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerator AttackRoutine(EnemyAttackType attackType)
    {
        isAttacking = true;
        float currentWindup = 0f;
        float currentAnimTime = 0f;

        switch (attackType)
        {
            case EnemyAttackType.Normal:
                if (animator != null) animator.SetTrigger("NormalAttack");
                currentWindup = normalAttackWindup;
                currentAnimTime = normalAttackTime;
                break;
            case EnemyAttackType.Heavy:
                if (animator != null) animator.SetTrigger("HeavyAttack");
                currentWindup = heavyAttackWindup;
                currentAnimTime = heavyAttackTime;
                break;
            case EnemyAttackType.Combo:
                if (animator != null) animator.SetTrigger("ComboAttack");
                currentWindup = comboAttackWindup;
                currentAnimTime = comboAttackTime;
                break;
            case EnemyAttackType.Guard:
                if (animator != null) animator.SetTrigger("Guard"); // ★AnimatorにGuardを追加してください
                isEnemyParrying = true; // ガード状態開始
                currentWindup = 0f;     // 即座に発生
                currentAnimTime = guardDuration;
                break;
        }

        yield return new WaitForSeconds(currentWindup);
        yield return new WaitForSeconds(currentAnimTime);

        attackTimer = 0f;
        isAttacking = false;
        isParryable = false;
        isEnemyParrying = false; // ガード状態解除
    }

    private void CancelAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (parryCoroutine != null)
        {
            StopCoroutine(parryCoroutine);
            parryCoroutine = null;
        }

        if (animator != null)
        {
            animator.speed = 1.0f;
            animator.ResetTrigger("NormalAttack");
            animator.ResetTrigger("HeavyAttack");
            animator.ResetTrigger("ComboAttack");
            animator.ResetTrigger("Guard");
        }

        isAttacking = false;
        isParryable = false;
        isEnemyParrying = false;
        attackTimer = 0f;
        DisableWeaponCollider();
    }

    public void StartParryWindow()
    {
        if (!isAttacking || isDead) return;

        if (parryCoroutine != null) StopCoroutine(parryCoroutine);
        parryCoroutine = StartCoroutine(ParrySequenceRoutine());
    }

    private IEnumerator ParrySequenceRoutine()
    {
        ShowParrySignal();

        if (animator != null) animator.speed = 0f;

        isParryable = true;

        yield return new WaitForSeconds(attackWindupDelay);

        if (animator != null) animator.speed = 1.0f;

        yield return new WaitForSeconds(parryWindowDuration);
        isParryable = false;
    }

    private void ShowParrySignal()
    {
        if (parrySignalEffect != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * (1f * transform.localScale.y);
            GameObject effect = Instantiate(parrySignalEffect, spawnPos, Quaternion.identity);
            effect.transform.localScale = Vector3.Scale(effect.transform.localScale, transform.localScale);
            Destroy(effect, 1.0f);
        }

        if (audioSource != null && parrySignalSound != null)
        {
            audioSource.PlayOneShot(parrySignalSound);
        }
    }

    public void OnParried(Transform playerTransform)
    {
        Debug.Log("敵：パリィされました！");

        if (audioSource != null && parrySuccessSound != null)
        {
            audioSource.PlayOneShot(parrySuccessSound);
        }

        DisableWeaponCollider();
        CancelAttack();
        StartCoroutine(ParryStunRoutine(playerTransform));
    }

    public bool TryAcceptParry(Transform playerTransform)
    {
        if (isDead || !isParryable)
        {
            return false;
        }

        isParryable = false;
        OnParried(playerTransform);
        return true;
    }

    private IEnumerator KnockbackRoutine(Transform attacker)
    {
        isKnockedBack = true;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            Vector3 knockbackDir = transform.position - attacker.position;
            knockbackDir.y = 0;
            knockbackDir.Normalize();

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(knockbackDuration);

        if (!isDead)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }

            if (agent != null)
            {
                agent.enabled = true;
                agent.ResetPath();
                agent.isStopped = false;
            }
        }

        isKnockedBack = false;
    }

    private IEnumerator ParryStunRoutine(Transform playerTransform)
    {
        isKnockedBack = true;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            Vector3 knockbackDir = transform.position - playerTransform.position;
            knockbackDir.y = 0;
            knockbackDir.Normalize();

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(knockbackDir * 15f, ForceMode.Impulse);
        }

        if (animator != null) animator.SetTrigger("GetHit");

        yield return new WaitForSeconds(2.0f);

        if (!isDead)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }

            if (agent != null)
            {
                agent.enabled = true;
                agent.ResetPath();
                agent.isStopped = false;
            }
        }

        isKnockedBack = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (hpSlider != null)
        {
            hpSlider.transform.parent.gameObject.SetActive(false);
        }

        CancelAttack();
        Debug.Log("★敵が死亡しました。モーションを再生します。");

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.Play("Die", 0, 0f);
        }

        Invoke("DestroyEnemy", 2.0f);
    }

    public void EnableWeaponCollider()
    {
        if (!isAttacking || isDead || isKnockedBack)
        {
            return;
        }

        if (weaponCollider != null) weaponCollider.enabled = true;
    }

    public void DisableWeaponCollider()
    {
        if (weaponCollider != null) weaponCollider.enabled = false;
    }

    void DestroyEnemy()
    {
        Destroy(gameObject);
    }
}