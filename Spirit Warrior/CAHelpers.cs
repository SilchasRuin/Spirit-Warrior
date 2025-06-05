using Dawnsbury.Audio;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;

namespace Spirit_Warrior
{
    internal static class CAHelpers
    {
        public static CombatAction ChooseWeapon(Creature self, Item item)
        {
            CombatAction chooseWeapon = new CombatAction(self, item.Illustration, $"Choose {item.Name}", new Trait[0],
                            $"Choose {item.Name}, so your weapon can benefit from your handwraps of mighty blows. This will cause your previously chosen weapon to lose the effect of your handwraps.", Target.Self())
                            .WithSoundEffect(SfxName.MagicWeapon)
                            .WithActionCost(0)
                            .WithEffectOnSelf(async self =>
                            {
                            QEffect? oldInvestedEffect = self.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon);
                            if (oldInvestedEffect != null)
                            {
                                List<Item> oldWeapon = new List<Item>() { oldInvestedEffect.Tag as Item };
                                QEffect? ogPotency = self.FindQEffect(ModData.QEffectIds.OldPotency);
                                QEffect? ogStriking = self.FindQEffect(ModData.QEffectIds.OldStriking);
                                QEffect? ogProperty = self.FindQEffect(ModData.QEffectIds.OldProperty);
                                CrushingHeavenLogic.ResetWeapons(self, item, oldWeapon);
                                CrushingHeavenLogic.HandleOldRune(self, ogPotency.Tag as Item, oldWeapon);
                                CrushingHeavenLogic.HandleOldRune(self, ogStriking.Tag as Item, oldWeapon);
                                CrushingHeavenLogic.HandleOldRune(self, ogProperty.Tag as Item, oldWeapon);
                                oldInvestedEffect.ExpiresAt = ExpirationCondition.Immediately;
                                ogPotency.ExpiresAt = ExpirationCondition.Immediately;
                                ogStriking.ExpiresAt = ExpirationCondition.Immediately;
                                ogProperty.ExpiresAt = ExpirationCondition.Immediately;
                            }
                            Item oldPotencyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties != null && rune.RuneProperties.RuneKind == RuneKind.WeaponPotency);
                            self.AddQEffect(new QEffect()
                            {
                                Tag = oldPotencyRune,
                                Id = ModData.QEffectIds.OldPotency
                            });
                            Item oldStrikingRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties != null && rune.RuneProperties.RuneKind == RuneKind.WeaponStriking);

                            self.AddQEffect(new QEffect()
                            {
                                Tag = oldStrikingRune,
                                Id = ModData.QEffectIds.OldStriking
                            });

                            Item oldPropertyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties != null && rune.RuneProperties.RuneKind == RuneKind.WeaponProperty);
                            self.AddQEffect(new QEffect()
                            {
                                Tag = oldPropertyRune,
                                Id = ModData.QEffectIds.OldProperty
                            });

                            self.AddQEffect(new QEffect($"Chosen Weapon ({item.Name})",
                                "This weapon benefits from your handwraps of mighty blows' runestones.")
                            {
                                Tag = item,
                                Id = ModData.QEffectIds.CrushingEarthWeapon,
                                ExpiresAt = ExpirationCondition.Never,
                                Illustration = item.Illustration
                            });
                        });

            return chooseWeapon;
        }
        public static CombatAction ChooseWeaponStart(Creature self, Item item)
        {
            CombatAction chooseWeaponStart = new CombatAction(self, item.Illustration, $"Choose {item.Name}", new Trait[0],
                            $"Choose {item.Name}, so your weapon can benefit from your handwraps of mighty blows. This will cause your previously chosen weapon to lose the effect of your handwraps.", Target.Self())
                            .WithSoundEffect(SfxName.MagicWeapon)
                            .WithActionCost(0);
            Item oldPotencyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties != null && rune.RuneProperties.RuneKind == RuneKind.WeaponPotency);
            self.AddQEffect(new QEffect()
            {
                Tag = oldPotencyRune,
                Id = ModData.QEffectIds.OldPotency
            });
                                Item oldStrikingRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties != null && rune.RuneProperties.RuneKind == RuneKind.WeaponStriking);

                                self.AddQEffect(new QEffect()
                                {
                                    Tag = oldStrikingRune,
                                    Id = ModData.QEffectIds.OldStriking
                                });

                                Item oldPropertyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties != null && rune.RuneProperties.RuneKind == RuneKind.WeaponProperty);
                                self.AddQEffect(new QEffect()
                                {
                                    Tag = oldPropertyRune,
                                    Id = ModData.QEffectIds.OldProperty
                                });

                                self.AddQEffect(new QEffect($"Chosen Weapon ({item.Name})",
                                    "This weapon benefits from your handwraps of mighty blows' runestones.")
                                {
                                    Tag = item,
                                    Id = ModData.QEffectIds.CrushingEarthWeapon,
                                    ExpiresAt = ExpirationCondition.Never,
                                    Illustration = item.Illustration
                                });
            return chooseWeaponStart;
        }
    }
}