using UnityEditor;
using UnityEngine;

namespace PorteDes5Gemmes.Editeur
{
    /// <summary>
    /// Réglages d'import automatiques pour tout ce qui est dans Assets/_Game/Resources/Art :
    /// sprites nets (filtre « Point », pas de compression), 1 pixel = 1 unité,
    /// point d'ancrage en bas pour les objets posés au sol.
    /// </summary>
    public class ImportPixelArt : AssetPostprocessor
    {
        const string Dossier = "Assets/_Game/Resources/Art/";

        static readonly string[] AncresEnBas = { "decor_", "champignon_", "panneau_", "anim_fleur" };

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dossier)) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 1;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;

            var reglages = new TextureImporterSettings();
            imp.ReadTextureSettings(reglages);
            reglages.spriteMeshType = SpriteMeshType.FullRect;
            reglages.spriteExtrude = 0;
            string nom = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool enBas = false;
            foreach (var p in AncresEnBas)
                if (nom.StartsWith(p) && nom != "decor_branch") enBas = true;
            reglages.spriteAlignment = (int)(enBas ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            imp.SetTextureSettings(reglages);
        }
    }
}
