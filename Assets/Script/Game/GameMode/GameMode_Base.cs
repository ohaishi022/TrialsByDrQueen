using UnityEngine;

public abstract class GameMode_Base : MonoBehaviour
{
    protected GameSceneManager sceneManager;

    public virtual void Initialize(GameSceneManager sceneManager)
    {
        this.sceneManager = sceneManager;
    }

    public abstract void StartGameMode();
    public abstract void EndGameMode();
}