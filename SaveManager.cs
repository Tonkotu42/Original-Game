using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] GameObject player;
   public void GoSave()
    {
        PlayerPrefs.SetInt("currentHP", player.GetComponent<PlayerController>().currentHP);
        PlayerPrefs.Save();
    }

    public void GoLoad()
    {

    }
}
