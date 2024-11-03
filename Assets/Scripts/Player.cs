using System.Collections;
using System.Collections.Generic;
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

    public Role m_Role;
    public Side m_Side;
    public bool InTurn;
    public bool ActionDone;
    public GameObject HandUI;
    public GameObject PieceSelector;
    public InRoundState CurrentState;
    public TextMeshProUGUI _text;
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

    public int HP { get; set; }

    private void Awake()
    {
        _hand = new List<Piece>();
        _UIHand = new List<GameObject>();
        CurrentState = InRoundState.Waiting;
    }

    private void Start()
    {
        _submit = InputSystem.actions.FindAction("Submit");
        _cancel = InputSystem.actions.FindAction("Cancel");
        _move = InputSystem.actions.FindAction("Navigate");
        _finish = InputSystem.actions.FindAction("Finish");
    }


    public void OnGameStart()
    {
    }

    public void OnGameEnd()
    {
    }

    public void OnMatchStart()
    {
        ActionDone = false;
        var king = SweatShop.Instance.DrawPiece(m_Side, PieceType.King);
        Assert.IsNotNull(king);
        Board.Instance.DropPiece(king, m_Side == Side.White ? new Vector2Int(0, 4) : new Vector2Int(7, 3));
    }

    public void OnMatchEnd()
    {
        SweatShop.Instance.ReCyclePieces(Board.Instance.GetPieces());
    }

    public void OnRoundStart()
    {
        ActionDone = false;
        CurrentState = InRoundState.Waiting;
    }

    public void OnRoundEnd()
    {
    }

    public void OnTurnStart()
    {
        ShowHand();
        GetHand();
        CurrentState = InRoundState.Selecting;
    }

    public void OnTurnEnd()
    {
        Board.Instance.HidePlaceableCells();
        CurrentState = InRoundState.Waiting;
    }
    

    public void OnUpdate()
    {
        if (!InTurn)
            return;

        if (CurrentState == InRoundState.Selecting)
        {
            _text.SetText(SweatShop.Instance.GetRemainPiecesCount(m_Side).ToString());
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

            if (_cancel.WasPressedThisFrame())
            {
                _cursor.SetActive(false);
                _UIHand[m_HandIndex].SetActive(true);
                _selectedPiece = null;
                ActionDone = true;
            }
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

                    var newCursor = new Vector2Int(cp.x, cp.y);
                    for (var i = 1; i < 8; i++)
                    {
                        var next = newCursor + movement * i;
                        if (Board.Instance.CanPlaceAt(next.x, next.y))
                        {
                            Board.Instance.PlaceAt(_cursor, next.x, next.y);
                            break;
                        }
                    }
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
                _cursor.SetActive(false);
                _UIHand.RemoveAt(m_HandIndex);
                _hand.RemoveAt(m_HandIndex); // remove from hand and board instance
                m_HandIndex = 0;
                Board.Instance.DropPiece(_selectedPiece, Board.Instance.ToBoard(_cursor.transform.position));
                if (_UIHand.Count > 0) PieceSelector.SetActive(true);
                CurrentState = InRoundState.Selecting;
            }
        }
    }


    public void Initialize(int hp, int atk, Side side, InGame gameManager)
    {
        HP = hp;
        _atk = atk;
        m_Side = side;
        m_Role = Role.None;
        m_GameManager = gameManager;
    }


    private void ShowHand()
    {
        HandUI.SetActive(true);
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
        StartCoroutine(DrawPieces(3));
    }

    // a coroutine for draw n pieces
    private IEnumerator DrawPieces(int n)
    {
        for (var i = 0; i < n; i++)
        {
            PieceSelector.SetActive(true);
            var p = SweatShop.Instance.DrawPiece(m_Side);
            PutPieceIntoHand(p.Type);
            _hand.Add(p);

            // wait for 0.3 seconds
            yield return new WaitForSeconds(1.0f);
        }
    }
}