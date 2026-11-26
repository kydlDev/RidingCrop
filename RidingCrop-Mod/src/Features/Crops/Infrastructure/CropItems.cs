using System;
using System.Collections.Generic;
using System.Linq;
using KydlCore.Common;
using KydlCore.Features.Items.Contracts;
using UnityEngine;
using RidingCrop.Core;

namespace RidingCrop;

/// <summary>
/// Makes the seven crops: a COPY of the vanilla Club for each tier (same animations and base stats), with the Club's mesh swapped for ours,
/// a material from Valheim's item shader plus the tier's colour texture, the tier's stats (<see cref="CropBalance"/>), its recipe, a bleed
/// effect, the sounds and an inventory icon rendered at runtime.
/// </summary>
internal static class CropItems
{
    // Hand alignment: fixed values. Rotation is in degrees around the crop model's own axes; GripOffset is in metres from the model's
    // original grip point toward the tip; Scale 1 = as modelled (1 metre long).
    private const float RotationX = 270f;
    private const float RotationY = 180f;
    private const float RotationZ = 0f;
    private const float GripOffset = 0.18f;
    private const float ModelScale = 1f;

    // Inventory icon: crop standing upright, then tipped this many degrees to the right (clockwise).
    private const float IconTiltDegrees = 45f;

    // The visual objects we created; we rotate THESE (not Valheim's "attach" node, which the game resets on equip).
    private static readonly List<Transform> Visuals = new List<Transform>();

    /// <summary>The registered items (for the test hooks).</summary>
    internal static List<GameObject> Items { get; } = new List<GameObject>();

    private static bool _done;

