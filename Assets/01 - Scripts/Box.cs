using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Box : MonoBehaviour, IPlaceable
{
    [SerializeField] private Material successMaterial;
    [SerializeField] private int successAnimationRepeatCount;
    [SerializeField] private float successScaleDuration;
    
    private bool _isOnLetterTile;
    private Material _defaultMaterial;
    private Renderer _meshRenderer;

    private void Awake()
    {
        _isOnLetterTile = false;
        _meshRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        _defaultMaterial = _meshRenderer.material;
    }

    public void StartPlacement()
    {
        StartCoroutine(Drag());
    }

    public bool IsValidPlacement()
    {
        return _isOnLetterTile;
    }

    public Vector3 GetPlacementOffset()
    {
        var diff = Vector3.zero;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.forward, out hit, 20, LayerMask.GetMask("Tile")))
        {
            diff = hit.transform.position - transform.position;
            diff.z = 0;
        }

        return diff;
    }

    public void SuccessAnimation()
    {
        DOVirtual.Color(_defaultMaterial.color, successMaterial.color, successScaleDuration * 2, ColorUpdate);
        var sequence = DOTween.Sequence();
        for (int i = 0; i < successAnimationRepeatCount; i++)
        {
            sequence.Append(transform.DOScaleZ(2, successScaleDuration));
            sequence.Append(transform.DOScaleZ(1, successScaleDuration));
        }
        sequence.Play();
    }

    private IEnumerator Drag()
    {
        //Debug.Log("BoxDrag Start");
        while (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Debug.DrawRay(transform.position, Vector3.forward * 20, Color.cyan);
            if (Physics.Raycast(transform.position + Vector3.back * 0.1f, Vector3.forward, out hit, 20,
                    LayerMask.GetMask("Tile", "Placeable")))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Tile"))
                {
                    _isOnLetterTile = hit.collider.gameObject.GetComponent<Tile>().IsValid();
                    _meshRenderer.material = _isOnLetterTile ? successMaterial : _defaultMaterial;
                }
                else
                {
                    _isOnLetterTile = false;
                    _meshRenderer.material = _defaultMaterial;
                }
            }
            yield return null;
        }
        _meshRenderer.material = _defaultMaterial;
        //Debug.Log("BoxDrag End");
    }

    private void ColorUpdate(Color value)
    {
        _meshRenderer.material.color = value;
    }
}
