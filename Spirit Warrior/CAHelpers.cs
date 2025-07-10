using Dawnsbury.Audio;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;

namespace Spirit_Warrior
{
    internal static class CaHelpers
    {
        public static CombatAction ChooseWeapon(Creature self, Item item)
        {
            CombatAction chooseWeapon = new CombatAction(self, item.Illustration, $"Choose {item.Name}", [],
                            $"Choose {item.Name}, so your weapon can benefit from your handwraps of mighty blows. This will cause your previously chosen weapon to lose the effect of your handwraps.", Target.Self())
                            .WithSoundEffect(SfxName.MagicWeapon)
                            .WithActionCost(0)
                            .WithEffectOnSelf(warrior =>
                            {
                            QEffect? oldInvestedEffect = warrior.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon);
                            if (oldInvestedEffect != null)
                            {
                                Item oldWeapon = (oldInvestedEffect.Tag as Item)!;
                                QEffect? ogPotency = warrior.FindQEffect(ModData.QEffectIds.OldPotency);
                                QEffect? ogStriking = warrior.FindQEffect(ModData.QEffectIds.OldStriking);
                                QEffect? ogProperty = warrior.FindQEffect(ModData.QEffectIds.OldProperty);
                                CrushingHeavenLogic.ResetWeapons(oldWeapon);
                                CrushingHeavenLogic.HandleOldRune((ogPotency!.Tag as Item)!, oldWeapon);
                                CrushingHeavenLogic.HandleOldRune((ogStriking!.Tag as Item)!, oldWeapon);
                                CrushingHeavenLogic.HandleOldRune((ogProperty!.Tag as Item)!, oldWeapon);
                                oldInvestedEffect.ExpiresAt = ExpirationCondition.Immediately;
                                ogPotency.ExpiresAt = ExpirationCondition.Immediately;
                                ogStriking.ExpiresAt = ExpirationCondition.Immediately;
                                ogProperty.ExpiresAt = ExpirationCondition.Immediately;
                            }
                            Item oldPotencyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is
                            {
                                RuneKind: RuneKind.WeaponPotency
                            })!;
                            warrior.AddQEffect(new QEffect()
                            {
                                Tag = oldPotencyRune,
                                Id = ModData.QEffectIds.OldPotency
                            });
                            Item oldStrikingRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is
                            {
                                RuneKind: RuneKind.WeaponStriking
                            })!;

                            warrior.AddQEffect(new QEffect()
                            {
                                Tag = oldStrikingRune,
                                Id = ModData.QEffectIds.OldStriking
                            });

                            Item oldPropertyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is
                            {
                                RuneKind: RuneKind.WeaponProperty
                            })!;
                            warrior.AddQEffect(new QEffect()
                            {
                                Tag = oldPropertyRune,
                                Id = ModData.QEffectIds.OldProperty
                            });

                            warrior.AddQEffect(ChosenWeapon(item));
                            return Task.CompletedTask;
                            });

            return chooseWeapon;
        }
        public static CombatAction ChooseWeaponStart(Creature self, Item item)
        {
            CombatAction chooseWeaponStart = new CombatAction(self, item.Illustration, $"Choose {item.Name}", [],
                            $"Choose {item.Name}, so your weapon can benefit from your handwraps of mighty blows. This will cause your previously chosen weapon to lose the effect of your handwraps.", Target.Self())
                            .WithSoundEffect(SfxName.MagicWeapon)
                            .WithActionCost(0);
            Item oldPotencyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is { RuneKind: RuneKind.WeaponPotency })!;
            self.AddQEffect(new QEffect()
            {
                Tag = oldPotencyRune,
                Id = ModData.QEffectIds.OldPotency
            });
            Item oldStrikingRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is { RuneKind: RuneKind.WeaponStriking })!;

            self.AddQEffect(new QEffect()
            {
                Tag = oldStrikingRune,
                Id = ModData.QEffectIds.OldStriking
            });

            Item oldPropertyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is { RuneKind: RuneKind.WeaponProperty })!;
            self.AddQEffect(new QEffect()
            {
                Tag = oldPropertyRune,
                Id = ModData.QEffectIds.OldProperty
            });

            self.AddQEffect(ChosenWeapon(item));
            return chooseWeaponStart;
        }

        private static QEffect ChosenWeapon(Item item)
        {
            QEffect chosenWeapon = new QEffect($"Chosen Weapon ({item.Name})",
            "This weapon benefits from your handwraps of mighty blows' runestones.")
            {
                Tag = item,
                Id = ModData.QEffectIds.CrushingEarthWeapon,
                ExpiresAt = ExpirationCondition.Never,
                Illustration = item.Illustration
            };
            return chosenWeapon;
        }
        public static bool IsItemChosenWeapon(Item? item, Creature self)
        {
            QEffect? chosenWeapon = self.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon);
            if (chosenWeapon == null) return false;
            return item == chosenWeapon.Tag as Item;
        }
    }
}