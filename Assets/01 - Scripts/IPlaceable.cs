using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlaceable
{
    public void StartPlacement();
    public bool IsValidPlacement();
    public Vector3 GetPlacementOffset();
    public void SuccessAnimation();
}
