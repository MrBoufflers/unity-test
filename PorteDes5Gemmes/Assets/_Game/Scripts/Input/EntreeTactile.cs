using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Lit les doigts (plusieurs à la fois, pour le rituel de la gemme) et la souris dans l'éditeur,
    /// puis prévient l'objet Touchable touché.
    /// </summary>
    public class EntreeTactile : MonoBehaviour
    {
        public static EntreeTactile Instance { get; private set; }

        /// <summary>Faux pendant le panneau parent ou l'écran de fin.</summary>
        public bool Actif = true;

        Camera cam;
        readonly Dictionary<int, Touchable> enCours = new Dictionary<int, Touchable>();

        void Awake()
        {
            Instance = this;
            cam = Camera.main;
        }

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            if (cam == null) cam = Camera.main;
            bool doigts = false;
            foreach (var t in ETouch.activeTouches)
            {
                doigts = true;
                int id = t.touchId;
                if (t.phase == TouchPhase.Began) Poser(id, t.screenPosition);
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) Lever(id);
            }

            if (!doigts && Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) Poser(-1, Mouse.current.position.ReadValue());
                if (Mouse.current.leftButton.wasReleasedThisFrame) Lever(-1);
            }
        }

        void Poser(int doigt, Vector2 ecran)
        {
            if (!Actif || cam == null) return;
            Vector2 monde = cam.ScreenToWorldPoint(ecran);
            var cible = Chercher(monde);
            if (cible == null) return;
            enCours[doigt] = cible;
            cible.Poser(doigt);
        }

        void Lever(int doigt)
        {
            if (enCours.TryGetValue(doigt, out var cible))
            {
                enCours.Remove(doigt);
                if (cible) cible.Lever(doigt);
            }
        }

        /// <summary>Touche directement un point du monde (utilisé par le mode de test automatique).</summary>
        public Touchable Simuler(Vector2 monde, int doigt = 100)
        {
            var cible = Chercher(monde);
            if (cible != null) { cible.Poser(doigt); cible.Lever(doigt); }
            return cible;
        }

        Touchable Chercher(Vector2 monde)
        {
            var touches = Physics2D.OverlapPointAll(monde);
            Touchable meilleur = null;
            int meilleurOrdre = int.MinValue;
            foreach (var col in touches)
            {
                var t = col.GetComponent<Touchable>();
                if (t == null || !t.Actif || !t.isActiveAndEnabled) continue;
                var sr = t.GetComponentInChildren<SpriteRenderer>();
                int ordre = t.Priorite * 10000 + (sr ? sr.sortingOrder : 0);
                if (ordre > meilleurOrdre)
                {
                    meilleurOrdre = ordre;
                    meilleur = t;
                }
            }
            return meilleur;
        }
    }
}
