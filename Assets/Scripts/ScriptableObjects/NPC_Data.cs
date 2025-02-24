using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New NPC Data", menuName = "ScriptableObjects/Dialogue/Create NPC Data")]
public class NPC_Data : ScriptableObject
{
    public string npcName;
    public List<DialogueData> dialogueList;
}
