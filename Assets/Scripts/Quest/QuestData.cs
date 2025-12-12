using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Game/Quest Data", fileName = "NewQuest")]
public class QuestData : ScriptableObject
{
    [LabelText("Nome Quest")]
    public string questName;

    [LabelText("Step della quest")]
    [TextArea]
    public string[] steps;
}
