using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SweatShop : MonoBehaviour
{
    [FormerlySerializedAs("_roundTextPrefab")] [FormerlySerializedAs("_roundNumberPrefab")]
    public GameObject _roundText;

    public GameObject _piecePrefab;
    public GameObject PieceUIPrefab;
    private List<Piece> _blackPieces;
    private List<Piece> _pieces;

    private Sprite[] _piecesSprites;

    private List<Piece> _usedPieces;
    private List<Piece> _whitePieces;

    public static SweatShop Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern to ensure there's only one instance of SweatShop
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: if you want this to persist between scenes
        }

        _piecesSprites = Resources.LoadAll<Sprite>("Pieces/ChessAssets");
        _piecesSprites = _piecesSprites.Concat(Resources.LoadAll<Sprite>("Pieces/ChessAssetsUI")).ToArray();
        // Initialize _pieces
        _pieces = new List<Piece>();
        _blackPieces = new List<Piece>();
        _whitePieces = new List<Piece>();
        _usedPieces = new List<Piece>();

        var csvData = Resources.Load<TextAsset>("PieceData");
        var rawData = csvData.text.Split('\n');
        for (var i = 1; i < rawData.Length; i++)
        {
            var line = rawData[i];
            if (string.IsNullOrWhiteSpace(line)) continue; // Skip empty lines

            var fields = line.Split(',');
            var pieceName = fields[0];
            var count = int.Parse(fields[1]);
            var atk = int.Parse(fields[2]);
            var hp = int.Parse(fields[3]);

            Debug.Log($"{pieceName} {count} {atk} {hp}");

            // Add black pieces
            for (var j = 0; j < count; j++)
            {
                var blackPiece =
                    Instance.CreateInstance<Piece>(hp, atk, Side.Black, Role.None, pieceName.ToPieceType());
                _blackPieces.Add(blackPiece);
            }

            // Add white pieces
            for (var j = 0; j < count; j++)
            {
                var whitePiece =
                    Instance.CreateInstance<Piece>(hp, atk, Side.White, Role.None, pieceName.ToPieceType());
                _whitePieces.Add(whitePiece);
            }
        }
    }

    public List<Piece> GetPieces(Side side)
    {
        return side == Side.White ? _whitePieces : _blackPieces;
    }

    public Sprite GetSpriteByName(string name)
    {
        return Array.Find(_piecesSprites, sprite => sprite.name == name);
    }

    public Piece DrawPiece(Side side, PieceType? pieceType = null)
    {
        if (!pieceType.HasValue)
        {
            if (side == Side.White)
            {
                if (_whitePieces.Count == 0) return null;
                var dr = _whitePieces.ElementAt(Random.Range(0, _whitePieces.Count));
                _usedPieces.Add(dr);
                _whitePieces.Remove(dr);
                return dr;
            }
            else
            {
                if (_blackPieces.Count == 0) return null;
                var dr = _blackPieces.ElementAt(Random.Range(0, _blackPieces.Count));
                _usedPieces.Add(dr);
                _blackPieces.Remove(dr);
                return dr;
            }
        }

        if (side == Side.Black)
        {
            var dr = _blackPieces.Single(p => p.Type == pieceType);
            if (!dr) return null;
            _usedPieces.Add(dr);
            _blackPieces.Remove(dr);
            return dr;
        }
        else
        {
            var dr = _whitePieces.Single(p => p.Type == pieceType);
            if (!dr) return null;
            Assert.IsNotNull(dr);
            _usedPieces.Add(dr);
            _whitePieces.Remove(dr);
            return dr;
        }
    }

    public void ReCyclePieces(List<Piece> pieces)
    {
        // ♻️
        foreach (var piece in pieces)
            if (piece.Side == Side.Black) _blackPieces.Add(piece);
            else _whitePieces.Add(piece);
    }

    public int GetRemainPiecesCount(Side side)
    {
        return side == Side.White ? _whitePieces.Count : _blackPieces.Count;
    }

    public void CreateRoundText(string text)
    {
        _roundText.GetComponent<RoundTextAnimator>().ShowRound(text);
    }

    public T CreateInstance<T>(int hp, int atk, Side side, Role role, PieceType type) where T : Piece
    {
        var pieceObject = Instantiate(Instance._piecePrefab, Vector3.zero, Quaternion.identity);
        // pieceObject.SetActive(false);
        var piece = pieceObject.AddComponent<T>();
        piece.Disable();
        piece.Initialize(hp, atk, side, role, type);
        Instance._pieces.Add(piece);
        return piece;
    }
}