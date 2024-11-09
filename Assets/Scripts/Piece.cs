using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Piece : MonoBehaviour
{
    public Role m_Role;
    public Side m_Side;
    public HealthBar m_HealthBar;
    public GameObject m_Buffs;

    private int _ability;

    private int _atk;
    private int _def;
    private int _hp;
    public List<Skill> _skills = new();
    private int _speed;
    private Dictionary<Skill, GameObject> m_SkillIcon = new();
    private float maxHealthBarSize;

    private SpriteRenderer spriteRenderer;
    public float HpPercentage => (float)HP / MaxHealth;

    public int ATK
    {
        get
        {
            var atk =
                _skills.Where(s => s.SkillType == SkillType.AttackStrike);
            var mul = atk.Aggregate(1, (current, skill) => current * skill.EffectValue);
            return mul * (_atk + _skills.Where(s => s.SkillType == SkillType.AttackBoost).Sum(s => s.EffectValue));
        }
        set => _atk = value;
    }

    public int HP
    {
        get => _hp;
        set => _hp = value;
    }

    public int DEF
    {
        get { return _def + _skills.Where(s => s.SkillType == SkillType.DefenseBoost).Sum(s => s.EffectValue); }
        set => _def = value;
    }

    public int ABILITY
    {
        get => _ability;
        set => _ability = value;
    }

    public int SPEED
    {
        get => _speed;
        set => _speed = value;
    }

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

    public Sprite m_Sprite
    {
        get => spriteRenderer.sprite;
        set => spriteRenderer.sprite = value;
    }

    public int MaxHealth { get; set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Reset()
    {
        HP = MaxHealth;
        transform.rotation = Quaternion.identity;
        _skills.Clear();
        foreach (var skill in m_SkillIcon) Destroy(skill.Value);
        m_SkillIcon.Clear();
        m_HealthBar.Reset();
        // find all children with tag SkillText and destroy them
        foreach (Transform child in transform)
            if (child.CompareTag("SkillText"))
                Destroy(child.gameObject);
    }

    private void Start()
    {
    }

    public void FightStart()
    {
    }

    public void ShowSkillText(string skillName)
    {
        SweatShop.Instance.ShowSkillText(skillName, transform);
    }

    public void FightEnd()
    {
        if (!IsAlive()) return;
        var healing = _skills.Where(s => s.SkillType == SkillType.HealthRefill).Sum(s => s.EffectValue);
        if (healing > 0)
        {
            HP += healing;
            ShowSkillText("+" + healing + "HP");
            if (HP > MaxHealth) HP = MaxHealth;
        }

        foreach (var skill in _skills) skill.Duration -= 1;
        var sks = _skills.Where(s => s.Duration <= 0).ToList();
        foreach (var skill in sks) Destroy(m_SkillIcon[skill]);
        _skills = _skills.Where(s => s.Duration > 0).ToList();
    }


    public void Initialize(int hp, int atk, int def, int ability, int speed, Side side, Role role, PieceType type)
    {
        m_HealthBar.SetMaxHealth(hp);
        MaxHealth = hp;
        _hp = hp;
        _atk = atk;
        _def = def;
        _ability = ability;
        _speed = speed;
        m_Side = side;
        m_Role = role;
        Type = type;
        spriteRenderer.sprite =
            SweatShop.Instance.GetSpriteByName((Type + m_Side.ToAbbreviation()).Trim());
        if (m_Side == Side.White) spriteRenderer.flipX = true;
    }

    public void Acquire(Skill skill)
    {
        _skills.Add(skill);
        var skillIcon = SweatShop.Instance.GetSkillIcon(skill.SkillType, m_Buffs.transform);
        m_SkillIcon.Add(skill, skillIcon);
    }

    public void ApplySkill()
    {
    }

    public void Move()
    {
    }

    public void PlaceAt(int x, int y)
    {
        transform.position = Board.Instance.ToWorld(new Vector3Int(x, y, 0));
    }


    public void Enable()
    {
        spriteRenderer.enabled = true;
        foreach (Transform child in transform) child.gameObject.SetActive(true);
    }

    public void Disable()
    {
        spriteRenderer.enabled = false;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
    }

    public IEnumerator Deadage()
    {
        // destroy all skills
        foreach (var skillIcon in m_SkillIcon) Destroy(skillIcon.Value);
        _skills.Clear();
        // Duration of the animation in seconds
        var duration = 0.3f;
        var elapsedTime = 0f;

        // Initial and target rotations around the Z-axis for 2D
        var startRotation = transform.rotation.eulerAngles.z;
        var endRotation = startRotation + 90f;

        // Animate rotation over time
        while (elapsedTime < duration)
        {
            var t = elapsedTime / duration;
            var zRotation = Mathf.LerpAngle(startRotation, endRotation, t);
            transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final rotation is set
        transform.rotation = Quaternion.Euler(0f, 0f, endRotation);

        // Make the piece disappear
        Disable();
    }

    public List<Vector2Int> GetMovableCells()
    {
        var movableCells = new List<Vector2Int>();
        var currentPosition = Board.Instance.ToBoard2D(transform.position);

        switch (Type)
        {
            case PieceType.King:
            case PieceType.Pawn:
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

                // case PieceType.Pawn:
                //     // Pawn moves differently based on Side and Role
                //     if (m_Side == Side.White)
                //     {
                //         AddMoveIfValid(currentPosition + Vector2Int.right, movableCells); // Forward move
                //         // if (m_Role == Role.Attacker)
                //         // {
                //         //     AddMoveIfEnemy(currentPosition + new Vector2Int(1, 1), movableCells); // Attack diagonally right
                //         //     AddMoveIfEnemy(currentPosition + new Vector2Int(-1, 1), movableCells); // Attack diagonally left
                //         // }
                //     }
                //     else if (m_Side == Side.Black)
                //     {
                //         AddMoveIfValid(currentPosition + Vector2Int.left, movableCells); // Forward move
                //         // if (m_Role == Role.Attacker)
                //         // {
                //         //     AddMoveIfEnemy(currentPosition + new Vector2Int(1, -1),
                //         //         movableCells); // Attack diagonally right();
                //         //     AddMoveIfEnemy(currentPosition + new Vector2Int(-1, -1),
                //         //         movableCells); // Attack diagonally left
                //         // }
                //     }

                break;
        }


        return movableCells;
    }

    public IEnumerator Hurt(int damage)
    {
        if (_skills.Any(s => s.SkillType == SkillType.Evasion))
        {
            ShowSkillText("Evasion");
            var skill = _skills.First(s => s.SkillType == SkillType.Evasion);
            _skills.Remove(skill);
            Destroy(m_SkillIcon[skill]);
            m_SkillIcon.Remove(skill);
        }
        else
        {
            HP -= damage;
            if (HP <= 0) HP = 0;
            StartCoroutine(HitAnimation());
            StartCoroutine(m_HealthBar.HealthBarAnimation(HP));
            if (HP <= 0) yield return StartCoroutine(Deadage());
        }
    }

    // coroutine for hurt animation
    public IEnumerator HitAnimation()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    // coroutine for healthbar animation

// Helper method to add single moves if within bounds
    private void AddMoveIfValid(Vector2Int position, List<Vector2Int> moves)
    {
        if (Board.Instance.IsWithinBounds(position) && Board.Instance.CanMoveToCell(position, this))
            moves.Add(position);
    }

    private void AddMoveIfEnemy(Vector2Int position, List<Vector2Int> moves)
    {
        if (Board.Instance.HaveEnemyPieceAt(position, Side)) moves.Add(position);
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

    public bool IsAlive()
    {
        return HP > 0;
    }
}