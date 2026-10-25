using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PSSO", menuName = "Scriptable Objects/PSSO")]
public class PSSO : ScriptableObject
{
    [SerializeField] private int hp;
    [SerializeField] private int mp;
    [SerializeField] private int attack;
    [SerializeField] private int defence;

    public int HP { get => hp; }
    public int MP { get => mp; }
    public int Attack { get => attack; }
    public int Defence { get => defence; }
}
