#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú: Tools > El Viejo Capi > Crear Menú de Pausa
/// Arma todo el Canvas de pausa con las imágenes, lo conecta con los scripts
/// y lo guarda como prefab en Assets/Prefabs/MenuPausa.prefab
/// </summary>
public static class CrearMenuPausa
{
    const string RutaPrefab = "Assets/Prefabs/MenuPausa.prefab";
    static readonly Color Dorado = new Color32(232, 200, 112, 255);
    static TMP_FontAsset fuente;

    [MenuItem("Tools/El Viejo Capi/Crear Menú de Pausa")]
    static void Crear()
    {
        fuente = BuscarFuente();

        // ---------------------------------------------------- Sprites (los configura como Sprite y 9-slice)
        var sPanel = CargarSprite("Panel_Pausa", new Vector4(90, 90, 90, 90));
        var sTitPausa = CargarSprite("Titulo_Pausa", Vector4.zero);
        var sTitAjustes = CargarSprite("Titulo_Ajustes", Vector4.zero);
        var sSlFondo = CargarSprite("Slider_Fondo", new Vector4(20, 0, 20, 0));
        var sSlRelleno = CargarSprite("Slider_Relleno", new Vector4(20, 0, 20, 0));
        var sSlManija = CargarSprite("Slider_Manija", Vector4.zero);
        var sTgFondo = CargarSprite("Toggle_Fondo", Vector4.zero);
        var sTgMarca = CargarSprite("Toggle_Marca", Vector4.zero);
        var sDdFondo = CargarSprite("Dropdown_Fondo", new Vector4(16, 16, 16, 16));
        var sDdFlecha = CargarSprite("Dropdown_Flecha", Vector4.zero);

        // ---------------------------------------------------- Canvas raíz
        var root = new GameObject("MenuPausa", typeof(RectTransform), typeof(Canvas),
                                  typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // encima de todo el HUD
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Fondo oscuro que tapa el juego
        var contenido = Nuevo("Contenido", root.transform, typeof(Image));
        Estirar(contenido);
        contenido.GetComponent<Image>().color = new Color(0, 0, 0, 0.72f);

        // ---------------------------------------------------- Panel principal
        var pPrin = Imagen("PanelPrincipal", contenido.transform, sPanel, new Vector2(0, 0), new Vector2(760, 900), true);
        Imagen("Titulo", pPrin.transform, sTitPausa, new Vector2(0, 320), new Vector2(620, 161), false);
        var bReanudar = Boton("Btn_Reanudar", pPrin.transform, "Reanudar", new Vector2(0, 150));
        var bAjustes = Boton("Btn_Ajustes", pPrin.transform, "Ajustes", new Vector2(0, 0));
        var bMenu = Boton("Btn_MenuPrincipal", pPrin.transform, "MenuPrincipal", new Vector2(0, -150));
        var bSalir = Boton("Btn_SalirWindows", pPrin.transform, "SalirWindows", new Vector2(0, -300));

        // ---------------------------------------------------- Panel ajustes
        var pAj = Imagen("PanelAjustes", contenido.transform, sPanel, new Vector2(0, 0), new Vector2(1000, 960), true);
        Imagen("Titulo", pAj.transform, sTitAjustes, new Vector2(0, 360), new Vector2(620, 161), false);

        float y = 220, paso = 82;
        var fGeneral = Fila("Fila_VolumenGeneral", pAj.transform, "Volumen general", y);
        var slGeneral = Deslizador(fGeneral.transform, sSlFondo, sSlRelleno, sSlManija);
        var fMusica = Fila("Fila_Musica", pAj.transform, "Música", y -= paso);
        var slMusica = Deslizador(fMusica.transform, sSlFondo, sSlRelleno, sSlManija);
        var fEfectos = Fila("Fila_Efectos", pAj.transform, "Efectos", y -= paso);
        var slEfectos = Deslizador(fEfectos.transform, sSlFondo, sSlRelleno, sSlManija);
        var fCalidad = Fila("Fila_Calidad", pAj.transform, "Calidad", y -= paso);
        var ddCalidad = Desplegable(fCalidad.transform, sDdFondo, sDdFlecha, sTgMarca);
        var fRes = Fila("Fila_Resolucion", pAj.transform, "Resolución", y -= paso);
        var ddRes = Desplegable(fRes.transform, sDdFondo, sDdFlecha, sTgMarca);
        var fComp = Fila("Fila_PantallaCompleta", pAj.transform, "Pantalla completa", y -= paso);
        var tgComp = Casilla(fComp.transform, sTgFondo, sTgMarca);
        var fVs = Fila("Fila_VSync", pAj.transform, "VSync", y -= paso);
        var tgVs = Casilla(fVs.transform, sTgFondo, sTgMarca);
        var bVolver = Boton("Btn_Volver", pAj.transform, "Volver", new Vector2(0, -380));

        // ---------------------------------------------------- Scripts
        var menu = root.AddComponent<MenuPausa>();
        menu.contenido = contenido;
        menu.panelPrincipal = pPrin;
        menu.panelAjustes = pAj;
        menu.botonReanudar = bReanudar;
        menu.botonAjustes = bAjustes;
        menu.botonMenuPrincipal = bMenu;
        menu.botonSalirWindows = bSalir;
        menu.botonVolver = bVolver;

        var aj = root.AddComponent<AjustesJuego>();
        aj.sliderGeneral = slGeneral;
        aj.sliderMusica = slMusica;
        aj.sliderEfectos = slEfectos;
        aj.filaMusica = fMusica;
        aj.filaEfectos = fEfectos;
        aj.dropdownCalidad = ddCalidad;
        aj.dropdownResolucion = ddRes;
        aj.togglePantallaCompleta = tgComp;
        aj.toggleVSync = tgVs;

        pAj.SetActive(false);
        contenido.SetActive(false); // actívalo a mano si quieres editarlo en la escena

        CapaUI(root.transform);

        // ---------------------------------------------------- Prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, RutaPrefab, InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(root, "Crear Menú de Pausa");
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[MenuPausa] Listo. Prefab guardado en {RutaPrefab}. Arrástralo a cada escena jugable.");
    }

    // ==================================================================== Helpers

    static Sprite CargarSprite(string nombre, Vector4 borde)
    {
        foreach (var guid in AssetDatabase.FindAssets(nombre + " t:Texture2D"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) != nombre) continue;

            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            bool cambio = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; cambio = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; cambio = true; }
            if (imp.spriteBorder != borde) { imp.spriteBorder = borde; cambio = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; cambio = true; }
            if (cambio) imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
        Debug.LogWarning($"[MenuPausa] No encontré la imagen '{nombre}'. ¿La importaste al proyecto?");
        return null;
    }

