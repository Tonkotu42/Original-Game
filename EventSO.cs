using UnityEngine;
using System.Collections.Generic;
using System.Collections;


[CreateAssetMenu(fileName = "EventSO", menuName = "Scriptable Objects/EventSO")]
public class EventSO : ScriptableObject
{
    public List<Events> eventList = new List<Events>();
    [System.Serializable]
    public class Events
    {
        [SerializeField] public string personName;
        [TextArea]
        [SerializeField] public string words;
        [SerializeField] int yes;
        [SerializeField] int no;


        public string Words { get => words; }
        public int Yes { get => yes; }
        public int No { get => no; }

        //public string eventName;
        //public string eventDescription;
        //public int eventID;
        //public int eventType; // 0: Normal, 1: Battle, 2: Dialogue, 3: Puzzle, 4: Cutscene
        //public int eventDifficulty; // 0: Easy, 1: Medium, 2: Hard
        //public int eventReward; // Reward for completing the event
        //public bool isCompleted; // Whether the event has been completed
    }
}
