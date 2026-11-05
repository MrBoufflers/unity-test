using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Un tableau du jeu (ici la forêt, gemme rouge). Construit le décor depuis le JSON,
    /// puis déroule : accueil → intro de Lumi → tâche d'un enfant → tâche de l'autre →
    /// rituel de la gemme → fête → la gemme rejoint la couronne → Lumi s'endort.
    /// </summary>
    public class Plateau : MonoBehaviour
    {
        /// <summary>Après « Recommencer », on saute l'écran d'accueil.</summary>
        static bool sauterAccueil;

        public string NomTableau = "foret";

        public TableauData Donnees { get; private set; }
        public Panier Panier { get; private set; }
        public Hud Hud { get; private set; }
        public Lumi Lumi { get; private set; }
        public string Etat { get; private set; } = "chargement";

        /// <summary>Mode de test : le jeu se joue tout seul (voir JeuAutomatique).</summary>
        public bool Automatique;

        Interface ui;
        ITour tourAvecAide;
        float chronoAide;
        int niveauAide;
        bool aidesActives;
        bool commence;
        SpriteRenderer nuit;
        readonly List<(ObjetCache def, bool bon, SpriteRenderer sr)> objetsTour = new List<(ObjetCache, bool, SpriteRenderer)>();
        readonly Dictionary<string, SpriteRenderer> objetsAnimes = new Dictionary<string, SpriteRenderer>();

        void Start()
        {
            Donnees = TableauData.Charger(NomTableau);
            if (Donnees == null) return;
            Construire();
            ui = Interface.Instance;
            if (ui != null)
            {
                ui.Recommencer += Recommencer;
            }
            Hud.ReglagesDemandes += () => ui?.OuvrirReglages(Donnees.lines.voice_test);
            Hud.RecommencerDemande += Recommencer;
            StartCoroutine(Deroulement());
        }

        // ─── Construction du décor ────────────────────────────────────────────

        void Construire()
        {
            Fabrique.Sprite("Fond", Donnees.background, transform, Vector2.zero, Couches.Fond);

            foreach (var d in Donnees.decor)
            {
                Vector2 pos = Coords.DepuisScene(d.x, d.y);
                if (d.type == "branch")
                {
                    // La branche part de son point d'attache vers la droite (30 pixels)
                    Fabrique.Sprite("Branche", "decor_branch", transform, pos + new Vector2(16, 3), 289);
                    continue;
                }
                string image = $"decor_{d.type}_{Mathf.RoundToInt(d.scale * 100)}";
                if (Fabrique.Image(image) == null) image = $"decor_{d.type}_100";
                var sr = Fabrique.Sprite(d.type, image, transform, pos, Coords.OrdreSelonHauteur(pos.y));
                // Le décor réagit quand on le touche, sans jamais gêner le jeu (priorité basse)
                if (d.type != "grass")
                {
                    var zone = Fabrique.ZoneTactile(sr.gameObject, sr.sprite.bounds.size * 0.8f, Fabrique.CentreImage(sr), 0);
                    var tr = sr.transform;
                    zone.Pose += _ => StartCoroutine(Anim.Dandine(tr, 3f, 0.5f));
                }
            }

            ConstruireAnimes();

            // Les champignons (cible + distracteurs). Avec shuffle_positions, les couleurs changent de place à chaque partie.
            foreach (var tour in Donnees.turns)
            {
                if (tour.type != "find_objects") continue;
                var places = new List<ObjetCache>();
                places.AddRange(tour.targets);
                places.AddRange(tour.distractors);
                var couleurs = new List<(ObjetCache def, bool bon)>();
                foreach (var t in tour.targets) couleurs.Add((t, true));
                foreach (var t in tour.distractors) couleurs.Add((t, false));
                if (tour.shuffle_positions && !Automatique) Melanger(couleurs);
                for (int i = 0; i < places.Count; i++)
                {
                    var (def, bon) = couleurs[i];
                    Vector2 pos = Coords.DepuisScene(places[i].x, places[i].y);
                    var sr = Fabrique.Sprite("Champignon " + def.color, "champignon_" + def.color, transform, pos, Coords.OrdreSelonHauteur(pos.y));
                    var zone = Fabrique.ZoneTactile(sr.gameObject, Fabrique.TailleAvecMarge(sr), Fabrique.CentreImage(sr), 20);
                    var tr = sr.transform;
                    zone.Pose += _ => { Bruitages.Instance?.Pouic(); StartCoroutine(Anim.Dandine(tr, 10f, 0.5f)); };
                    objetsTour.Add((def, bon, sr));
                }
            }

            Panier = new Panier(this, transform, Coords.DepuisScene(Donnees.basket) + new Vector2(0, 2));
            Hud = Hud.Creer(transform);
            Lumi = Lumi.Creer(transform, new Vector2(190, 110), Coords.DepuisScene(Donnees.lumi_home));
            gameObject.AddComponent<Lucioles>();

            nuit = Fabrique.Sprite("Nuit", "fx_pixel", transform, Vector2.zero, Couches.Nuit);
            nuit.transform.localScale = new Vector3(120, 80, 1);
            nuit.color = new Color(0.04f, 0.06f, 0.15f, 0f);
        }

        static void Melanger<T>(List<T> liste)
        {
            for (int i = liste.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (liste[i], liste[j]) = (liste[j], liste[i]);
            }
        }

        void ConstruireAnimes()
        {
            if (Donnees.ambient_hotspots == null) return;
            foreach (var h in Donnees.ambient_hotspots)
            {
                Vector2 pos = Coords.DepuisScene(h.x, h.y);
                SpriteRenderer sr;
                switch (h.reaction)
                {
                    case "fly_away_and_back":
                        sr = Fabrique.Sprite("Oiseau", "anim_oiseau_0", transform, pos, 290);
                        break;
                    case "shiver":
                        sr = Fabrique.Sprite("Buisson", "decor_bush_80", transform, pos, Coords.OrdreSelonHauteur(pos.y - 8));
                        break;
                    case "blink":
                        sr = Fabrique.Sprite("Hibou", "anim_hibou_0", transform, pos, 290);
                        break;
                    case "bloom":
                        sr = Fabrique.Sprite("Fleur", "anim_fleur_0", transform, pos, Coords.OrdreSelonHauteur(pos.y - 8));
                        break;
                    default:
                        continue;
                }
                // Images ancrées en bas : le point du JSON désigne le centre de l'objet
                if (sr.sprite != null && sr.sprite.bounds.center.y > 0.5f)
                    sr.transform.localPosition -= new Vector3(0, Mathf.Round(sr.sprite.bounds.center.y), 0);
                objetsAnimes[h.id] = sr;
                var zone = Fabrique.ZoneTactile(sr.gameObject, Fabrique.TailleAvecMarge(sr, 0.6f), Fabrique.CentreImage(sr), 1);
                string reaction = h.reaction;
                zone.Pose += _ => StartCoroutine(Reagir(reaction, sr, zone));
            }
        }

        IEnumerator Reagir(string reaction, SpriteRenderer sr, Touchable zone)
        {
            if (!zone.Actif) yield break;
            zone.Actif = false;
            switch (reaction)
            {
                case "fly_away_and_back":
                {
                    Bruitages.Instance?.Pouic();
                    Vector3 depart = sr.transform.localPosition;
                    var battre = StartCoroutine(Battre(sr));
                    yield return Anim.Arc(sr.transform, depart + new Vector3(110, 40, 0), 18f, 1.1f);
                    yield return new WaitForSeconds(1.2f);
                    sr.flipX = true;
                    yield return Anim.Arc(sr.transform, depart, 18f, 1.3f);
                    sr.flipX = false;
                    StopCoroutine(battre);
                    sr.sprite = Fabrique.Image("anim_oiseau_0");
                    break;
                }
                case "shiver":
                    Bruitages.Instance?.Tap();
                    yield return Anim.Tremble(sr.transform, 2f, 0.6f);
                    break;
                case "blink":
                    Bruitages.Instance?.Tap();
                    for (int i = 0; i < 2; i++)
                    {
                        sr.sprite = Fabrique.Image("anim_hibou_1");
                        yield return new WaitForSeconds(0.18f);
                        sr.sprite = Fabrique.Image("anim_hibou_0");
                        yield return new WaitForSeconds(0.18f);
                    }
                    break;
                case "bloom":
                    Bruitages.Instance?.Succes();
                    sr.sprite = Fabrique.Image("anim_fleur_1");
                    yield return Anim.Saut(sr.transform, 3, 0.4f);
                    yield return new WaitForSeconds(2.5f);
                    sr.sprite = Fabrique.Image("anim_fleur_0");
                    break;
            }
            zone.Actif = true;
        }

        IEnumerator Battre(SpriteRenderer sr)
        {
            var a = Fabrique.Image("anim_oiseau_1");
            var b = Fabrique.Image("anim_oiseau_2");
            while (true)
            {
                sr.sprite = (Mathf.FloorToInt(Time.time * 12f) % 2 == 0) ? a : b;
                yield return null;
            }
        }

        // ─── Déroulement du tableau ──────────────────────────────────────────

        IEnumerator Deroulement()
        {
            var voix = Voix.Instance;
            Hud.JoueurActif(null);

            if (!sauterAccueil && ui != null && !Automatique)
            {
                Etat = "accueil";
                yield return ui.Accueil();
            }
            sauterAccueil = false;
            commence = true;

            Etat = "intro";
            yield return voix.Dire(Donnees.lines.intro);

            // Les tâches des deux enfants. Le parent peut choisir qui commence.
            string premier = Reglages.PremierJoueur;
            int compteMemoire = -1;
            foreach (var tour in Donnees.turns)
            {
                string joueur = premier == "elea" ? tour.player : Reglages.Autre(tour.player);
                Etat = joueur + "_tour";
                Hud.JoueurActif(joueur);

                ITour t;
                if (tour.type == "find_objects")
                {
                    t = new TourTrouver(this, tour, joueur, objetsTour);
                }
                else if (tour.type == "choose_number")
                {
                    // Les champignons restants s'effacent : l'écran reste peu chargé pour compter
                    foreach (var o in objetsTour) if (!o.bon && o.sr) StartCoroutine(Anim.Fondu(o.sr, 0f, 0.6f));
                    int reponse = tour.answer_from == "previous_turn_count" && compteMemoire >= 0 ? compteMemoire : Panier.Nombre;
                    t = new TourCompter(this, tour, joueur, reponse);
                }
                else
                {
                    Debug.LogWarning("Type de tâche inconnu : " + tour.type);
                    continue;
                }

                // La consigne est dite pendant que la tâche commence (les objets réagissent tout de suite) ;
                // l'aide de Lumi ne démarre qu'une fois la phrase finie.
                string consigne = tour.prompt?.Pour(joueur);
                StartCoroutine(voix.Dire(consigne, true));
                yield return AvecAides(t);
                t.Nettoyer();
                if (t.Resultat >= 0) compteMemoire = t.Resultat;
                yield return new WaitForSeconds(0.5f);
            }

            // La gemme apparaît
            Hud.JoueurActif(null);
            Etat = "gemme";
            Vector2 posGemme = Coords.DepuisScene(Donnees.gem_position);
            var halo = Fabrique.Sprite("Halo gemme", "fx_halo", transform, posGemme, Couches.Gemme - 1);
            var gemme = Fabrique.Sprite("Gemme", "gemme_" + Donnees.gem, transform, posGemme, Couches.Gemme);
            Bruitages.Instance?.Succes();
            StartCoroutine(Anim.Pop(halo.transform, 0.6f));
            yield return Anim.Pop(gemme.transform, 0.6f);
            StartCoroutine(Flotter(gemme.transform, halo));
            var rituel = new TouchGemme(this, gemme);
            StartCoroutine(voix.Dire(Donnees.lines.gem_touch, true));
            yield return AvecAides(rituel);
            rituel.Nettoyer();

            // La fête !
            Etat = "fete";
            Bruitages.Instance?.Fete();
            Explosion(posGemme, rituel.Fin == "ensemble" ? 26 : 18);
            StartCoroutine(ExplosionApres(Hud.PositionPortrait("elea") + new Vector2(0, 10), 12, 0.3f));
            StartCoroutine(ExplosionApres(Hud.PositionPortrait("isao") + new Vector2(0, 10), 12, 0.45f));
            var phrase = StartCoroutine(voix.Dire(Donnees.lines.celebration));
            yield return new WaitForSeconds(0.6f);
            Destroy(halo.gameObject);
            Bruitages.Instance?.Envol();
            Vector3 creux = Hud.PositionCreux(Donnees.gem);
            StartCoroutine(Anim.Echelle(gemme.transform, new Vector3(0.55f, 0.55f, 1f), 0.9f));
            yield return Anim.Arc(gemme.transform, creux, 30f, 0.9f);
            Destroy(gemme.gameObject);
            Hud.Remplir(Donnees.gem);
            Bruitages.Instance?.Succes();
            while (voix.EnTrainDeParler) yield return null;
            yield return new WaitForSeconds(0.8f);

            // Fin de session : la nuit tombe et Lumi s'endort sur sa branche
            Etat = "fin";
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(Anim.Fondu(nuit, 0.75f, 1.5f));
            Bruitages.Instance?.Dodo();
            StartCoroutine(Lumi.Dormir(Coords.DepuisScene(Donnees.lumi_sleep_spot) + new Vector2(0, 5)));
            yield return voix.Dire(Donnees.lines.goodnight);
            Hud.MontrerRecommencer();
            Etat = "termine";
        }

        IEnumerator Flotter(Transform tr, SpriteRenderer halo)
        {
            Vector3 p0 = tr.localPosition;
            while (tr != null)
            {
                tr.localPosition = p0 + new Vector3(0, Mathf.Round(Mathf.Sin(Time.time * 2.5f) * 2f), 0);
                if (halo) halo.color = new Color(1, 1, 1, 0.7f + Mathf.Sin(Time.time * 4f) * 0.3f);
                if (Etat == "fete") yield break;
                yield return null;
            }
        }

        // ─── Aide progressive (Lumi) ─────────────────────────────────────────
        // 8 s : Lumi redit la consigne · 15 s : elle volette près de la cible ·
        // 25 s : elle se pose dessus et la cible scintille · 35 s : « Je vais t'aider ! »

        IEnumerator AvecAides(ITour t)
        {
            tourAvecAide = t;
            chronoAide = 0f;
            niveauAide = 0;
            aidesActives = true;
            yield return t.Jouer();
            aidesActives = false;
            tourAvecAide = null;
            t.Marquer(false);
            Lumi.Maison();
        }

        void Update()
        {
            if (!aidesActives || tourAvecAide == null) return;
            // Pas d'aide pendant que Lumi parle (on ne coupe jamais une phrase)
            if (Voix.Instance != null && Voix.Instance.EnTrainDeParler) return;
            chronoAide += Time.deltaTime;
            var delais = Donnees.hint_delays_s;
            if (niveauAide < delais.Length && chronoAide >= delais[niveauAide])
            {
                Aide(niveauAide);
                niveauAide++;
            }
            else if (niveauAide >= delais.Length && chronoAide >= Donnees.auto_solve_after_s)
            {
                niveauAide++;
                aidesActives = false;
                StartCoroutine(AideFinale());
            }
        }

        void Aide(int niveau)
        {
            var cible = tourAvecAide.CibleAide();
            switch (niveau)
            {
                case 0:
                    Voix.Instance.Repeter();
                    break;
                case 1:
                    if (cible.HasValue) Lumi.VoletterPres(cible.Value);
                    break;
                default:
                    if (cible.HasValue) Lumi.SePoser(cible.Value);
                    tourAvecAide.Marquer(true);
                    break;
            }
        }

        IEnumerator AideFinale()
        {
            var t = tourAvecAide;
            yield return Voix.Instance.Dire(Donnees.lines.auto_help);
            t?.Resoudre();
            // Si la tâche demande plusieurs gestes, l'aide reprend depuis le début
            if (tourAvecAide == t && t != null)
            {
                chronoAide = 0f;
                niveauAide = 0;
                aidesActives = true;
            }
        }

        /// <summary>L'enfant a progressé : on remet l'aide à zéro et Lumi rentre.</summary>
        public void Progres()
        {
            chronoAide = 0f;
            niveauAide = 0;
            tourAvecAide?.Marquer(false);
            Lumi.Maison();
        }

        public void ArreterAides() => aidesActives = false;

        // ─── Effets ──────────────────────────────────────────────────────────

        void Explosion(Vector2 centre, int nombre)
        {
            Color[] couleurs =
            {
                new Color(1f, 0.92f, 0.4f), new Color(1f, 0.5f, 0.5f), new Color(0.6f, 0.85f, 1f),
                new Color(1f, 1f, 1f), new Color(0.75f, 1f, 0.6f),
            };
            for (int i = 0; i < nombre; i++)
            {
                var e = Fabrique.Sprite("Confetti", i % 3 == 0 ? "fx_etincelle" : "fx_pixel", transform, centre, Couches.Effets);
                e.color = couleurs[i % couleurs.Length];
                if (i % 3 != 0) e.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                float ang = (i / (float)nombre) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float dist = Random.Range(25f, 55f);
                Vector3 vers = (Vector3)centre + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0) * dist;
                StartCoroutine(Confetti(e, vers));
            }
        }

        IEnumerator ExplosionApres(Vector2 c, int n, float delai)
        {
            yield return new WaitForSeconds(delai);
            Explosion(c, n);
        }

        IEnumerator Confetti(SpriteRenderer e, Vector3 vers)
        {
            yield return Anim.Deplacer(e.transform, vers, 0.6f, Anim.Sortie);
            yield return Anim.Deplacer(e.transform, vers + new Vector3(0, -20, 0), 0.8f);
            yield return Anim.Fondu(e, 0f, 0.3f);
            Destroy(e.gameObject);
        }

        public void Recommencer()
        {
            sauterAccueil = true;
            Voix.Instance?.Taire();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public bool ACommence => commence;
    }
}
