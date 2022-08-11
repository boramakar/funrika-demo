using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomColorGenerator
{
    [SerializeField] private static int highMax = 256;
    [SerializeField] private static int highMin = 180;
    [SerializeField] private static int midMax = 180;
    [SerializeField] private static int midMin = 90;
    [SerializeField] private static int lowMax = 90;
    [SerializeField] private static int lowMin = 0;
    
    public static Color GenerateRandomColor(float alpha = 1f)
    {
        var highCount = Random.Range(0, 4);
        var midCount = Random.Range(0, 4 - highCount);
        var lowCount = 3 - (highCount + midCount);
        var colorValues = new List<int>(3);
        for (int i = 0; i < 3; i++)
        {
            colorValues.Add(0);
        }

        for (int i = 0; i < highCount; i++)
        {
            var unusedIndex = GetUnusedIndex(colorValues, 0, 0, 3);
            colorValues[unusedIndex] = Random.Range(highMin, highMax);
        }
        
        for (int i = 0; i < midCount; i++)
        {
            var unusedIndex = GetUnusedIndex(colorValues, 0, 0, 3);
            colorValues[unusedIndex] = Random.Range(midMin, midMax);
        }
        
        for (int i = 0; i < lowCount; i++)
        {
            var unusedIndex = GetUnusedIndex(colorValues, 0, 0, 3);
            colorValues[unusedIndex] = Random.Range(lowMin, lowMax);
        }

        return new Color(colorValues[0] / 255f, colorValues[1] / 255f, colorValues[2] / 255f, alpha);
    }

    private static int GetUnusedIndex(List<int> list, int unusedValue, int min, int max)
    {
        var index = Random.Range(min, max);
        while (list[index] != unusedValue)
        {
            index = Random.Range(min, max);
        }

        return index;
    }
}
