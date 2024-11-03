using UnityEngine;

public interface  INGameEvent
{
   public abstract void OnGameStart();
   public abstract void OnGameEnd();
   public abstract void OnMatchStart();   
   public abstract void OnMatchEnd();
   public abstract void OnRoundStart();
   public abstract void OnRoundEnd();
}