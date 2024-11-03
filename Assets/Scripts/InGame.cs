using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InGame : IState, INGameEvent
{
    public enum InGameState
    {
        InRound,
        InTurn,
        RoundStart,
        RoundEnd,
        GameStart,
        GameEnd,
        MatchStart,
        MatchEnd,
        TurnStart,
        TurnEnd
    }

    public enum TurnState
    {
        Attacking,
        Defending,
        Idle
    }

    private readonly List<Player> _players;
    private readonly int[] _playerWins;
    private int _currentMatch;

    private int _currentRound;
    public int AttackerIndex;
    public int CurrentPlayerIndex;
    public InGameState CurrentState;
    public TurnState CurrentTurnState;
    public int DefenderIndex;

    public InGame(GameStateMachine machine) : base(machine)
    {
        var defaultHp = 100;
        var defaultAtk = 100;
        _players = new List<Player>();
        var p1 = GameObject.Find("PlayerA").GetComponent<Player>();
        var p2 = GameObject.Find("PlayerB").GetComponent<Player>();
        p1.Initialize(defaultHp, defaultAtk, Side.Black, this);
        p2.Initialize(defaultHp, defaultAtk, Side.White, this);
        _players.Add(p1);
        _players.Add(p2);
        _playerWins = new int[2];
    }

    public void OnGameStart()
    {
        OnUpdateState(InGameState.GameStart);
        Debug.Log("OnGameStart");
    }

    public void OnGameEnd()
    {
        OnUpdateState(InGameState.GameEnd);
        Debug.Log("OnGameEnd");
    }

    public void OnRoundStart()
    {
        OnUpdateState(InGameState.RoundStart);
        Debug.Log("OnRoundStart");
        _currentRound++;
        SweatShop.Instance.CreateRoundText("Round " + _currentRound);

        _players[AttackerIndex].m_Role = Role.Attacker;
        _players[DefenderIndex].m_Role = Role.Defender;
        _players[AttackerIndex].InTurn = true;
        _players[DefenderIndex].InTurn = false;
        _players.ForEach(p => p.OnRoundStart());
        CurrentTurnState = TurnState.Idle;
    }

    public void OnRoundEnd()
    {
        OnUpdateState(InGameState.RoundEnd);
        Debug.Log("OnRoundEnd");
        AttackerIndex = (AttackerIndex + 1) % 2;
        DefenderIndex = (DefenderIndex + 1) % 2;
        _players.ForEach(p => p.OnRoundEnd());
        Board.Instance.DoneWithPieces = false;
    }

    public void OnMatchStart()
    {
        OnUpdateState(InGameState.MatchStart);
        Debug.Log("OnMatchStart");
        _currentMatch++;
        _players.ForEach(p => p.OnMatchStart());
        AttackerIndex = _currentMatch % 2;
        DefenderIndex = (_currentMatch + 1) % 2;
    }

    public void OnMatchEnd()
    {
        OnUpdateState(InGameState.MatchEnd);
        Debug.Log("OnMatchEnd");
        _playerWins[_players[0].HP > _players[1].HP ? 0 : 1] += 1;
        _players.ForEach(p => p.OnMatchEnd());
    }

    public void OnTurnStart()
    {
        OnUpdateState(InGameState.TurnStart);
        if (CurrentTurnState == TurnState.Idle)
        {
            CurrentTurnState = TurnState.Attacking;
            CurrentPlayerIndex = AttackerIndex;
            _players[AttackerIndex].InTurn = true;
            _players[DefenderIndex].InTurn = false;
            _players[AttackerIndex].OnTurnStart();
        }
        else if (CurrentTurnState == TurnState.Attacking)
        {
            CurrentTurnState = TurnState.Defending;
            CurrentPlayerIndex = DefenderIndex;
            _players[AttackerIndex].InTurn = false;
            _players[DefenderIndex].InTurn = true;
            _players[DefenderIndex].OnTurnStart();
        }

        OnUpdateState(InGameState.InTurn);
    }

    public void OnTurnEnd()
    {
        OnUpdateState(InGameState.TurnEnd);
        if (CurrentTurnState == TurnState.Attacking) _players[AttackerIndex].OnTurnEnd();
        if (CurrentTurnState == TurnState.Defending) _players[DefenderIndex].OnTurnEnd();
    }

    public override void OnEnter()
    {
        _currentRound = _currentMatch = 0;
        OnGameStart();
    }

    public override void OnUpdate()
    {
        if (_currentRound == 0) OnMatchStart();
        if (_currentRound == 10) OnMatchEnd();
        if (CurrentState == InGameState.MatchStart ||
            (CurrentState == InGameState.RoundEnd && _currentRound != 10)) OnRoundStart();
        if (_players.All(p => p.ActionDone) && Board.Instance.DoneWithPieces) OnRoundEnd();
        if (CurrentState == InGameState.RoundStart ||
            (CurrentState == InGameState.TurnEnd && _players.Any(p => !p.ActionDone))) OnTurnStart();
        if (CurrentState == InGameState.InTurn && _players[CurrentPlayerIndex].ActionDone) OnTurnEnd();

        if (_players.All(p => p.ActionDone) && !Board.Instance.DealingWithPieces && CurrentState == InGameState.TurnEnd)
        {
            Board.Instance.SetupSide(_players[AttackerIndex].m_Side, Role.Attacker);
            Board.Instance.SetupSide(_players[DefenderIndex].m_Side, Role.Defender);
            Board.Instance.MakeThePiecesAlive();
        }

        if (_playerWins.Any(w => w >= 2)) OnGameEnd();
        _players.ForEach(p => p.OnUpdate());
    }

    public void OnUpdateState(InGameState state)
    {
        CurrentState = state;
        Machine.OnStateChanged?.Invoke(CurrentState.ToString());
    }

    public override void OnExit()
    {
    }
}