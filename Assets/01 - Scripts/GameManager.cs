using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    var obj = new GameObject("GameManager");
                    _instance = obj.AddComponent<GameManager>();
                }
            }

            return _instance;
        }
    }
    private static GameManager _instance;

    [SerializeField] private int levelCount = 3;

    [HideInInspector] public int currentLevel;
    [HideInInspector] public string levelFilePath;

    private int _levelResetIndex;

    private void Awake()
    {
        if(_instance != null)
            Destroy(gameObject);
        else
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _levelResetIndex = levelCount + 1;
    }

    public void LoadLevel(int levelIndex)
    {
        currentLevel = levelIndex % _levelResetIndex + (levelIndex / _levelResetIndex);
        levelFilePath = $"Levels/Level{currentLevel}";
        var operation = SceneManager.LoadSceneAsync((int) Scenes.Gameplay);
        operation.completed += InitializeLevel;
    }

    private void InitializeLevel(AsyncOperation asyncOperation)
    {
        var levelManager = FindObjectOfType<LevelManager>();
        levelManager.FillBoard(levelFilePath);
    }

    public void NextLevel()
    {
        LoadLevel(currentLevel + 1);
    }
    
    //Events
    public event Action<bool> BusyEvent;
    public void SetBusy(bool isBusy)
    {
        BusyEvent?.Invoke(isBusy);
    }

    public event Action<int> PiecePlacementSuccessEvent;
    public void PlacementSuccess(int pieceID)
    {
        PiecePlacementSuccessEvent?.Invoke(pieceID);
    }

    public event Action LevelSuccessEvent;
    public void LevelSuccess()
    {
        LevelSuccessEvent?.Invoke();
    }
}

public enum Scenes
{
    Menu,
    Gameplay
}
