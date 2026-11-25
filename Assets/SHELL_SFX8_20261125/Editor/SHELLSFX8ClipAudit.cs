// Safe audio import audit. Does not alter scene, prefab or game scripts.
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SHELLSFX8ClipAudit
{
    private static readonly string[] names = {
        "11_lever_on", "12_lever_off", "16_platform_move", "17_checkpoint", "18_menu_hover"
    };
    private const string Prefix = "Assets/SHELL_SFX8_20261125/Resources/SHELL_SFX8/";

    [MenuItem("Tools/SHELL/SFX 5/Verify 5 WAV files")]
    private static void Verify()
    {
        int ok = 0;
        var report = new StringBuilder();
        report.AppendLine("[SHELL SFX5] WAV import audit");
        foreach (string name in names)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Prefix + name + ".wav");
            if (clip == null) report.AppendLine("MISSING: " + name);
            else
            {
                ++ok;
                report.AppendLine("OK: " + name + " (" + clip.length.ToString("F2") + "s)");
            }
        }
        report.AppendLine("Total " + ok + "/5. Import does not prove gameplay wiring.");
        if (ok == 5) Debug.Log(report.ToString());
        else Debug.LogError(report.ToString());
        EditorUtility.DisplayDialog("SHELL SFX 5 audit", ok + "/5 WAV files imported. See Console.", "OK");
    }
}
