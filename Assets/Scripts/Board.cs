using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private Grid grid;

    public Vector3Int BoardOffset;

    public bool DoneWithPieces;
    public GameObject _placeablePrefab;
    public GameObject _cursor;
    public List<GameObject> _placeableTiles;
    public List<Piece> _pieces;
    public List<Piece> _deadPieces;

    private Dictionary<Vector2Int, GameObject> m_ObjectsOnBoard;
    private Side m_Side;

    public static Board Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern to ensure there's only one instance of SweatShop
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        m_ObjectsOnBoard = new Dictionary<Vector2Int, GameObject>();
        grid = GetComponent<Grid>();
        BoardOffset = new Vector3Int(-3, -4, 0);
        _pieces = new List<Piece>();
        _deadPieces = new List<Piece>();
        _cursor.SetActive(false);
    }

    public void Reset()
    {
        m_ObjectsOnBoard.Clear();
        _pieces.Clear();
        _deadPieces.Clear();
        _placeableTiles.ForEach(t => Destroy(t));
        _placeableTiles.Clear();
    }

    public void MakeThePiecesAlive()
    {
        DoneWithPieces = false;
        GetSkills();
        StartCoroutine(Fight());
    }

    private void GetSkills()
    {
        _pieces.Where(p => p.IsAlive()).ToList().ForEach(p =>
        {
            for (var i = 0; i < p.ABILITY; i++)
            {
                var sk = SweatShop.Instance.GetRandomSkill();
                if (sk != null) p.Acquire(sk);
            }
        });
    }

    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private IEnumerator Fight()
    {
        _pieces.ForEach(p => p.FightStart());
        var attackers = _pieces.Where(p => p.Role == Role.Attacker && p.Type != PieceType.King && p.IsAlive())
            .OrderByDescending(p => p.Type);
        var defenders = _pieces.Where(p => p.Role == Role.Defender && p.IsAlive()).ToList();

        foreach (var attacker in attackers)
            for (var _ = 0; _ < attacker.SPEED; _++)
            {
                if (!attacker.IsAlive()) break;
                var reachable = attacker.GetMovableCells();
                var attackable = defenders.Where(d => reachable.Contains(ToBoard2D(d.transform.position)))
                    .OrderByDescending(p => p.Type).ToList();

                if (attackable.Count > 0)
                {
                    var defender = attackable[0];
                    yield return StartCoroutine(Attack(attacker, defender, defenders));
                }
                else
                {
                    // move toward king
                    var king = defenders.FirstOrDefault(p => p.Type == PieceType.King && p.Role == Role.Defender);
                    if (king)
                    {
                        var closet = new Vector2Int(999, 999);
                        var kingCell = ToBoard2D(king.transform.position);
                        foreach (var re in reachable)
                            if (!IsOccupied(re) && ManhattanDistance(re, kingCell) <
                                ManhattanDistance(closet, kingCell))
                                closet = re;

                        if (closet != new Vector2Int(999, 999))
                        {
                            m_ObjectsOnBoard.Remove(ToBoard2D(attacker.transform.position));
                            yield return StartCoroutine(MoveTowards(attacker, closet));
                        }
                    }
                }
            }

        _pieces.ForEach(p => p.FightEnd());
        DoneWithPieces = true;
    }

    private IEnumerator Attack(Piece attacker, Piece defender, List<Piece> defenders)
    {
        var originalPosition = ToWorld(ToBoard(attacker.transform.position));
        var defenderPosition = ToWorld(ToBoard(defender.transform.position));
        // Move attacker towards defender
        var elapsedTime = 0f;
        while (elapsedTime < 0.3f)
        {
            attacker.transform.position = Vector3.Lerp(originalPosition, defenderPosition, elapsedTime / 0.3f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        attacker.transform.position = ToWorld(ToBoard(defenderPosition));


        var baseDamage = attacker.ATK * 1.5f;

        var defenderTotalDEF = defender.DEF * 1.5f;

        var initialDamage = baseDamage - defenderTotalDEF;

        var damageAfterReduction = initialDamage * 0.8f;

        var actualDamage = Mathf.Max(Mathf.RoundToInt(damageAfterReduction), 1);

        if (Random.value < 0.2f)
            actualDamage = Mathf.RoundToInt(actualDamage * 1.5f);
        var counterDamage = 0;
        if (Random.value < 0.3f)
        {
            var counterBaseDamage = defender.ATK * 1.0f;
            var attackerTotalDEF = attacker.DEF * 1.0f;
            var counterInitialDamage = counterBaseDamage - attackerTotalDEF;
            counterDamage = Mathf.Max(Mathf.RoundToInt(counterInitialDamage), 1);
        }

        yield return StartCoroutine(RunBothCoroutines());

        IEnumerator RunBothCoroutines()
        {
            if (counterDamage > 0)
            {
                var attackerCoroutine = StartCoroutine(attacker.Hurt(actualDamage));
                var defenderCoroutine = StartCoroutine(defender.Hurt(counterDamage));
                yield return attackerCoroutine;
                yield return defenderCoroutine;
            }
            else
            {
                var defenderCoroutine = StartCoroutine(defender.Hurt(actualDamage));
                yield return defenderCoroutine;
            }
            // Wait until both coroutines are done
        }

        if (attacker.HP <= 0)
        {
            m_ObjectsOnBoard.Remove(ToBoard2D(originalPosition));
            attacker.Disable();
            _deadPieces.Add(attacker);
        }

        // Check defender's HP and handle accordingly
        if (defender.HP <= 0)
        {
            defenders.Remove(defender);
            defender.Disable();
            m_ObjectsOnBoard.Remove(ToBoard2D(defenderPosition));
            m_ObjectsOnBoard.Remove(ToBoard2D(originalPosition));
            m_ObjectsOnBoard.Add(ToBoard2D(attacker.transform.position), attacker.gameObject);
            _deadPieces.Add(defender);
        }

        if (attacker.IsAlive() && defender.IsAlive())
        {
            // Move attacker back to the original position if the defender is still alive
            elapsedTime = 0f;
            while (elapsedTime < 0.3f)
            {
                attacker.transform.position = Vector3.Lerp(defenderPosition, originalPosition, elapsedTime / 0.3f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            attacker.transform.position = originalPosition;
        }
    }

    private IEnumerator MoveTowards(Piece attacker, Vector2Int position)
    {
        var originalPosition = ToWorld(ToBoard(attacker.transform.position));
        // Calculate the direction towards the king and move a step closer (0.5 seconds)
        var elapsedTime = 0f;
        while (elapsedTime < 0.3f)
        {
            attacker.transform.position = Vector3.Lerp(originalPosition, ToWorld2D(position), elapsedTime / 0.3f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Update the final position
        attacker.transform.position = ToWorld2D(position);
        m_ObjectsOnBoard[ToBoard2D(attacker.transform.position)] = attacker.gameObject;
    }


    private List<Vector2Int> GetPlaceableCells(Side side)
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
            .Where(piece => piece.Side == side && piece.IsAlive())
            .ToList()
            .ForEach(piece => placeableCells.AddRange(piece.GetMovableCells()));

        // Filter out occupied cells
        return placeableCells
            .Distinct()
            .Where(loc => _pieces.All(piece => !piece.IsAlive() || ToBoard2D(piece.transform.position) != loc))
            .ToList();
    }

    public Piece GetPieceAt(Vector2Int loc)
    {
        return _pieces.Find(piece => ToBoard2D(piece.transform.position) == loc);
    }

    public bool HaveEnemyPieceAt(Vector2Int loc, Side side)
    {
        return _pieces.Any(piece =>
            piece.Side != side && ToBoard2D(piece.transform.position) == loc && piece.IsAlive());
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

    private void UpdatePlaceableCells()
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
        if (piece.Type != PieceType.King)
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

    public List<Piece> GetAllPieces()
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
        var otherPiece = _pieces.Find(p => p.gameObject == m_ObjectsOnBoard[pos] && p.IsAlive());
        return !otherPiece || otherPiece.Side != piece.Side;
    }

    public bool IsOccupied(Vector2Int pos)
    {
        if (!m_ObjectsOnBoard.ContainsKey(pos)) return false;
        var otherPiece = _pieces.Find(p => p.gameObject == m_ObjectsOnBoard[pos] && p.IsAlive());
        return otherPiece;
    }
}