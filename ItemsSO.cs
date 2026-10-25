using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;

[CreateAssetMenu]
public class ItemsSO : ScriptableObject
{
    public List<Item> itemList = new List<Item>();
    [System.Serializable]
    public class Item
    {

        [SerializeField] itemType type;
        [SerializeField] string itemName;
        [TextArea]
        [SerializeField] string description;
        [SerializeField] int effect;
        [SerializeField] Sprite itemImage;

        public enum itemType
        {
            coin,
            recovery,
            weapon,
            armor,
            tool,
        }

        public string ItemName { get => itemName; }
        public int ItemEffect { get => effect; }
        public itemType ItemType { get => (itemType)type; }
        public Sprite ItemImage { get => itemImage; }
    }
}
