//#define GENERATION_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class LevelManager : MonoBehaviour
{
    [BoxGroup("References")] [SerializeField] private Transform boardParent;
    [BoxGroup("References")] [SerializeField] private Transform piecesParent;
    [BoxGroup("References")] [SerializeField] private TextMeshProUGUI levelTitle;
    [BoxGroup("References")] [SerializeField] private Image blackout;

    [BoxGroup("Tiles")] [SerializeField] private GameObject tilePrefab;
    [BoxGroup("Tiles")] [SerializeField] private float tileSize = 2; //Assume all tiles are square
    [BoxGroup("Tiles")] [SerializeField] private Material letterTileMaterial;
    [BoxGroup("Tiles")] [SerializeField] private Material emptyTileMaterial;

    [BoxGroup("Pieces")] [SerializeField] private GameObject piecePrefab;
    [BoxGroup("Pieces")] [SerializeField] private Transform piecesLimiterTopLeft;
    [BoxGroup("Pieces")] [SerializeField] private Transform piecesLimiterBottomRight; //Assume all tiles are square

    [BoxGroup("Placeables")] [SerializeField] private GameObject placeablePrefab;
    [BoxGroup("Placeables")] [SerializeField] private Material defaultMaterial;
    
    [BoxGroup("Hint")] [SerializeField] private GameObject hintBlocker;
    [BoxGroup("Hint")] [SerializeField] private int autoPlaceLimit;

    private GameManager _gameManager;
    private int[,] _board;
    private LevelData _levelData;
    private int _letterTileCount;
    private bool _generatePiece;
    private WaitForSeconds _waitOneSecond;
    private WaitForSeconds _waitHalfSecond;
    private WaitForFixedUpdate _waitFixedUpdate;
    private int[,] _partitionedBoard;
    private Dictionary<int, Vector3> _piecePositions;
    private bool _isBusy;
    private List<int> _unusedPiecePositions;
    private List<int> _usedPiecePositions;
    private List<Vector3> _cornerPoints;
    private List<Tuple<int, Vector3>> _pieceSizes;
    private int _autoPlaceCount;

    private void Awake()
    {
        _gameManager = GameManager.Instance;
        _isBusy = false;
        _generatePiece = false;
        _letterTileCount = 0;
        _waitOneSecond = new WaitForSeconds(0.5f);
        _waitHalfSecond = new WaitForSeconds(0.25f);
        _waitFixedUpdate = new WaitForFixedUpdate();
        _piecePositions = new Dictionary<int, Vector3>();
        _autoPlaceCount = 0;
        blackout.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        _gameManager.PiecePlacementSuccessEvent += PlacementSuccess;
    }

    private void OnDisable()
    {
        if (_gameManager != null)
        {
            _gameManager.PiecePlacementSuccessEvent -= PlacementSuccess; //this causes memory leak??
        }
    }

    [Button]
    private void GeneratePiece()
    {
        _generatePiece = true;
    }

    public void FillBoard(string path)
    {
        ClearAllChildren(boardParent);

        var jsonFile = Resources.Load<TextAsset>(path);
        string json = jsonFile.ToString();
        // _levelData = JsonUtility.FromJson<LevelData>(json);
        _levelData = JsonConvert.DeserializeObject<LevelData>(json);
        levelTitle.text = $"Level {_levelData.levelID}";
        var rowCount = _levelData.rows;
        var columnCount = _levelData.columns;
        var xCenter = (rowCount - 1) / 2f;
        var yCenter = (columnCount - 1) / 2f;
        _board = new int[columnCount, rowCount]; //column, row order to match x, y plane directions
        for (int i = 0; i < columnCount; i++)
        {
            var distance = yCenter - i;
            var offset = tileSize * distance;
            var column = new GameObject($"Column {i}");
            column.transform.parent = boardParent;
            column.transform.localPosition = Vector3.left * offset;
            for (int j = rowCount - 1; j >= 0; j--)
            {
                var tileState = _levelData.data[j][i];
                var x = i;
                var y = rowCount - j - 1;
                _board[x, y] = tileState - 1;
                distance = xCenter - j;
                offset = tileSize * distance;
                var tile = Instantiate(tilePrefab, column.transform).GetComponent<Tile>();
                if (tileState == 0)
                {
                    tile.SetState(false, emptyTileMaterial, offset);
                }
                else
                {
                    tile.SetState(true, letterTileMaterial, offset);
                    _letterTileCount++;
                }
                // tile.SetPieceID(x + y);
            }
        }

        StartCoroutine(GeneratePieces());
    }

    private IEnumerator GeneratePieces()
    {
        Random.InitState(_levelData.levelID * _levelData.levelID);

        #region Data

        _partitionedBoard = _board;
#if GENERATION_DEBUG
        Debug.Log($"Board: {_partitionedBoard}");
#endif
        ClearAllChildren(piecesParent);
        yield return null;
        var remainingTiles = _letterTileCount;
        var pieceCount = 0;
        var averagePieceCount = 9;
        var lastX = -1;
        var lastY = 0;
        var piecePlacementReferencePosition = piecesLimiterTopLeft.position;
        var averageSize = _letterTileCount / averagePieceCount;
        var minSize = averageSize - 2;
        var maxSize = averageSize + 2;
        _unusedPiecePositions = new List<int>();
        _usedPiecePositions = new List<int>();

        _pieceSizes = new List<Tuple<int, Vector3>>();
        _cornerPoints = new List<Vector3>();
        _cornerPoints.Add(piecePlacementReferencePosition);

        #endregion

        // Fill each tile
        while (remainingTiles > 0)
        {
#if GENERATION_DEBUG
            while (!_generatePiece)
            {
                yield return null;
            }
            _generatePiece = false;
#endif

            #region Initialize piece data

            pieceCount++;
            var pieceSize = Random.Range(Mathf.Min(minSize, remainingTiles), Mathf.Min(maxSize + 1, remainingTiles));
            var piece = Instantiate(piecePrefab, piecesParent);
            piece.name = $"Piece {pieceCount}";
            var pieceTransform = piece.transform;
            var material = new Material(defaultMaterial);
            var color = RandomColorGenerator.GenerateRandomColor();
            material.color = color;
            material.name = $"{pieceCount}: {color}";

            #endregion

            #region SetStartingPoint

            //Get starting point on board
            var boardPosition = new Vector3();
            var found = false;
            lastY--;
            while (!found && lastY < _levelData.rows - 1)
            {
                lastY++;
                while (!found && lastX < _levelData.columns - 1)
                {
                    lastX++;
#if GENERATION_DEBUG
                    boardParent.GetChild(lastX).GetChild(lastY).GetComponent<Renderer>().material.color = Color.blue;
                    yield return _waitHalfSecond;
                    boardParent.GetChild(lastX).GetChild(lastY).GetComponent<Renderer>().material.color = Color.white;
#endif
                    if (_partitionedBoard[lastX, lastY] != 0) continue;

                    boardPosition.x = lastX;
                    boardPosition.y = lastY;
                    _partitionedBoard[lastX, lastY] = pieceCount;
                    found = true;
#if GENERATION_DEBUG
                    boardParent.GetChild(lastX).GetChild(lastY).GetComponent<Renderer>().material.color = Color.green;
                    yield return _waitHalfSecond;
                    boardParent.GetChild(lastX).GetChild(lastY).GetComponent<Renderer>().material.color = Color.white;
#endif
                }

                if (!found)
                    lastX = -1;
            }

            #endregion

            #region Initialize shape generation

            //Set local offset calculation data
            var currentPosition = Vector3.zero;
            var boundaries = Vector4.zero;
            //Generate first block
            var placeable = Instantiate(placeablePrefab, pieceTransform.position, Quaternion.identity, pieceTransform);
            placeable.GetComponent<Renderer>().material = material;
#if GENERATION_DEBUG
            boardParent.GetChild(lastX).GetChild(lastY).GetComponent<Tile>().SetPieceID(pieceCount);
            Debug.Log($"Generation | StartingPoint - Piece: {pieceCount} - Position: {boardPosition}");
#endif
            
            #endregion
            
            #region Generate shape

            List<int> availableDirections;
            for (int i = 1; i < pieceSize; i++)
            {
                //Get random direction
                var isDirectionValid = false;
                availableDirections = new List<int>() {0, 1, 2, 3};
                while (!isDirectionValid && availableDirections.Count > 0)
                {
                    var directionIndex = Random.Range(0, availableDirections.Count);
                    var direction = availableDirections[directionIndex];
                    availableDirections.RemoveAt(directionIndex);
                    //Offset current position based on direction
                    switch (direction)
                    {
                        // Move Up
                        case 0:
                            if (TryAddBlock(pieceCount, Vector3.up, ref boardPosition, ref currentPosition))
                            {
                                isDirectionValid = true;
                                if (currentPosition.y > boundaries.w)
                                {
                                    boundaries.w += 1;
                                }
                            }

                            break;
                        // Move Down
                        case 1:
                            if (TryAddBlock(pieceCount, Vector3.down, ref boardPosition, ref currentPosition))
                            {
                                isDirectionValid = true;
                                if (currentPosition.y < boundaries.y)
                                {
                                    boundaries.y -= 1;
                                }
                            }

                            break;
                        // Move Right
                        case 2:
                            if (TryAddBlock(pieceCount, Vector3.right, ref boardPosition, ref currentPosition))
                            {
                                isDirectionValid = true;
                                if (currentPosition.x > boundaries.z)
                                {
                                    boundaries.z += 1;
                                }
                            }

                            break;
                        // Move Left
                        case 3:
                            if (TryAddBlock(pieceCount, Vector3.left, ref boardPosition, ref currentPosition))
                            {
                                isDirectionValid = true;
                                if (currentPosition.x < boundaries.x)
                                {
                                    boundaries.x -= 1;
                                }
                            }

                            break;
                    }

#if GENERATION_DEBUG
                    yield return _waitOneSecond;
#endif
                }

                //terminate loop, no more space available to extend this piece
                if (!isDirectionValid)
                {
#if GENERATION_DEBUG
                    Debug.Log($"Generation | Piece: {pieceCount} - DeadEnd: {currentPosition}");
#endif
                    pieceSize = i;
                }
                else
                {
                    //Add to pieceOffset if boundary is extended
                    placeable = Instantiate(placeablePrefab, pieceTransform.position + (currentPosition * tileSize),
                        Quaternion.identity, pieceTransform);
                    placeable.GetComponent<Renderer>().material = material;
                }
            }

            #endregion

            #region Center placeables on piece

            piece.GetComponent<PieceHandler>().InitPlaceables();
            var centerOffset =
                (new Vector3(boundaries.x, boundaries.y, 0) + new Vector3(boundaries.z, boundaries.w, 0)) / 2;
            centerOffset *= tileSize;
            // offset all pieces to center it based on piece position
            for (int i = 0; i < pieceSize; i++)
            {
                pieceTransform.GetChild(i).localPosition -= centerOffset;
#if GENERATION_DEBUG
                yield return _waitOneSecond;
#endif
            }

            #endregion

            #region Set hint data

            var tilePosition = boardParent.GetChild((int) boardPosition.x).GetChild((int) boardPosition.y).position;
            var placementPosition = tilePosition + centerOffset;
            placementPosition.z = 0;
            _piecePositions.Add(pieceCount, placementPosition);
            _unusedPiecePositions.Add(pieceCount);

            #endregion

            #region Piece placement on screen

            var pieceBounds = new Vector3(boundaries.z, boundaries.w, 0) - new Vector3(boundaries.x, boundaries.y, 0);
            pieceBounds += Vector3.right + Vector3.up;
            pieceBounds *= tileSize;
            pieceBounds += Vector3.one * tileSize;
            // pieceBounds.x = pieceBounds.x * tileSize + tileSize;
            // pieceBounds.y = pieceBounds.y * tileSize + tileSize;
            _pieceSizes.Add(new Tuple<int, Vector3>(pieceCount - 1, pieceBounds));
            var boxCollider = piece.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(pieceBounds.x, pieceBounds.y, tileSize);
            piece.transform.position += Vector3.forward * 10;

            #endregion
            
            remainingTiles -= pieceSize;
        }

        StartCoroutine(PlacePiecesOnScreen());
    }

    private IEnumerator PlacePiecesOnScreen()
    {
        _pieceSizes.Sort((a, b) => b.Item2.magnitude.CompareTo(a.Item2.magnitude));

        var limiterPosition = piecesLimiterBottomRight.position;
        var xLimit = limiterPosition.x;
        var yLimit = limiterPosition.y;
        //for each piece
        for (int i = 0; i < _pieceSizes.Count; i++)
        {
            var piece = piecesParent.GetChild(_pieceSizes[i].Item1);
            var pieceBounds = _pieceSizes[i].Item2;
            var found = false;
            //find a viable starting point
            for (int j = 0; !found && j < _cornerPoints.Count; j++)
            {
                var cornerPoint = _cornerPoints[j];
                piece.transform.position =
                    cornerPoint + Vector3.right * pieceBounds.x / 2 + Vector3.down * pieceBounds.y / 2;
#if GENERATION_DEBUG
                Debug.Log($"Piece: {piece.GetSiblingIndex()} - Corner: {cornerPoint}");
                yield return _waitOneSecond;
                #else 
                yield return _waitFixedUpdate;
#endif
                //if piece fits into remaining space
                Collider[] hitColliders =
                    Physics.OverlapBox(piece.transform.position, pieceBounds / 2 - Vector3.one * 0.05f,
                        Quaternion.identity, LayerMask.GetMask("Piece"));
                if (cornerPoint.x + pieceBounds.x <= xLimit && cornerPoint.y - pieceBounds.y >= yLimit &&
                    hitColliders.Length == 1)
                {
                    //generate new starting points to replace the used one
                    _cornerPoints.RemoveAt(j);
                    _cornerPoints.Add(cornerPoint + Vector3.down * pieceBounds.y);
                    _cornerPoints.Add(cornerPoint + Vector3.right * pieceBounds.x);
                    found = true;
                }
            }

            if (!found)
            {
                Debug.Log("Piece placement failed");
            }
        }

        blackout.DOFade(0, 0.5f).OnComplete(() => blackout.gameObject.SetActive(false)); //remove blackout
    }

    private bool TryAddBlock(int pieceID, Vector3 direction, ref Vector3 boardPosition, ref Vector3 currentPosition)
    {
        var localOffset = currentPosition + direction;
        var nextBoardPosition = boardPosition + localOffset;
#if GENERATION_DEBUG
        boardParent.GetChild((int) nextBoardPosition.x).GetChild((int) nextBoardPosition.y)
            .GetComponent<Renderer>().material.color = Color.yellow;
#endif
        var inBounds = IsInBounds(nextBoardPosition);
        if (!inBounds || _partitionedBoard[(int) nextBoardPosition.x, (int) nextBoardPosition.y] != 0) return false;
#if GENERATION_DEBUG
        boardParent.GetChild((int) nextBoardPosition.x).GetChild((int) nextBoardPosition.y)
            .GetComponent<Renderer>().material.color = Color.red;
        Debug.Log($"Generation | Piece: {pieceID} - Position: {nextBoardPosition}");
        boardParent.GetChild((int) nextBoardPosition.x).GetChild((int) nextBoardPosition.y)
            .GetComponent<Tile>().SetPieceID(pieceID);
#endif
        _partitionedBoard[(int) nextBoardPosition.x, (int) nextBoardPosition.y] = pieceID;
        currentPosition = localOffset;
        return true;
    }

    private bool IsInBounds(Vector3 nextPosition)
    {
        return nextPosition.x >= 0 && nextPosition.x < _levelData.columns &&
               nextPosition.y >= 0 && nextPosition.y < _levelData.rows;
    }

    private void ClearAllChildren(Transform parent)
    {
        if (Application.isPlaying)
            while (parent.childCount > 0)
            {
                DestroyImmediate(parent.GetChild(0).gameObject);
            }
        else
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
    }

    public void AutoPlacePiece()
    {
        if (_isBusy || _unusedPiecePositions.Count == 0) return;
        _isBusy = true;
        var randomPieceIndex = Random.Range(0, _unusedPiecePositions.Count);
        var randomPieceKey = _unusedPiecePositions[randomPieceIndex];
        var randomPiecePosition = _piecePositions[randomPieceKey];
        var piece = piecesParent.GetChild(randomPieceKey - 1);
        _usedPiecePositions.Add(randomPieceKey);
        _unusedPiecePositions.RemoveAt(randomPieceIndex);
        _autoPlaceCount++;
        if (_autoPlaceCount > autoPlaceLimit)
        {
            hintBlocker.SetActive(true);
        }
        piece.GetComponent<PieceHandler>().AutoPlace(randomPiecePosition, AutoPlaceComplete);
    }

    private void AutoPlaceComplete()
    {
        _isBusy = false;
        GameManager.Instance.SetBusy(false);
        CheckSuccess();
    }

    private void PlacementSuccess(int id)
    {
        _unusedPiecePositions.Remove(id);
        _usedPiecePositions.Add(id);
        CheckSuccess();
    }

    private void CheckSuccess()
    {
        if (_unusedPiecePositions.Count == 0)
        {
            GameManager.Instance.LevelSuccess();
            for (int i = 0; i < piecesParent.childCount; i++)
            {
                DOVirtual.DelayedCall(0.1f * i, piecesParent.GetChild(i).GetComponent<PieceHandler>().SuccessAnimation);
            }
        }
    }
}