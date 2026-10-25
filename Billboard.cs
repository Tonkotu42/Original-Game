using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // メインカメラのTransformを取得
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform == null) return;

        // UIが常にカメラの方向を向くようにする
        // transform.LookAt(mainCameraTransform) だと裏返る場合があるため、カメラの前方ベクトルを利用
        transform.LookAt(transform.position + mainCameraTransform.forward);
    }
}