    static TMP_FontAsset BuscarFuente()
    {
        foreach (var guid in AssetDatabase.FindAssets("Cinzel t:TMP_FontAsset"))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        return TMP_Settings.defaultFontAsset;
    }

    static GameObject Nuevo(string nombre, Transform padre, params System.Type[] comps)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        foreach (var c in comps) go.AddComponent(c);
        go.transform.SetParent(padre, false);
        return go;
    }

    static RectTransform Pos(GameObject go, Vector2 pos, Vector2 tam)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    static void Estirar(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject Imagen(string nombre, Transform padre, Sprite s, Vector2 pos, Vector2 tam, bool sliced)
    {
        var go = Nuevo(nombre, padre, typeof(Image));
        Pos(go, pos, tam);
        var img = go.GetComponent<Image>();
        img.sprite = s;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.preserveAspect = !sliced;
        img.raycastTarget = sliced; // el panel bloquea clics; el título no
        return go;
    }

    static Button Boton(string nombre, Transform padre, string clave, Vector2 pos)
    {
        var normal = CargarSprite($"Boton_{clave}_normal", Vector4.zero);
        var hover = CargarSprite($"Boton_{clave}_hover", Vector4.zero);
        var pressed = CargarSprite($"Boton_{clave}_pressed", Vector4.zero);

        var go = Nuevo(nombre, padre, typeof(Image), typeof(Button));
        Pos(go, pos, new Vector2(560, 152)); // 700x190 al 80 %
        var img = go.GetComponent<Image>();
        img.sprite = normal;
        img.preserveAspect = true;

        var b = go.GetComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.SpriteSwap;
        var ss = b.spriteState;
        ss.highlightedSprite = hover;
        ss.pressedSprite = pressed;
        ss.selectedSprite = normal;
        ss.disabledSprite = normal;
        b.spriteState = ss;
        return b;
    }

    static TextMeshProUGUI Texto(Transform padre, string texto, float tam, TextAlignmentOptions al)
    {
        var go = Nuevo("Texto", padre, typeof(TextMeshProUGUI));
        var t = go.GetComponent<TextMeshProUGUI>();
        if (fuente) t.font = fuente;
        t.text = texto;
        t.fontSize = tam;
        t.color = Dorado;
        t.alignment = al;
        t.raycastTarget = false;
        return t;
    }

    static GameObject Fila(string nombre, Transform padre, string etiqueta, float y)
    {
        var fila = Nuevo(nombre, padre);
        Pos(fila, new Vector2(0, y), new Vector2(820, 70));
        var t = Texto(fila.transform, etiqueta, 34, TextAlignmentOptions.MidlineLeft);
        t.gameObject.name = "Etiqueta";
        Pos(t.gameObject, new Vector2(-210, 0), new Vector2(400, 70));
        return fila;
    }

    static Slider Deslizador(Transform fila, Sprite fondo, Sprite relleno, Sprite manija)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = "Slider";
        go.transform.SetParent(fila, false);
        Pos(go, new Vector2(230, 0), new Vector2(360, 44));

        var bg = go.transform.Find("Background");
        if (bg)
        {
            Estirar(bg.gameObject);
            var i = bg.GetComponent<Image>(); i.sprite = fondo; i.type = Image.Type.Sliced; i.color = Color.white;
        }
        var area = go.transform.Find("Fill Area");
        if (area) Estirar(area.gameObject);
        var fill = go.transform.Find("Fill Area/Fill");
        if (fill)
        {
            ((RectTransform)fill).sizeDelta = Vector2.zero;
            var i = fill.GetComponent<Image>(); i.sprite = relleno; i.type = Image.Type.Sliced; i.color = Color.white;
        }
        var hArea = go.transform.Find("Handle Slide Area");
        if (hArea)
        {
            Estirar(hArea.gameObject);
            var rt = (RectTransform)hArea; rt.offsetMin = new Vector2(14, 0); rt.offsetMax = new Vector2(-14, 0);
        }
        var h = go.transform.Find("Handle Slide Area/Handle");
        if (h)
        {
            ((RectTransform)h).sizeDelta = new Vector2(58, 14);
            var i = h.GetComponent<Image>(); i.sprite = manija; i.preserveAspect = true; i.color = Color.white;
        }
        var s = go.GetComponent<Slider>();
        s.minValue = 0; s.maxValue = 1; s.value = 1;
        var cb = s.colors; cb.highlightedColor = new Color(1f, 0.92f, 0.75f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); s.colors = cb;
        return s;
    }

    static Toggle Casilla(Transform fila, Sprite fondo, Sprite marca)
    {
        var go = DefaultControls.CreateToggle(new DefaultControls.Resources());
        go.name = "Toggle";
        go.transform.SetParent(fila, false);
        Pos(go, new Vector2(80, 0), new Vector2(56, 56));

        var label = go.transform.Find("Label");
        if (label) Object.DestroyImmediate(label.gameObject);

        var bg = go.transform.Find("Background");
        if (bg)
        {
            Estirar(bg.gameObject);
            var i = bg.GetComponent<Image>(); i.sprite = fondo; i.type = Image.Type.Simple; i.color = Color.white;
        }
        var chk = go.transform.Find("Background/Checkmark");
        if (chk)
        {
            Estirar(chk.gameObject);
            var i = chk.GetComponent<Image>(); i.sprite = marca; i.color = Color.white;
        }
        var t = go.GetComponent<Toggle>(); t.isOn = true;
        return t;
    }

    static TMP_Dropdown Desplegable(Transform fila, Sprite fondo, Sprite flecha, Sprite marca)
    {
        var go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        go.name = "Dropdown";
        go.transform.SetParent(fila, false);
        Pos(go, new Vector2(230, 0), new Vector2(360, 60));

        var img = go.GetComponent<Image>(); img.sprite = fondo; img.type = Image.Type.Sliced; img.color = Color.white;

        var label = go.transform.Find("Label");
        if (label)
        {
            var t = label.GetComponent<TextMeshProUGUI>();
            if (fuente) t.font = fuente;
            t.fontSize = 28; t.color = Dorado;
            var rt = (RectTransform)label; rt.offsetMin = new Vector2(24, 4); rt.offsetMax = new Vector2(-56, -4);
        }
        var arrow = go.transform.Find("Arrow");
        if (arrow)
        {
            var i = arrow.GetComponent<Image>(); i.sprite = flecha; i.color = Color.white; i.preserveAspect = true;
            var rt = (RectTransform)arrow;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(34, 34); rt.anchoredPosition = new Vector2(-30, 0);
        }
        var template = go.transform.Find("Template");
        if (template)
        {
            var rt = (RectTransform)template; rt.sizeDelta = new Vector2(rt.sizeDelta.x, 300);
            var i = template.GetComponent<Image>(); i.sprite = fondo; i.type = Image.Type.Sliced; i.color = Color.white;
        }
        var item = go.transform.Find("Template/Viewport/Content/Item");
        if (item)
        {
            var rtItem = (RectTransform)item; rtItem.sizeDelta = new Vector2(rtItem.sizeDelta.x, 44);
            var content = (RectTransform)item.parent; content.sizeDelta = new Vector2(content.sizeDelta.x, 48);
            var tg = item.GetComponent<Toggle>();
            if (tg)
            {
                var cb = tg.colors;
                cb.normalColor = new Color(1, 1, 1, 0);
                cb.highlightedColor = new Color(1, 1, 1, 1);
                cb.selectedColor = new Color(1, 1, 1, 0.6f);
                cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1);
                tg.colors = cb;
            }
        }
        var itemBg = go.transform.Find("Template/Viewport/Content/Item/Item Background");
        if (itemBg) itemBg.GetComponent<Image>().color = new Color32(140, 26, 16, 255);
        var itemChk = go.transform.Find("Template/Viewport/Content/Item/Item Checkmark");
        if (itemChk)
        {
            var i = itemChk.GetComponent<Image>(); i.sprite = marca; i.color = Color.white; i.preserveAspect = true;
        }
        var itemLabel = go.transform.Find("Template/Viewport/Content/Item/Item Label");
        if (itemLabel)
        {
            var t = itemLabel.GetComponent<TextMeshProUGUI>();
            if (fuente) t.font = fuente;
            t.fontSize = 24; t.color = Dorado;
        }
        return go.GetComponent<TMP_Dropdown>();
    }

    static void CapaUI(Transform t)
    {
        int capa = LayerMask.NameToLayer("UI");
        if (capa < 0) return;
        t.gameObject.layer = capa;
        foreach (Transform h in t) CapaUI(h);
    }
}
#endif
