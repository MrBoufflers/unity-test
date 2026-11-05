using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Mode de test : lancé avec l'argument « -jeu-auto », le tableau se joue tout seul
    /// (y compris des erreurs et une attente pour voir l'aide de Lumi) et prend des captures d'écran.
    /// Sert à vérifier le jeu sans téléphone. Sans cet argument, ce composant ne fait rien.
    /// </summary>
    public class JeuAutomatique : MonoBehaviour
    {
        string dossier;
        Plateau plateau;
        int numero;

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-jeu-auto"))
            {
                enabled = false;
                return;
            }
            int i = Array.IndexOf(args, "-captures");
            dossier = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "captures");
            Directory.CreateDirectory(dossier);
            plateau = FindAnyObjectByType<Plateau>();
            if (plateau) plateau.Automatique = true;
            PlayerPrefs.DeleteAll();
            StartCoroutine(Jouer());
        }

        IEnumerator Capture(string nom)
        {
            yield return new WaitForEndOfFrame();
            numero++;
            string chemin = Path.Combine(dossier, $"{numero:00}_{nom}.png");
            ScreenCapture.CaptureScreenshot(chemin);
            Debug.Log("[jeu-auto] capture " + chemin + " (état : " + plateau.Etat + ")");
            yield return null;
        }

        IEnumerator Attendre(Func<bool> condition, float max = 60f)
        {
            float t = 0f;
            while (!condition() && t < max) { t += Time.deltaTime; yield return null; }
            if (t >= max) Debug.LogError("[jeu-auto] délai dépassé, état : " + plateau.Etat);
        }

        static Touchable Trouver(string nom, Func<Touchable, bool> filtre = null)
        {
            return FindObjectsByType<Touchable>()
                .Where(t => t.name == nom && t.Actif && (filtre == null || filtre(t)))
                .OrderBy(t => t.transform.position.x)
                .FirstOrDefault();
        }

        static void Toucher(Touchable t, int doigt = 50)
        {
            if (t == null) { Debug.LogError("[jeu-auto] objet introuvable"); return; }
            Debug.Log("[jeu-auto] touche " + t.name);
            t.Poser(doigt);
            t.Lever(doigt);
        }

        IEnumerator Jouer()
        {
            yield return Attendre(() => plateau.Etat == "intro");
            yield return new WaitForSeconds(1.5f);
            yield return Capture("intro");

            yield return Attendre(() => plateau.Etat.EndsWith("_tour"));
            yield return new WaitForSeconds(0.5f);
            yield return Capture("tour1_consigne");

            // Une erreur : un champignon bleu fait « pouic »
            Toucher(Trouver("Champignon blue"));
            yield return new WaitForSeconds(0.2f);
            yield return Capture("tour1_pouic");
            yield return Attendre(() => !Voix.Instance.EnTrainDeParler);

            Toucher(Trouver("Champignon red"));
            yield return new WaitForSeconds(1.4f);
            Toucher(Trouver("Champignon red", t => t.transform.parent == plateau.transform));
            yield return new WaitForSeconds(1.4f);
            yield return Capture("tour1_deux_trouves");

            // On ne touche plus rien : l'aide de Lumi doit arriver (redit, volette, se pose)
            yield return new WaitForSeconds(17f);
            yield return Capture("tour1_aide_lumi_volette");
            yield return new WaitForSeconds(10f);
            yield return Capture("tour1_aide_lumi_posee");
            Toucher(Trouver("Champignon red", t => t.transform.parent == plateau.transform));

            yield return Attendre(() => plateau.Etat.EndsWith("_tour") && plateau.Etat != "elea_tour");
            yield return new WaitForSeconds(1.5f);
            yield return Capture("tour2_panneaux");

            // Mauvaise réponse : on compte ensemble
            Toucher(Trouver("Panneau 2"));
            yield return new WaitForSeconds(2.5f);
            yield return Capture("tour2_compter_ensemble");
            yield return new WaitForSeconds(4f);
            Toucher(Trouver("Panneau 3"));
            yield return new WaitForSeconds(0.6f);
            yield return Capture("tour2_bonne_reponse");

            yield return Attendre(() => plateau.Etat == "gemme");
            yield return new WaitForSeconds(2.5f);
            yield return Capture("gemme");

            // Deux doigts en même temps sur la gemme
            var gemme = Trouver("Gemme");
            gemme.Poser(1);
            gemme.Poser(2);
            yield return new WaitForSeconds(0.35f);
            yield return Capture("fete");
            yield return new WaitForSeconds(1.6f);
            yield return Capture("gemme_dans_la_couronne");

            yield return Attendre(() => plateau.Etat == "termine");
            yield return new WaitForSeconds(1f);
            yield return Capture("fin_lumi_dort");

            Interface.Instance.OuvrirReglages("Bonjour, je suis Lumi !");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("reglages_parent");

            Debug.Log("[jeu-auto] terminé");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }
    }
}
