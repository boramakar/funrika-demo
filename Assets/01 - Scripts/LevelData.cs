using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    public int levelID;
    public int rows;
    public int columns;
    public List<List<int>> data;
}
