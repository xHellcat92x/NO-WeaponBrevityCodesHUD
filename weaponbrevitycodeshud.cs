// ==============================================================================================
// HUDWeaponBrevityCodes
// Version 1.2.1
// GUID: com.hellcat92.weaponbrevitycodeshud_1.2.1
// Author: Hellcat92
// Date: 06 August 2026
// ==============================================================================================

using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using TMPro;
using System.Reflection;

namespace HUDWeaponBrevityCodes
{
    public enum BracketStyle
    {
        None,
        Square,
        Round,
        Equals,
        Pipe,
        GreaterLess
    }

    [BepInPlugin("com.hellcat92.weaponbrevitycodeshud", "HUD Weapon Brevity Codes", "1.2.1")]
    public class Plugin : BaseUnityPlugin
    {
        // --- Mod Control ---
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> BrevityLayerEnabled;
        public static ConfigEntry<bool> GunBrevityEnabled;

        // --- Style ---
        public static ConfigEntry<BracketStyle> WeaponBracketStyle;
        public static ConfigEntry<BracketStyle> BrevityBracketStyle;

        public static ConfigEntry<bool> WeaponBracketSpacingEnabled;
        public static ConfigEntry<bool> BrevityBracketSpacingEnabled;

        public static ConfigEntry<Color> HudColor;

        // --- Settings ---
        public static ConfigEntry<int> FontSize;
        public static ConfigEntry<int> ManualSpacingOffset;
        public static ConfigEntry<int> HUDVerticalOffset;

        private void Awake()
        {
            Logger.LogInfo("HUD Weapon Brevity Codes 1.2.1 Loaded");

            // -------------------------
            // CATEGORY: Mod Control
            // -------------------------
            ModEnabled = Config.Bind("Mod Control", "Enable / Disable Mod", true);
            BrevityLayerEnabled = Config.Bind("Mod Control", "Show Brevity Layer", true);

            GunBrevityEnabled = Config.Bind(
                "Mod Control",
                "Include Gun Brevity Code",
                false,
                "Adds the 'GUNS GUNS GUNS' brevity code for gun weapons"
            );

            // -------------------------
            // CATEGORY: Style
            // -------------------------
            WeaponBracketStyle = Config.Bind("Style", "Weapon Bracket Style", BracketStyle.GreaterLess);
            BrevityBracketStyle = Config.Bind("Style", "Brevity Bracket Style", BracketStyle.Square);

            WeaponBracketSpacingEnabled = Config.Bind(
                "Style",
                "Weapon Bracket Spacing Enabled",
                false,
                "Adds spaces inside weapon bracket styles"
            );

            BrevityBracketSpacingEnabled = Config.Bind(
                "Style",
                "Brevity Bracket Spacing Enabled",
                false,
                "Adds spaces inside brevity bracket styles"
            );

            HudColor = Config.Bind(
                "Style",
                "HUD Text Tint",
                new Color(0.30f, 1f, 0.30f, 1f),
                "Tint of the HUD text"
            );

            // -------------------------
            // CATEGORY: Settings
            // -------------------------
            FontSize = Config.Bind("Settings", "Font Size", 16);
            ManualSpacingOffset = Config.Bind("Settings", "Manual Spacing Offset", 20);
            HUDVerticalOffset = Config.Bind("Settings", "HUD Vertical Offset", 230);

            gameObject.AddComponent<HUDWatcher>();
        }

        public static Color32 GetHudColor()
        {
            Color c = HudColor.Value;
            return new Color32(
                (byte)(c.r * 255),
                (byte)(c.g * 255),
                (byte)(c.b * 255),
                (byte)(c.a * 255)
            );
        }
    }

    public class HUDWatcher : MonoBehaviour
    {
        private CombatHUD hud;
        private Aircraft aircraft;
        private Transform hudCenter;

        private BrevityHUDController controller;

        private void Update()
        {
            var newHud = FindObjectOfType<CombatHUD>();
            if (newHud != hud)
            {
                hud = newHud;
                ResetInjection();
            }

            if (hud == null)
                return;

            if (aircraft != hud.aircraft)
            {
                aircraft = hud.aircraft;
                ResetInjection();
            }

            if (aircraft == null)
                return;

            var fh = SceneSingleton<FlightHud>.i;
            if (fh == null)
                return;

            var newCenter = fh.GetHUDCenter();
            if (newCenter != hudCenter)
            {
                hudCenter = newCenter;
                ResetInjection();
            }

            if (hudCenter == null)
                return;

            if (controller == null)
                InjectHUD();
        }

        private void ResetInjection()
        {
            if (controller != null)
            {
                Destroy(controller.gameObject);
                controller = null;
            }
        }

        private void InjectHUD()
        {
            GameObject go = new GameObject("BrevityHUDText");
            go.transform.SetParent(hudCenter, false);

            controller = go.AddComponent<BrevityHUDController>();
            controller.aircraft = aircraft;
        }
    }

    public class BrevityHUDController : MonoBehaviour
    {
        public Aircraft aircraft;

        private TextMeshProUGUI weaponText;
        private TextMeshProUGUI brevityText;

