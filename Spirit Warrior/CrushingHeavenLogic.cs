using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
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
                    var dice = handwraps.WeaponProperties!.DamageDieCount;
                    var bonus = handwraps.WeaponProperties.ItemBonus;
                    if (dice <= 1 && bonus <= 0) return;
                    weaponChoice.WeaponProperties!.DamageDieCount =
                        Math.Max(weaponChoice.WeaponProperties.DamageDieCount, dice);
                    weaponChoice.WeaponProperties.ItemBonus =
                        Math.Max(weaponChoice.WeaponProperties.ItemBonus, bonus);
                }
            };
        }
        public static void ResetWeapons(Item attack)
        {
            attack.Runes.RemoveAll(rune => rune.RuneProperties != null);
            attack.WeaponProperties = GenerateDefaultWeaponProperties(attack);
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
            Item propertyRune = oldRune;
            attack.Runes.Add(propertyRune);
            propertyRune.RuneProperties!.ModifyItem(attack);
        }

        private static WeaponProperties GenerateDefaultWeaponProperties(Item attack)
        {
            Item baseItem = new Item(attack.Illustration, attack.Name, attack.Traits.ToArray()).WithWeaponProperties(new WeaponProperties($"1d{attack.WeaponProperties!.DamageDieSize}", attack.WeaponProperties.DamageKind));

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
                    if (weapons.Count >= 1 && weapons[0].HasTrait(Trait.Melee) && weapons[0].HasTrait(Trait.Finesse) | weapons[0].HasTrait(Trait.Agile) | !weapons[0].HasTrait(Trait.TwoHanded))
                    {
                        itemOptions.Add(weapons[0]);
                    }
                    if (weapons.Count == 2 && weapons[1].HasTrait(Trait.Melee) && weapons[1].HasTrait(Trait.Finesse) | weapons[1].HasTrait(Trait.Agile) | !weapons[1].HasTrait(Trait.TwoHanded))
                    {
                        itemOptions.Add(weapons[1]);
                    }
                    if (self.Owner.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon) != null)
                    {
                        itemOptions.Remove((Item)self.Owner.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon)?.Tag!);
                    }
                    SubmenuPossibility menu = new(new ModdedIllustration("TXAssets/Choice.png"), "Choose Weapon");
                    menu.Subsections.Add(new PossibilitySection("Choose Weapon"));

                    foreach (Item? item in itemOptions)
                    {
                        if (item != null)
                            menu.Subsections[0].AddPossibility(
                                (ActionPossibility)CaHelpers.ChooseWeapon(self.Owner, item)
                            );
                    }
                    foreach (Possibility possibility1 in menu.Subsections[0].Possibilities)
                    {
                        ActionPossibility possibility = (ActionPossibility)possibility1;
                        possibility.PossibilitySize = PossibilitySize.Half;
                    }
                    menu.WithPossibilityGroup("Spirit Warrior");
                    return menu;
                }
            };
        }
    }
}