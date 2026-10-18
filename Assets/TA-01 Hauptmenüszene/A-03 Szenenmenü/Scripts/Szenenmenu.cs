using UnityEngine;
using UnityEngine.SceneManagement;

// Pausenmenü im Spiel, wird mit der X-Taste am Controller geöffnet
public class Szenenmenu : MonoBehaviour
{
    [SerializeField] private GameObject szenenmenue;
    [SerializeField] private string hauptmenueSceneName = "MainMenu";

    private bool menuOffen;

    private void Start()
    {
        MenueAnzeigen(false);
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.X))
            MenueAnzeigen(!menuOffen);
    }

    // Menü offen = Spiel pausiert
    private void MenueAnzeigen(bool offen)
    {
        menuOffen = offen;
        szenenmenue.SetActive(offen);

        Time.timeScale = offen ? 0f : 1f;
        AudioListener.pause = offen;
    }

    public void Fortsetzen()
    {
        MenueAnzeigen(false);
    }

    public void Neustart()
    {
        MenueAnzeigen(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Hauptmenue()
    {
        MenueAnzeigen(false);
        SceneManager.LoadScene(hauptmenueSceneName);
    }

    public void Beenden()
    {
        MenueAnzeigen(false);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
