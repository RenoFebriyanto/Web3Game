// QuestProgressModel.cs
using System;

[Serializable]
public class QuestProgressModel
{
    public string questId;
    public int current;
    public bool claimed;

    public QuestProgressModel() { }

    public QuestProgressModel(string id, int cur, bool c)
    {
        questId = id; current = cur; claimed = c;
    }
}
