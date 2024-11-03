using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Piece : MonoBehaviour
{
    public Role m_Role;
    public Side m_Side;

    private SpriteRenderer spriteRenderer;

    public int ATK { get; set; }

    public int HP { get; set; }

    public PieceType Type { get; set; }

    public Side Side
    {
        get => m_Side;
        set => m_Side = value;
    }
    public Role Role 
    {
        get => m_Role;
        set => m_Role = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
    }


    public void Initialize(int hp, int atk, Side side, Role role, PieceType type)
    {
        HP = hp;
        ATK = atk;
        m_Side = side;
        m_Role = role;
        Type = type;
        spriteRenderer.sprite =
            SweatShop.Instance.GetSpriteByName((Type + m_Side.ToAbbreviation()).Trim());
        if (m_Side == Side.White) spriteRenderer.flipX = true;
    }

    public void Move()
    {
    }

    public void PlaceAt(int x, int y)
    {
        transform.position = Board.Instance.ToWorld(new Vector3Int(x, y, 0));
    }

    public void OnRoundEnd()
    {
        m_Role.Switch();
    }

    public void Enable()
    {
        spriteRenderer.enabled = true;
    }

    public void Disable()
    {
        spriteRenderer.enabled = false;
    }

    public List<Vector2Int> GetMovableCells()
    {
        var movableCells = new List<Vector2Int>();
        var currentPosition = Board.Instance.ToBoard2D(transform.position);

        switch (Type)
        {
            case PieceType.King:
                // King can move one cell in any direction
                AddMoveIfValid(currentPosition + Vector2Int.up, movableCells);
                AddMoveIfValid(currentPosition + Vector2Int.down, movableCells);
                AddMoveIfValid(currentPosition + Vector2Int.left, movableCells);
                AddMoveIfValid(currentPosition + Vector2Int.right, movableCells);
                AddMoveIfValid(currentPosition + Vector2Int.up + Vector2Int.left, movableCells);
                AddMoveIfValid(currentPosition + Vector2Int.up + Vector2Int.right, movableCells);
                AddMoveIfValid(currentPosition + Vector2Int.down + Vector2Int.left, movableCells);
                AddMoveIfValid(currentPosition + Vector2Int.down + Vector2Int.right, movableCells);
                break;

            case PieceType.Queen:
                // Queen can move any number of cells in any direction
                AddLineMoves(currentPosition, Vector2Int.up, movableCells);
                AddLineMoves(currentPosition, Vector2Int.down, movableCells);
                AddLineMoves(currentPosition, Vector2Int.left, movableCells);
                AddLineMoves(currentPosition, Vector2Int.right, movableCells);
                AddLineMoves(currentPosition, Vector2Int.up + Vector2Int.left, movableCells);
                AddLineMoves(currentPosition, Vector2Int.up + Vector2Int.right, movableCells);
                AddLineMoves(currentPosition, Vector2Int.down + Vector2Int.left, movableCells);
                AddLineMoves(currentPosition, Vector2Int.down + Vector2Int.right, movableCells);
                break;

            case PieceType.Bishop:
                // Bishop can move diagonally
                AddLineMoves(currentPosition, Vector2Int.up + Vector2Int.left, movableCells);
                AddLineMoves(currentPosition, Vector2Int.up + Vector2Int.right, movableCells);
                AddLineMoves(currentPosition, Vector2Int.down + Vector2Int.left, movableCells);
                AddLineMoves(currentPosition, Vector2Int.down + Vector2Int.right, movableCells);
                break;

            case PieceType.Knight:
                // Knight moves in L shapes
                AddMoveIfValid(currentPosition + new Vector2Int(2, 1), movableCells);
                AddMoveIfValid(currentPosition + new Vector2Int(2, -1), movableCells);
                AddMoveIfValid(currentPosition + new Vector2Int(-2, 1), movableCells);
                AddMoveIfValid(currentPosition + new Vector2Int(-2, -1), movableCells);
                AddMoveIfValid(currentPosition + new Vector2Int(1, 2), movableCells);
                AddMoveIfValid(currentPosition + new Vector2Int(1, -2), movableCells);
                AddMoveIfValid(currentPosition + new Vector2Int(-1, 2), movableCells);
                AddMoveIfValid(currentPosition + new Vector2Int(-1, -2), movableCells);
                break;

            case PieceType.Rook:
                // Rook can move vertically or horizontally
                AddLineMoves(currentPosition, Vector2Int.up, movableCells);
                AddLineMoves(currentPosition, Vector2Int.down, movableCells);
                AddLineMoves(currentPosition, Vector2Int.left, movableCells);
                AddLineMoves(currentPosition, Vector2Int.right, movableCells);
                break;

            case PieceType.Pawn:
                // Pawn moves differently based on Side and Role
                if (m_Side == Side.White)
                {
                    AddMoveIfValid(currentPosition + Vector2Int.right, movableCells); // Forward move
                    if (m_Role == Role.Attacker)
                    {
                        AddMoveIfValid(currentPosition + new Vector2Int(1, 1), movableCells); // Attack diagonally right
                        AddMoveIfValid(currentPosition + new Vector2Int(-1, 1), movableCells); // Attack diagonally left
                    }
                }
                else if (m_Side == Side.Black)
                {
                    AddMoveIfValid(currentPosition + Vector2Int.left, movableCells); // Forward move
                    if (m_Role == Role.Attacker)
                    {
                        AddMoveIfValid(currentPosition + new Vector2Int(1, -1),
                            movableCells); // Attack diagonally right
                        AddMoveIfValid(currentPosition + new Vector2Int(-1, -1),
                            movableCells); // Attack diagonally left
                    }
                }

                break;
        }

        print(Type);
        movableCells.ForEach(c => print(c.ToString()));
        return movableCells;
    }

// Helper method to add single moves if within bounds
    private void AddMoveIfValid(Vector2Int position, List<Vector2Int> moves)
    {
        if (Board.Instance.IsWithinBounds(position) && Board.Instance.CanMoveToCell(position, this))
            moves.Add(position);
    }

// Helper method to add line moves (e.g., for Rook, Bishop, Queen)
    private void AddLineMoves(Vector2Int start, Vector2Int direction, List<Vector2Int> moves)
    {
        var current = start + direction;
        while (Board.Instance.IsWithinBounds(current) && Board.Instance.CanMoveToCell(current, this))
        {
            moves.Add(current);
            if (Board.Instance.IsOccupied(current))
                break; // Stop if there's a piece blocking the path
            current += direction;
        }
    }
}

public class PieceComparer : IComparer<Piece>
{
    public int Compare(Piece x, Piece y)
    {
        return x.Type.CompareTo(y.Type);
    }
}