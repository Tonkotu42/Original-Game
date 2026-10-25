using JetBrains.Annotations;
using UnityEngine;

public class WeaponCollider : MonoBehaviour
{
    private Collider weaponCollider;

    void Awake()
    {
        weaponCollider = GetComponent<Collider>();
        // ゲーム開始時は当たり判定を消しておく
        DisableAttackCollider();
    }

    public void EnableAttackCollider()
    {
        if (weaponCollider != null)
        {
            if (weaponCollider != null)
            {
                weaponCollider.enabled = true;
            }
        }
    }

    public void DisableAttackCollider()
    {
            if (weaponCollider != null)
            {
                weaponCollider.enabled = false;
            }
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