    /// <summary>Adds every crop. Runs once, when the vanilla prefabs exist.</summary>
    internal static void AddAll()
    {
        if (_done)
        {
            ModLog.Verbose($"CropItems.AddAll: already done, nothing to repeat - items={Items.Count}");
            { if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.AddAll: returns because the crops were already added (done={_done}, items={Items.Count}) (line 43)"); return; }
        }

        _done = true;

        for (int i = 0; i < CropTable.All.Length; i++)
        {
            try
            {
                AddTier(CropTable.All[i], i);
            }
            catch (Exception ex)
            {
                ModLog.Error("CropItems: could not add '" + CropTable.All[i].PrefabName + "': " + ex);
            }
        }

        // Now (after every icon was rendered un-rotated) turn the models into their hand pose.
        ApplyAlignment();

        if (Items.Count < CropTable.All.Length)
        {
            ModLog.Error("CropItems: Riding Crop family INCOMPLETE: only " + Items.Count + " of " + CropTable.All.Length + " item(s) registered. See the errors above.");
        }
        else
        {
            ModLog.Info("CropItems: Riding Crop family registered: " + Items.Count + " of " + CropTable.All.Length + " item(s).");
        }
    }

    private static string StationName(CropStation station)
    {
        if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.StationName(station={station})");
        switch (station)
        {
            case CropStation.Forge: { if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.StationName: returns \"forge\" (line 78) - inputs (station={station})"); return "forge"; }
            case CropStation.BlackForge: { if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.StationName: returns \"blackforge\" (line 79) - inputs (station={station})"); return "blackforge"; }
            default: { if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.StationName: returns \"piece_workbench\" (line 80) - inputs (station={station})"); return "piece_workbench"; }
        }
    }

    private static void AddTier(CropDef tier, int index)
    {
        // No silent fallback to another tier's texture: that is how crops ended up looking identical before.
        if (!CropAssets.Albedos.ContainsKey(tier.AlbedoName))
        {
            ModLog.Error("CropItems: skipping '" + tier.PrefabName + "' (tier " + index + "): texture '" + tier.AlbedoName + "' is not in the asset bundle.");
            return;
        }

        KyResource[] requirements = tier.Ingredients.Select(i => new KyResource(i.Item, i.Amount) { AmountPerLevel = i.UpgradeAmount }).ToArray();
        var recipe = new KyRecipe { CraftingStation = StationName(tier.Station), MinStationLevel = tier.StationLevel, Amount = 1, Resources = requirements };

        GameObject prefab = KyPrefabs.Clone(tier.PrefabName, CropTable.VanillaBasePrefab);
        ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        if (drop == null)
        {
            ModLog.Error("CropItems: could not clone vanilla prefab '" + CropTable.VanillaBasePrefab + "' (prefab=" + (prefab != null) + "). '" + tier.PrefabName + "' not added.");
            return;
        }

        ApplyVisuals(prefab, tier);
        ModLog.Verbose("CropItems: '" + tier.PrefabName + "': station " + tier.Station + " level " + tier.StationLevel + " (quality 1), ingredients: "
            + string.Join(", ", requirements.Select(r => r.Amount + " " + r.Item + " (+" + r.AmountPerLevel + "/upgrade)").ToArray()));

        ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
        shared.m_name = tier.DisplayName;
        shared.m_description = tier.Description;
        drop.m_itemData.m_dropPrefab = prefab;

        // All crops train (and are boosted by) the same "RidingCrop" skill instead of the Club's skill.
        if (CropSkill.Type != Skills.SkillType.None)
        {
            shared.m_skillType = CropSkill.Type;
        }

        SE_RidingCropBleed bleed = ApplyBalance(shared, tier, index);
        CropSounds.Apply(shared);

        RenderIcon(prefab, shared, bleed, tier);

        KyPrefabs.Add(prefab);
        KyResult result = KyItems.Contribute(new KyContribution
        {
            Id = "ridingcrop:" + tier.PrefabName.ToLowerInvariant(),
            ModGuid = Plugin.ModGuid,
            DisplayName = tier.DisplayName,
            Prefab = prefab,
            Kind = KyKind.Item,
            Recipe = recipe,
            RegisterInGame = true,
            Tags = new[] { "RidingCrop", "weapon" },
        });
        if (!result.Accepted)
        {
            ModLog.Error("CropItems: KY-Items refused '" + tier.PrefabName + "': " + result.Reason);
            return;
        }

        Items.Add(prefab);
        ModLog.Verbose("CropItems: '" + tier.DisplayName + "' (" + tier.PrefabName + ") registered as tier " + index);
    }

    // Inventory icon: a miniature of the crop (just its mesh + this tier's material), standing upright and then tilted to the right.
    // The icon camera (KyIcons, same view as Jotunn's) looks along -Z with screen-right = world -X, so rolling the model around Z by a POSITIVE angle tips its top
    // toward the right of the icon. If this fails the item still works, it just keeps the Club's icon.
    private static void RenderIcon(GameObject itemPrefab, ItemDrop.ItemData.SharedData shared, SE_RidingCropBleed bleed, CropDef tier)
    {
        GameObject iconSource = null;
        try
        {
            iconSource = CreateIconSource(itemPrefab) ?? itemPrefab;
            Sprite icon = KyIcons.Render(new KyIconRequest(iconSource)
            {
                Width = 128,
                Height = 128,
                Rotation = Quaternion.Euler(0f, 0f, IconTiltDegrees),
            });
            if (icon != null)
            {
                shared.m_icons = new[] { icon };
                if (bleed != null)
                {
                    bleed.m_icon = icon; // shown in the status list of a bleeding player (PvP)
                }

                // No separate skill icon exists, so the skill uses the first crop's inventory icon that rendered successfully.
                // The game reads the icon when it creates the player's Skills object, which happens after this point.
                CropSkill.SetIcon(icon);

                ModLog.Verbose("CropItems: '" + tier.PrefabName + "' icon rendered (tilt " + IconTiltDegrees + " degrees)");
            }
            else
            {
                ModLog.Warn("CropItems: icon render returned nothing; '" + tier.PrefabName + "' keeps the Club's icon.");
            }
        }
        catch (Exception ex)
        {
            ModLog.Warn("CropItems: icon render failed; '" + tier.PrefabName + "' keeps the Club's icon. " + ex.Message);
        }
        finally
        {
            if (iconSource != null && iconSource != itemPrefab)
            {
                UnityEngine.Object.DestroyImmediate(iconSource);
            }
        }
    }

    // A bare object with only our mesh and the tier's material, no parents and no inherited Club transforms, so the icon always
    // shows the crop upright (long axis = up) before the tilt is applied. Returns null if the visual is missing.
    private static GameObject CreateIconSource(GameObject itemPrefab)
    {
        if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.CreateIconSource(itemPrefab={itemPrefab})");
        MeshRenderer visual = itemPrefab.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.gameObject.name == "RidingCropVisual");
        if (visual == null || CropAssets.Mesh == null)
        {
            { if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.CreateIconSource: returns null (line 201) - inputs (itemPrefab={itemPrefab})"); return null; }
        }

        var go = new GameObject("RidingCropIconSource");
        go.AddComponent<MeshFilter>().sharedMesh = CropAssets.Mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = visual.sharedMaterial;
        { var returnedAt207 = go; if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.CreateIconSource: returns {returnedAt207} (line 207)"); return returnedAt207; }
    }

    // Every crop deals ONLY blunt damage + a bleed (see CropBalance for the ladder). Valheim has no bleed damage type, so the bleed is a
    // custom status effect (SE_RidingCropBleed) that the game applies to whatever the crop hits (SharedData.m_attackStatusEffect).
    // Each tier needs its own effect because the damage per second differs.
    private static SE_RidingCropBleed ApplyBalance(ItemDrop.ItemData.SharedData shared, CropDef tier, int index)
    {
        float blunt = CropBalance.Blunt(index);
        float bleedPerSecond = CropBalance.BleedPerSecond(index);
        float maxDurability = CropBalance.Durability(index);

        float clubBlunt = shared.m_damages.m_blunt;
        shared.m_damages = new HitData.DamageTypes { m_blunt = blunt };   // blunt only: drops anything else the Club had
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_blunt = CropBalance.BluntPerLevel };
        shared.m_maxQuality = CropBalance.MaxQuality;

        float clubDurability = shared.m_maxDurability;
        shared.m_maxDurability = maxDurability;
        shared.m_durabilityPerLevel = CropBalance.DurabilityPerLevel;

        // Combat stats. No secondary attack: an Attack with no animation counts as "none".
        shared.m_secondaryAttack = new Attack();
        shared.m_attack.m_attackStamina = CropBalance.Stamina(index);
        shared.m_attack.m_staggerMultiplier = CropBalance.StaggerMultiplier(index);   // stagger damage = blunt x this
        shared.m_attack.m_lastChainDamageMultiplier = CropBalance.LastChainDamageMultiplier;
        shared.m_attackForce = CropBalance.Knockback(index);
        shared.m_blockPower = CropBalance.BlockArmor(index);
        shared.m_timedBlockBonus = CropBalance.ParryBonus;
        shared.m_deflectionForce = CropBalance.BlockForce(index);

        SE_RidingCropBleed bleed = null;
        if (bleedPerSecond > 0f)
        {
            bleed = CreateBleed(tier, bleedPerSecond);
            if (bleed != null)
            {
                shared.m_attackStatusEffect = bleed;
                shared.m_attackStatusEffectChance = 1f; // every hit makes the target bleed
            }
        }

        ModLog.Verbose("CropItems: '" + tier.PrefabName + "': blunt " + blunt.ToString("0.#") + " (Club " + clubBlunt.ToString("0.#") + "), bleed "
            + bleedPerSecond.ToString("0.#") + "/s for " + CropBalance.BleedDurationSeconds.ToString("0.#") + "s, +" + CropBalance.BluntPerLevel.ToString("0.#")
            + " blunt per upgrade level (max quality " + CropBalance.MaxQuality + ": " + CropBalance.BluntAtQuality(index, CropBalance.MaxQuality).ToString("0.#")
            + "), stagger " + CropBalance.Stagger(index).ToString("0.#") + ", knockback " + CropBalance.Knockback(index).ToString("0.#")
            + ", stamina " + CropBalance.Stamina(index).ToString("0.#") + ", block armor " + CropBalance.BlockArmor(index).ToString("0.#")
            + ", durability " + maxDurability.ToString("0.#") + " (Club " + clubDurability.ToString("0.#") + ")");
        { var returnedAt255 = bleed; if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.ApplyBalance: returns {returnedAt255} (line 255)"); return returnedAt255; }
    }

    private static SE_RidingCropBleed CreateBleed(CropDef tier, float damagePerSecond)
    {
        try
        {
            var bleed = ScriptableObject.CreateInstance<SE_RidingCropBleed>();
            bleed.name = "SE_" + tier.PrefabName + "_Bleed";       // the name becomes the effect's id (hash): must be unique
            bleed.Configure(damagePerSecond, CropBalance.BleedDurationSeconds);
            bleed.m_name = "Bleeding";
            bleed.m_tooltip = "Bleeding: " + damagePerSecond.ToString("0.#") + " damage per second for " + CropBalance.BleedDurationSeconds.ToString("0.#") + " seconds.";
            // The icon is filled in once the crop's inventory icon has been rendered (see RenderIcon).
            if (!KyStatusEffects.Add(bleed))
            {
                ModLog.Warn("CropItems: KydlCore did not accept the bleed effect '" + bleed.name + "'; '" + tier.PrefabName + "' will not make targets bleed.");
                return null;
            }

            { var returnedAt274 = bleed; if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.CreateBleed: returns {returnedAt274} (line 274)"); return returnedAt274; }
        }
        catch (Exception ex)
        {
            ModLog.Error("CropItems: could not create the bleed effect for '" + tier.PrefabName + "': " + ex);
            return null;
        }
    }

    private static void ApplyVisuals(GameObject prefab, CropDef tier)
    {
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
        if (filters.Length == 0 || renderers.Length == 0)
        {
            ModLog.Error("CropItems: the cloned prefab for '" + tier.PrefabName + "' has no MeshFilter/MeshRenderer to swap.");
            return;
        }

        Texture2D albedo;
        if (!CropAssets.Albedos.TryGetValue(tier.AlbedoName, out albedo))
        {
            ModLog.Error("CropItems: texture '" + tier.AlbedoName + "' for '" + tier.PrefabName + "' is missing; visuals not applied.");
            return;
        }

        // Build our material from the vanilla one, so it keeps Valheim's own item shader
        // (a plain Unity 'Standard' shader would usually render pink in Valheim).
        var material = new Material(renderers[0].sharedMaterial) { name = "RidingCropMaterial_" + tier.PrefabName };
        material.mainTexture = albedo;
        // Assigning a texture is not enough: the Standard-style shader only samples these maps when its keyword is on.
        SetTexture(material, "_BumpMap", CropAssets.Normal, "_NORMALMAP");
        SetTexture(material, "_MetallicGlossMap", CropAssets.MetalGloss, "_METALLICGLOSSMAP");

        foreach (MeshFilter filter in filters)
        {
            MeshRenderer oldRenderer = filter.GetComponent<MeshRenderer>();

            // Put our mesh on a new CHILD object. Valheim resets the rotation of the "attach" node when the item is equipped,
            // so rotating the child is the reliable way to align the crop in the hand.
            var visual = new GameObject("RidingCropVisual");
            visual.layer = filter.gameObject.layer;
            visual.transform.SetParent(filter.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = CropAssets.Mesh;
            MeshRenderer newRenderer = visual.AddComponent<MeshRenderer>();
            newRenderer.sharedMaterials = new[] { material };
            if (oldRenderer != null)
            {
                newRenderer.shadowCastingMode = oldRenderer.shadowCastingMode;
                newRenderer.receiveShadows = oldRenderer.receiveShadows;
                UnityEngine.Object.DestroyImmediate(oldRenderer);
            }

            UnityEngine.Object.DestroyImmediate(filter);
            Visuals.Add(visual.transform);
        }

        ModLog.Verbose("CropItems: '" + tier.PrefabName + "': swapped " + filters.Length + " mesh(es), texture '" + albedo.name + "'. Shader in use: " + material.shader.name);
    }

    private static void ApplyAlignment()
    {
        if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.ApplyAlignment: {Visuals.Count} visual(s), rotation=({RotationX}, {RotationY}, {RotationZ}) degrees, grip offset={GripOffset} m, scale={ModelScale}");
        Quaternion rotation = Quaternion.Euler(RotationX, RotationY, RotationZ);
        foreach (Transform t in Visuals)
        {
            if (t == null)
            {
                continue;
            }

            t.localRotation = rotation;
            t.localScale = Vector3.one * ModelScale;
            // Slide the model along its own long axis (Y) so the hand ends up further up the handle.
            t.localPosition = rotation * new Vector3(0f, -GripOffset * ModelScale, 0f);
        }
    }

    // Assigns a texture to a material slot and switches the matching shader keyword on (or off when there is no texture).
    // The material is a copy of the Club's, so with no texture of our own we clear the slot: the Club's map would otherwise
    // stay and be drawn over our mesh, whose UVs are different.
    private static void SetTexture(Material material, string property, Texture2D texture, string keyword)
    {
        if (!material.HasProperty(property))
        {
            ModLog.Warn("CropItems: shader '" + material.shader.name + "' has no '" + property + "' slot; skipped.");
            return;
        }

        if (texture == null)
        {
            material.SetTexture(property, null);
            material.DisableKeyword(keyword);
            { if (RuleLog.IsOn()) ModLog.Verbose($"CropItems.SetTexture: returns (line 367) - inputs (material={material}, property={property}, texture={texture}, keyword={keyword})"); return; }
        }

        material.SetTexture(property, texture);
        material.EnableKeyword(keyword);
    }
}
