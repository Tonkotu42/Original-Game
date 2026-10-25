using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static ESSO;
using System.Collections; // コルーチンを使うために必要

public class DogManager : MonoBehaviour
{
    [SerializeField] GameObject talkWindow;
    [SerializeField] TextMeshProUGUI talkText;
    [SerializeField] EventSO eventSO;
    [SerializeField] GameObject choiceButtonsContainer;
    [SerializeField] GameObject arrowUI; // ★追加: インスペクターで頭の上の矢印を登録する

    // DogManager.cs に追加するイメージ
    [SerializeField] private float interactDistance = 5f; // ←インスペクターで好きな距離に変更可能に
    private Transform playerTransform;
    [Header("タイピング演出設定")]
    [SerializeField] float textSpeed; // 1文字あたりの表示速度（秒）
    [SerializeField] AudioSource audioSource; // 文字音用のオーディオ（任意）
    [SerializeField] AudioClip typeSound;     // 文字音のSE（任意）

    private bool talkWindowFlag =false;
    private int progressFlag = 0;
    private string currentText;
    private bool hasChoices = false;

    public bool IsTalking => talkWindowFlag;
    // ★追加: 外部からプレイヤーが近くにいるか参照できるプロパティ
    public bool IsPlayerNearby => isPlayerNearby;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private Animator playerAnimator;
    private bool isPlayerNearby = false; // ★追加: プレイヤーが近くにいるかどうかのフラグ

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 最初はウィンドウとボタンを隠しておく
        talkWindow.SetActive(false);
        choiceButtonsContainer.SetActive(false);
        if (arrowUI != null) arrowUI.SetActive(false); // ★最初矢印は隠す

        // プレイヤーのTransformをあらかじめ取得しておく
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // ★追加: 毎フレームプレイヤーとの距離を計算し、トリガーの代わりに判定する
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerNearby = (distance <= interactDistance);

            // 距離内かつ会話中でなければArrowUIを表示
            if (isPlayerNearby && !talkWindowFlag)
            {
                if (arrowUI != null) arrowUI.SetActive(true);

                if (Input.GetMouseButtonDown(0))
                {
                    StartDialogue();
                }
                return;
            }
            else
            {
                if (arrowUI != null) arrowUI.SetActive(false);
            }
        }

            // 会話ウィンドウが開いていて、かつタイピング中でないとき
            if (talkWindowFlag && !isTyping)
        {
            // かつ、選択肢がない会話文の場合
            if (!hasChoices)
            {
                // マウスクリック、またはスペースキー（またはEnter等）が押されたら次へ
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    // 選択肢がないので、自動的に「Yes」に設定されている次のインデックスへ進む
                    progressFlag = eventSO.eventList[progressFlag].Yes;
                    EventProgress();
                }
            }
        }
    }

    // ★追加: 会話を開始する独立した関数
    void StartDialogue()
    {
        if (arrowUI != null) arrowUI.SetActive(false); // 会話が始まったら矢印は消す
        CameraController.UnlockCursor();

        // 近くにいるプレイヤーからAnimatorを取得する
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("IsTalking", true);
            }
        }

        progressFlag = 0;
        talkWindow.SetActive(true);
        talkWindowFlag = true;
        EventProgress();
    }

    // ★修正: 武器ではなく「Player」のタグが近づいたら矢印を出す
    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (!talkWindowFlag && arrowUI != null)
            {
                arrowUI.SetActive(true); // 矢印を表示
            }
        }
    }

    // ★追加: プレイヤーが離れたら矢印を消す
    void OnTriggerExit(Collider col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (arrowUI != null)
            {
                arrowUI.SetActive(false); // 矢印を非表示
            }
        }
    }



    // 一文字ずつ表示するコルーチン
    IEnumerator TypeText(string textToType)
    {
        isTyping = true;
        talkText.text = ""; // テキストをクリア

        // 文字列を1文字ずつループ処理
        foreach (char letter in textToType.ToCharArray())
        {
            talkText.text += letter; // 1文字追加

            // サウンドが設定されていれば、文字ごとにピッと鳴らす
            if (audioSource != null && typeSound != null)
            {
                audioSource.PlayOneShot(typeSound);
            }

            // 指定した秒数だけ待機
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;

        // ★文字がすべて表示し終わった後の処理を分岐
        if (hasChoices)
        {
            // 選択肢があるノードなら、ボタンを表示する
            if (choiceButtonsContainer != null) choiceButtonsContainer.SetActive(true);
        }
        else
        {
            // 選択肢がないノードなら、UIに「▼」などの送りマークを表示するとより親切です
            talkText.text += " ▼";
        }

    }


    void EventProgress()
    {
        // 会話終了の合図（例: 次のインデックスが -1、またはリストの範囲外）
        if (progressFlag == -1 || progressFlag >= eventSO.eventList.Count)
        {
            EndDialogue();
            return;
        }

        // 次のテキストを表示する前に、選択肢ボタンを一時的に隠す（誤操作防止）
        if (choiceButtonsContainer != null)
        {
            choiceButtonsContainer.SetActive(false);
        }

        // インスペクターのデータで、Yes と No の遷移先が「異なる数値」なら選択肢あり、
        // 「全く同じ数値」なら選択肢なし（一本道）とみなします。
        int nextYes = eventSO.eventList[progressFlag].Yes;
        int nextNo = eventSO.eventList[progressFlag].No;

        hasChoices = (nextYes != nextNo);

        // 現在の会話データを取得してテキストを表示
        currentText = eventSO.eventList[progressFlag].Words;

        // すでに動いているタイピング演出があれば止める
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // タイピング演出を開始
        typingCoroutine = StartCoroutine(TypeText(currentText));
        //talkText.text = currentText;

    }

    void EndDialogue()
    {
        // ★追加: 会話が終わったので、ステートマシンを通常状態（IsTalking = false）に戻す
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsTalking", false);
        }


        talkWindow.SetActive(false);
        if (choiceButtonsContainer != null) choiceButtonsContainer.SetActive(false);
        talkWindowFlag = false;
        progressFlag = 0; // 次回のためにリセット
        CameraController.LockCursor();
    }

    public void ClickEventButton(bool yesno)
    {
        // 文字のタイピング中にボタンが押された場合は無視する（バグ防止）
        if (isTyping) return;

        // 1. まず次のインデックス（進行先）を決定する
        if (yesno)
        {
            progressFlag = eventSO.eventList[progressFlag].Yes;
        }
        else
        {
            progressFlag = eventSO.eventList[progressFlag].No;
        }

        // 2. 決定したインデックスのテキストを表示（または終了判定）
        EventProgress();
    }

}
