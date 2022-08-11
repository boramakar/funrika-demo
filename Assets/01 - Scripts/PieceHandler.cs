using System.Collections;
using DG.Tweening;
using UnityEngine;

public class PieceHandler : MonoBehaviour
{
    [SerializeField] private Vector3 dragOffset = new Vector3(0, 5, -2);
    [SerializeField] private float autoMoveHeightDuration = 0.1f;
    [SerializeField] private float autoMovePositionDuration = 0.3f;

    private GameManager _gameManager;
    private Camera _mainCamera;
    private IPlaceable[] _placeables;
    private Vector3 _initialPosition;
    private bool _isBusy;

    private void Awake()
    {
        _gameManager = GameManager.Instance;
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        _gameManager.BusyEvent += SetBusy;
        _gameManager.LevelSuccessEvent += LevelSuccess;
    }

    private void OnDisable()
    {
        if (_gameManager != null)
        {
            _gameManager.BusyEvent -= SetBusy;
            _gameManager.LevelSuccessEvent -= LevelSuccess;
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
    }

    private void LevelSuccess()
    {
        _isBusy = true;
    }

    public void InitPlaceables()
    {
        _placeables = GetComponentsInChildren<IPlaceable>();
    }

    private void OnMouseDown()
    {
        if (_isBusy) return;
        _initialPosition = transform.position;
        StartCoroutine(Drag());
        foreach (var placeable in _placeables)
        {
            placeable.StartPlacement();
        }
    }

    private IEnumerator Drag()
    {
        //Debug.Log("Drag Start");
        while (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 50, Color.red);
            if (Physics.Raycast(ray, out hit, 50, LayerMask.GetMask("RaycastLayer")))
            {
                transform.position = hit.point + dragOffset;
            }

            yield return null;
        }

        //Debug.Log("Drag End");

        CheckPlacement();
    }

    public void AutoPlace(Vector3 position, TweenCallback callback)
    {
        var height = Vector3.forward * dragOffset.z;
        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOMove(transform.position + height, autoMoveHeightDuration));
        sequence.Append(transform.DOMove(position + height, autoMovePositionDuration));
        sequence.Append(transform.DOMove(position, autoMoveHeightDuration));
        sequence.AppendCallback(() => { _initialPosition = transform.position; });
        sequence.AppendCallback(callback);
        sequence.Play();
    }

    private void CheckPlacement()
    {
        var success = true;
        foreach (var placeable in _placeables)
        {
            success = success && placeable.IsValidPlacement();
        }

        if (success)
        {
            NearestPlacement();
            GameManager.Instance.PlacementSuccess(transform.GetSiblingIndex() + 1);
        }
        else
        {
            transform.position = _initialPosition;
        }
    }

    private void NearestPlacement()
    {
        var currentPosition = transform.position;
        currentPosition.z = 0;
        currentPosition += _placeables[0].GetPlacementOffset();
        transform.position = currentPosition;
        _initialPosition = currentPosition;
    }

    public void SuccessAnimation()
    {
        // Debug.Log($"Success Animation: {transform.GetSiblingIndex()}");
        var count = 0;
        foreach (var placeable in _placeables)
        {
            DOVirtual.DelayedCall(count * 0.05f, placeable.SuccessAnimation);
            count++;
        }
    }
}