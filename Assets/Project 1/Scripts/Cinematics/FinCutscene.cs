using UnityEngine;
using UnityEngine.Playables;

public class FinCutscene : MonoBehaviour
{
    [SerializeField] PlayableDirector director;
    [SerializeField] MonoBehaviour controladorJugador;
    [SerializeField] AudioSource musicaFondo;

    void OnEnable()  => director.stopped += AlTerminar;
    void OnDisable() => director.stopped -= AlTerminar;

    void AlTerminar(PlayableDirector d)
    {
        if (d != director) return;

        controladorJugador.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        musicaFondo.Play();
    }
}