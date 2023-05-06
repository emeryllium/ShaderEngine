using MelonLoader;
using BTD_Mod_Helper;
using ShaderEngine;
using UnityEngine;
using BTD_Mod_Helper.Api.ModOptions;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Settings;
using Il2CppAssets.Scripts.Unity.UI_New.ChallengeEditor;

[assembly: MelonInfo(typeof(ShaderEngine.ShaderEngine), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace ShaderEngine;

public class ShaderEngine : BloonsTD6Mod
{
    public static Dictionary<string, Dictionary<string, object>> settings;
    public static Dictionary<Material, bool> materials;
    public static AssetBundle bundle;
    public static ShaderEngine se;

    public override void OnApplicationStart()
    {
        if(!Directory.Exists(Directory.GetCurrentDirectory() + "\\Mods\\ShaderEngine\\"))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Mods\\ShaderEngine\\");
        }

        se = this;
        ReloadShaders(true);

        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<ShaderEngine_CameraBehavior>();
        ModHelper.Msg<ShaderEngine>("ShaderEngine loaded.");
    }

    public static void sReloadShaders(bool initSettings)
    {
        se.ReloadShaders(initSettings);
    }

    public void ReloadShaders(bool initSettings)
    {
        settings = new Dictionary<string, Dictionary<string, object>>();
        materials = new Dictionary<Material, bool>();

        Dictionary<string, object> savedSettings = new Dictionary<string, object>();

        if (File.Exists(Directory.GetCurrentDirectory() + "\\Mods\\BloonsTD6 Mod Helper\\Mod Settings\\ShaderEngine.json"))
        {
            string json = File.ReadAllText(Directory.GetCurrentDirectory() + "\\Mods\\BloonsTD6 Mod Helper\\Mod Settings\\ShaderEngine.json");
            savedSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory() + "\\Mods\\ShaderEngine\\").Where(f => Path.GetExtension(f).Equals(".json")))
        {
            string json = File.ReadAllText(file);
            Dictionary<string, Dictionary<string, object>>? shaders = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);

            if (shaders == null) continue;

            if(bundle == null)
                bundle = AssetBundle.LoadFromMemory(Il2CppSystem.IO.File.ReadAllBytes(Path.ChangeExtension(file, "assets")));

            foreach (string shader in shaders.Keys)
            {
                settings.Add(shader, shaders[shader]);
                Material mat = new Material(bundle.LoadAsset(shader).Cast<Shader>());

                ModSettingCategory category = new ModSettingCategory(Regex.Replace(shader, "([a-z])([A-Z])", "$1 $2"));

                bool enabled = false;
                if (savedSettings.ContainsKey(shader + "_Enabled"))
                {
                    enabled = (bool)savedSettings[shader + "_Enabled"];
                }

                if(initSettings)
                    ModSettings.Add(shader + "_Enabled", new ModSettingBool(enabled) { category = category });
                materials.Add(mat, enabled);

                foreach (string setting in shaders[shader].Keys)
                {
                    string name = shader + setting;

                    object s;

                    if (savedSettings.ContainsKey(name))
                    {
                        s = savedSettings[name];
                    }
                    else
                    {
                        s = shaders[shader][setting];
                    }

                    if (s is long l)
                    {
                        if(initSettings)
                            ModSettings.Add(name, new ModSettingInt((int)l) { displayName = Regex.Replace(setting, "([a-z])([A-Z])", "$1 $2").Replace("_", ""), category = category });
                        mat.SetInt(setting, (int)l);
                    }
                    if (s is double d)
                    {
                        if(initSettings)
                            ModSettings.Add(name, new ModSettingDouble(d) { displayName = Regex.Replace(setting, "([a-z])([A-Z])", "$1 $2").Replace("_", ""), category = category });
                        mat.SetFloat(setting, (float)d);
                    }
                }
            }
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if(Camera.allCameras.Count > 0)
        {
            foreach(Camera cam in Camera.allCameras)
            {
                if (!cam.gameObject.TryGetComponent(out ShaderEngine_CameraBehavior t))
                {
                    cam.gameObject.AddComponent<ShaderEngine_CameraBehavior>();
                }
            }
        }
        foreach(Material mat in materials.Keys)
        {
            if (mat == null)
            {
                ReloadShaders(false);
                break;
            }
        }
    }

    [HarmonyPatch(typeof(ExtraSettingsScreen), nameof(ExtraSettingsScreen.Close))]
    class SettingsPatch
    {
        static void Prefix()
        {
            sReloadShaders(false);
        }
    }
}