        private void Start()
        {
            weaponText = CreateTMP("WeaponLine");
            brevityText = CreateTMP("BrevityLine");

            // ============================
            // Apply cockpit HUD shader/material
            // ============================
            var hud = FindObjectOfType<CombatHUD>();
            if (hud != null)
            {
                var field = typeof(CombatHUD).GetField("targetInfo",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                var cockpitTMP = field?.GetValue(hud) as TextMeshProUGUI;

                if (cockpitTMP != null)
                {
                    Material cockpitMat = new Material(cockpitTMP.fontSharedMaterial);

                    weaponText.font = cockpitTMP.font;
                    brevityText.font = cockpitTMP.font;

                    weaponText.fontSharedMaterial = cockpitMat;
                    brevityText.fontSharedMaterial = cockpitMat;
                }
            }
        }

        private TextMeshProUGUI CreateTMP(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(this.transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();

            tmp.color = Plugin.GetHudColor();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;

            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(800f, 40f);

            return tmp;
        }

        private void LateUpdate()
        {
            if (!Plugin.ModEnabled.Value)
            {
                weaponText.text = "";
                brevityText.text = "";
                return;
            }

            if (aircraft == null || aircraft.weaponManager == null)
                return;

            weaponText.fontSize = Plugin.FontSize.Value;
            brevityText.fontSize = Plugin.FontSize.Value;

            int baseOffset = Plugin.HUDVerticalOffset.Value;

            weaponText.rectTransform.anchoredPosition = new Vector2(0f, baseOffset);
            brevityText.rectTransform.anchoredPosition = new Vector2(0f, baseOffset - Plugin.ManualSpacingOffset.Value);

            weaponText.color = Plugin.GetHudColor();
            brevityText.color = Plugin.GetHudColor();

            UpdateHUD();
        }

        private void UpdateHUD()
        {
            var station = aircraft.weaponManager.currentWeaponStation;
            if (station == null || station.WeaponInfo == null)
                return;

            var info = station.WeaponInfo;

            string designation = string.IsNullOrEmpty(info.shortName)
                ? info.weaponName
                : info.shortName;

            weaponText.text = ApplyWeaponBracketStyle(designation);

            string brevity = GetBrevity(info.shortName);

            if (Plugin.BrevityLayerEnabled.Value && !string.IsNullOrEmpty(brevity))
                brevityText.text = ApplyBrevityBracketStyle(brevity);
            else
                brevityText.text = "";
        }

        private string ApplyWeaponBracketStyle(string text)
        {
            bool spaced = Plugin.WeaponBracketSpacingEnabled.Value;
            return ApplyBracketStyleInternal(text, Plugin.WeaponBracketStyle.Value, spaced);
        }

        private string ApplyBrevityBracketStyle(string text)
        {
            bool spaced = Plugin.BrevityBracketSpacingEnabled.Value;
            return ApplyBracketStyleInternal(text, Plugin.BrevityBracketStyle.Value, spaced);
        }

        private string ApplyBracketStyleInternal(string text, BracketStyle style, bool spaced)
        {
            string left = "";
            string right = "";

            switch (style)
            {
                case BracketStyle.None: return text;
                case BracketStyle.Square: left = "["; right = "]"; break;
                case BracketStyle.Round: left = "("; right = ")"; break;
                case BracketStyle.Equals: left = "="; right = "="; break;
                case BracketStyle.Pipe: left = "|"; right = "|"; break;
                case BracketStyle.GreaterLess: left = ">"; right = "<"; break;
                default: return text;
            }

            return spaced ? $"{left} {text} {right}" : $"{left}{text}{right}";
        }

        private string GetBrevity(string s)
        {
            switch (s)
            {
                case "IRM-S1":
                case "IRM-S2":
                case "MMR-S3":
                    return "FOX-2";

                case "AAM-29":
                case "AAM-36":
                    return "FOX-3";

                case "ARAD-116":
                    return "MAGNUM";

                case "JAMMER POD":
                    return "MUSIC";

                case "PAB-250":
                case "PAB-125":
                case "GPO-500":
                case "GPO-2P":
                case "Demolition Bomb":
                    return "PAVEWAY";

                case "PAB-80LR":
                case "PAB-250LR":
                    return "BATS";

                case "GBM-500LR":
                    return "PIG";

                case "ASHM-300":
                case "AGM-99":
                    return "BRUISER / GREYHOUND";

                case "AGM-48":
                case "AGM-68":
                case "ATP-1":
                    return "RIFLE";

                case "Eyeball Mk.II":
                    return "CYCLOPS";

                case "AGR-18":
                case "AGR-24":
                    return "SHOTGUN";

                case "Tusko-B":
                    return "LONG RIFLE";

                case "ALM-C450":
                    return "GREYHOUND / BRUISER";

                case "TBM HE":
                    return "COMET";

                case "GPO-N (1.5kt)":
                case "GPO-N (250kt)":
                case "ALND-4 (20kt)":
                    return "RED OCTOBER";

                case "TBM 20kt":
                    return "RED METEOR";

                case "Infantry Squad":
                    return "BOOTS";

                case "GUN 35MM":
                case "GUN 57MM":
                case "GUN 20MM POD":
                case "GUN 30MM ROT":
                case "GUN 12.7MM":
                case "GUN 27MM":
                case "GUN 20MM ROT":
                case "GUN 25MM ROT":
                case "GUN 25MM POD":
                case "GUN 76MM":
                    return Plugin.GunBrevityEnabled.Value ? "GUNS GUNS GUNS" : "";

                default:
                    return "";
            }
        }
    }
}
