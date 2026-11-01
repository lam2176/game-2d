using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dựng scene đăng nhập Zodiac_Login (camera, EventSystem, <see cref="LoginScreen"/>) và đặt nó đứng đầu Build Settings
/// — game mở vào màn đăng nhập trước màn chọn con giáp. Giao diện do LoginScreen dựng lúc chạy.
/// </summary>
public static class ZodiacLoginSceneBuilder
{
    public const string ScenePath = "Assets/Zodiac/Scenes/" + LoginScreen.SceneName + ".unity";

    static readonly Color Background = new Color(0.3f, 0.06f, 0.05f);

    [MenuItem("Tools/Zodiac/Dựng màn đăng nhập", true)]
    static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/Zodiac/Dựng màn đăng nhập")]
    static void BuildInteractive()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build(ZodiacCharacterDefs.EnsureRoster());
        PutFirstInBuildSettings();
    }

    /// <summary>Gọi từ dòng lệnh: Unity -batchmode -executeMethod ZodiacLoginSceneBuilder.BuildBatch -quit</summary>
    public static void BuildBatch()
    {
        Build(ZodiacCharacterDefs.EnsureRoster());
        PutFirstInBuildSettings();
        AssetDatabase.SaveAssets();
    }

    public static void Build(ZodiacRoster roster)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ZodiacPrototypeBuilder.CreateCamera(Vector3.zero, Background);
        var screen = new GameObject("LoginScreen").AddComponent<LoginScreen>();
        screen.roster = roster;
        ZodiacPrototypeBuilder.CreateEventSystem(null);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    public static void PutFirstInBuildSettings()
    {
        var rest = EditorBuildSettings.scenes.Where(s => s.path != ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(rest).ToArray();
    }
}
