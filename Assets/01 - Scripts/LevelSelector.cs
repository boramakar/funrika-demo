using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    private int _levelIndex;
    
    public void SetLevel(int index)
    {
        _levelIndex = index;
    }

    public void LoadLevel()
    {
        GameManager.Instance.LoadLevel(_levelIndex + 1);
    }
}
