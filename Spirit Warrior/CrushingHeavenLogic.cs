using Dawnsbury.Audio;
using Dawnsbury.Campaign.LongTerm;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using System.Data;
using System.Text;

namespace Spirit_Warrior
{
    public static class CrushingHeavenLogic
    {
        public static PossibilitySectionId SWExtra = ModManager.RegisterEnumMember<PossibilitySectionId>("SpiritWarriorActionsExtra");

        public static QEffect CuttingHeavenWeaponQEffect()
        {
            return new QEffect()
            {
                StateCheck = self =>
                {
                    Creature spiritWarrior = self.Owner;
                    if (spiritWarrior.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon) != null)
                    {
                        QEffect chosenWeaponQF = spiritWarrior.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon);
                        Item handwraps = StrikeRules.GetBestHandwraps(self.Owner);
                        List<Item> weaponChoice = new List<Item>() {chosenWeaponQF.Tag as Item};
                        foreach (Item weapon in weaponChoice) 
                        { 
                                weapon.WeaponProperties.DamageDieCount = 1;
                                weapon.WeaponProperties.ItemBonus = 0;
                        }
                        ResetWeapons(spiritWarrior, handwraps, weaponChoice);

                        HandleRune(spiritWarrior, handwraps, weaponChoice, RuneKind.WeaponPotency);
                        HandleRune(spiritWarrior, handwraps, weaponChoice, RuneKind.WeaponStriking);
                        HandleRune(spiritWarrior, handwraps, weaponChoice, RuneKind.WeaponProperty);

                        int dice = 1;
                        int bonus = 0;
                        dice = handwraps.WeaponProperties.DamageDieCount;
                        bonus = handwraps.WeaponProperties.ItemBonus;

                        if (dice > 1 || bonus > 0)
                        {
                            foreach (Item weapon in weaponChoice)
                            {
                                weapon.WeaponProperties.DamageDieCount = Math.Max(weapon.WeaponProperties.DamageDieCount, dice);
                                weapon.WeaponProperties.ItemBonus = Math.Max(weapon.WeaponProperties.ItemBonus, bonus);
                            }
                        }
                    }
                }
            };
        }
        public static void ResetWeapons(Creature spiritWarrior, Item handwraps, List<Item> weaponChoice)
        {
            // Remove this rune slot
            foreach (Item attack in weaponChoice)
            {
                attack.Runes.RemoveAll(rune => rune.RuneProperties != null);
                attack.WeaponProperties = GenerateDefaultWeaponProperties(attack);
            }
        }
        public static void HandleRune(Creature spiritWarrior, Item handwraps, List<Item> weaponChoice, RuneKind type)
        {
            var runes = handwraps.Runes.Where(rune => rune.RuneProperties != null && rune.RuneProperties.RuneKind == type);

            foreach (Item rune in runes)
            {
                foreach (Item attack in weaponChoice)
                {
                    if (rune.RuneProperties?.CanBeAppliedTo == null || rune.RuneProperties?.CanBeAppliedTo(rune, attack) == null)
                    {
                        attack.Runes.Add(rune);
                        rune.RuneProperties!.ModifyItem(attack);
                    }
                }
            }
        }
        public static void HandleOldRune(Creature spiritWarrior, Item oldRune, List<Item> weaponChoice)
        {
            Item? propertyRune = oldRune;

            // Re-add this rune slot
            if (propertyRune != null)
            {
                foreach (Item attack in weaponChoice)
                {
                    attack.Runes.Add(propertyRune);
                    propertyRune.RuneProperties.ModifyItem(attack);
                }
            }
        }

        public static WeaponProperties GenerateDefaultWeaponProperties(Item attack)
        {
            Item baseItem = new Item(attack.Illustration, attack.Name, attack.Traits.ToArray()).WithWeaponProperties(new WeaponProperties($"1d{attack.WeaponProperties.DamageDieSize}", attack.WeaponProperties.DamageKind));

            if (attack.WeaponProperties.RangeIncrement > 0)
            {
                baseItem.WeaponProperties.WithRangeIncrement(attack.WeaponProperties.RangeIncrement);
            }

            return baseItem.WeaponProperties;
        }

        public static QEffect SWInvestedWeaponQEffect()
        {
            return new QEffect()
            {
                ProvideSectionIntoSubmenu = (self, submenu) => {
                    if (submenu.SubmenuId == SubmenuId.OtherManeuvers && StrikeRules.GetBestHandwraps(self.Owner) != null)
                    {
                        return new PossibilitySection("Spirit Warrior").WithPossibilitySectionId(SWExtra);
                    }
                    return null;
                },
                ProvideActionIntoPossibilitySection = (self, section) => {
                    if (section.PossibilitySectionId != SWExtra)
                    {
                        return null;
                    }

                    // Determine options
                    List<Item> itemOptions = new List<Item>();
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
                        itemOptions.Remove((Item)self.Owner.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon).Tag);
                    }

                    SubmenuPossibility menu = new SubmenuPossibility(new ModdedIllustration("TXAssets/Choice.png"), "Choose Weapon");
                    menu.Subsections.Add(new PossibilitySection("Choose Weapon"));

                    foreach (Item item in itemOptions)
                    {
                        menu.Subsections[0].AddPossibility((ActionPossibility) CAHelpers.ChooseWeapon(self.Owner, item)
                        );
                    }

                    foreach (ActionPossibility possibility in menu.Subsections[0].Possibilities)
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