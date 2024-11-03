﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private Grid grid;

    public Vector3Int BoardOffset;

    public bool DealingWithPieces;
    public bool DoneWithPieces;
    public GameObject _placeablePrefab;
    public GameObject _cursor;
    public List<GameObject> _placeableTiles;
    public List<Piece> _pieces;

    public Dictionary<Vector2Int, GameObject> m_ObjectsOnBoard;
    private Side m_Side;

    public static Board Instance { get; private set; }

    private void Awake()
    {
        DealingWithPieces = false;
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

        m_ObjectsOnBoard = new Dictionary<Vector2Int, GameObject>();
        grid = GetComponent<Grid>();
        BoardOffset = new Vector3Int(-3, -4, 0);
        _pieces = new List<Piece>();
        _cursor.SetActive(false);
    }

    public void MakeThePiecesAlive()
    {
        DealingWithPieces = true;
        DoneWithPieces = false;
        StartCoroutine(Fight());
    }

    private IEnumerator Fight()
    {
        var attackers = _pieces.Where(p => p.Role == Role.Attacker && p.Type != PieceType.King)
            .OrderByDescending(p => p.Type);
        var defenders = _pieces.Where(p => p.Role == Role.Defender).ToList();

        foreach (var attacker in attackers)
        {
            var reachable = attacker.GetMovableCells();
            var attackable = defenders.Where(d => reachable.Contains(ToBoard2D(d.transform.position)))
                .OrderBy(p => p.Type).ToList();

            if (attackable.Count > 0)
            {
                var defender = attackable[0];
                yield return StartCoroutine(Attack(attacker, defender, defenders));
            }
            else
            {
                // move toward king
                var king = defenders.FirstOrDefault(p => p.Type == PieceType.King);
                if (king)
                {
                    var closet = new Vector3(999, 999, 999);
                    foreach (var re in reachable)
                    {
                        var re3 = ToWorld2D(re);
                        if (Vector3.Distance(re3, king.transform.position) <
                            Vector3.Distance(closet, king.transform.position))
                            closet = re3;
                    }

                    if (closet != new Vector3(999, 999, 999))
                        yield return StartCoroutine(MoveTowards(attacker, closet));
                }
            }
        }

        DealingWithPieces = false;
        DoneWithPieces = true;
    }

    private IEnumerator Attack(Piece attacker, Piece defender, List<Piece> defenders)
    {
        var originalPosition = ToWorld(ToBoard(attacker.transform.position));
        var defenderPosition = ToWorld(ToBoard(defender.transform.position));
        // Move attacker towards defender
        var elapsedTime = 0f;
        while (elapsedTime < 0.5f)
        {
            attacker.transform.position = Vector3.Lerp(originalPosition, defenderPosition, elapsedTime / 0.5f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        attacker.transform.position = defenderPosition;

        // Execute attack
        defender.HP -= Mathf.Max(attacker.ATK - 2, 0); // Consider defender's defense
        attacker.HP -= defender.ATK;

        // Check defender's HP and handle accordingly
        if (defender.HP <= 0)
        {
            defenders.Remove(defender);
            defender.gameObject.SetActive(false);
            _pieces.Remove(defender);
        }
        else
        {
            // Move attacker back to the original position if the defender is still alive
            elapsedTime = 0f;
            while (elapsedTime < 0.5f)
            {
                attacker.transform.position = Vector3.Lerp(defenderPosition, originalPosition, elapsedTime / 0.5f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            attacker.transform.position = originalPosition;
        }
    }

    private IEnumerator MoveTowards(Piece attacker, Vector3 position)
    {
        var originalPosition = ToWorld(ToBoard(attacker.transform.position));
        position = ToWorld(ToBoard(position));
        // Calculate the direction towards the king and move a step closer (0.5 seconds)
        var elapsedTime = 0f;
        while (elapsedTime < 0.5f)
        {
            attacker.transform.position = Vector3.Lerp(originalPosition, position, elapsedTime / 0.5f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Update the final position
        attacker.transform.position = Vector3.MoveTowards(originalPosition, position, 1f); // Adjust the step as needed
    }


    public List<Vector2Int> GetPlaceableCells(Side side)
    {
        var placeableCells = new List<Vector2Int>();

        // Add initial rows based on side
        var startRow = side == Side.White ? 0 : 7;
        var secondRow = side == Side.White ? 1 : 6;

        for (var x = 0; x < 8; x++)
        {
            placeableCells.Add(new Vector2Int(startRow, x));
            placeableCells.Add(new Vector2Int(secondRow, x));
        }

        // Get movable cells for pieces of the specified side
        _pieces
            .Where(piece => piece.Side == side)
            .ToList()
            .ForEach(piece => placeableCells.AddRange(piece.GetMovableCells()));

        // Filter out occupied cells
        return placeableCells
            .Distinct()
            .Where(loc => _pieces.All(piece => ToBoard2D(piece.transform.position) != loc))
            .ToList();
    }

    public List<Vector2Int> ShowPlaceableCells(Side side)
    {
        m_Side = side;
        var l = GetPlaceableCells(side);
        foreach (var loc in l)
        {
            if (m_ObjectsOnBoard.ContainsKey(loc))
                continue;
            var placeable = Instantiate(_placeablePrefab, transform);
            PlaceAt(placeable, loc.x, loc.y);
            m_ObjectsOnBoard.Add(loc, placeable);
        }

        return l;
    }

    public void SetupSide(Side side, Role role)
    {
        _pieces.ForEach(piece =>
        {
            if (piece.Side == side)
                piece.Role = role;
        });
    }

    public void UpdatePlaceableCells()
    {
        var l = GetPlaceableCells(m_Side);
        foreach (var loc in l)
        {
            if (m_ObjectsOnBoard.ContainsKey(loc))
                continue;
            var placeable = Instantiate(_placeablePrefab, transform);
            PlaceAt(placeable, loc.x, loc.y);
            m_ObjectsOnBoard.Add(loc, placeable);
        }
    }

    public void HidePlaceableCells()
    {
        // remove placeables
        var placeables = m_ObjectsOnBoard.Where(kvp => kvp.Value.CompareTag("Placeable")).ToArray();
        foreach (var kvp in placeables)
        {
            m_ObjectsOnBoard.Remove(kvp.Key);
            Destroy(kvp.Value);
        }
    }

    public void DropPiece(Piece piece, Vector2Int position)
    {
        if (m_ObjectsOnBoard.ContainsKey(position) &&
            m_ObjectsOnBoard[position].CompareTag("Placeable"))
        {
            var placeable = m_ObjectsOnBoard[position];
            m_ObjectsOnBoard.Remove(position);
            Destroy(placeable);
        }

        piece.Enable();
        piece.transform.position = Instance.ToWorld2D(position);
        _pieces.Add(piece);
        m_ObjectsOnBoard.Add(position, piece.gameObject);
        UpdatePlaceableCells();
    }

    public void DropPiece(Piece piece, Vector3Int position)
    {
        DropPiece(piece, new Vector2Int(position.x, position.y));
    }

    public bool CanPlaceAt(int x, int y)
    {
        return m_ObjectsOnBoard.ContainsKey(new Vector2Int(x, y)) &&
               m_ObjectsOnBoard[new Vector2Int(x, y)].CompareTag("Placeable");
    }

    public void PlaceAt(GameObject go, int x, int y)
    {
        foreach (var (k, v) in m_ObjectsOnBoard)
            if (v.CompareTag("Placeable"))
                v.SetActive(true);

        if (m_ObjectsOnBoard.ContainsKey(new Vector2Int(x, y)) &&
            m_ObjectsOnBoard[new Vector2Int(x, y)].CompareTag("Placeable") && go.CompareTag("Piece"))
            m_ObjectsOnBoard[new Vector2Int(x, y)].SetActive(false);
        go.transform.position = Instance.ToWorld(new Vector3Int(x, y, 0));
    }


    public void Move(GameObject go, int x, int y)
    {
        var bp = ToBoard(go.transform.position);
        PlaceAt(go, bp.x + x, bp.y + y);
    }

    public void MoveTo(GameObject go, int x, int y)
    {
        PlaceAt(go, x, y);
    }

    public Vector3Int ToBoard(Vector3 pos)
    {
        return grid.WorldToCell(pos) - BoardOffset;
    }

    public Vector2Int ToBoard2D(Vector3 pos)
    {
        return new Vector2Int(ToBoard(pos).x, ToBoard(pos).y);
    }

    public Vector3 ToWorld(Vector3Int pos)
    {
        return grid.CellToWorld(pos + BoardOffset);
    }

    public Vector3 ToWorld2D(Vector2Int pos)
    {
        return grid.CellToWorld(new Vector3Int(pos.x, pos.y) + BoardOffset);
    }

    public List<Piece> GetPieces()
    {
        return _pieces;
    }

    public bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }

    public bool CanMoveToCell(Vector2Int pos, Piece piece)
    {
        if (!m_ObjectsOnBoard.ContainsKey(pos)) return true;
        var otherPiece = _pieces.Find(p => p.gameObject == m_ObjectsOnBoard[pos]);
        return !otherPiece || otherPiece.Side != piece.Side;
    }

    public bool IsOccupied(Vector2Int pos)
    {
        return _pieces.Any(p => ToBoard2D(p.transform.position) == pos);
    }
}