using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PorteDes5Gemmes.Editeur
{
    /// <summary>
    /// Met le projet en place et fabrique le jeu. Accessible dans le menu « Porte des 5 gemmes »
    /// de l'éditeur, ou en ligne de commande :
    ///   unity run PorteDes5Gemmes -- -executeMethod PorteDes5Gemmes.Editeur.Configuration.Installer
    ///   unity run PorteDes5Gemmes -- -executeMethod PorteDes5Gemmes.Editeur.Configuration.ConstruireAndroid
    /// </summary>
    public static class Configuration
    {
        const string Scene = "Assets/_Game/Scenes/Foret.unity";
        const string Profil = "Assets/_Game/Rendu/VolumeForet.asset";

        [MenuItem("Porte des 5 gemmes/1. Importer les ressources TextMeshPro")]
        public static void ImporterTmp()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0)
            {
                Debug.Log("TextMeshPro : ressources déjà présentes.");
                return;
            }
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            string paquet = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(paquet, false);
            AssetDatabase.Refresh();
            Debug.Log("TextMeshPro : ressources importées.");
        }

        [MenuItem("Porte des 5 gemmes/2. Créer la scène et régler le projet")]
        public static void Installer()
        {
            ImporterTmp();
            AssetDatabase.ImportAsset("Assets/_Game/Resources/Art", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            ReglerProjet();
            CreerScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Porte des 5 gemmes : projet prêt.");
        }

        static void ReglerProjet()
        {
            PlayerSettings.companyName = "Boufflers";
            PlayerSettings.productName = "La Porte des 5 gemmes";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "fr.boufflers.porte5gemmes");

            // Téléphone en paysage uniquement (les deux sens)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Android : téléphones 64 bits récents
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 1;

            // Version ordinateur (pour tester) : fenêtre 1280 x 720
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            Debug.Log("Niveaux de qualité disponibles : " + string.Join(", ", QualitySettings.names));
        }

        static VolumeProfile ProfilVolume()
        {
            var profil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Profil);
            if (profil != null) return profil;
            Directory.CreateDirectory(Path.GetDirectoryName(Profil));
            profil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profil, Profil);
            // Léger halo sur ce qui brille (Lumi, la gemme) et vignette douce.
            // Pas de tonemapping : il changerait les couleurs choisies pixel par pixel.
            var bloom = profil.Add<Bloom>(true);
            bloom.intensity.Override(0.5f);
            bloom.threshold.Override(0.9f);
            var vignette = profil.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            foreach (var c in profil.components) AssetDatabase.AddObjectToAsset(c, profil);
            EditorUtility.SetDirty(profil);
            AssetDatabase.SaveAssets();
            return profil;
        }

        static void CreerScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Caméra pixel parfaite : 320 x 180 pixels de référence, agrandis sans flou
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 90;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0b, 0x10, 0x26, 0xff);
            camGo.AddComponent<AudioListener>();
            var donnees = cam.GetUniversalAdditionalCameraData();
            donnees.renderPostProcessing = true;
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 1;
            ppc.refResolutionX = 320;
            ppc.refResolutionY = 180;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;

            // Lumière globale 2D (sinon les sprites « Lit » restent noirs)
            var lumiere = new GameObject("Global Light 2D");
            var l2d = lumiere.AddComponent<Light2D>();
            l2d.lightType = Light2D.LightType.Global;
            l2d.intensity = 1f;

            var volGo = new GameObject("Global Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = ProfilVolume();

            var jeu = new GameObject("Jeu");
            jeu.AddComponent<Voix>();
            jeu.AddComponent<Bruitages>();
            jeu.AddComponent<EntreeTactile>();
            jeu.AddComponent<Plateau>();
            jeu.AddComponent<JeuAutomatique>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var ui = new GameObject("Interface", typeof(RectTransform));
            ui.AddComponent<Interface>();

            Directory.CreateDirectory(Path.GetDirectoryName(Scene));
            EditorSceneManager.SaveScene(scene, Scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        }

        static string[] Scenes() => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem("Porte des 5 gemmes/3. Construire pour Android (APK)")]
        public static void ConstruireAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Construire(BuildTarget.Android, "Builds/Android/PorteDes5Gemmes.apk");
        }

        [MenuItem("Porte des 5 gemmes/4. Construire pour Linux (tests)")]
        public static void ConstruireLinux() => Construire(BuildTarget.StandaloneLinux64, "Builds/Linux/PorteDes5Gemmes.x86_64");

        [MenuItem("Porte des 5 gemmes/5. Construire pour Windows")]
        public static void ConstruireWindows() => Construire(BuildTarget.StandaloneWindows64, "Builds/Windows/PorteDes5Gemmes.exe");

        static void Construire(BuildTarget cible, string sortie)
        {
            var options = new BuildPlayerOptions
            {
                scenes = Scenes(),
                locationPathName = sortie,
                target = cible,
                options = BuildOptions.None,
            };
            var rapport = BuildPipeline.BuildPlayer(options);
            var r = rapport.summary;
            Debug.Log($"Construction {cible} : {r.result}, {r.totalErrors} erreur(s), {r.totalSize / (1024 * 1024)} Mo → {sortie}");
            if (Application.isBatchMode && r.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
