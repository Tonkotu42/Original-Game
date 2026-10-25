using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ESSO", menuName = "Scriptable Objects/ESSO")]
public class ESSO : ScriptableObject
{
    public List<EnemyStatus> enemyStatusList = new List<EnemyStatus>();
    [System.Serializable]
    public class EnemyStatus
    {
        [SerializeField] string enemyName;
        [SerializeField] int hp;
        [SerializeField] int mp;
        [SerializeField] int attack;
        [SerializeField] int defence;
        [SerializeField] enemyType type;

        public enum enemyType
        {
            nomal,
            fire,
            water
        }


        public int HP { get => hp; }
        public int MP { get => mp; }
        public int Attack { get => attack; }
        public int Defence { get => defence; }
    }


}
