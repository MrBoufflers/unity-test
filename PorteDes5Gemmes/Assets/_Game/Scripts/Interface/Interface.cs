using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Interface à l'écran (uGUI + TextMeshPro) : écran d'accueil avec le gros bouton « Jouer »,
    /// sous-titres pour le parent, et panneau des réglages parent (ouvert par un appui long de 3 s).
    /// Les enfants n'ont rien à lire : tout ce qui est écrit ici s'adresse au parent.
    /// </summary>
    public class Interface : MonoBehaviour
    {
        public static Interface Instance { get; private set; }

        public event Action Recommencer;

        static readonly Color Creme = new Color32(0xff, 0xf8, 0xe1, 0xff);
        static readonly Color Brun = new Color32(0x3e, 0x27, 0x23, 0xff);
        static readonly Color Bois = new Color32(0xb0, 0x7a, 0x48, 0xff);
        static readonly Color BoisFonce = new Color32(0x7a, 0x4f, 0x2c, 0xff);
        static readonly Color Nuit = new Color32(0x0b, 0x10, 0x26, 0xff);
        static readonly Color Rose = new Color32(0xec, 0x40, 0x7a, 0xff);
        static readonly Color Bleu = new Color32(0x1e, 0x88, 0xe5, 0xff);

        RectTransform racine;
        GameObject accueil;
        readonly List<Image> etoiles = new List<Image>();
        GameObject bandeau;
        TextMeshProUGUI sousTitre;
        GameObject reglages;
        TextMeshProUGUI texteVolume, texteVitesse, texteSousTitres, texteVoix;
        Image boutonElea, boutonIsao;
        string phraseTest;
        bool jouer;

        void Awake()
        {
            Instance = this;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;
            gameObject.AddComponent<GraphicRaycaster>();
            racine = (RectTransform)transform;

            ConstruireSousTitres();
            ConstruireReglages();
        }

        void Start()
        {
            var voix = Voix.Instance;
            if (voix != null)
            {
                voix.DebutPhrase += texte =>
                {
                    sousTitre.text = texte;
                    bandeau.SetActive(Reglages.SousTitres);
                };
                voix.FinPhrase += () => bandeau.SetActive(false);
            }
        }

        // ─── Écran d'accueil ────────────────────────────────────────────────

        public IEnumerator Accueil()
        {
            ConstruireAccueil();
            jouer = false;
            while (!jouer) yield return null;
            Bruitages.Instance?.Tap();
            var groupe = accueil.GetComponent<CanvasGroup>();
            yield return Anim.Duree(0.5f, t => groupe.alpha = 1f - t);
            Destroy(accueil);
            etoiles.Clear();
        }

        void ConstruireAccueil()
        {
            accueil = Panneau("Accueil", racine, Nuit);
            accueil.AddComponent<CanvasGroup>();
            var rnd = new System.Random(11);
            for (int i = 0; i < 40; i++)
            {
                var e = Panneau("Étoile", accueil.transform, new Color(1f, 1f, 0.85f, 0.8f));
                var rt = (RectTransform)e.transform;
                rt.anchorMin = rt.anchorMax = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
                float s = rnd.Next(2) == 0 ? 6 : 10;
                rt.sizeDelta = new Vector2(s, s);
                etoiles.Add(e.GetComponent<Image>());
            }

            var titre = Texte("Titre", accueil.transform, "La Porte des 5 gemmes", 56, Creme);
            var rtt = titre.rectTransform;
            rtt.anchorMin = new Vector2(0, 0.72f);
            rtt.anchorMax = new Vector2(1, 0.92f);
            rtt.offsetMin = rtt.offsetMax = Vector2.zero;

            var sousTitreAccueil = Texte("Héros", accueil.transform, "Isao & Elea", 34, new Color(1f, 0.96f, 0.6f));
            var rts = sousTitreAccueil.rectTransform;
            rts.anchorMin = new Vector2(0, 0.62f);
            rts.anchorMax = new Vector2(1, 0.72f);
            rts.offsetMin = rts.offsetMax = Vector2.zero;

            var go = new GameObject("Jouer", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(accueil.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = Fabrique.Image("bouton_jouer");
            img.preserveAspect = true;
            var rtb = (RectTransform)go.transform;
            rtb.anchorMin = rtb.anchorMax = new Vector2(0.5f, 0.36f);
            rtb.sizeDelta = new Vector2(220, 220);
            go.GetComponent<Button>().onClick.AddListener(() => jouer = true);
            StartCoroutine(Respire(rtb));
        }

        IEnumerator Respire(RectTransform rt)
        {
            while (rt != null)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.05f;
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        void Update()
        {
            for (int i = 0; i < etoiles.Count; i++)
            {
                if (etoiles[i] == null) continue;
                var c = etoiles[i].color;
                c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * (0.6f + i % 5 * 0.2f) + i));
                etoiles[i].color = c;
            }
        }

        // ─── Sous-titres ────────────────────────────────────────────────────

        void ConstruireSousTitres()
        {
            bandeau = Panneau("Sous-titres", racine, new Color(0.04f, 0.06f, 0.15f, 0.72f));
            var rt = (RectTransform)bandeau.transform;
            rt.anchorMin = new Vector2(0.2f, 0.02f);
            rt.anchorMax = new Vector2(0.8f, 0.12f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            bandeau.GetComponent<Image>().raycastTarget = false;
            sousTitre = Texte("Texte", bandeau.transform, "", 26, Creme);
            sousTitre.raycastTarget = false;
            Remplir(sousTitre.rectTransform, 12);
            bandeau.SetActive(false);
        }

        // ─── Réglages parent ────────────────────────────────────────────────

        public void OuvrirReglages(string phraseDeTest)
        {
            phraseTest = phraseDeTest;
            MettreAJour();
            reglages.SetActive(true);
            reglages.transform.SetAsLastSibling();
            Time.timeScale = 0f;
            if (EntreeTactile.Instance) EntreeTactile.Instance.Actif = false;
        }

        void FermerReglages()
        {
            reglages.SetActive(false);
            Time.timeScale = 1f;
            if (EntreeTactile.Instance) EntreeTactile.Instance.Actif = true;
        }

        void ConstruireReglages()
        {
            reglages = Panneau("Réglages parent", racine, new Color(0.04f, 0.06f, 0.15f, 0.75f));
            var carte = Panneau("Carte", reglages.transform, Creme);
            var rc = (RectTransform)carte.transform;
            rc.anchorMin = new Vector2(0.18f, 0.06f);
            rc.anchorMax = new Vector2(0.82f, 0.94f);
            rc.offsetMin = rc.offsetMax = Vector2.zero;
            var colonne = carte.AddComponent<VerticalLayoutGroup>();
            colonne.padding = new RectOffset(36, 36, 24, 24);
            colonne.spacing = 10;
            colonne.childControlHeight = true;
            colonne.childControlWidth = true;
            colonne.childForceExpandHeight = false;

            Ligne(carte.transform, Texte("Titre", null, "Réglages parent", 40, Brun), 56);

            Ligne(carte.transform, Texte("Ordre", null, "Qui commence ?", 28, Brun), 36);
            var rangOrdre = Rang(carte.transform, 64);
            boutonElea = Bouton(rangOrdre, "Elea d'abord", () => { Reglages.PremierJoueur = "elea"; MettreAJour(); }).GetComponent<Image>();
            boutonIsao = Bouton(rangOrdre, "Isao d'abord", () => { Reglages.PremierJoueur = "isao"; MettreAJour(); }).GetComponent<Image>();
            Ligne(carte.transform, Texte("Note", null, "L'ordre s'applique au prochain « Recommencer ».", 20, BoisFonce), 26);

            var rangVolume = Rang(carte.transform, 60);
            texteVolume = Texte("Volume", rangVolume, "", 26, Brun);
            texteVolume.alignment = TextAlignmentOptions.MidlineLeft;
            Bouton(rangVolume, "-", () => { Reglages.VolumeVoix -= 0.1f; MettreAJour(); }, 90);
            Bouton(rangVolume, "+", () => { Reglages.VolumeVoix += 0.1f; MettreAJour(); }, 90);

            var rangVitesse = Rang(carte.transform, 60);
            texteVitesse = Texte("Vitesse", rangVitesse, "", 26, Brun);
            texteVitesse.alignment = TextAlignmentOptions.MidlineLeft;
            Bouton(rangVitesse, "-", () => { Reglages.VitesseVoix -= 0.05f; MettreAJour(); }, 90);
            Bouton(rangVitesse, "+", () => { Reglages.VitesseVoix += 0.05f; MettreAJour(); }, 90);

            var rangSt = Rang(carte.transform, 60);
            texteSousTitres = Texte("Sous-titres", rangSt, "", 26, Brun);
            texteSousTitres.alignment = TextAlignmentOptions.MidlineLeft;
            Bouton(rangSt, "Changer", () => { Reglages.SousTitres = !Reglages.SousTitres; MettreAJour(); }, 190);

            texteVoix = Texte("Voix", null, "", 20, BoisFonce);
            Ligne(carte.transform, texteVoix, 26);

            var rangActions = Rang(carte.transform, 68);
            Bouton(rangActions, "Tester la voix", () => Voix.Instance?.DireSansAttendre(phraseTest));
            Bouton(rangActions, "Recommencer", () => { FermerReglages(); Recommencer?.Invoke(); });
            Bouton(rangActions, "Fermer", FermerReglages);

            reglages.SetActive(false);
        }

        void MettreAJour()
        {
            texteVolume.text = $"Volume de la voix : {Mathf.RoundToInt(Reglages.VolumeVoix * 100)} %";
            texteVitesse.text = $"Vitesse de la voix : {Reglages.VitesseVoix:0.00}";
            texteSousTitres.text = "Sous-titres : " + (Reglages.SousTitres ? "affichés" : "cachés");
            bool synthese = Voix.Instance != null && Voix.Instance.SyntheseDisponible;
            texteVoix.text = synthese
                ? "Voix : synthèse vocale française du téléphone."
                : "Pas de voix française sur cet appareil : activez les sous-titres.";
            bool elea = Reglages.PremierJoueur == "elea";
            boutonElea.color = elea ? Rose : Bois;
            boutonIsao.color = elea ? Bois : Bleu;
        }

        // ─── Petites briques d'interface ────────────────────────────────────

        static GameObject Panneau(string nom, Transform parent, Color couleur)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = couleur;
            Remplir((RectTransform)go.transform, 0);
            return go;
        }

        static void Remplir(RectTransform rt, float marge)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(marge, marge);
            rt.offsetMax = new Vector2(-marge, -marge);
        }

        static TextMeshProUGUI Texte(string nom, Transform parent, string texte, float taille, Color couleur)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            if (parent) go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = texte;
            t.fontSize = taille;
            t.color = couleur;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            if (parent && parent.GetComponent<HorizontalLayoutGroup>())
            {
                var le = go.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
            }
            return t;
        }

        static void Ligne(Transform parent, TextMeshProUGUI t, float hauteur)
        {
            t.transform.SetParent(parent, false);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = hauteur;
        }

        static Transform Rang(Transform parent, float hauteur)
        {
            var go = new GameObject("Rang", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = hauteur;
            return go.transform;
        }

        static GameObject Bouton(Transform parent, string libelle, Action action, float largeur = -1)
        {
            var go = new GameObject("Bouton " + libelle, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Bois;
            var le = go.AddComponent<LayoutElement>();
            if (largeur > 0) le.preferredWidth = largeur;
            else le.flexibleWidth = 1;
            go.GetComponent<Button>().onClick.AddListener(() => { Bruitages.Instance?.Tap(); action(); });
            var t = Texte("Libellé", go.transform, libelle, 26, Creme);
            Remplir(t.rectTransform, 4);
            return go;
        }
    }
}
