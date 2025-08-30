using UnityEngine;

public class GameManager : MonoBehaviour
{
    public SaveData playerData;


    private void OnApplicationQuit()
    {
        SaveManager.SaveGame(playerData);
    }

    private void QuitGame()
    {
        SaveManager.SaveGame(playerData);
        Application.Quit();
    }
}
