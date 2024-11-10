using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, INGameEvent
{
    public enum InRoundState
    {
        Selecting,
        Placing,
        Waiting
    }

    private static readonly Vector3 whiteAnchor = new(-2.96f, -0.49f, 0);
    private static readonly Vector3 blackAnchor = new(2.96f, 2.88f, 0);

    public Role m_Role;
    public Side m_Side;
    public bool InTurn;
    public bool ActionDone;
    public GameObject HandUI;
    public GameObject PieceSelector;
    public InRoundState CurrentState;
    public TextMeshProUGUI _text;
    public Animator m_Animator;

    public string m_PlayerName;
    public HealthBar m_HealthBar;
    private readonly Dictionary<Action, string> m_ActionAnimationMap = new(); // <Animation Name, Animation Clip>
    private int _atk;
    private InputAction _cancel;
    private GameObject _cursor;
    private InputAction _finish;
    private List<Piece> _hand;
    private InputAction _move;
    private Piece _selectedPiece;
    private InputAction _submit;
    private List<GameObject> _UIHand;

    private InGame m_GameManager;
    private int m_HandIndex;
    private SpriteRenderer m_SpriteRenderer;
    private bool ShowHP;
    public bool Done { get; set; }

    public int HP { get; set; }
    private int MaxHp { get; set; }

    private void Awake()
    {
        _hand = new List<Piece>();
        _UIHand = new List<GameObject>();
        CurrentState = InRoundState.Waiting;
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        ShowHP = false;
        Done = true;
    }

    public void Reset()
    {
        HP = MaxHp;
        CurrentState = InRoundState.Waiting;
        _hand.Clear();
        foreach (var go in _UIHand)
            Destroy(go);
        _UIHand.Clear();
        _selectedPiece = null;
        m_HealthBar.Reset();
        PieceSelector.SetActive(false);
    }

    private void Start()
    {
        _submit = InputSystem.actions.FindAction("Submit");
        _cancel = InputSystem.actions.FindAction("Cancel");
        _move = InputSystem.actions.FindAction("Navigate");
        _finish = InputSystem.actions.FindAction("Finish");
    }


    public void GameStart()
    {
    }

    public void GameEnd()
    {
    }

    public void MatchStart()
    {
        StartCoroutine(Enter()); // Start the Enter coroutine()
        ActionDone = false;
        var king = SweatShop.Instance.DrawPiece(m_Side, PieceType.King);
        king.HP = HP;
        king.ATK = _atk;
        Assert.IsNotNull(king);
        king.m_HealthBar.Disable();
        Board.Instance.DropPiece(king, m_Side == Side.White ? new Vector2Int(0, 3) : new Vector2Int(7, 4));
        ShowHP = true;
    }

    public void MatchEnd()
    {
        ShowHP = false;
        StartCoroutine(Leave()); // Start the Leave coroutine()
        Reset();
    }


    public void RoundStart()
    {
        ActionDone = false;
        CurrentState = InRoundState.Waiting;
        PieceSelector.SetActive(false);
    }

    public void RoundEnd()
    {
    }

    public IEnumerator Leave()
    {
        Done = false;
        var targetPosition = transform.position;
        var speed = 2f; // Speed of movement

        switch (m_Side)
        {
            case Side.White:
                targetPosition = whiteAnchor;
                // Set target position to the left
                targetPosition.x -= 4f; // Move left by 10 units

                // Flip the sprite to face left
                var scaleLeft = transform.localScale;
                scaleLeft.x = -Mathf.Abs(scaleLeft.x);
                transform.localScale = scaleLeft;
                break;

            case Side.Black:
                targetPosition = blackAnchor;
                // Set target position to the right
                targetPosition.x += 4f; // Move right by 10 units

                // Ensure the sprite is facing right
                var scaleRight = transform.localScale;
                scaleRight.x = Mathf.Abs(scaleRight.x);
                transform.localScale = scaleRight;
                break;
        }

        // Play the walk animation
        m_Animator.Play(m_ActionAnimationMap[Action.Walk]);

        // Move towards the target position over time
        while (Mathf.Abs(transform.position.x - targetPosition.x) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        // After reaching the target position, stop the walk animation
        m_Animator.Play(m_ActionAnimationMap[Action.Idle]);
        // Optionally, deactivate the GameObject
        transform.position = targetPosition;
        Done = true;
    }

    public IEnumerator Enter()
    {
        Done = false;
        Vector3 startPosition = new();
        Vector3 targetPosition = new();
        var speed = 2f; // Speed of movement

        switch (m_Side)
        {
            case Side.White:
                targetPosition = whiteAnchor;
                startPosition = targetPosition;
                // Start off-screen to the left
                startPosition.x -= 4f;
                // Ensure the sprite is facing right
                var scaleRight = transform.localScale;
                scaleRight.x = Mathf.Abs(scaleRight.x);
                transform.localScale = scaleRight;
                break;

            case Side.Black:
                targetPosition = blackAnchor;
                startPosition = targetPosition;
                // Start off-screen to the right
                startPosition.x += 4f;
                // Flip the sprite to face left
                var scaleLeft = transform.localScale;
                scaleLeft.x = -Mathf.Abs(scaleLeft.x);
                transform.localScale = scaleLeft;
                break;
        }

        // Set the initial position off-screen
        transform.position = startPosition;

        // Play the walk animation
        m_Animator.Play(m_ActionAnimationMap[Action.Walk]);

        // Move towards the target position over time
        while (Mathf.Abs(transform.position.x - targetPosition.x) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        // After reaching the target position, play the idle animation
        m_Animator.Play(m_ActionAnimationMap[Action.Idle]);
        Done = true;
    }

    public void TurnStart()
    {
        ShowHand();
        GetHand();
        CurrentState = InRoundState.Selecting;
        m_Animator.Play(m_ActionAnimationMap[Action.Attack]);
    }

    public void TurnEnd()
    {
        Board.Instance.HidePlaceableCells();
        _UIHand.ForEach(Destroy);
        _UIHand.Clear();
        PieceSelector.SetActive(false);
        m_Animator.Play(m_ActionAnimationMap[Action.Idle]);
        CurrentState = InRoundState.Waiting;
    }


    public IEnumerator Hurt(int damage)
    {
        HP -= damage;
        if (damage > 0)
        {
            StartCoroutine(HitAnimation());
            yield return StartCoroutine(m_HealthBar.HealthBarAnimation(HP));
            if (HP <= 0) yield return StartCoroutine(Deadage());
        }
    }

    public IEnumerator Deadage()
    {
        Done = false;
        m_Animator.Play(m_ActionAnimationMap[Action.Dead]);
        // if the animation is not done yet, wait for it
        while (m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            yield return null;
        Done = true;
    }

    public IEnumerator HitAnimation()
    {
        m_SpriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        m_SpriteRenderer.color = Color.white;
    }

    // coroutine for healthbar animation

    public void OnUpdate()
    {
        if (ShowHP)
        {
            var newHP = Board.Instance.GetAllPieces().Single(p => p.m_Side == m_Side && p.Type == PieceType.King).HP;
            var p = Board.Instance.GetAllPieces();
            if (HP > newHP) StartCoroutine(Hurt(HP - newHP));
            else HP = newHP;
        }

        if (!InTurn)
            return;

        if (CurrentState == InRoundState.Selecting)
        {
            _text.SetText(SweatShop.Instance.GetRemainPiecesCount(m_Side) + " pieces left");
            if (_UIHand.Count > 0 && m_HandIndex < _UIHand.Count && Done)
            {
                PieceSelector.SetActive(true);
                PieceSelector.transform.position = _UIHand[m_HandIndex].transform.position;
            }

            // Get wasd
            if (_move.WasPressedThisFrame())
            {
                if (_UIHand.Count == 0) return;
                if (_move.ReadValue<Vector2>().x < 0)
                    m_HandIndex = Mathf.Max(0, m_HandIndex - 1);
                else if (_move.ReadValue<Vector2>().x > 0) m_HandIndex = Mathf.Min(_UIHand.Count - 1, m_HandIndex + 1);
                PieceSelector.transform.position = _UIHand[m_HandIndex].transform.position;
            }

            // update hand selector place
            if (_submit.WasPressedThisFrame())
            {
                var c = Board.Instance.ShowPlaceableCells(m_Side);
                _cursor = Board.Instance._cursor;
                if (c.Count > 0 && _hand.Count > 0)
                {
                    _cursor.SetActive(true);
                    Board.Instance.PlaceAt(_cursor, c[0].x, c[0].y);
                    PieceSelector.SetActive(false);
                    _UIHand[m_HandIndex].SetActive(false);
                    _selectedPiece = _hand[m_HandIndex];
                    CurrentState = InRoundState.Placing;
                }
            }

            if (_finish.WasPressedThisFrame()) ActionDone = true;
        }
        else if (CurrentState == InRoundState.Placing)
        {
            if (_move.WasPressedThisFrame())
            {
                var input = _move.ReadValue<Vector2>();

                if (input != Vector2.zero)
                {
                    var movement = GetMovementDirection(input);
                    var cp = Board.Instance.ToBoard(_cursor.transform.position);

                    var newCursor = new Vector2Int(cp.x, cp.y) + movement;
                    if (Board.Instance.IsWithinBounds(newCursor))
                        Board.Instance.PlaceAt(_cursor, newCursor.x, newCursor.y); // remove cursor();
                }

                // Helper function to determine movement direction based on input
                Vector2Int GetMovementDirection(Vector2 input)
                {
                    if (input.x < 0) return new Vector2Int(-1, 0);
                    if (input.x > 0) return new Vector2Int(1, 0);
                    if (input.y < 0) return new Vector2Int(0, -1);
                    if (input.y > 0) return new Vector2Int(0, 1);
                    return new Vector2Int(0, 0);
                }
            }

            if (_cancel.WasPressedThisFrame())
            {
                _cursor.SetActive(false);
                _UIHand[m_HandIndex].SetActive(true);
                PieceSelector.SetActive(true);
                _selectedPiece = null;
                CurrentState = InRoundState.Selecting;
            }

            if (_submit.WasPressedThisFrame())
            {
                var c = Board.Instance.ToBoard(_cursor.transform.position); // get cursor()
                if (!Board.Instance.CanPlaceAt(c.x, c.y)) return;
                _cursor.SetActive(false);
                _UIHand.RemoveAt(m_HandIndex);
                _hand.RemoveAt(m_HandIndex); // remove from hand and board instance
                m_HandIndex = 0;
                Board.Instance.DropPiece(_selectedPiece, c);
                if (_UIHand.Count > 0 && !PieceSelector.activeSelf)
                {
                    PieceSelector.SetActive(true);
                    PieceSelector.transform.position = _UIHand[0].transform.position;
                }

                CurrentState = InRoundState.Selecting;
            }
        }
    }

    public void Initialize(int hp, int atk, Side side, InGame gameManager, string playerName)
    {
        HP = hp;
        MaxHp = hp;
        _atk = atk;
        m_Side = side;
        m_Role = Role.None;
        m_GameManager = gameManager;
        m_PlayerName = playerName;
        m_HealthBar.SetMaxHealth(HP);
        m_ActionAnimationMap.Add(Action.Attack, "Attack");
        m_ActionAnimationMap.Add(Action.Walk, "Walk");
        m_ActionAnimationMap.Add(Action.Idle, "Idle");
        m_ActionAnimationMap.Add(Action.Dead, "Die");
        m_Animator.Play("Idle");
    }


    private void ShowHand()
    {
        HandUI.SetActive(true);
        if (_hand.Count > 0) _hand.ForEach(p => PutPieceIntoHand(p.Type));
    }


    private void PutPieceIntoHand(PieceType type)
    {
        var piece = Instantiate(SweatShop.Instance.PieceUIPrefab, HandUI.transform);
        piece.GetComponent<SpriteRenderer>().sprite =
            SweatShop.Instance.GetSpriteByName("UI" + type + m_Side.ToAbbreviation());
        _UIHand.Add(piece);
    }

    private void GetHand()
    {
        if (SweatShop.Instance.GetRemainPiecesCount(m_Side) > 0) DrawPieces(3);
    }

    public List<Piece> GetPieces()
    {
        return _hand;
    }

    // a coroutine for draw n pieces
    private void DrawPieces(int n)
    {
        for (var i = 0; i < n; i++)
        {
            var p = SweatShop.Instance.DrawPiece(m_Side);
            PutPieceIntoHand(p.Type);
            _hand.Add(p);
        }
    }

    private enum Action
    {
        Attack,
        Dead,
        Walk,
        Idle
    }
}