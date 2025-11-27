using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class FreeFireMod : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.enhancedmovement";
    public const string PluginName = "EnhancedMovement";
    public const string PluginVersion = "1.1.0";

    internal static new ManualLogSource Logger;

    private Harmony harmony;

    public static ConfigEntry<bool> enableCanFireWhileSprinting;
    public static ConfigEntry<bool> enableCanFireWhileSliding;
    public static ConfigEntry<bool> enableCanAimWhileSliding;
    public static ConfigEntry<bool> enableCanAimWhileReloading;
    public static ConfigEntry<bool> enableCanAimWhileSprinting;
    public static ConfigEntry<bool> enableWallrunning;
    public static ConfigEntry<bool> enableStrafing;
    public static ConfigEntry<bool> enableWingsuitStrafing;

    private void Awake()
    {
        Logger = base.Logger;

        enableCanFireWhileSprinting = Config.Bind("Movement Modifications", "CanFireWhileSprinting", true, "Allows firing weapons while sprinting.");
        enableCanFireWhileSliding = Config.Bind("Movement Modifications", "CanFireWhileSliding", true, "Allows firing weapons while sliding.");
        enableCanAimWhileSliding = Config.Bind("Movement Modifications", "CanAimWhileSliding", true, "Allows aiming weapons while sliding.");
        enableCanAimWhileReloading = Config.Bind("Movement Modifications", "CanAimWhileReloading", true, "Allows aiming weapons while reloading.");
        enableCanAimWhileSprinting = Config.Bind("Movement Modifications", "CanAimWhileSprinting", true, "Allows aiming weapons while sprinting.");
        enableWallrunning = Config.Bind("Movement Modifications", "Wallrunning", true, "Enables wallrunning ability.");
        enableStrafing = Config.Bind("Movement Modifications", "Strafing", true, "Enables improved strafing speed.");
        enableWingsuitStrafing = Config.Bind("Movement Modifications", "WingsuitStrafing", true, "Enables strafing while flying with wingsuit.");

        var configFile = enableCanFireWhileSprinting.ConfigFile;
        var watcher = new FileSystemWatcher(Paths.ConfigPath, $"{PluginGUID}.cfg");
        watcher.Changed += (s, e) => { Logger.LogInfo("Config file changed, reloading"); configFile.Reload(); };
        watcher.EnableRaisingEvents = true;

        var harmony = new Harmony(PluginGUID);

        MethodInfo setupMethod = AccessTools.Method(typeof(Gun), "Setup", new Type[] { typeof(Player), typeof(PlayerAnimation), typeof(IGear) });
        if (setupMethod == null)
        {
            Logger.LogError("Could not find Gun.Setup method!");
            return;
        }
        HarmonyMethod prefix = new HarmonyMethod(typeof(Patches), nameof(Patches.ModifyWeaponPrefix));
        harmony.Patch(setupMethod, prefix: prefix);

        MethodInfo onStartAimMethod = AccessTools.Method(typeof(Gun), "OnStartAim");
        if (onStartAimMethod == null)
        {
            Logger.LogError("Could not find Gun.OnStartAim method!");
            return;
        }
        HarmonyMethod onStartAimPrefix = new HarmonyMethod(typeof(Patches), nameof(Patches.OnStartAimPrefix));
        HarmonyMethod onStartAimPostfix = new HarmonyMethod(typeof(Patches), nameof(Patches.OnStartAimPostfix));
        harmony.Patch(onStartAimMethod, prefix: onStartAimPrefix, postfix: onStartAimPostfix);

        MethodInfo canAimMethod = AccessTools.Method(typeof(Gun), "CanAim");
        if (canAimMethod == null)
        {
            Logger.LogError("Could not find Gun.CanAim method!");
            return;
        }
        HarmonyMethod canAimPrefix = new HarmonyMethod(typeof(Patches), nameof(Patches.CanAimPrefix));
        harmony.Patch(canAimMethod, prefix: canAimPrefix);

        MethodInfo getterMethod = AccessTools.PropertyGetter(typeof(Player), "EnableWallrun");
        if (getterMethod == null)
        {
            return;
        }
        HarmonyMethod wallrunPrefix = new HarmonyMethod(typeof(Patches), nameof(Patches.EnableWallrunGetPrefix));
        harmony.Patch(getterMethod, prefix: wallrunPrefix);

        // Patch strafing
        harmony.PatchAll(typeof(StrafingSpeedPatch));

        // Patch wingsuit
        harmony.PatchAll(typeof(WingsuitStrafingPatch));

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }
}

public class ModGunData : MonoBehaviour
{
    public bool CanAimWhileSprinting { get; set; }
    public bool WasSprinting { get; set; }
}

