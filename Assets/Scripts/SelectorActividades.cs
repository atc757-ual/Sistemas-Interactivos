using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SelectorActividades : MonoBehaviour
{
    [Header("Botones de Juegos")]
    public Button botonSeguimiento;
    public Button botonBurbujas;
    public Button botonDestellos;
    public Button botonLaberinto;
    public Button botonIntruso;
    public Button botonLluvia;
    public Button botonVolver;

    void Start()
    {
        if (botonSeguimiento != null) botonSeguimiento.onClick.AddListener(() => SceneManager.LoadScene("ActividadSeguimiento"));
        if (botonBurbujas != null) botonBurbujas.onClick.AddListener(() => SceneManager.LoadScene("ActividadBurbujas"));
        if (botonDestellos != null) botonDestellos.onClick.AddListener(() => SceneManager.LoadScene("ActividadDestellos"));
        if (botonLaberinto != null) botonLaberinto.onClick.AddListener(() => SceneManager.LoadScene("ActividadLaberinto"));
        if (botonIntruso != null) botonIntruso.onClick.AddListener(() => SceneManager.LoadScene("ActividadBuscaIntruso"));
        if (botonLluvia != null) botonLluvia.onClick.AddListener(() => SceneManager.LoadScene("ActividadLluvia"));

        if (botonVolver != null) botonVolver.onClick.AddListener(() => SceneManager.LoadScene("MenuPrincipal"));
    }
}