using HarmonyLib;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityModManagerNet;

namespace TweakableWeaponCategories
{
    public class Settings : UnityModManager.ModSettings
    {
        public class Category
        {
            public WeaponCategory Type;
            public bool Visible = false;
            public List<WeaponSubCategory> SubCategories;
        }

        public Category[] WeaponCategories;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    public class TweakableWeaponCategories
    {
        public static Settings Settings;
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool Enabled;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Settings = Settings.Load<Settings>(modEntry);

            if (Settings.WeaponCategories == null)
            {
                Settings.WeaponCategories = WeaponCategoryExtension.Data.Select(i => new Settings.Category
                {
                    Type = i.Category,
                    SubCategories = ((WeaponSubCategory[])i.SubCategories.Clone()).ToList()
                }).ToArray();
            }

            Array.Sort(Settings.WeaponCategories, (a, b) => string.Compare(a.Type.ToString(), b.Type.ToString()));

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            Logger = modEntry.Logger;

            Harmony harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }

        public static bool HasSubCategory(WeaponCategory category, WeaponSubCategory sub_category)
        {
            if (sub_category == WeaponSubCategory.None) return true;
            Settings.Category settings_category = Settings.WeaponCategories.FirstOrDefault(i => i.Type == category);
            return settings_category != null && settings_category.SubCategories.Contains(sub_category);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Array subcategories = Enum.GetValues(typeof(WeaponSubCategory));

            foreach (Settings.Category category in Settings.WeaponCategories)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($" {category.Type}");
                GUILayout.FlexibleSpace();
                category.Visible = GUILayout.Toggle(category.Visible, "");
                GUILayout.EndHorizontal();

                if (category.Visible)
                {
                    for (int subcategory_idx = 0; subcategory_idx < subcategories.Length; ++subcategory_idx)
                    {
                        GUILayout.BeginHorizontal();

                        WeaponSubCategory subcategory = (WeaponSubCategory)subcategory_idx;
                        bool last_value = HasSubCategory(category.Type, subcategory);
                        bool value = GUILayout.Toggle(last_value, $" {subcategory}", GUILayout.ExpandWidth(false));

                        if (last_value != value)
                        {
                            if (value)
                            {
                                category.SubCategories.Add(subcategory);
                            }
                            else
                            {
                                category.SubCategories.Remove(subcategory);
                            }
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(8);
                }
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }

    [HarmonyPatch(typeof(WeaponCategoryExtension), nameof(WeaponCategoryExtension.HasSubCategory))]
    public static class WeaponCategoryExtension_HasSubCategory
    {
        [HarmonyPostfix]
        public static void Postfix(WeaponCategory category, WeaponSubCategory subCategory, ref bool __result)
        {
            if (TweakableWeaponCategories.Enabled)
            {
                __result = TweakableWeaponCategories.HasSubCategory(category, subCategory);
            }
        }
    }

    // This patch redirects a call to UnitProficiency.Contains to our own stub.
    // Each proficiency feature contains a list of weapons that are allowed. This is different from weapon subcategories.
    // We will make them the same, e.g. if the subcategory includes simple, then simple weapon proficiency allows it to be equipped.
    [HarmonyPatch(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.CanBeEquippedInternal))]
    public static class ItemEntityWeapon_CanBeEquippedInternal
    {
        private static MethodInfo Method__UnityProficiency_Contains = AccessTools.Method(typeof(UnitProficiency), nameof(UnitProficiency.Contains), new Type[] { typeof(WeaponCategory) });
        private static MethodInfo Method__CanBeEquipped = AccessTools.Method(typeof(ItemEntityWeapon_CanBeEquippedInternal), nameof(ItemEntityWeapon_CanBeEquippedInternal.CanBeEquipped));

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].Calls(Method__UnityProficiency_Contains))
                {
                    il[i].opcode = OpCodes.Ldarg_1; // load owner
                    il.Insert(i + 1, new CodeInstruction(OpCodes.Call, Method__CanBeEquipped));
                }
            }

            return il.AsEnumerable();
        }

        public static bool HasFeature(this UnitDescriptor _this, string guid)
        {
            return _this.Unit.Facts.Get<Feature>((i) => i.Blueprint.AssetGuid == guid) != null;
        }

        private static bool CanBeEquipped(UnitProficiency _, WeaponCategory category, UnitDescriptor owner)
        {
            if (TweakableWeaponCategories.Enabled)
            {
                bool allowed_simple = TweakableWeaponCategories.HasSubCategory(category, WeaponSubCategory.Simple) && owner.HasFeature("e70ecf1ed95ca2f40b754f1adb22bbdd");
                bool allowed_martial = TweakableWeaponCategories.HasSubCategory(category, WeaponSubCategory.Martial) && owner.HasFeature("203992ef5b35c864390b4e4a1e200629");
                bool allowed_monk = TweakableWeaponCategories.HasSubCategory(category, WeaponSubCategory.Monk) && owner.HasFeature("c7d6f5244c617734a8a76b6785a752b4");
                return allowed_simple || allowed_martial || allowed_monk;
            }

            return owner.Proficiencies.Contains(category);
        }
    }

    // This is an patch to change the BlueprintItemWeapon::IsMonk call - this is because the code never actually checks
    // if a weapon has the Monk subcategory, it only checks whether the blueprint is flagged as a monk item.
    [HarmonyPatch(typeof(MonkNoArmorAndMonkWeaponFeatureUnlock), nameof(MonkNoArmorAndMonkWeaponFeatureUnlock.CheckEligibility))]
    public static class Minimap_SetMapMode
    {
        private static MethodInfo Method__IsMonk = AccessTools.PropertyGetter(typeof(BlueprintItemWeapon), nameof(BlueprintItemWeapon.IsMonk));
        private static MethodInfo Method__IsMonkWeapon = AccessTools.Method(typeof(Minimap_SetMapMode), nameof(Minimap_SetMapMode.IsMonkWeapon));

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            foreach (CodeInstruction inst in il)
            {
                if (inst.Calls(Method__IsMonk))
                {
                    inst.opcode = OpCodes.Call;
                    inst.operand = Method__IsMonkWeapon;
                }
            }

            return il.AsEnumerable();
        }

        private static bool IsMonkWeapon(BlueprintItemWeapon instance)
        {      
            return TweakableWeaponCategories.Enabled ? TweakableWeaponCategories.HasSubCategory(instance.Category, WeaponSubCategory.Monk) : instance.IsMonk;
        }
    }
}

