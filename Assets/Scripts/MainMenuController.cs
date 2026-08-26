using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("TattooRoom");
    }

    public void OnSettingsButtonClicked()
    {
        Debug.Log("เปิดหน้าตั้งค่า");
    }
}