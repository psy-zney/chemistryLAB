using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace ChemistryLab.Desktop
{
    // Explicit command-line measurement in the built player; never runs in normal gameplay.
    public static class LabPerformanceReview
    {
        [Serializable] private sealed class Report
        {
            public string unityVersion, cpu, gpu, graphicsApi, scenario, measurement;
            public int width,height,samples,timingSamples;
            public double meanCpuMs,meanGpuMs,p95FrameMs,meanFrameMs;
            public long drawCalls,batches,triangles,setPassCalls;
            public bool gpuTimingAvailable,drawCallCounterAvailable;
        }
        public static IEnumerator Run(DesktopLabGame game)
        {
            game.ResumeFromUi();game.Player.enabled=false;
            Application.runInBackground=true;
            QualitySettings.vSyncCount=0;
            Application.targetFrameRate=-1;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            var camera=game.Player.ViewCamera;
            // Hidden Windows players skip their backbuffer. Force a real 1080p
            // offscreen render per sampled frame instead of timing an idle window.
            var renderTarget=new RenderTexture(1920,1080,24) { antiAliasing=QualitySettings.antiAliasing };
            renderTarget.Create();
            camera.targetTexture=renderTarget;
            camera.enabled=false;
            var args=Environment.GetCommandLineArgs();
            var path=Path.Combine(Application.persistentDataPath,"lab-performance.json");
            for(var i=0;i+1<args.Length;i++) if(args[i]=="-profileOutput") path=args[i+1];
            var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
            var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
            var passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count");
            var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
            try
            {
                game.Player.transform.position=new Vector3(0f,.02f,2.1f);
                foreach(var chemical in new[]{"calcium-carbonate","hydrochloric-acid"})
                { game.SelectChemical(chemical);game.ToggleSampleOnPreparationSurface(LabStation.Workbench);game.AddSelectedToVessel(LabStation.Workbench); }
                game.SkipReactionCamera();yield return null;
                game.Player.transform.position=new Vector3(0f,.02f,-2.9f);
                foreach(var chemical in new[]{"copper-sulfate","sodium-hydroxide"})
                { game.SelectChemical(chemical);game.ToggleSampleOnPreparationSurface(LabStation.FumeHood);game.AddSelectedToVessel(LabStation.FumeHood); }
                game.SkipReactionCamera();yield return null;
                game.ToggleInspector(false);
                camera.transform.position=new Vector3(4.6f,1.64f,4.8f);
                camera.transform.LookAt(new Vector3(-.8f,1.1f,-1.8f));
                camera.fieldOfView=66f;
                // Freeze neither chemistry nor VFX: measure both stations through their lifecycle.
                for(var i=0;i<90;i++) { camera.Render();FrameTimingManager.CaptureFrameTimings();yield return null; }
                var report=new Report { unityVersion=Application.unityVersion,cpu=SystemInfo.processorType,
                    gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),
                    width=Screen.width,height=Screen.height,scenario="room overview, gas at bench and precipitate in hood",
                    measurement="Windows player; hidden window, forced 1920x1080 camera render per frame; uncapped; 90 warmup and 300 sampled frames; excludes backbuffer presentation" };
                var times=new List<float>(300);
                var timings=new FrameTiming[1];
                double cpu=0,gpu=0;long drawSum=0,batchSum=0,triangleSum=0,passSum=0;
                for(var i=0;i<300;i++)
                {
                    camera.Render();FrameTimingManager.CaptureFrameTimings();yield return null;
                    times.Add(Time.unscaledDeltaTime*1000f);
                    if(FrameTimingManager.GetLatestTimings(1,timings)>0)
                    {
                        cpu+=timings[0].cpuFrameTime;gpu+=timings[0].gpuFrameTime;report.timingSamples++;
                    }
                    drawSum+=draws.LastValue;triangleSum+=triangles.LastValue;passSum+=passes.LastValue;
                    batchSum+=batches.LastValue;
                }
                times.Sort();report.samples=times.Count;
                foreach(var time in times) report.meanFrameMs+=time/times.Count;
                report.p95FrameMs=times[Mathf.FloorToInt((times.Count-1)*.95f)];
                report.meanCpuMs=report.timingSamples>0 ? cpu/report.timingSamples : 0;
                report.meanGpuMs=report.timingSamples>0 ? gpu/report.timingSamples : 0;
                report.gpuTimingAvailable=report.meanGpuMs>0;
                report.drawCalls=drawSum/report.samples;report.triangles=triangleSum/report.samples;report.setPassCalls=passSum/report.samples;
                report.batches=batchSum/report.samples;
                report.drawCallCounterAvailable=draws.Valid && drawSum>0;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(path,JsonUtility.ToJson(report,true));
                Debug.Log("LAB_PERFORMANCE_CAPTURE "+JsonUtility.ToJson(report));
            }
            finally
            {
                draws.Dispose();triangles.Dispose();passes.Dispose();batches.Dispose();
                camera.targetTexture=null;camera.enabled=true;
                renderTarget.Release();UnityEngine.Object.Destroy(renderTarget);
            }
            Application.Quit();
        }
    }
}
