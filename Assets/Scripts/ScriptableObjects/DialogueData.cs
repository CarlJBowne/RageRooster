
/*
Original Authors
    André Cardoso - Github
License
    This project is licensed under the MIT License - see the LICENSE.md file for details
*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Data", menuName = "ScriptableObjects/Dialogue/New Conversation")]
public class DialogueData : ScriptableObject
{
   [TextArea(4,4)]
   public List<string> conversationBlock;
}
