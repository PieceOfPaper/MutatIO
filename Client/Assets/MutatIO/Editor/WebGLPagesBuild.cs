using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MutatIO.Editor
{
    /// <summary>
    /// GitHub Pages 배포용 WebGL 빌드. CI(GameCI)는 -executeMethod로 BuildForPages를 호출합니다.
    /// GitHub Pages는 Content-Encoding 헤더를 지정할 수 없으므로 Gzip + Decompression Fallback으로 빌드하고,
    /// 빌드가 끝나면 프로젝트의 압축 설정을 원래대로 되돌립니다.
    /// </summary>
    public static class WebGLPagesBuild
    {
        // 현재 배포 대상은 서버 없이 플레이 가능한 로컬 프로토타입 씬입니다.
        static readonly string[] Scenes = { "Assets/MutatIO/Scenes/Prototype.unity" };

        const string DefaultOutputPath = "Builds/WebGL";

        [MenuItem("MutatIO/Build/WebGL (GitHub Pages)")]
        public static void BuildFromMenu()
        {
            var report = Build(DefaultOutputPath);
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.GetFullPath(DefaultOutputPath));
        }

        // GameCI 진입점. 실패 시 0이 아닌 종료 코드로 끝냅니다.
        public static void BuildForPages()
        {
            var outputPath = GetArgument("-customBuildPath") ?? DefaultOutputPath;
            var report = Build(outputPath);
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static BuildReport Build(string outputPath)
        {
            var previousCompression = PlayerSettings.WebGL.compressionFormat;
            var previousFallback = PlayerSettings.WebGL.decompressionFallback;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = Scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None,
                });

                var summary = report.summary;
                Debug.Log($"[WebGLPagesBuild] {summary.result}: {summary.outputPath} " +
                          $"({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime}, errors {summary.totalErrors})");
                return report;
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = previousCompression;
                PlayerSettings.WebGL.decompressionFallback = previousFallback;
            }
        }

        static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }
            return null;
        }
    }
}
