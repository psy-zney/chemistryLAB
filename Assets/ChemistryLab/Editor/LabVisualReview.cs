using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChemistryLab.Desktop.Editor
{
    // Explicit opt-in review of the actual runtime room. No production scene decoration.
    [InitializeOnLoad]
    public static class LabVisualReview
    {
        private const string PendingKey = "ChemistryLab.VisualReview.Pending";
        private const string OutputKey = "ChemistryLab.VisualReview.Output";
        static LabVisualReview() { EditorApplication.playModeStateChanged += OnPlayState; }

        public static void Capture()
        {
            var args = Environment.GetCommandLineArgs();
            var output = "output/visual-upgrade/after";
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-reviewOutput") output = args[i + 1];
            Directory.CreateDirectory(output);
            SessionState.SetString(OutputKey, output);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(PendingKey + ".Reduced", LabAccessibility.ReducedMotion);
            SessionState.SetInt(PendingKey + ".Language", (int)LabLocalization.Current);
            SessionState.SetString(PendingKey + ".Inventory", Path.Combine(Application.persistentDataPath,"chemistry-inventory.json"));
            var inventory=SessionState.GetString(PendingKey + ".Inventory","");
            SessionState.SetBool(PendingKey + ".InventoryExisted",File.Exists(inventory));
            if(File.Exists(inventory)) File.Copy(inventory,Path.Combine(output,"inventory-backup.tmp"),true);
            EditorSceneManager.OpenScene("Assets/ChemistryLab/Scenes/DesktopChemistryLab.unity");
            Debug.Log("VISUAL_REVIEW_EDITOR version=" + Application.unityVersion
                + " project=" + Application.dataPath + " scene=" + SceneManager.GetActiveScene().path
                + " pipeline=" + (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                    ? "Built-in" : UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name)
                + " compiling=" + EditorApplication.isCompiling + " play=" + EditorApplication.isPlaying);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var game = UnityEngine.Object.FindAnyObjectByType<DesktopLabGame>();
                game.StartCoroutine(Guard(Run(game)));
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                LabAccessibility.ReducedMotion=SessionState.GetBool(PendingKey + ".Reduced",false);
                LabLocalization.Current=(LabLanguage)SessionState.GetInt(PendingKey + ".Language",0);
                var inventory=SessionState.GetString(PendingKey + ".Inventory","");
                var backup=Path.Combine(SessionState.GetString(OutputKey,""),"inventory-backup.tmp");
                if(SessionState.GetBool(PendingKey + ".InventoryExisted",false)) File.Copy(backup,inventory,true);
                else if(File.Exists(inventory)) File.Delete(inventory);
                SessionState.SetBool(PendingKey, false);
                EditorApplication.Exit(SessionState.GetInt(PendingKey + ".Exit", 0));
            }
        }

        private static IEnumerator Guard(IEnumerator routine,bool finish = true)
        {
            while (true)
            {
                object next = null;
                var more = false;
                try { more = routine.MoveNext(); if (more) next = routine.Current; }
                catch (Exception error)
                {
                    Debug.LogException(error);
                    SessionState.SetInt(PendingKey + ".Exit", 1);
                    EditorApplication.ExitPlaymode();
                    yield break;
                }
                if (!more) break;
                if(next is IEnumerator nested) yield return Guard(nested,false);
                else yield return next;
            }
            if(finish) EditorApplication.ExitPlaymode();
        }

        private static IEnumerator Run(DesktopLabGame game)
        {
            yield return null;
            SessionState.SetInt(PendingKey + ".Exit", 0);
            game.ResumeFromUi();
            game.Player.enabled = false;
            game.ToggleInspector(false);
            LabAccessibility.ReducedMotion=false;
            var camera = game.Player.ViewCamera;
            var arms = camera.transform.Find("POV Chemist Arms");
            if (arms != null) arms.gameObject.SetActive(false);
            var output = SessionState.GetString(OutputKey, "output/visual-upgrade/after");
            yield return new WaitForSecondsRealtime(1f);
            View(camera, new Vector3(4.6f, 1.64f, 4.8f), new Vector3(-0.8f, 1.1f, -1.8f), 66f);
            Save(game, output, "01-room");
            View(camera, new Vector3(1.1f, 1.64f, 2.3f), new Vector3(0f, 1.02f, 0f), 60f);
            Save(game, output, "02-workbench");
            View(camera, new Vector3(2.5f, 1.64f, -2.25f), new Vector3(0f, 1.62f, -4.65f), 62f);
            Save(game, output, "03-hood");
            View(camera, new Vector3(-3.9f, 1.64f, 1.3f), new Vector3(-6.1f, 1.55f, -0.9f), 62f);
            Save(game, output, "04-shelves");
            View(camera, new Vector3(3.4f, 1.64f, 1.8f), new Vector3(5.25f, 1.04f, 3.35f), 62f);
            Save(game, output, "05-sink");
            game.Player.transform.position = new Vector3(0f, 0.02f, 2.1f);
            View(camera, new Vector3(0f, 1.64f, 1.5f), new Vector3(-0.5f, 1.08f, -0.5f), 66f);
            if (arms != null) arms.gameObject.SetActive(true);
            game.SelectChemical("copper-sulfate");
            yield return null;
            Save(game, output, "06-held");
            game.ToggleSampleOnPreparationSurface(LabStation.Workbench);
            yield return null;
            Save(game, output, "07-staged");
            if (arms != null) arms.gameObject.SetActive(false);
            game.AddSelectedToVessel(LabStation.Workbench);
            game.SelectChemical("sodium-hydroxide");
            game.ToggleSampleOnPreparationSurface(LabStation.Workbench);
            game.AddSelectedToVessel(LabStation.Workbench);
            game.SkipReactionCamera();
            game.ToggleInspector(false);
            yield return new WaitForSecondsRealtime(0.2f);
            View(camera, new Vector3(0.17f, 1.23f, 0.28f), new Vector3(0f, 1.11f, 0f), 42f);
            yield return new WaitForSecondsRealtime(1f);
            Save(game, output, "08-precipitate");
            game.WashVessels();
            game.SelectChemical("calcium-carbonate");
            game.ToggleSampleOnPreparationSurface(LabStation.Workbench);
            game.AddSelectedToVessel(LabStation.Workbench);
            game.SelectChemical("hydrochloric-acid");
            game.ToggleSampleOnPreparationSurface(LabStation.Workbench);
            game.AddSelectedToVessel(LabStation.Workbench);
            game.SkipReactionCamera();
            game.ToggleInspector(false);
            yield return new WaitForSecondsRealtime(0.2f);
            View(camera, new Vector3(0.17f, 1.23f, 0.28f), new Vector3(0f, 1.11f, 0f), 42f);
            yield return new WaitForSecondsRealtime(1f);
            Save(game, output, "09-gas");
            yield return VerifyPresentation(game,output);
            Debug.Log("VISUAL_REVIEW_PASS output=" + output + " play=" + Application.isPlaying);
        }

        private static IEnumerator VerifyPresentation(DesktopLabGame game,string output)
        {
            var camera=game.Player.ViewCamera;
            var work=GameObject.Find("Workbench Vessel").transform;
            var hood=GameObject.Find("Fume Hood Vessel").transform;
            var liquid=work.Find("Vessel Contents").GetComponent<Renderer>();
            var bubbles=work.Find("Rising Gas Bubbles").GetComponent<ParticleSystem>();
            View(camera,new Vector3(0f,1.64f,1.35f),new Vector3(-.82f,1.085f,-.72f),66f);
            Physics.SyncTransforms();
            Require(Physics.Raycast(camera.transform.position,camera.transform.forward,out var trayHit,3.35f)
                && trayHit.collider.GetComponentInParent<SamplePreparationInteractable>()!=null,
                "authored art leaves preparation tray raycast clear");
            View(camera,new Vector3(0f,1.64f,1.35f),work.position+Vector3.up*.19f,66f);
            Require(Physics.Raycast(camera.transform.position,camera.transform.forward,out var vesselHit,3.35f)
                && vesselHit.collider.GetComponentInParent<VesselInteractable>()!=null,
                "authored art leaves vessel raycast clear");
            var gasTime=bubbles.time;
            var colour=liquid.sharedMaterial.color;
            game.SetPaused(true);
            yield return new WaitForSecondsRealtime(.3f);
            Require(Mathf.Abs(bubbles.time-gasTime)<.001f && liquid.sharedMaterial.color==colour,"pause freezes particles and colour");
            game.SetPaused(false);
            game.Player.transform.position=new Vector3(0f,.02f,-2.9f);
            Load(game,LabStation.FumeHood,"copper-sulfate","sodium-hydroxide");
            game.SkipReactionCamera();
            yield return new WaitForSecondsRealtime(.2f);
            Require(work.Find("Settled Precipitate").gameObject.activeSelf==false
                && hood.Find("Settled Precipitate").gameObject.activeSelf,"two vessel sediment states are independent");
            Require(liquid.sharedMaterial!=hood.Find("Vessel Contents").GetComponent<Renderer>().sharedMaterial,
                "two vessel liquid materials are independent");
            game.WashVessels();
            Require(bubbles.particleCount==0 && !work.Find("Settled Precipitate").gameObject.activeSelf
                && !hood.Find("Settled Precipitate").gameObject.activeSelf,"cleanup clears both vessel effects");
            game.Player.transform.position=new Vector3(0f,.02f,2.1f);
            game.SelectChemical("copper-sulfate");
            game.AddSelectedToVessel(LabStation.Workbench);
            Require(game.GetVesselAdditionCount(LabStation.Workbench)==0,"held sample cannot load vessel");
            game.ToggleSampleOnPreparationSurface(LabStation.Workbench);
            game.Player.transform.position=new Vector3(0f,.02f,5f);
            game.AddSelectedToVessel(LabStation.Workbench);
            Require(game.GetVesselAdditionCount(LabStation.Workbench)==0 && game.HasStagedSample(LabStation.Workbench),
                "remote load preserves staged sample");
            game.Player.transform.position=new Vector3(0f,.02f,2.1f);
            game.AddSelectedToVessel(LabStation.Workbench);
            Require(game.GetVesselAdditionCount(LabStation.Workbench)==1 && !game.HasStagedSample(LabStation.Workbench),
                "in range staged sample loads");
            var beforePosition=camera.transform.localPosition;
            var beforeRotation=camera.transform.localRotation;
            var beforeFov=camera.fieldOfView;
            LabAccessibility.ReducedMotion=true;
            game.SelectChemical("sodium-hydroxide");
            game.ToggleSampleOnPreparationSurface(LabStation.Workbench);
            game.AddSelectedToVessel(LabStation.Workbench);
            yield return null;
            Require(camera.transform.localPosition==beforePosition && camera.transform.localRotation==beforeRotation
                && Mathf.Approximately(camera.fieldOfView,beforeFov),"reduced motion keeps camera still");
            game.SkipReactionCamera();
            yield return null;
            Require(!game.ReactionCameraActive,"close view skip ends presentation");
            Require(camera.transform.localPosition==beforePosition && camera.transform.localRotation==beforeRotation
                && Mathf.Approximately(camera.fieldOfView,beforeFov),"close view restores exact camera state");
            game.ToggleInspector(false);
            View(camera,new Vector3(.17f,1.23f,.28f),new Vector3(0f,1.11f,0f),42f);
            Save(game,output,"10-reduced-motion");
            game.CollectProduct(LabStation.Workbench);
            Require(!game.CanCollectProduct(LabStation.Workbench)
                && work.Find("Settled Precipitate").gameObject.activeSelf,"collection keeps sediment and is once only");
            game.WashVessels();
            game.ToggleInspector(false);
            game.Player.transform.position=new Vector3(0f,.02f,-2.9f);
            var health=game.SafetySystem.Health;
            Load(game,LabStation.FumeHood,"sodium-sulfide","hydrochloric-acid");
            game.SkipReactionCamera();
            yield return null;
            Require(game.CurrentOutcome.Hazard!=null && game.SafetySystem.FumeHoodFanOn,"hazard outcome retains hood controls");
            Require(!hood.Find("Thermal Haze And Fumes").GetComponent<ParticleSystem>().isPlaying,
                "colourless hazardous gas does not become coloured smoke");
            game.WashVessels();
            game.Player.transform.position=new Vector3(0f,.02f,2.1f);
            Load(game,LabStation.Workbench,"sodium-sulfide","hydrochloric-acid");
            game.SkipReactionCamera();
            yield return null;
            Require(game.SafetySystem.Health<health,"uncontrolled hazardous gas retains health consequences");
            game.WashVessels();
            LabAccessibility.ReducedMotion=false;
            game.Player.transform.position=new Vector3(0f,.02f,2.1f);
            Load(game,LabStation.Workbench,"iron","copper-sulfate");
            Require(game.CurrentOutcome.Effect==ReactionEffect.Colour,"existing colour reaction resolves without new chemistry rules");
            game.SkipReactionCamera();yield return null;
            var initialColour=liquid.sharedMaterial.color;
            yield return new WaitForSecondsRealtime(.2f);
            Require(liquid.sharedMaterial.color!=initialColour,"colour develops during committed reaction");
            Time.timeScale=20f;
            yield return new WaitForSecondsRealtime(game.CurrentOutcome.EstimatedCompletionSeconds/20f+.1f);
            Time.timeScale=1f;
            var finalColour=game.CurrentOutcome.DisplayColour;
            var displayed=liquid.sharedMaterial.color;
            Require(Mathf.Abs(finalColour.r-displayed.r)<.005f && Mathf.Abs(finalColour.g-displayed.g)<.005f
                && Mathf.Abs(finalColour.b-displayed.b)<.005f,"completed solution keeps outcome hue");
            game.ToggleInspector(false);
            View(camera,new Vector3(.17f,1.23f,.28f),new Vector3(0f,1.11f,0f),42f);
            Save(game,output,"11-colour-outcome");
            game.WashVessels();
            game.AdjustVesselTemperature(LabStation.Workbench,75f);
            Load(game,LabStation.Workbench,"sodium-hydroxide","hydrochloric-acid");
            game.SkipReactionCamera();yield return null;
            Require(game.CurrentOutcome.Effect==ReactionEffect.Heat && game.CurrentOutcome.TemperatureC>=90f
                && work.Find("Thermal Haze And Fumes").GetComponent<ParticleSystem>().isPlaying,"data supported hot nongas mixture emits restrained white haze");
            game.ToggleInspector(false);
            View(camera,new Vector3(.17f,1.23f,.28f),new Vector3(0f,1.11f,0f),42f);
            yield return new WaitForSecondsRealtime(.4f);
            Save(game,output,"12-hot-mixture");
            game.WashVessels();
            game.ToggleInspector(false);
            View(camera,new Vector3(1.1f,1.64f,2.3f),new Vector3(0f,1.02f,0f),60f);
            foreach(var language in new[]{LabLanguage.Vietnamese,LabLanguage.English})
            {
                if(LabLocalization.Current!=language) game.ToggleLanguage();
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1920,1200),new Vector2Int(2560,1080)})
                {
                    yield return null;
                    Save(game,output,"hud-"+language+"-"+size.x+"x"+size.y,size.x,size.y);
                }
            }
            MeasureOverdraw(game,output);
            game.WashVessels();
            game.Player.transform.position=new Vector3(0f,.02f,2.1f);
            Load(game,LabStation.Workbench,"copper-sulfate","sodium-hydroxide");
            game.SkipReactionCamera();
            for(var frame=0;frame<8 && game.ReactionCameraActive;frame++) yield return null;
            game.ToggleInspector(false);
            View(camera,new Vector3(.17f,1.23f,.28f),new Vector3(0f,1.11f,0f),42f);
            var video=Path.Combine(output,"video-frames");Directory.CreateDirectory(video);
            Time.captureDeltaTime=1f/12f;
            for(var i=0;i<90;i++)
            {
                yield return null;
                Save(game,video,"frame-"+i.ToString("D4"),1280,720);
            }
            Time.captureDeltaTime=0f;
            File.WriteAllText(Path.Combine(output,"play-mode-checks.txt"),"Passed: tray/vessel raycasts; staged workflow; remote/hand load rejection; independent vessel materials/sediment; cleanup; once-only collection; reduced motion camera; skip/restore; pause; colour development and final hue; hot nongas haze; colourless hazard and safety consequences. HUD captures: VI/EN at 16:9, 16:10, ultrawide.\n");
        }
        [Serializable] private sealed class OverdrawReport
        {
            public string method="Unity replacement shader; scene only, HUD excluded; opaque depth first; transparent fragments with alpha mask > 0.025; coverage count, not GPU milliseconds";
            public int width,height,maxLayers;
            public float meanLayers,coveredFraction;
        }
        private static void MeasureOverdraw(DesktopLabGame game,string output)
        {
            const int width=1920,height=1080;
            var camera=game.Player.ViewCamera;
            var originalTarget=camera.targetTexture;var originalActive=RenderTexture.active;
            var mask=camera.cullingMask;var clear=camera.clearFlags;var background=camera.backgroundColor;
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGBFloat);
            var pixels=new Texture2D(width,height,TextureFormat.RGBAFloat,false,true);
            try
            {
                camera.targetTexture=target;camera.cullingMask=mask & ~(1<<5);
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                camera.RenderWithShader(AssetDatabase.LoadAssetAtPath<Shader>("Assets/ChemistryLab/Editor/ReviewOverdraw.shader"),"RenderType");
                RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                var report=new OverdrawReport { width=width,height=height };
                var colours=pixels.GetPixels();var sum=0f;var covered=0;
                foreach(var pixel in colours)
                { sum+=pixel.r;if(pixel.r>.5f) covered++;report.maxLayers=Mathf.Max(report.maxLayers,Mathf.RoundToInt(pixel.r)); }
                report.meanLayers=sum/colours.Length;report.coveredFraction=(float)covered/colours.Length;
                File.WriteAllText(Path.Combine(output,"overdraw.json"),JsonUtility.ToJson(report,true));
            }
            finally
            {
                camera.targetTexture=originalTarget;RenderTexture.active=originalActive;
                camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=background;
                UnityEngine.Object.Destroy(pixels);RenderTexture.ReleaseTemporary(target);
            }
        }
        private static void Load(DesktopLabGame game,LabStation station,string a,string b)
        {
            foreach(var chemical in new[]{a,b})
            {
                game.SelectChemical(chemical);game.ToggleSampleOnPreparationSurface(station);game.AddSelectedToVessel(station);
            }
        }
        private static void Require(bool result,string message)
        {
            if(!result) throw new InvalidOperationException("VISUAL_REVIEW_ASSERT_FAIL "+message);
            Debug.Log("VISUAL_REVIEW_ASSERT_PASS "+message);
        }

        private static void View(Camera camera, Vector3 position, Vector3 target, float fov)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = fov;
        }

        private static void Save(DesktopLabGame game, string output, string name, int width = 1920, int height = 1080)
        {
            var camera = game.Player.ViewCamera;
            var canvases = game.Hud.GetComponentsInChildren<Canvas>(true);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            var distances = new float[canvases.Length];
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            target.antiAliasing = 2;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                for (var i = 0; i < canvases.Length; i++)
                {
                    modes[i] = canvases[i].renderMode;
                    cameras[i] = canvases[i].worldCamera;
                    distances[i] = canvases[i].planeDistance;
                    if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = Mathf.Max(camera.nearClipPlane + 0.02f, 0.1f);
                }
                Canvas.ForceUpdateCanvases();
                // Dynamic font atlases can rebuild on the first render after a new label.
                camera.Render();
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(output, name + ".png"), pixels.EncodeToPNG());
                Debug.Log("VISUAL_REVIEW_CAPTURE " + name + " draws=" + UnityStats.drawCalls
                    + " triangles=" + UnityStats.triangles);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                for (var i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = modes[i];
                    canvases[i].worldCamera = cameras[i];
                    canvases[i].planeDistance = distances[i];
                }
                UnityEngine.Object.Destroy(pixels);
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
