using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Spawner spawner;

    public void StartGame(string gameMode)
    {
        if(spawner._runner == null)
        {
            if(gameMode == "Host")
            {
                spawner.StartGame(Fusion.GameMode.Host);
            }
            else if(gameMode == "Client")
            {
                spawner.StartGame(Fusion.GameMode.Client);
            }
        }
    }
}
