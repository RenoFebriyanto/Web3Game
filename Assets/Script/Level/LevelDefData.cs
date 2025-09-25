// Assets/Script/Level/LevelDefData.cs
using System;
using UnityEngine;

[Serializable]
public class LevelDefData
{
    // identifier
    public string id;           // e.g. "level_1"
    public int number;          // human number (1..N)

    // designer-set conditions
    public bool locked = true;
    public int targetDistanceMeters = 20; // contoh: jarak yg harus ditempuh
    public int starThreshold1 = 1;
    public int starThreshold2 = 2;
    public int starThreshold3 = 3;

    // runtime / progress (compatibility fields)
    // beberapa skrip/inspector lama mungkin mengharapkan nama ini
    public int bestStars = 0;   // berapa star terbaik pemain sudah capai (0..3)
    public bool unlocked = false; // duplicate of 'locked' inverted for ease

    // optional: store other runtime progress values
    public bool completed = false;

    // ctor
    public LevelDefData() { }

    public LevelDefData(string id, int number, bool locked = true, int target = 20, int s1 = 1, int s2 = 2, int s3 = 3)
    {
        this.id = id;
        this.number = number;
        this.locked = locked;
        this.unlocked = !locked;
        this.targetDistanceMeters = target;
        this.starThreshold1 = s1;
        this.starThreshold2 = s2;
        this.starThreshold3 = s3;
        this.bestStars = 0;
        this.completed = false;
    }
}
