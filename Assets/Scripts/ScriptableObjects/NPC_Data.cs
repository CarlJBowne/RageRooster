
/*
Original Authors
    André Cardoso - Github
License
    This project is licensed under the MIT License - see the LICENSE.md file for details
*/

using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New NPC Data", menuName = "ScriptableObjects/Dialogue/Create NPC Data")]
public class NPC_Data : ScriptableObject
{
    public string npcName;
    public List<DialogueData> dialogueList;

    [HideInEditMode, DisableInPlayMode]
    public int dialogueID = 0;
    public List<WorldChange> worldChanges;

    private void OnEnable()
    {
        dialogueID = 0;
        for (int i = 0; i < worldChanges.Count; i++)
            if (worldChanges[i].Enabled == true) WorldChangeFired(); 
            else worldChanges[i].Action += WorldChangeFired;
    }
    private void OnDisable()
    {
        for (int i = 0; i < worldChanges.Count; i++)
            worldChanges[i].Action -= WorldChangeFired;
    }

    void WorldChangeFired() => dialogueID++;
    
    public void OnConversationFinished()
    {
        if (worldChanges.Count > dialogueID && worldChanges[dialogueID] != null)  
            worldChanges[dialogueID].Enabled = true;
    }
}
