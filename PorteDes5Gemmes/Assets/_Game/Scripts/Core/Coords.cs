using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Le jeu est dessiné pour un écran de 320 x 180 pixels (1 unité Unity = 1 pixel).
    /// Les tableaux JSON gardent le repère du jeu web (1600 x 900, y vers le bas).
    /// </summary>
    public static class Coords
    {
        public const int LargeurRef = 320;
        public const int HauteurRef = 180;
        const float Echelle = 5f; // 1600 / 320

        public static Vector2 DepuisScene(float x, float y)
        {
            return new Vector2(Mathf.Round(x / Echelle - LargeurRef / 2f), Mathf.Round(HauteurRef / 2f - y / Echelle));
        }

        public static Vector2 DepuisScene(Point p) => DepuisScene(p.x, p.y);

        /// <summary>Ordre d'affichage : plus un objet est bas à l'écran, plus il passe devant.</summary>
        public static int OrdreSelonHauteur(float yMonde) => 200 - Mathf.RoundToInt(yMonde);

        public static Vector3 Arrondi(Vector2 p) => new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0f);
    }

    /// <summary>Ordres d'affichage des grandes couches.</summary>
    public static class Couches
    {
        public const int Fond = -100;
        public const int PanierDos = 330;
        public const int PanierContenu = 331;
        public const int PanierFace = 332;
        public const int Panneaux = 340;
        public const int Gemme = 360;
        public const int Nuit = 400;
        public const int Lumi = 450;
        public const int Effets = 460;
        public const int Hud = 500;
        public const int Accueil = 600;
    }
}
