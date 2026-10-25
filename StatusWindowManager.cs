using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusWindowManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI hpValue;
    [SerializeField] TextMeshProUGUI mpValue;
    [SerializeField] TextMeshProUGUI atValue;
    [SerializeField] TextMeshProUGUI dfValue;
    [SerializeField] PSSO playerStatusSO;
    [SerializeField] PlayerController playerController;

    // ★変更：Startではなく、ウィンドウが表示される（SetActive(true)になる）たびに呼ばれる OnEnable を使う
    void OnEnable()
    {
        UpdateStatusDisplay();
    }

    // ステータスの数値をUIに反映させる専用メソッド
    public void UpdateStatusDisplay()
    {
        // プレイヤーやSOが未アタッチの場合のエラーを防ぐ保険
        if (playerController == null || playerStatusSO == null) return;

        if (hpValue != null) hpValue.text = playerController.currentHP.ToString() + "/" + playerStatusSO.HP.ToString();
        if (mpValue != null) mpValue.text = playerController.currentMP.ToString() + "/" + playerStatusSO.MP.ToString();
        if (atValue != null) atValue.text = playerStatusSO.Attack.ToString();
        if (dfValue != null) dfValue.text = playerStatusSO.Defence.ToString();
    }

    void Update()
    {
        // ウィンドウが開いている間だけ、HPやMPの変動をリアルタイムで反映する
        if (playerController != null && playerStatusSO != null)
        {
            if (hpValue != null) hpValue.text = playerController.currentHP.ToString() + "/" + playerStatusSO.HP.ToString();
            if (mpValue != null) mpValue.text = playerController.currentMP.ToString() + "/" + playerStatusSO.MP.ToString();
        }
    }
}