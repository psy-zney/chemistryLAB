using System.Collections.Generic;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    /// <summary>Inner envelope of the approved 0.18 m flask, in vessel-local metres.</summary>
    public sealed class VesselContentsGeometry
    {
        // Physical presentation geometry only; no reaction/property records live here.
        private static readonly Vector2[] Profile = {
            new Vector2(.024f,.050f), new Vector2(.036f,.054f),
            new Vector2(.050f,.054f), new Vector2(.065f,.049f),
            new Vector2(.085f,.040f), new Vector2(.105f,.030f),
            new Vector2(.125f,.018f)
        };
        public const float Bottom = .024f;
        public float Surface { get; private set; } = .04f;
        public Mesh Liquid { get; } = new Mesh { name = "Flask shaped solution and meniscus" };
        public Mesh Sediment { get; } = new Mesh { name = "Flask bottom sediment" };
        private float lastVolume = -1f;
        private float lastSediment = -1f;

        public static float RadiusAt(float y)
        {
            for (var i = 1; i < Profile.Length; i++)
                if (y <= Profile[i].x)
                    return Mathf.Lerp(Profile[i-1].y,Profile[i].y,
                        Mathf.InverseLerp(Profile[i-1].x,Profile[i].x,y));
            return Profile[Profile.Length-1].y;
        }
        public void SetVolume(float litres)
        {
            if (Mathf.Approximately(litres,lastVolume)) return;
            lastVolume=litres;
            var low=Bottom+.002f;
            var high=Profile[Profile.Length-1].x;
            for(var i=0;i<18;i++)
            {
                var mid=(low+high)*.5f;
                if(VolumeBelow(mid)<Mathf.Max(.002f,litres)*.001f) low=mid; else high=mid;
            }
            Surface=(low+high)*.5f;
            Build(Liquid,Surface,true);
        }
        private static float VolumeBelow(float y)
        {
            var volume=0f;
            for(var i=1;i<Profile.Length;i++)
            {
                var bottom=Profile[i-1].x;
                if(y<=bottom) break;
                var top=Mathf.Min(y,Profile[i].x);
                var r0=RadiusAt(bottom); var r1=RadiusAt(top);
                volume+=Mathf.PI*(top-bottom)*(r0*r0+r0*r1+r1*r1)/3f;
            }
            return volume;
        }
        public void SetSediment(float height)
        {
            if(Mathf.Abs(height-lastSediment)<.00005f) return;
            lastSediment=height;
            Build(Sediment,Bottom+Mathf.Max(.0002f,height),false);
        }
        private static void Build(Mesh mesh,float surface,bool meniscus)
        {
            const int segments=64;
            var rings=new List<Vector2>();
            rings.Add(Profile[0]);
            for(var i=1;i<Profile.Length && Profile[i].x<surface-.0008f;i++) rings.Add(Profile[i]);
            var radius=RadiusAt(surface);
            rings.Add(new Vector2(surface+(meniscus?.00055f:0f),radius));
            rings.Add(new Vector2(surface,radius-(meniscus?.0012f:.0002f)));
            var vertices=new Vector3[rings.Count*segments+2];
            var uv=new Vector2[vertices.Length];
            var triangles=new List<int>();
            for(var j=0;j<rings.Count;j++)
                for(var i=0;i<segments;i++)
                {
                    var a=i*Mathf.PI*2f/segments;
                    var index=j*segments+i;
                    vertices[index]=new Vector3(Mathf.Cos(a)*rings[j].y,rings[j].x,Mathf.Sin(a)*rings[j].y);
                    uv[index]=new Vector2((float)i/segments,rings[j].x/.18f);
                    if(j==0) continue;
                    var previous=(j-1)*segments+i;
                    var next=(j-1)*segments+(i+1)%segments;
                    triangles.Add(previous);triangles.Add(index);triangles.Add(next);
                    triangles.Add(next);triangles.Add(index);triangles.Add(j*segments+(i+1)%segments);
                }
            var bottom=vertices.Length-2;var top=vertices.Length-1;
            vertices[bottom]=new Vector3(0f,Bottom,0f);
            vertices[top]=new Vector3(0f,surface,0f);
            for(var i=0;i<segments;i++)
            {
                triangles.Add(bottom);triangles.Add(i);triangles.Add((i+1)%segments);
                triangles.Add(top);triangles.Add((rings.Count-1)*segments+(i+1)%segments);
                triangles.Add((rings.Count-1)*segments+i);
            }
            mesh.Clear();mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        public void Dispose()
        {
            Object.Destroy(Liquid);Object.Destroy(Sediment);
        }
    }
}
