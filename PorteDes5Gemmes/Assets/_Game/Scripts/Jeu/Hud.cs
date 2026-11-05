using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Tout ce qui reste à l'écran : la couronne des 5 creux (barre de progression),
    /// les portraits d'Elea (à gauche) et d'Isao (à droite), le bouton oreille,
    /// la roue des réglages parent (appui long) et le bouton recommencer.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public static readonly string[] GemmesDuNiveau = { "red", "orange", "yellow", "green", "blue" };

        /// <summary>Durée de l'appui long sur la roue pour ouvrir les réglages parent.</summary>
        public const float AppuiLongParent = 3f;

        public event Action ReglagesDemandes;
        public event Action RecommencerDemande;

        readonly Dictionary<string, SpriteRenderer> creux = new Dictionary<string, SpriteRenderer>();
        readonly Dictionary<string, SpriteRenderer> portraits = new Dictionary<string, SpriteRenderer>();
        SpriteRenderer anneau;
        string actif;
        Transform roue;
        Coroutine appuiLong;
        SpriteRenderer recommencer;

        public static Hud Creer(Transform parent)
        {
            var go = new GameObject("Hud");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<Hud>();
            h.Construire();
            return h;
        }

        void Construire()
        {
            // Couronne : 5 creux en haut de l'écran
            for (int i = 0; i < GemmesDuNiveau.Length; i++)
            {
                var pos = PositionCreux(GemmesDuNiveau[i]);
                Fabrique.Sprite("Creux " + GemmesDuNiveau[i], "creux_gemme", transform, pos, Couches.Hud);
                var g = Fabrique.Sprite("Gemme " + GemmesDuNiveau[i], "gemme_" + GemmesDuNiveau[i] + "_petite", transform, pos, Couches.Hud + 1);
                g.enabled = false;
                creux[GemmesDuNiveau[i]] = g;
            }

            // Portraits : Elea à gauche, Isao à droite (comme dans le jeu web)
            anneau = Fabrique.Sprite("Anneau actif", "anneau_actif", transform, new Vector2(-141, -64), Couches.Hud + 1);
            anneau.enabled = false;
            portraits["elea"] = Fabrique.Sprite("Portrait Elea", "portrait_elea", transform, new Vector2(-141, -64), Couches.Hud);
            portraits["isao"] = Fabrique.Sprite("Portrait Isao", "portrait_isao", transform, new Vector2(141, -64), Couches.Hud);
            foreach (var p in portraits.Values)
            {
                var t = Fabrique.ZoneTactile(p.gameObject, new Vector2(32, 32), Vector2.zero, 5);
                var tr = p.transform;
                t.Pose += _ => { Bruitages.Instance?.Tap(); StartCoroutine(Anim.Saut(tr, 4, 0.35f)); };
            }

            // Bouton oreille : répète la consigne
            var oreille = Fabrique.Sprite("Bouton oreille", "bouton_oreille", transform, new Vector2(-141, 70), Couches.Hud);
            var to = Fabrique.ZoneTactile(oreille.gameObject, new Vector2(40, 40), Vector2.zero, 10);
            to.Pose += _ =>
            {
                Bruitages.Instance?.Tap();
                StartCoroutine(Anim.Saut(oreille.transform, 3, 0.3f));
                Voix.Instance?.Repeter();
            };

            // Roue des réglages parent : petite, discrète, appui long de 3 secondes
            var r = Fabrique.Sprite("Réglages parent", "bouton_reglages", transform, new Vector2(150, 79), Couches.Hud);
            r.color = new Color(1, 1, 1, 0.7f);
            roue = r.transform;
            var tr2 = Fabrique.ZoneTactile(r.gameObject, new Vector2(26, 26), Vector2.zero, 10);
            tr2.Pose += _ => { if (appuiLong == null) appuiLong = StartCoroutine(AppuiLong()); };
            tr2.Leve += _ => ArreterAppuiLong();

            // Recommencer : n'apparaît qu'à la fin
            recommencer = Fabrique.Sprite("Bouton recommencer", "bouton_recommencer", transform, new Vector2(0, -20), Couches.Accueil);
            var trc = Fabrique.ZoneTactile(recommencer.gameObject, new Vector2(40, 40), Vector2.zero, 20);
            trc.Pose += _ => { Bruitages.Instance?.Tap(); RecommencerDemande?.Invoke(); };
            recommencer.gameObject.SetActive(false);
        }

        IEnumerator AppuiLong()
        {
            float t = 0f;
            while (t < AppuiLongParent)
            {
                t += Time.unscaledDeltaTime;
                roue.localRotation = Quaternion.Euler(0, 0, -t * 120f);
                yield return null;
            }
            ArreterAppuiLong();
            ReglagesDemandes?.Invoke();
        }

        void ArreterAppuiLong()
        {
            if (appuiLong != null) StopCoroutine(appuiLong);
            appuiLong = null;
            roue.localRotation = Quaternion.identity;
        }

        public Vector2 PositionCreux(string gemme)
        {
            int i = Array.IndexOf(GemmesDuNiveau, gemme);
            if (i < 0) i = 0;
            return new Vector2((i - 2) * 19, 76);
        }

        public void Remplir(string gemme)
        {
            if (creux.TryGetValue(gemme, out var g))
            {
                g.enabled = true;
                StartCoroutine(Anim.Pop(g.transform, 0.4f));
            }
        }

        /// <summary>Le portrait de l'enfant qui joue s'illumine ; l'autre pâlit un peu.</summary>
        public void JoueurActif(string joueur)
        {
            actif = joueur;
            foreach (var kv in portraits)
            {
                bool lui = kv.Key == joueur;
                kv.Value.color = (joueur == null || lui) ? Color.white : new Color(0.75f, 0.75f, 0.8f, 1f);
                kv.Value.transform.localScale = Vector3.one;
            }
            anneau.enabled = joueur != null;
            if (joueur != null)
            {
                anneau.transform.localPosition = portraits[joueur].transform.localPosition;
                StartCoroutine(Anim.Pop(anneau.transform, 0.4f));
                StartCoroutine(Anim.Saut(portraits[joueur].transform, 6, 0.45f));
            }
        }

        public Vector2 PositionPortrait(string joueur) => portraits[joueur].transform.localPosition;

        public void MontrerRecommencer()
        {
            recommencer.gameObject.SetActive(true);
            StartCoroutine(Anim.Pop(recommencer.transform, 0.5f));
        }

        void Update()
        {
            if (actif != null && anneau.enabled)
            {
                float a = 0.7f + Mathf.Sin(Time.time * 4f) * 0.3f;
                anneau.color = new Color(1, 1, 1, a);
            }
        }
    }
}
