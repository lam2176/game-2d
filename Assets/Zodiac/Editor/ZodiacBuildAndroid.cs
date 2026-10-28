using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Xuất game thành file cài Android (Builds/Android/12ConGiap.apk) — chép sang điện thoại rồi cài, hoặc cắm cáp rồi
/// dùng "Build And Run". Cần module Android Build Support (kèm OpenJDK, Android SDK &amp; NDK) trong Unity Hub.
///   • Trong Editor: menu "Tools/Zodiac/Xuất bản Android (APK)" — tự chuyển nền tảng sang Android nếu chưa.
///   • Ngoài Editor (Unity phải đang tắt): Unity.exe -batchmode -quit -projectPath . -buildTarget Android
///     -executeMethod ZodiacBuildAndroid.BuildAndroidBatch
/// Màn ngang, ARM64 + IL2CPP, ký bằng khoá debug (đủ để tự cài, không đưa lên Play Store).
/// </summary>
public static class ZodiacBuildAndroid
{
    public const string OutputPath = "Builds/Android/12ConGiap.apk";
    const string PackageName = "com.tunglam.zodiac";

    [MenuItem("Tools/Zodiac/Xuất bản Android (APK)", true)]
    static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/Zodiac/Xuất bản Android (APK)")]
    static void BuildMenu()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("[ZodiacBuild] Không chuyển được sang Android — đã cài module Android Build Support chưa?");
            return;
        }
        if (Build()) EditorUtility.RevealInFinder(OutputPath);
    }

    /// <summary>Gọi từ dòng lệnh. Thoát Unity với mã 0 nếu thành công, 1 nếu lỗi.</summary>
    public static void BuildAndroidBatch()
    {
        bool ok;
        try
        {
            ok = Build();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ok = false;
        }
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("[ZodiacBuild] Build Settings chưa có scene — chạy Tools/Zodiac/Dựng prototype trước.");
            return false;
        }

        PlayerSettings.productName = "12 Con Giáp";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // Chỉ màn ngang, xoay được hai chiều.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        // Máy chủ tài khoản (Server/ZodiacServer) chạy http trong mạng nhà
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        EditorUserBuildSettings.buildAppBundle = false;

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });
        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[ZodiacBuild] Xong: {OutputPath} ({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0}s)");
            return true;
        }
        Debug.LogError($"[ZodiacBuild] Build thất bại ({summary.result}), {summary.totalErrors} lỗi — xem log phía trên.");
        return false;
    }
}
