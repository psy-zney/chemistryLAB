using UnityEngine;
using UnityEngine.Rendering;

namespace ChemistryLab.Desktop
{
    /// <summary>Authored visual children, with procedural gameplay anchors/colliders retained.</summary>
    public static class ModernLabArt
    {
        public static void Install(Transform world)
        {
            Replace(world.Find("Central Workbench"), "Workbench", Vector3.zero,
                "Bench Top", "Bench Leg");
            Attach(world.Find("Central Workbench"), "BenchTools", Vector3.zero);
            Replace(world.Find("Fume Hood"), "Hood", new Vector3(0f,0f,-4.65f),
                "Hood Back","Hood Header","Hood Base","Hood Sash","Hood Left","Hood Right");
            Replace(world.Find("Storage Left"),"StorageLeft",Vector3.zero,"Cabinet Back","Shelf ","Cabinet End");
            Replace(world.Find("Storage Right"),"StorageRight",Vector3.zero,"Cabinet Back","Shelf ","Cabinet End");
            Replace(world.Find("Sink Station"),"Sink",new Vector3(5.25f,0f,3.35f),
                "Sink Counter","Sink Basin","Sink Tap");
            Replace(world,"RoomDetail",Vector3.zero,"Back Window ","Wall Base Trim");
            foreach (Transform child in world)
                if (child.name.EndsWith("Wall",System.StringComparison.Ordinal) || child.name=="Ceiling" || child.name=="Ceiling Panel")
                {
                    // The broad key represents light arriving from ceiling fixtures/windows.
                    // The runtime shell must not occlude that virtual area illumination.
                    var renderer=child.GetComponent<Renderer>();
                    if(renderer!=null) renderer.shadowCastingMode=ShadowCastingMode.Off;
                }
                else if(child.name.StartsWith("Floor Grid",System.StringComparison.Ordinal))
                {
                    var scale=child.localScale;
                    if(child.name.StartsWith("Floor Grid X",System.StringComparison.Ordinal)) scale.x=.003f;
                    else scale.z=.003f;
                    child.localScale=scale;
                }
            foreach (var tray in world.GetComponentsInChildren<SamplePreparationInteractable>())
                Replace(tray.transform,"PreparationTray",Vector3.zero,"Preparation Mat");
            TaskLight(world,"Workbench task light",new Vector3(0f,3.27f,0f),1.65f,5f);
            TaskLight(world,"Hood task light",new Vector3(0f,2.69f,-4.65f),1.25f,2.8f);
            // Runtime geometry cannot use a bake made before it exists. Capture one bounded
            // reflection after assembly instead of rebaking or refreshing probes every frame.
            var probeObject=new GameObject("Assembled laboratory reflection");
            probeObject.transform.SetParent(world,false);
            probeObject.transform.position=new Vector3(0f,1.8f,0f);
            var probe=probeObject.AddComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Realtime;
            probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution=128;
            probe.size=new Vector3(14f,3.6f,12f);
            probe.boxProjection=true;
            probe.cullingMask=~(1<<5);
            probe.RenderProbe();
        }

        public static GameObject Attach(Transform anchor,string resource,Vector3 position)
        {
            var template=Resources.Load<GameObject>("Art/"+resource);
            if(template==null || anchor==null) return null;
            var instance=Object.Instantiate(template,anchor,false);
            instance.name="Authored "+resource;
            instance.transform.localPosition=position;
            return instance;
        }

        public static bool ReplaceBottle(Transform anchor,ChemicalModelKind kind)
        {
            var authored=Attach(anchor,"ReagentBottle",Vector3.zero);
            if(authored==null) return false;
            var shell=Resources.Load<Material>("Art/"+(kind==ChemicalModelKind.Powder ? "Amber"
                : kind==ChemicalModelKind.Metal ? "Ivory" : "Glass"));
            foreach(var renderer in authored.GetComponentsInChildren<MeshRenderer>())
                if(renderer.name.EndsWith("_Glass",System.StringComparison.Ordinal)) renderer.sharedMaterial=shell;
            foreach(Transform child in anchor)
                if(child.name=="Reagent Bottle Body" || child.name=="Reagent Bottle Shoulder"
                    || child.name=="Reagent Bottle Neck" || child.name=="Cap")
                    child.GetComponent<Renderer>().enabled=false;
            return true;
        }

        private static void Replace(Transform anchor,string resource,Vector3 position,params string[] prefixes)
        {
            var authored=Attach(anchor,resource,position);
            if(authored==null) return;
            foreach(Transform child in anchor)
            {
                if(child==authored.transform) continue;
                foreach(var prefix in prefixes)
                    if(child.name.StartsWith(prefix,System.StringComparison.Ordinal))
                    {
                        var renderer=child.GetComponent<Renderer>();
                        if(renderer!=null) renderer.enabled=false;
                    }
            }
        }
        private static void TaskLight(Transform parent,string name,Vector3 position,float intensity,float range)
        {
            var instance=new GameObject(name);
            instance.transform.SetParent(parent,false);
            instance.transform.position=position;
            instance.transform.rotation=Quaternion.Euler(90f,0f,0f);
            var light=instance.AddComponent<Light>();
            light.type=LightType.Spot;
            light.spotAngle=105f;
            light.color=new Color(1f,.97f,.90f);
            light.intensity=intensity;
            light.range=range;
            light.shadows=LightShadows.None;
            light.renderMode=LightRenderMode.ForcePixel;
        }

        public static Material MaterialFor(string key)
        {
            string name;
            switch(key)
            {
                case "Floor": name="Tile"; break;
                case "Wall": case "WallSecondary": case "Ceiling": case "Bench": name="Ivory"; break;
                case "BenchTop": name="Worktop"; break;
                case "Steel": name="Steel"; break;
                case "SteelDark": name="DarkSteel"; break;
                case "CeilingLight": name="Diffuser"; break;
                case "BubbleParticle": case "PrecipitateParticle": case "FumeParticle": name="ReactionParticle"; break;
                default: return null;
            }
            return Resources.Load<Material>("Art/"+name);
        }
    }
}
