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
            CombatAction chooseWeapon = new CombatAction(self, item.Illustration, $"Choose {item.Name}", [Trait.Basic, Trait.DoesNotBreakStealth, Trait.DoNotShowOverheadOfActionName],
                            $"Choose {item.Name}, so your weapon can benefit from your handwraps of mighty blows. This will cause your previously chosen weapon to lose the effect of your handwraps.", 
                            Target.Self().WithAdditionalRestriction(a => a.CarriedItems.Any(wrap => wrap.HasTrait(Trait.HandwrapsOfMightyBlows) && wrap.IsWorn) ? null : "You must be wearing a Handwraps of Mighty Blows."))
                            .WithSoundEffect(SfxName.MagicWeapon)
                            .WithActionCost(0)
                            .WithEffectOnSelf(warrior =>
                            {
                                QEffect? oldInvested = warrior.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon);
                                if (oldInvested != null)
                                {
                                    oldInvested.ExpiresAt = ExpirationCondition.Immediately;
                                }
                                Item? oldPotencyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is
                                {
                                    RuneKind: RuneKind.WeaponPotency
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldPotencyRune,
                                    Id = ModData.QEffectIds.OldPotency
                                });
                                Item? oldStrikingRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is
                                {
                                    RuneKind: RuneKind.WeaponStriking
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldStrikingRune,
                                    Id = ModData.QEffectIds.OldStriking
                                });
                                List<Item> oldPropertyRunes = item.Runes.Where(rune => rune.RuneProperties is
                                {
                                    RuneKind: RuneKind.WeaponProperty
                                }).ToList();
                                Item? oldPropertyRune1 = oldPropertyRunes.Count > 0 ? oldPropertyRunes[0] : null;
                                Item? oldPropertyRune2 = oldPropertyRunes.Count > 1 ? oldPropertyRunes[1] : null;
                                Item? oldPropertyRune3 = oldPropertyRunes.Count > 2 ? oldPropertyRunes[2] : null;
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldPropertyRune1,
                                    Id = ModData.QEffectIds.OldProperty1
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldPropertyRune2,
                                    Id = ModData.QEffectIds.OldProperty2
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldPropertyRune3,
                                    Id = ModData.QEffectIds.OldProperty3
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = item.Name,
                                    Id = ModData.QEffectIds.OldName
                                });
                                warrior.AddQEffect(ChosenWeapon(item));
                                return Task.CompletedTask;
                            });

            return chooseWeapon;
        }
        public static void ChooseWeaponStart(Creature warrior, Item item)
        {
            Item? oldPotencyRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is
                                {
                                    RuneKind: RuneKind.WeaponPotency
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldPotencyRune,
                                    Id = ModData.QEffectIds.OldPotency
                                });
                                Item? oldStrikingRune = item.Runes.FirstOrDefault(rune => rune.RuneProperties is
                                {
                                    RuneKind: RuneKind.WeaponStriking
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldStrikingRune,
                                    Id = ModData.QEffectIds.OldStriking
                                });
                                List<Item> oldPropertyRunes = item.Runes.Where(rune => rune.RuneProperties is
                                {
                                    RuneKind: RuneKind.WeaponProperty
                                }).ToList();
                                Item? oldPropertyRune1 = oldPropertyRunes.Count > 0 ? oldPropertyRunes[0] : null;
                                Item? oldPropertyRune2 = oldPropertyRunes.Count > 1 ? oldPropertyRunes[1] : null;
                                Item? oldPropertyRune3 = oldPropertyRunes.Count > 2 ? oldPropertyRunes[2] : null;
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldPropertyRune1,
                                    Id = ModData.QEffectIds.OldProperty1
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldPropertyRune2,
                                    Id = ModData.QEffectIds.OldProperty2
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = oldPropertyRune3,
                                    Id = ModData.QEffectIds.OldProperty3
                                });
                                warrior.AddQEffect(new QEffect()
                                {
                                    Tag = item.Name,
                                    Id = ModData.QEffectIds.OldName
                                });
                                warrior.AddQEffect(ChosenWeapon(item));
        }

        private static QEffect ChosenWeapon(Item item)
        {
            QEffect chosenWeapon = new($"Chosen Weapon ({item.Name})",
            "This weapon benefits from your handwraps of mighty blows' runestones.")
            {
                Tag = item,
                Id = ModData.QEffectIds.CrushingEarthWeapon,
                ExpiresAt = ExpirationCondition.Never,
                Illustration = item.Illustration,
                WhenExpires = qf =>
                {
                    Creature warrior = qf.Owner;
                    if (qf is not { Tag: Item oldWeapon }) return;
                    QEffect? ogPotency = warrior.FindQEffect(ModData.QEffectIds.OldPotency);
                    QEffect? ogStriking = warrior.FindQEffect(ModData.QEffectIds.OldStriking);
                    QEffect? ogProperty1 = warrior.FindQEffect(ModData.QEffectIds.OldProperty1);
                    QEffect? ogProperty2 = warrior.FindQEffect(ModData.QEffectIds.OldProperty2);
                    QEffect? ogProperty3 = warrior.FindQEffect(ModData.QEffectIds.OldProperty3);
                    QEffect? ogName = warrior.FindQEffect(ModData.QEffectIds.OldName);
                    CrushingHeavenLogic.ResetWeapons(oldWeapon);
                    if (ogPotency is { Tag: Item potency })
                    {
                        CrushingHeavenLogic.HandleOldRune(potency, oldWeapon);
                        ogPotency.ExpiresAt = ExpirationCondition.Immediately;
                    }
                    if (ogStriking is { Tag: Item striking })
                    {
                        CrushingHeavenLogic.HandleOldRune(striking, oldWeapon);
                        ogStriking.ExpiresAt = ExpirationCondition.Immediately;
                    }
                    if (ogProperty1 is { Tag: Item property1 })
                    {
                        CrushingHeavenLogic.HandleOldRune(property1, oldWeapon);
                        ogProperty1.ExpiresAt = ExpirationCondition.Immediately;
                    }
                    if (ogProperty2 is { Tag: Item property2 })
                    {
                        CrushingHeavenLogic.HandleOldRune(property2, oldWeapon);
                        ogProperty2.ExpiresAt = ExpirationCondition.Immediately;
                    }
                    if (ogProperty3 is { Tag: Item property3 })
                    {
                        CrushingHeavenLogic.HandleOldRune(property3, oldWeapon);
                        ogProperty3.ExpiresAt = ExpirationCondition.Immediately;
                    }
                    if (ogName is { Tag: string name })
                    {
                        oldWeapon.Name = name;
                        ogName.ExpiresAt = ExpirationCondition.Immediately;
                    }
                }
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