using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        TurnEnd,
        AutoMove,
        PLAYER_A,
        PLAYER_B
    }

    public enum TurnState
    {
        Attacking,
        Defending,
        Idle
    }

    private readonly bool _AnimationDone;

    private readonly List<Player> _players;
    private readonly int[] _playerWins;
    private int _currentMatch;

    private int _currentRound;
    public int AttackerIndex;
    public int CurrentPlayerIndex;
    public InGameState CurrentState;
    public TurnState CurrentTurnState;
    public int DefenderIndex;
    public GameStatText StatText;

    public InGame(GameStateMachine machine) : base(machine)
    {
        var defaultHp = 25;
        var defaultAtk = 10;
        _players = new List<Player>();
        var p1 = GameObject.Find("PlayerA").GetComponent<Player>();
        var p2 = GameObject.Find("PlayerB").GetComponent<Player>();
        p1.Initialize(defaultHp, defaultAtk, Side.Black, this, "PLAYER_A");
        p2.Initialize(defaultHp, defaultAtk, Side.White, this, "PLAYER_B");
        _players.Add(p1);
        _players.Add(p2);
        _playerWins = new int[2];
        StatText = GameObject.Find("StateBanner").GetComponent<GameStatText>();
        _AnimationDone = true;
    }

    public void GameStart()
    {
        OnUpdateState(InGameState.GameStart);
        Debug.Log("OnGameStart");
    }

    public void GameEnd()
    {
        OnUpdateState(InGameState.GameEnd);
        Debug.Log("OnGameEnd");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadSceneAsync("Finished");
    }

    public void RoundStart()
    {
        OnUpdateState(InGameState.RoundStart);
        Debug.Log("OnRoundStart");
        _currentRound++;
        SweatShop.Instance.CreateRoundText("Round " + _currentRound);

        _players[AttackerIndex].m_Role = Role.Attacker;
        _players[DefenderIndex].m_Role = Role.Defender;
        _players[AttackerIndex].InTurn = true;
        _players[DefenderIndex].InTurn = false;
        _players.ForEach(p => p.RoundStart());
        CurrentTurnState = TurnState.Idle;
    }

    public void RoundEnd()
    {
        OnUpdateState(InGameState.RoundEnd);
        Debug.Log("OnRoundEnd");
        AttackerIndex = (AttackerIndex + 1) % 2;
        DefenderIndex = (DefenderIndex + 1) % 2;
        _players.ForEach(p => p.RoundEnd());
        Board.Instance.DoneWithPieces = false;
    }

    public void MatchStart()
    {
        OnUpdateState(InGameState.MatchStart);
        Debug.Log("OnMatchStart");
        _currentMatch++;
        AttackerIndex = _currentMatch % 2;
        DefenderIndex = (_currentMatch + 1) % 2;
        _players[AttackerIndex].m_Side = Side.White;
        _players[DefenderIndex].m_Side = Side.Black;
        _players.ForEach(p => p.MatchStart());
    }

    public void MatchEnd()
    {
        OnUpdateState(InGameState.MatchEnd);
        Debug.Log("OnMatchEnd");
        if (_players[AttackerIndex].HP <= 0 || _players[DefenderIndex].HP <= 0)
        {
            var winner = _players.Single(p => p.HP > 0).m_PlayerName;
            _playerWins[_players.Single(p => p.m_PlayerName == "PLAYER_A").HP <= 0 ? 0 : 1] += 1;
            SweatShop.Instance.CreateRoundText("The Winner is " + winner + "!");
        }
        else
        {
            SweatShop.Instance.CreateRoundText("Draw!");
        }

        var pieces = new List<Piece>();
        pieces.AddRange(Board.Instance.GetAllPieces());
        _players.ForEach(p => pieces.AddRange(p.GetPieces()));
        SweatShop.Instance.ReCyclePieces(pieces);

        Board.Instance.Reset();
        _players.ForEach(p => p.MatchEnd());
        _currentRound = 0;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Machine.ChangeState(new Finished(Machine));
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private InGameState ToState(string state)
    {
        return (InGameState)Enum.Parse(typeof(InGameState), state);
    }

    public void TurnStart()
    {
        OnUpdateState(InGameState.TurnStart);
        if (CurrentTurnState == TurnState.Idle)
        {
            CurrentTurnState = TurnState.Attacking;
            CurrentPlayerIndex = AttackerIndex;
            OnUpdateState(ToState(_players[AttackerIndex].m_PlayerName));
            _players[AttackerIndex].InTurn = true;
            _players[DefenderIndex].InTurn = false;
            _players[AttackerIndex].TurnStart();
        }
        else if (CurrentTurnState == TurnState.Attacking)
        {
            CurrentTurnState = TurnState.Defending;
            CurrentPlayerIndex = DefenderIndex;
            OnUpdateState(ToState(_players[DefenderIndex].m_PlayerName));
            _players[AttackerIndex].InTurn = false;
            _players[DefenderIndex].InTurn = true;
            _players[DefenderIndex].TurnStart();
        }

        if (_players[CurrentPlayerIndex].m_PlayerName == "PLAYER_A") OnUpdateState(InGameState.PLAYER_A);
        else OnUpdateState(InGameState.PLAYER_B);

        // OnUpdateState(InGameState.InTurn);
    }

    public void TurnEnd()
    {
        OnUpdateState(InGameState.TurnEnd);
        if (CurrentTurnState == TurnState.Attacking) _players[AttackerIndex].TurnEnd();
        if (CurrentTurnState == TurnState.Defending) _players[DefenderIndex].TurnEnd();
    }

    private void AutoMove()
    {
        Board.Instance.SetupSide(_players[AttackerIndex].m_Side, Role.Attacker);
        Board.Instance.SetupSide(_players[DefenderIndex].m_Side, Role.Defender);
        Board.Instance.MakeThePiecesAlive();
        OnUpdateState(InGameState.AutoMove);
    }

    public override void OnEnter()
    {
        _currentRound = _currentMatch = 0;
        GameStart();
    }

    public override void OnUpdate()
    {
        if (!_AnimationDone || _players.Any(p => !p.Done)) return;

        switch (CurrentState)
        {
            case InGameState.GameStart:
                MatchStart();
                break;
            case InGameState.MatchStart:
                RoundStart();
                break;
            case InGameState.MatchEnd:
                if (_playerWins.Any(w => w >= 2)) GameEnd();
                else MatchStart();
                break;
            case InGameState.RoundStart:
                TurnStart();
                break;
            case InGameState.PLAYER_A:
                if (_players.Single(p => p.m_PlayerName == "PLAYER_A").ActionDone) TurnEnd();
                break;
            case InGameState.PLAYER_B:
                if (_players.Single(p => p.m_PlayerName == "PLAYER_B").ActionDone) TurnEnd();
                break;
            case InGameState.RoundEnd:
                if (_players.Any(p => p.HP <= 0) || _currentRound == 10) MatchEnd();
                else if (_currentRound < 10) RoundStart();
                break;
            case InGameState.TurnEnd:
                if (_players.Any(p => !p.ActionDone)) TurnStart();
                else
                    AutoMove();
                break;
            case InGameState.AutoMove:
                if (Board.Instance.DoneWithPieces) RoundEnd();
                break;
            default:
                return;
        }

        _players.ForEach(p => p.OnUpdate());
        StatText.SetInfo(_currentRound, _playerWins[0], _playerWins[1], _currentMatch,
            _players[CurrentPlayerIndex].m_PlayerName, _players[CurrentPlayerIndex].m_Role);
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