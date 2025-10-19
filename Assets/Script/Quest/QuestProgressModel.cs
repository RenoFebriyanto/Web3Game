using System;

[Serializable]
public class QuestProgressModel
{
    public string questId;
    public int progress;
    public bool claimed;

    public QuestProgressModel() { }

    public QuestProgressModel(string id, int p, bool c)
    {
        questId = id; progress = p; claimed = c;
    }
}

[Serializable]
public class QuestProgressList
{
    public QuestProgressModel[] items;
}
