using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class MenuPrincipalModalSetup
{
    [MenuItem("Tobii Pro/1. Generar Modal de Cerrar Sesión en Menú", false, 1)]
    public static void CrearModalEnScena()
    {
        var menuPrincipal = Object.FindFirstObjectByType<HomeManager>();
        if (menuPrincipal == null)
        {
            Debug.LogError("No se encontró el script HomeManager en la escena. Asegúrate de estar en la escena del menú.");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No hay un Canvas en la escena.");
            return;
        }

        // Buscar si ya existe para no duplicarlo
        Transform existente = canvas.transform.Find("ModalCerrarSesion");
        if (existente != null)
        {
            Debug.LogWarning("El ModalCerrarSesion ya existe en la escena. Si quieres recrearlo, bórralo primero.");
            Selection.activeGameObject = existente.gameObject;
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // 1. Fondo bloqueador
        GameObject panelModalLogout = new GameObject("ModalCerrarSesion");
        Undo.RegisterCreatedObjectUndo(panelModalLogout, "Crear Modal Cerrar Sesion");
        panelModalLogout.transform.SetParent(canvas.transform, false);
        var imgFondo = panelModalLogout.AddComponent<Image>();
        imgFondo.color = new Color(0, 0, 0, 0.85f);
        var rtFondo = panelModalLogout.GetComponent<RectTransform>();
        rtFondo.anchorMin = Vector2.zero; rtFondo.anchorMax = Vector2.one;
        rtFondo.sizeDelta = Vector2.zero;

        // 2. Ventana
        GameObject ventana = new GameObject("Ventana");
        ventana.transform.SetParent(panelModalLogout.transform, false);
        var imgVentana = ventana.AddComponent<Image>();
        imgVentana.color = new Color(0.15f, 0.15f, 0.2f, 1f); 
        var rtVentana = ventana.GetComponent<RectTransform>();
        rtVentana.sizeDelta = new Vector2(600, 300);

        // 3. Texto
        GameObject txtObj = new GameObject("TextoPregunta");
        txtObj.transform.SetParent(ventana.transform, false);
        var texto = txtObj.AddComponent<TextMeshProUGUI>();
        texto.text = "¿SALIR DE LA SESIÓN?\n<size=22>Tendrás que volver a calibrar la próxima vez.</size>";
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontSize = 28;
        texto.color = Color.white;
        var rtTexto = txtObj.GetComponent<RectTransform>();
        rtTexto.anchorMin = new Vector2(0, 0.5f); rtTexto.anchorMax = new Vector2(1, 1);
        rtTexto.offsetMin = new Vector2(20, 0); rtTexto.offsetMax = new Vector2(-20, -20);

        // 4. Botón Cancelar
        GameObject btnCancObj = new GameObject("BtnCancelar");
        btnCancObj.transform.SetParent(ventana.transform, false);
        var imgCanc = btnCancObj.AddComponent<Image>();
        imgCanc.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        var btnCancelar = btnCancObj.AddComponent<Button>();
        var rtCanc = btnCancObj.GetComponent<RectTransform>();
        rtCanc.sizeDelta = new Vector2(220, 70);
        rtCanc.anchoredPosition = new Vector2(-130, -50);
        
        GameObject txtCancObj = new GameObject("Txt");
        txtCancObj.transform.SetParent(btnCancObj.transform, false);
        var tCanc = txtCancObj.AddComponent<TextMeshProUGUI>();
        tCanc.text = "CANCELAR"; tCanc.alignment = TextAlignmentOptions.Center; tCanc.color = Color.white; tCanc.fontSize = 24;
        var rtTxtCanc = txtCancObj.GetComponent<RectTransform>();
        rtTxtCanc.anchorMin = Vector2.zero; rtTxtCanc.anchorMax = Vector2.one; rtTxtCanc.sizeDelta = Vector2.zero;

        // 5. Botón Confirmar
        GameObject btnConfObj = new GameObject("BtnConfirmar");
        btnConfObj.transform.SetParent(ventana.transform, false);
        var imgConf = btnConfObj.AddComponent<Image>();
        imgConf.color = new Color(0.9f, 0.2f, 0.3f, 1f); 
        var btnConfirmar = btnConfObj.AddComponent<Button>();
        var rtConf = btnConfObj.GetComponent<RectTransform>();
        rtConf.sizeDelta = new Vector2(220, 70);
        rtConf.anchoredPosition = new Vector2(130, -50);
        
        GameObject txtConfObj = new GameObject("Txt");
        txtConfObj.transform.SetParent(btnConfObj.transform, false);
        var tConf = txtConfObj.AddComponent<TextMeshProUGUI>();
        tConf.text = "CERRAR SESIÓN"; tConf.alignment = TextAlignmentOptions.Center; tConf.color = Color.white; tConf.fontSize = 24;
        var rtTxtConf = txtConfObj.GetComponent<RectTransform>();
        rtTxtConf.anchorMin = Vector2.zero; rtTxtConf.anchorMax = Vector2.one; rtTxtConf.sizeDelta = Vector2.zero;

        // Asignar al script HomeManager
        Undo.RecordObject(menuPrincipal, "Asignar modal al HomeManager");
        menuPrincipal.panelModalLogout = panelModalLogout;
        menuPrincipal.modalBtnCancelar = btnCancelar;
        menuPrincipal.modalBtnConfirmar = btnConfirmar;
        
        // Desactivar modal y seleccionar para que el usuario lo vea
        panelModalLogout.SetActive(false);
        Selection.activeGameObject = panelModalLogout;
        
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("<color=green>¡Modal Creado y Asignado con Éxito!</color> Puedes editarlo en la Jerarquía.");
    }
}
