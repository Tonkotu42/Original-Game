using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float destroyTime = 1.0f; // 消えるまでの時間
    [SerializeField] private float moveSpeed = 2.0f;   // 上に登るスピード
    private TextMeshPro textMesh;
    private Transform mainCameraTransform;

    // ★追加：追従するための変数
    private Transform followTarget;
    private Vector3 offset;

    // ★追加：生成された時にターゲットを設定するメソッド
    public void Setup(Transform target, Vector3 initialOffset)
    {
        followTarget = target;
        offset = initialOffset;
    }

    void Start()
    {
        Destroy(gameObject, destroyTime);
        textMesh = GetComponent<TextMeshPro>();
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // ★変更：ターゲットが存在する場合は、ターゲットの位置に追従する
        if (followTarget != null)
        {
            transform.position = followTarget.position + offset;
        }

        // オフセット自体を少しずつ上に移動させる（文字が上に登る表現）
        offset += Vector3.up * moveSpeed * Time.deltaTime;

        // 常にカメラの方を向かせる（ビルボード）
        if (mainCameraTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCameraTransform.position);
        }

        // 徐々に透明にする（フェードアウト）
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a -= (1.0f / destroyTime) * Time.deltaTime;
            textMesh.color = c;
        }
    }
}