static class Patches
{
    private static readonly FieldInfo lockSprintingField = AccessTools.Field(typeof(Gun), "lockSprinting");

    public static void ModifyWeaponPrefix(Gun __instance, IGear prefab)
    {
        var modGunData = __instance.gameObject.AddComponent<ModGunData>();
        ModifyGunBaseStats(prefab, modGunData);
    }

    private static void ModifyGunBaseStats(IGear prefab, ModGunData modGunData)
    {
        if (prefab == null || prefab is not Gun gunPrefab) return;

        ref var gunData = ref gunPrefab.GunData;

        gunData.fireConstraints.canFireWhileSprinting = (FireConstraints.ActionFireMode)(FreeFireMod.enableCanFireWhileSprinting.Value ? 1 : 0);
        gunData.fireConstraints.canFireWhileSliding = (FireConstraints.ActionFireMode)(FreeFireMod.enableCanFireWhileSliding.Value ? 1 : 0);
        gunData.fireConstraints.canAimWhileSliding = (FireConstraints.ActionFireMode)(FreeFireMod.enableCanAimWhileSliding.Value ? 1 : 0);
        gunData.fireConstraints.canAimWhileReloading = FreeFireMod.enableCanAimWhileReloading.Value;

        modGunData.CanAimWhileSprinting = FreeFireMod.enableCanAimWhileSprinting.Value;

        bool lockSprintingValue = !FreeFireMod.enableCanAimWhileSprinting.Value;
        lockSprintingField.SetValue(gunPrefab, lockSprintingValue);
    }

    public static bool OnStartAimPrefix(Gun __instance)
    {
        var modGunData = __instance.gameObject.GetComponent<ModGunData>();
        if (modGunData != null && modGunData.CanAimWhileSprinting)
        {
            FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null)
            {
                PropertyInfo isSprintingProp = AccessTools.Property(typeof(Player), "IsSprinting");
                if (isSprintingProp != null)
                {
                    modGunData.WasSprinting = (bool)isSprintingProp.GetValue(player);
                }
            }
        }
        return true;
    }

    public static void OnStartAimPostfix(Gun __instance)
    {
        var modGunData = __instance.gameObject.GetComponent<ModGunData>();
        if (modGunData != null && modGunData.CanAimWhileSprinting && modGunData.WasSprinting)
        {
            FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null)
            {
                MethodInfo resumeSprintMethod = AccessTools.Method(typeof(Player), "ResumeSprint");
                if (resumeSprintMethod != null)
                {
                    resumeSprintMethod.Invoke(player, null);
                }
            }
            modGunData.WasSprinting = false;
        }
    }

    public static bool CanAimPrefix(Gun __instance, ref bool __result)
    {
        var modGunData = __instance.gameObject.GetComponent<ModGunData>();
        if (modGunData != null && modGunData.CanAimWhileSprinting)
        {
            FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null)
            {
                PropertyInfo isSprintingProp = AccessTools.Property(typeof(Player), "IsSprinting");
                if (isSprintingProp != null && (bool)isSprintingProp.GetValue(player))
                {
                    FieldInfo isAimInputHeldField = AccessTools.Field(typeof(Gun), "isAimInputHeld");
                    bool isAimInputHeld = isAimInputHeldField != null ? (bool)isAimInputHeldField.GetValue(__instance) : false;
                    if (isAimInputHeld)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
        }
        return true;
    }



    public static bool EnableWallrunGetPrefix(Player __instance, ref bool __result)
    {
        if (!__instance.IsLocalPlayer) return true;

        if (!FreeFireMod.enableWallrunning.Value) return true;

        __result = true;
        return false;
    }
}

