using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SweatShop : MonoBehaviour
{
    public GameObject _roundText;

    public GameObject _piecePrefab;
    public GameObject PieceUIPrefab;
    public GameObject m_SkillTextPrefab;
    public GameObject m_SkillIconPrefab;
    private readonly Dictionary<SkillType, Sprite> _skillIconsDict = new();
    private readonly Dictionary<string, Skill> _skills = new();
    private List<Piece> _blackPieces;
    private List<Piece> _pieces;

    private Sprite[] _piecesSprites;
    private Sprite[] _skillIcons;

    private List<Piece> _usedPieces;
    private List<Piece> _whitePieces;
    private RoundTextAnimator RTA;

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

        _skillIcons = Resources.LoadAll<Sprite>("Buff");
        // Initialize _pieces
        _pieces = new List<Piece>();
        _blackPieces = new List<Piece>();
        _whitePieces = new List<Piece>();
        _usedPieces = new List<Piece>();
        RTA = _roundText.GetComponent<RoundTextAnimator>();

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
            var def = int.Parse(fields[4]);
            var range = int.Parse(fields[5]);
            var speed = int.Parse(fields[6]);
            // Add black pieces
            for (var j = 0; j < count; j++)
            {
                var blackPiece =
                    Instance.CreateInstance<Piece>(hp, atk, def, range, speed, Side.Black, Role.None,
                        pieceName.ToPieceType());
                _blackPieces.Add(blackPiece);
                var whitePiece =
                    Instance.CreateInstance<Piece>(hp, atk, def, range, speed, Side.White, Role.None,
                        pieceName.ToPieceType());
                _whitePieces.Add(whitePiece);
            }
        }

        var skillDate = Resources.Load<TextAsset>("Skill").text.Split('\n');
        for (var i = 1; i < skillDate.Length; i++)
        {
            var line = skillDate[i];
            if (string.IsNullOrWhiteSpace(line)) continue; // Skip empty lines

            var fields = line.Split(',');
            var skillID = int.Parse(fields[0]);
            var skillName = fields[1];
            var effectType = fields[2];
            var effectValue = int.Parse(fields[3]);
            var duration = int.Parse(fields[4]);
            var probability = float.Parse(fields[5]);
            var SpriteName = fields[6];
            _skillIconsDict.Add((SkillType)skillID, _skillIcons.SingleOrDefault(s => s.name == SpriteName));
            var description = fields[7];
            var skill = new Skill(skillID, skillName, effectType, effectValue, duration, probability, description);
            _skills.Add(skillName, skill);
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
            _usedPieces.Add(dr);
            _whitePieces.Remove(dr);
            return dr;
        }
    }

    public void ReCyclePieces(List<Piece> pieces)
    {
        // ♻️
        foreach (var piece in pieces)
        {
            piece.Reset();
            piece.Disable();
            if (piece.Side == Side.Black) _blackPieces.Add(piece);
            else _whitePieces.Add(piece);
        }
    }

    public int GetRemainPiecesCount(Side side)
    {
        return side == Side.White ? _whitePieces.Count : _blackPieces.Count;
    }

    public void CreateRoundText(string text)
    {
        RTA.ShowRound(text);
    }

    public void ShowSkillText(string skillName, Transform tf)
    {
        CreateInstance<SkillTextAnimator>(skillName, tf);
    }

    public GameObject GetSkillIcon(SkillType skillType, Transform tf)
    {
        var go = Instantiate(m_SkillIconPrefab, tf);
        go.GetComponent<SpriteRenderer>().sprite = _skillIconsDict[skillType];
        return go;
    }

    public Skill GetRandomSkill()
    {
        // based on probability
        var random = Random.Range(0f, 1f);
        var currentProbability = 0f;
        foreach (var skill in _skills.Values)
        {
            currentProbability += skill.Probability;
            if (random < currentProbability)
                return new Skill(skill.SkillID, skill.SkillName, skill.EffectType, skill.EffectValue,
                    skill.Duration, skill.Probability, skill.Description);
        }

        return null;
    }

    public T CreateInstance<T>(int hp, int atk, int def, int range, int speed, Side side, Role role, PieceType type)
        where T : Piece
    {
        var pieceObject = Instantiate(Instance._piecePrefab, Vector3.zero, Quaternion.identity);
        // pieceObject.SetActive(false);
        var piece = pieceObject.GetComponent<T>();
        piece.Disable();
        piece.Initialize(hp, atk, def, range, speed, side, role, type);
        Instance._pieces.Add(piece);
        return piece;
    }

    public T CreateInstance<T>(string text, Transform tf) where T : SkillTextAnimator
    {
        var textObject = Instantiate(m_SkillTextPrefab, tf);
        var textAnimator = textObject.GetComponent<T>();
        textAnimator.Play(text, tf.position);
        return textAnimator;
    }
}