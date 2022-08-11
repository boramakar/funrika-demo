using DG.Tweening;
using UnityEngine;

public class NextLevelHandler : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject clickBlocker;

    private GameManager _gameManager;
    
    private void Awake()
    {
        _gameManager = GameManager.Instance;
        content.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _gameManager.LevelSuccessEvent += Appear;
    }

    private void OnDisable()
    {
        if (_gameManager != null)
        {
            _gameManager.LevelSuccessEvent -= Appear; //this causes memory leak??
        }
    }

    private void Appear()
    {
        content.gameObject.SetActive(true);
        var initialPosition = content.localPosition;
        content.localPosition += Vector3.down * 1000;
        content.DOLocalMove(initialPosition, 1f).OnComplete(() => clickBlocker.SetActive(false));
    }

    public void OnNextLevel()
    {
        GameManager.Instance.NextLevel();
    }
}
