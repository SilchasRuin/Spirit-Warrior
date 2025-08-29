using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Spirit_Warrior
{
    public static class CrushingHeavenLogic
    {
        private static readonly PossibilitySectionId SwExtra = ModManager.RegisterEnumMember<PossibilitySectionId>("SpiritWarriorActionsExtra");

        public static QEffect CuttingHeavenWeaponQEffect()
        {
            return new QEffect()
            {
                StateCheck = self =>
                {
                    Creature spiritWarrior = self.Owner;
                    if (spiritWarrior.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon) == null) return;
                    QEffect? chosenWeaponQf = spiritWarrior.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon);
                    Item? handwraps = StrikeRules.GetBestHandwraps(self.Owner);
                    if (chosenWeaponQf?.Tag is not Item weaponChoice || handwraps == null) return;
                    ResetWeapons(weaponChoice);
                    HandleRune(handwraps, weaponChoice, RuneKind.WeaponPotency);
                    HandleRune(handwraps, weaponChoice, RuneKind.WeaponStriking);
                    HandleRune(handwraps, weaponChoice, RuneKind.WeaponProperty);
                    int dice = handwraps.WeaponProperties!.DamageDieCount;
                    int bonus = handwraps.WeaponProperties.ItemBonus;
                    if (dice <= 1 && bonus <= 0) return;
                    weaponChoice.WeaponProperties!.DamageDieCount =
                        Math.Max(weaponChoice.WeaponProperties.DamageDieCount, dice);
                    weaponChoice.WeaponProperties.ItemBonus =
                        Math.Max(weaponChoice.WeaponProperties.ItemBonus, bonus);
                    if (handwraps.Runes.Any(rune => rune.RuneProperties is
                            { RuneKind: RuneKind.WeaponProperty }))
                    {
                        foreach (Item rune in handwraps.Runes.Where(rune => rune.RuneProperties is
                                     { RuneKind: RuneKind.WeaponProperty }))
                        {
                            weaponChoice.Name = $"{{Blue}}{rune.RuneProperties!.Prefix}{{/Blue}} " + weaponChoice.Name;
                        }
                    }
                    if (dice > 1)
                    {
                        string? str3 = weaponChoice.WeaponProperties.DamageDieCount switch
                        {
                            2 => "striking",
                            3 => "greater striking",
                            4 => "major striking",
                            _ => null
                        };
                        if (str3 != null) weaponChoice.Name = $"{{Blue}}{str3}{{/Blue}} {weaponChoice.Name}";
                    }
                    if (bonus > 0)
                    {
                        weaponChoice.Name = $"{{Blue}}+{weaponChoice.WeaponProperties.ItemBonus}{{/Blue}} " + weaponChoice.Name;
                    }
                }
            };
        }
        public static void ResetWeapons(Item attack)
        {
            List<Trait> itemTraits = Items.TryGetItemTemplate(attack.ItemName, out Item? item) ? item.Traits : attack.Traits;
            attack.Runes.RemoveAll(rune => rune.RuneProperties is {RuneKind: RuneKind.WeaponPotency or RuneKind.WeaponStriking or RuneKind.WeaponProperty});
            attack.WeaponProperties = GenerateDefaultWeaponProperties(attack);
            attack.Traits = itemTraits;
            attack.Name = attack.BaseHumanName;
        }

        internal static void HandleRune(Item handwraps, Item attack, RuneKind type)
        {
            var runes = handwraps.Runes.Where(rune => rune.RuneProperties != null && rune.RuneProperties.RuneKind == type);
            foreach (Item rune in runes)
            {
                if (rune.RuneProperties?.CanBeAppliedTo?.Invoke(rune, attack) != null) continue;
                attack.Runes.Add(rune);
                rune.RuneProperties!.ModifyItem(attack);
            }
        }
        public static void HandleOldRune(Item oldRune, Item attack)
        {
            attack.Runes.Add(oldRune);
            oldRune.RuneProperties!.ModifyItem(attack);
        }

        private static WeaponProperties GenerateDefaultWeaponProperties(Item attack)
        {
            List<Trait> itemTraits = Items.TryGetItemTemplate(attack.ItemName, out Item? item) ? item.Traits : attack.Traits;
            Item baseItem = new Item(attack.Illustration, attack.BaseHumanName, itemTraits.ToArray()).WithWeaponProperties(new WeaponProperties($"1d{attack.WeaponProperties!.DamageDieSize}", attack.WeaponProperties.DamageKind));
            if (attack.WeaponProperties.RangeIncrement > 0)
            {
                baseItem.WeaponProperties?.WithRangeIncrement(attack.WeaponProperties.RangeIncrement);
            }
            return baseItem.WeaponProperties!;
        }

        public static QEffect SwInvestedWeaponQEffect()
        {
            return new QEffect()
            {
                ProvideSectionIntoSubmenu = (self, submenu) => {
                    if (submenu.SubmenuId == SubmenuId.OtherManeuvers && StrikeRules.GetBestHandwraps(self.Owner) != null)
                    {
                        return new PossibilitySection("Spirit Warrior").WithPossibilitySectionId(SwExtra);
                    }
                    return null;
                },
                ProvideActionIntoPossibilitySection = (self, section) => {
                    if (section.PossibilitySectionId != SwExtra)
                    {
                        return null;
                    }
                    // Determine options
                    List<Item?> itemOptions = [];
                    List<Item> weapons = self.Owner.HeldItems;
                    if (weapons.Count >= 1 && SwHelpers.IsItemOcWeapon(weapons[0]))
                    {
                        itemOptions.Add(weapons[0]);
                    }
                    if (weapons.Count == 2 && SwHelpers.IsItemOcWeapon(weapons[1]))
                    {
                        itemOptions.Add(weapons[1]);
                    }
                    if (self.Owner.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon) != null)
                    {
                        itemOptions.Remove((Item)self.Owner.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon)?.Tag!);
                    }
                    SubmenuPossibility menu = new(new ModdedIllustration("TXAssets/Choice.png"), "Choose Weapon");
                    menu.Subsections.Add(new PossibilitySection("Choose Weapon"));
                    foreach (Item item in itemOptions.OfType<Item>())
                    {
                        menu.Subsections[0].AddPossibility(
                            (ActionPossibility)CaHelpers.ChooseWeapon(self.Owner, item)
                        );
                    }
                    menu.Subsections[0].AddPossibility(new ActionPossibility(new CombatAction(self.Owner, IllustrationName.BadWeapon, "None", [Trait.Basic, Trait.DoesNotBreakStealth, Trait.DoNotShowOverheadOfActionName], "Ends the effect of Cutting Heaven, Crushing Earth on a weapon.", 
                        Target.Self().WithAdditionalRestriction(a => a.HasEffect(ModData.QEffectIds.CrushingEarthWeapon) ? null : "Cutting Heaven, Crushing Earth is not currently being applied to a weapon.")
                        ).WithActionCost(0).WithEffectOnSelf(creature =>
                        {
                            creature.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.CrushingEarthWeapon);
                        })
                    ));
                    foreach (ActionPossibility possibility in menu.Subsections[0].Possibilities.Cast<ActionPossibility>())
                    {
                        possibility.PossibilitySize = PossibilitySize.Half;
                    }
                    menu.WithPossibilityGroup("Spirit Warrior");
                    return menu;
                }
            };
        }
    }
}