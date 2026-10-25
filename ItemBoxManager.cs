using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml.Serialization;
using UnityEditor.Experimental.GraphView;

public class ItemBoxManager : MonoBehaviour
{
    [SerializeField] ItemsSO itemsSO; //アイテムのScriptableObjectを格納する変数
    [SerializeField] Text itemText;
    [SerializeField] TextMeshProUGUI cointext;
    [SerializeField] TextMeshProUGUI portiontext;
    [SerializeField] GameObject player;
    [SerializeField] GameObject itemImage_prefab;
    [SerializeField] GameObject itemQty_prefab;
    [SerializeField] Transform itemBox3Image;
    [SerializeField] Transform itemBox3Text;



    public int getItem;
    private int[] itemQtyAry;//アイテムの所持数を格納する配列
    private string itemOpenText; //アイテムを入手した時の表示テキスト
    private string itemType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        itemQtyAry = new int[itemsSO.itemList.Count];
        Debug.Log("itemQTYARY = " + itemQtyAry.Length);
       
    }

    // Update is called once per frame
    void Update()
    { 
     
    }

    public void ItemGet()
    {
        itemQtyAry[getItem] = itemQtyAry[getItem] + 1;
        Debug.Log("アイテムを入手しました。");
        Debug.Log("アイテムNo." + getItem + "の所持数は" + itemQtyAry[getItem] + "個です。");
    }

    public void ItemOpen()
    {
        //cointext.text = itemQtyAry[0].ToString();
        //portiontext.text = itemQtyAry[1].ToString();

        //for (int i = 0; i < itemQtyAry.Length; i++)
        //{
        //    if (itemQtyAry[i] > 0)
        //    {
        //        itemOpenText = itemOpenText + itemsSO.itemList[i].ItemName + "  " + itemQtyAry[i].ToString() + "\n";
        //        itemText.GetComponent<Text>().text = itemOpenText;
        //    }
        //}
        //itemOpenText = "";
        //kokokara
        foreach (Transform n in itemBox3Image.transform)
        {
            GameObject.Destroy(n.gameObject);
        }
        foreach (Transform n in itemBox3Text.transform)
        {
            GameObject.Destroy(n.gameObject);
        }

        for (int i = 0; i < itemQtyAry.Length; i++)
        {
            if (itemQtyAry[i] > 0)
            {
                var itemNo = i;
                var obj = Instantiate(itemImage_prefab);
                obj.transform.SetParent(itemBox3Image);
                obj.GetComponent<Image>().sprite = itemsSO.itemList[i].ItemImage;

                var obj2 = Instantiate(itemQty_prefab);
                obj2.transform.SetParent(itemBox3Text);
                obj2.GetComponent<TextMeshProUGUI>().text = itemQtyAry[i].ToString();
                obj2.GetComponent<Button>().onClick.AddListener(() => ItemUse(itemNo));
            }
        }



        //kokomade


    }

    public void ItemUse(int itemNo) 
    {
        itemType = itemsSO.itemList[itemNo].ItemType.ToString();
        if (itemQtyAry[itemNo] > 0)
        {
            switch (itemType)
            {
                case "coin":
                    Debug.Log(itemType);
                    // コインを使用する処理
                    break;
                case "recovery":
                    // 回復アイテムを使用する処理
                    player.GetComponent<PlayerController>().currentHP = player.GetComponent<PlayerController>().currentHP + itemsSO.itemList[itemNo].ItemEffect;
                    itemQtyAry[itemNo] = itemQtyAry[itemNo] - 1;
                    break;
                default:
                    break;
                    // 他のアイテムタイプに対する処理を追加
            }
        }
        else
        {
            Debug.Log("No such item found.");
        }
        ItemOpen();
    }

}
