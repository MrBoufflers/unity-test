using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Le rituel de la gemme : « Touchez-la tous les deux ! »
    /// Deux doigts en même temps déclenchent la grande fête ; un seul doigt marche aussi
    /// (appui de 2 secondes ou 2 petits tapotements), pour ne jamais bloquer.
    /// </summary>
    public class TouchGemme : ITour
    {
        public const float FenetreDeuxDoigts = 0.6f;
        public const float AppuiSeul = 2f;
        public const int TapsSeul = 2;

        readonly Plateau plateau;
        readonly SpriteRenderer gemme;
        readonly Touchable zone;
        readonly Dictionary<int, float> poses = new Dictionary<int, float>();
        readonly List<(int doigt, float t)> recents = new List<(int, float)>();
        int taps;
        string fin;

        /// <summary>« ensemble », « seul » ou « aide ».</summary>
        public string Fin => fin;
        public int Resultat => fin == "ensemble" ? 2 : 1;

        public TouchGemme(Plateau plateau, SpriteRenderer gemme)
        {
            this.plateau = plateau;
            this.gemme = gemme;
            zone = Fabrique.ZoneTactile(gemme.gameObject, new Vector2(48, 48), Vector2.zero, 30);
            zone.Pose += Poser;
            zone.Leve += d => poses.Remove(d);
        }

        public IEnumerator Jouer()
        {
            while (fin == null) yield return null;
            zone.Actif = false;
        }

        void Poser(int doigt)
        {
            if (fin != null) return;
            float t = Time.unscaledTime;
            poses[doigt] = t;
            recents.RemoveAll(r => t - r.t > FenetreDeuxDoigts || r.doigt == doigt);
            recents.Add((doigt, t));
            Bruitages.Instance?.Tap();
            plateau.Progres();
            plateau.StartCoroutine(Anim.Dandine(gemme.transform, 8f, 0.35f));
            if (recents.Count >= 2) { Terminer("ensemble"); return; }
            taps++;
            if (taps >= TapsSeul) { Terminer("seul"); return; }
            plateau.StartCoroutine(VerifierAppui(doigt, t));
        }

        IEnumerator VerifierAppui(int doigt, float t)
        {
            yield return new WaitForSecondsRealtime(AppuiSeul);
            if (poses.TryGetValue(doigt, out var t2) && Mathf.Approximately(t2, t)) Terminer("seul");
        }

        void Terminer(string comment)
        {
            if (fin == null) fin = comment;
        }

        public Vector2? CibleAide() => fin == null ? (Vector2)gemme.transform.localPosition + new Vector2(0, 10) : (Vector2?)null;

        public void Marquer(bool oui)
        {
            var s = gemme.GetComponent<Scintille>();
            if (s) s.enabled = false;
            if (oui && fin == null) Scintille.Sur(gemme.gameObject, Vector2.zero, 16f).enabled = true;
        }

        public void Resoudre() => Terminer("aide");

        public void Nettoyer() => Marquer(false);
    }
}
