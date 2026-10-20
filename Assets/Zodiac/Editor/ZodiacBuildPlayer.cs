using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Xuất game thành bản Windows chạy độc lập (Builds/Windows/12ConGiap.exe) — chơi thử không cần mở Unity Editor.
///   • Trong Editor: menu "Tools/Zodiac/Xuất bản chơi thử (Windows)".
///   • Ngoài Editor (PowerShell, Unity phải đang tắt): tools/choi-thu.ps1 hoặc bấm đúp ChoiThu.bat ở thư mục gốc —
///     gọi Unity -batchmode -executeMethod ZodiacBuildPlayer.BuildWindowsBatch rồi mở game.
/// Bản build chạy cửa sổ 1600×900 (đổi được cỡ), không toàn màn hình.
/// </summary>
public static class ZodiacBuildPlayer
{
    public const string OutputPath = "Builds/Windows/12ConGiap.exe";

    [MenuItem("Tools/Zodiac/Xuất bản chơi thử (Windows)", true)]
    static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/Zodiac/Xuất bản chơi thử (Windows)")]
    static void BuildMenu()
    {
        if (Build()) EditorUtility.RevealInFinder(OutputPath);
    }

    /// <summary>
    /// Gọi từ dòng lệnh. Tham số thêm: -zodiacRebuild (dựng lại scene bằng Tools/Zodiac/Dựng prototype trước khi build).
    /// Thoát Unity với mã 0 nếu thành công, 1 nếu lỗi.
    /// </summary>
    public static void BuildWindowsBatch()
    {
        bool ok;
        try
        {
            if (Environment.GetCommandLineArgs().Contains("-zodiacRebuild"))
            {
                Debug.Log("[ZodiacBuild] Dựng lại prototype…");
                ZodiacPrototypeBuilder.BuildBatch();
            }
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
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
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
