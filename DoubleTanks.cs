using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;

namespace DoubleTanks
{
    public class DoubleTanks : ModBehaviour
    {
        public static bool DoubleOxygen { get; private set; } = true;
        public static bool DoubleFuel { get; private set; } = true;
        public static bool DoubleBoostDuration { get; private set; } = true;
        public static bool DoubleAirBoostRegen { get; private set; } = true;

        private void Start()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Configure(IModConfig config)
        {
            DoubleOxygen = config.GetSettingsValue<bool>("DoubleOxygen");
            DoubleFuel = config.GetSettingsValue<bool>("DoubleFuel");
            DoubleBoostDuration = config.GetSettingsValue<bool>("DoubleBoostDuration");
            DoubleAirBoostRegen = config.GetSettingsValue<bool>("DoubleAirBoostRegen");
        }
    }

    [HarmonyPatch(typeof(PlayerResources))]
    public static class PlayerResourcesPatch
    {
        private static float _lastFuel = -1f;
        private static float _lastOxygen = -1f;

        private static JetpackThrusterModel _jetpack;
        private static float _baseBoostSeconds = -1f;
        private static float _lastBoostFraction = -1f;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerResources.Awake))]
        public static void Awake_Postfix(PlayerResources __instance)
        {
            _lastFuel = __instance._currentFuel;
            _lastOxygen = __instance._currentOxygen;
            _jetpack = null;
            _baseBoostSeconds = -1f;
            _lastBoostFraction = -1f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerResources.Update))]
        public static void Update_Postfix(PlayerResources __instance)
        {
            // 1. 2x Fuel Duration
            if (DoubleTanks.DoubleFuel && __instance._currentFuel < _lastFuel)
            {
                float spent = _lastFuel - __instance._currentFuel;
                __instance._currentFuel += spent * 0.5f;
            }
            _lastFuel = __instance._currentFuel;

            // 2. 2x Oxygen Duration
            if (DoubleTanks.DoubleOxygen && __instance._currentOxygen < _lastOxygen)
            {
                float spent = _lastOxygen - __instance._currentOxygen;
                __instance._currentOxygen += spent * 0.5f;
            }
            _lastOxygen = __instance._currentOxygen;

            // 3. Locate JetpackThrusterModel once per loop
            if (_jetpack == null)
            {
                _jetpack = Object.FindObjectOfType<JetpackThrusterModel>();
                if (_jetpack != null)
                {
                    _baseBoostSeconds = _jetpack._boostSeconds;
                    _lastBoostFraction = _jetpack._boostChargeFraction;
                }
            }

            if (_jetpack != null)
            {
                // Apply 2x Boost Duration
                if (_baseBoostSeconds > 0f)
                {
                    _jetpack._boostSeconds = DoubleTanks.DoubleBoostDuration
                        ? _baseBoostSeconds * 2.0f
                        : _baseBoostSeconds;
                }

                // Apply 2x Air Boost Regen
                if (DoubleTanks.DoubleAirBoostRegen && !_jetpack._boostActivated)
                {
                    float gained = _jetpack._boostChargeFraction - _lastBoostFraction;
                    if (gained > 0f && _jetpack._boostChargeFraction < 1f)
                    {
                        _jetpack._boostChargeFraction = Mathf.Min(1f, _jetpack._boostChargeFraction + gained);
                    }
                }

                _lastBoostFraction = _jetpack._boostChargeFraction;
            }
        }
    }
}