[HarmonyPatch]
public static class WingsuitStrafingPatch
{
    [HarmonyPatch(typeof(Wingsuit), "OnJumpPressed")]
    [HarmonyPostfix]
    public static void OnJumpPressedPostfix(Wingsuit __instance)
    {
        if (!FreeFireMod.enableWingsuitStrafing.Value) return;
        var dataField = __instance.GetType().GetField("wingsuitData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dataField == null) return;

        var data = dataField.GetValue(__instance);
        if (data == null) return;

        var lockField = data.GetType().GetField("lockFlyDirection",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (lockField == null || !(bool)lockField.GetValue(data)) return;

        var playerField = __instance.GetType().GetField("player",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerField == null) return;

        var player = playerField.GetValue(__instance);
        if (player == null) return;

        Vector2 moveInput = PlayerInput.MoveInput();
        if (moveInput.magnitude < 0.1f) return;

        var isFlyingField = __instance.GetType().GetField("isFlying",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isFlyingField != null && (bool)isFlyingField.GetValue(__instance))
        {
            Vector3 normalizedInput = new Vector3(moveInput.x, moveInput.y, 0f).normalized;

            var flySpeedField = data.GetType().GetField("flySpeed",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (flySpeedField == null) return;

            float baseFlySpeed = (float)flySpeedField.GetValue(data);
            float strafeForce = Mathf.Abs(normalizedInput.x) * baseFlySpeed * 0.5f;

            if (strafeForce > 0)
            {
                Vector3 strafeVector = __instance.transform.right * (normalizedInput.x > 0 ? 1 : -1) * strafeForce;
                var addForceMethod = player.GetType().GetMethod("AddForce", new System.Type[] { typeof(Vector3) });
                if (addForceMethod != null)
                {
                    addForceMethod.Invoke(player, new object[] { strafeVector });
                }
            }
        }
    }

    [HarmonyPatch(typeof(Wingsuit), "FixedUpdate")]
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(Wingsuit __instance)
    {
        if (!FreeFireMod.enableWingsuitStrafing.Value) return;
        var isFlyingField = __instance.GetType().GetField("isFlying",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isFlyingField == null || !(bool)isFlyingField.GetValue(__instance)) return;

        Vector2 moveInput = PlayerInput.MoveInput();
        if (moveInput.magnitude < 0.01f) return;

        Vector3 normalizedMoveInput = new Vector3(moveInput.x, moveInput.y, 0f);
        float inputMagnitude = normalizedMoveInput.magnitude;
        if (inputMagnitude > 0)
        {
            normalizedMoveInput /= inputMagnitude;
        }

        var dataField = __instance.GetType().GetField("wingsuitData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dataField == null) return;

        var data = dataField.GetValue(__instance);
        if (data == null) return;

        var flySpeedField = data.GetType().GetField("flySpeed",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var flySpeedCurveField = data.GetType().GetField("flySpeedCurve",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var flySpeedCurveDurationField = data.GetType().GetField("flySpeedCurveDuration",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (flySpeedField == null || flySpeedCurveField == null) return;

        float baseFlySpeed = (float)flySpeedField.GetValue(data);
        var flySpeedCurve = flySpeedCurveField.GetValue(data) as AnimationCurve;
        float flySpeedCurveDuration = (flySpeedCurveDurationField != null)
            ? (float)flySpeedCurveDurationField.GetValue(data)
            : 1f;

        var flyStartTimeField = __instance.GetType().GetField("flyStartTime",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (flyStartTimeField == null) return;

        float flyStartTime = (float)flyStartTimeField.GetValue(__instance);
        float timeRatio = Mathf.Min((Time.time - flyStartTime) / flySpeedCurveDuration, 1f);
        float curveMultiplier = flySpeedCurve.Evaluate(timeRatio);

        var playerField = __instance.GetType().GetField("player",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerField == null) return;

        var player = playerField.GetValue(__instance);
        if (player == null) return;

        float strafeComponent = normalizedMoveInput.x;
        if (Mathf.Abs(strafeComponent) > 0.1f)
        {
            float strafeMagnitude = baseFlySpeed * curveMultiplier * 2f;
            Vector3 strafeDirection = __instance.transform.right * strafeComponent;
            Vector3 strafeForce = strafeDirection * strafeMagnitude;

            var addForceMethod = player.GetType().GetMethod("AddForce", new System.Type[] { typeof(Vector3) });
            if (addForceMethod != null)
            {
                addForceMethod.Invoke(player, new object[] { strafeForce });
            }
        }
    }
}

[HarmonyPatch]
public static class StrafingSpeedPatch
{
    [HarmonyTargetMethod]
    public static System.Reflection.MethodBase TargetMethod()
    {
        var playerType = System.Type.GetType("Pigeon.Movement.Player, Assembly-CSharp");
        if (playerType == null)
        {
            return null;
        }

        var awakeMethod = playerType.GetMethod("Awake",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (awakeMethod != null)
        {
            return awakeMethod;
        }

        var startMethod = playerType.GetMethod("Start",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (startMethod != null)
        {
            return startMethod;
        }

        return null;
    }

    [HarmonyPostfix]
    public static void Postfix(object __instance)
    {
        if (!FreeFireMod.enableStrafing.Value) return;
        var playerType = __instance.GetType();

        var strafeSpeedMultiplierField = playerType.GetField("strafeSpeedMultiplier",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var strafeSpeedMultiplierWhileMovingField = playerType.GetField("strafeSpeedMultiplierWhileMoving",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (strafeSpeedMultiplierField != null)
        {
            strafeSpeedMultiplierField.SetValue(__instance, 1.0f);
        }

        if (strafeSpeedMultiplierWhileMovingField != null)
        {
            strafeSpeedMultiplierWhileMovingField.SetValue(__instance, 1.0f);
        }
    }
}
