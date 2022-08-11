using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMeshID;
    private bool _hasLetter;
    private Renderer _meshRenderer;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetState(bool hasLetter, Material material, float offset)
    {
        _hasLetter = hasLetter;
        _meshRenderer.material = material;
        transform.localPosition = Vector3.up * (offset);
        SetPieceID(hasLetter ? 0 : -1);
    }

    public void SetPieceID(int id)
    {
        textMeshID.text = $"{id}";
    }

    public bool IsValid()
    {
        return _hasLetter;
    }
}
