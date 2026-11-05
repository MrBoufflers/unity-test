using System;
using UnityEngine;

namespace PorteDes5Gemmes
{
    // Description d'un tableau, lue depuis Resources/Tableaux/<nom>.json.
    // Les coordonnées sont celles du jeu web (scène de 1600 x 900, origine en haut à gauche) ;
    // Coords.DepuisScene les convertit en pixels Unity.

    [Serializable]
    public class Point
    {
        public float x;
        public float y;
    }

    [Serializable]
    public class ParJoueur
    {
        public string elea;
        public string isao;

        public string Pour(string joueur)
        {
            string texte = joueur == "isao" ? isao : elea;
            return string.IsNullOrEmpty(texte) ? (joueur == "isao" ? elea : isao) : texte;
        }
    }

    [Serializable]
    public class ElementDecor
    {
        public string type;
        public float x;
        public float y;
        public float scale = 1f;
    }

    [Serializable]
    public class ObjetCache
    {
        public string id;
        public float x;
        public float y;
        public string color;
    }

    [Serializable]
    public class Tour
    {
        public string player;
        public string type;
        public ParJoueur prompt;
        public ParJoueur success_line;
        public ParJoueur retry_line;
        public bool count_aloud;
        public bool shuffle_positions;
        public ObjetCache[] targets;
        public ObjetCache[] distractors;
        public string answer_from;
        public int[] choices;
        public float choices_y = 440f;
        public float choices_spacing = 330f;
    }

    [Serializable]
    public class PointAnime
    {
        public string id;
        public float x;
        public float y;
        public string reaction;
    }

    [Serializable]
    public class Repliques
    {
        public string intro;
        public string gem_touch;
        public string celebration;
        public string goodnight;
        public string[] count;
        public string auto_help;
        public string voice_test;

        public string Nombre(int n)
        {
            if (count != null && n >= 1 && n <= count.Length) return count[n - 1];
            return n + " !";
        }
    }

    [Serializable]
    public class TableauData
    {
        public string id;
        public string gem;
        public string background;
        public float[] hint_delays_s;
        public float auto_solve_after_s;
        public Point gem_position;
        public Point basket;
        public Point lumi_home;
        public Point lumi_sleep_spot;
        public ElementDecor[] decor;
        public Tour[] turns;
        public PointAnime[] ambient_hotspots;
        public Repliques lines;

        public static TableauData Charger(string nom)
        {
            var fichier = Resources.Load<TextAsset>("Tableaux/" + nom);
            if (fichier == null)
            {
                Debug.LogError($"Tableau introuvable : Resources/Tableaux/{nom}.json");
                return null;
            }
            return JsonUtility.FromJson<TableauData>(fichier.text);
        }
    }
}
