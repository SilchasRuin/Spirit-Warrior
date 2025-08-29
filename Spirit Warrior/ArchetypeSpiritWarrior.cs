using System.Buffers;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;

namespace Spirit_Warrior
{
    public static class ArchetypeSpiritWarrior
    {
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            //Dedication Feat
            Feat spiritWarriorDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                    ModData.Traits.SpiritWarriorArchetype,
                    "You are a warrior who trains your spirit and body to work in perfect harmony, enhancing your attacks with your spiritual energy while fighting with a ferocious martial technique that combines blade and fist.",
                    "The damage die for your fist changes to 1d6 instead of 1d4, and your fist gains the parry trait. You don’t take the normal –2 circumstance penalty when making a lethal attack with your fist or any other unarmed attacks. You gain the Overwhelming Combination action.")
                .WithRulesBlockForCombatAction(cr =>
                {
                    CombatAction overwhelm = CombatAction.CreateSimple(cr, "Overwhelming Combination", Trait.Flourish).WithActionCost(1);
                        overwhelm.Description =
                            "{b}Requirements{/b} You're wielding a one-handed melee weapon or a melee weapon with the agile or finesse trait" +
                            "\n\nMake two Strikes against a target within your reach, one with the required weapon and one with your fist unarmed attack. If both hit the same target, combine their damage for the purposes of its resistances and weaknesses. Apply your multiple attack penalty to each Strike normally.";
                        return overwhelm;
                })
            .WithOnCreature(self =>
            {
                self.WithUnarmedStrike(Item.ImprovedFist());
                self.AddQEffect(new QEffect()
                {
                    Id = QEffectId.PowerfulFist
                });
            });
            SwHelpers.AlternateOcAction(spiritWarriorDedication);
            AddParryAction(spiritWarriorDedication);
            ModManager.AddFeat(spiritWarriorDedication);
            Feat unholyBane = new TrueFeat(
                    ModData.FeatNames.UnholyBane,
                    4,
                    "You have sworn an oath to defend the helpless from dangerous unholy creatures, specifically the undead and fiends that threaten the Points of Light.",
                    "Attacks made as part of your Overwhelming Combination ability gain a +2 circumstance bonus to damage against undead and fiends, or +4 if you have master proficiency with the weapon you used. You also gain a +2 circumstance bonus to saves against spells from fiends and undead.",
                    [ModData.Traits.SpiritWarrior, Trait.Homebrew])
                .WithAvailableAsArchetypeFeat(ModData.Traits.SpiritWarriorArchetype)
                .WithOnCreature(self =>
                {
                    self.AddQEffect(new QEffect()
                    {
                        Id = ModData.QEffectIds.UnholyBane
                    });
                }
                )
                .WithPermanentQEffect("Attacks made as part of your Overwhelming Combination ability gain a +2 circumstance bonus to damage against undead and fiends, or +4 if you have master proficiency with the weapon you used. You also gain a +2 circumstance bonus to saves against spells from fiends and undead.",
                qf =>
                {
                    qf.BonusToDefenses = (_, action, defense) => action != null && action.HasTrait(Trait.Spell) && (defense.IsSavingThrow() ||
                        (!action.Owner.HasTrait(Trait.Undead) &&
                         !action.Owner.HasTrait(Trait.Fiend)))
                        ? new Bonus(2, BonusType.Circumstance, "Unholy Bane Oath")
                        : null;
                });
            ModManager.AddFeat(unholyBane);
            Feat flowingPalm = new TrueFeat(
                ModData.FeatNames.FlowingPalm,
                6,
                "The simple and precise movements of your hands allow you to deflect blows with the same efficacy as a raised shield.",
                "When you parry with your fist, increase the circumstance bonus to AC it grants from +1 to +2.",
                [ModData.Traits.SpiritWarrior])
                .WithAvailableAsArchetypeFeat(ModData.Traits.SpiritWarriorArchetype)
                .WithOnCreature(self =>
                {
                    self.AddQEffect(new QEffect("Flowing Palm", "When you parry with your fist, increase the circumstance bonus to AC it grants from +1 to +2.")
                    {
                        Id = ModData.QEffectIds.FlowingPalm
                    });
                }
                );
            ModManager.AddFeat(flowingPalm);
            Feat cuttingHeavenL = new(ModData.FeatNames.CuttingHeavenL, null,
                    "At the start of combat, Cutting Heaven, Crushing Earth will be applied to your left hand weapon.",
                    [], null);
            ModManager.AddFeat(cuttingHeavenL);
            Feat cuttingHeavenR = new(ModData.FeatNames.CuttingHeavenR, null,
                    "At the start of combat, Cutting Heaven, Crushing Earth will be applied to your right hand weapon.",
                    [], null);
            ModManager.AddFeat(cuttingHeavenR);
            Feat cuttingHeaven = new TrueFeat(
                ModData.FeatNames.CuttingHeaven,
                6,
                "Your skill in combining fist and blade has grown into a seamless art where each attack makes an opponent more vulnerable to the next.",
                "As long as you have invested and are wearing a set of handwraps of mighty blows, you also apply their runes to a single weapon you’re wielding that can be used with your Overwhelming Combination ability." +
                "\n\nYou gain the following benefits: " +
                "\n-When you successfully Strike an opponent with this weapon, it is off-guard to the next Strike you make against it with a fist unarmed attack before the end of your next turn. " +
                "\n-When you successfully Strike an opponent with your fist unarmed attack, it is off-guard to the next Strike you make against it with a one-handed, agile, or finesse melee weapon before the end of your next turn.",
                [ModData.Traits.SpiritWarrior])
                .WithAvailableAsArchetypeFeat(ModData.Traits.SpiritWarriorArchetype)
                .WithOnCreature(self =>
                {
                    self.AddQEffect(CrushingHeavenLogic.CuttingHeavenWeaponQEffect());
                    self.AddQEffect(CrushingHeavenLogic.SwInvestedWeaponQEffect());
                })
                .WithOnSheet(values =>
                {
                    values.AddSelectionOption(new SingleFeatSelectionOption("PrecombatCuttingHeaven", "Cutting Heaven - Chosen Weapon", SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, feat => feat.FeatName == ModData.FeatNames.CuttingHeavenL || feat.FeatName == ModData.FeatNames.CuttingHeavenR).WithIsOptional());
                })
                .WithPermanentQEffect("You apply runes from your handwraps to a weapon.", qfFeat =>
                {
                    qfFeat.Name = "Cutting Heaven";
                    qfFeat.StartOfCombat = (Func<QEffect, Task>)(async innerSelf =>
                    { 
                        if (!innerSelf.Owner.CarriedItems.Any(item => item.HasTrait(Trait.HandwrapsOfMightyBlows) && item.IsWorn)) return;
                        List<string> itemOptionsString = [];
                        List<Item?> itemOptions = [];
                        foreach (Item item in innerSelf.Owner.HeldItems.Where(SwHelpers.IsItemOcWeapon))
                        {
                            itemOptionsString.Add(item.Name);
                            itemOptions.Add(item);
                        }

                        itemOptionsString.Add("none");
                        if (!innerSelf.Owner.HasFeat(ModData.FeatNames.CuttingHeavenL) && !innerSelf.Owner.HasFeat(ModData.FeatNames.CuttingHeavenR) && itemOptions.Count > 0)
                        {
                            ChoiceButtonOption chosenOption = await innerSelf.Owner.AskForChoiceAmongButtons(
                                IllustrationName.QuestionMark,
                                "Choose a weapon to apply your handwraps runes to.",
                                itemOptionsString.ToArray()
                            );
                            if (itemOptionsString[chosenOption.Index] != "none")
                            {
                                Item? targetItem = itemOptions[chosenOption.Index];
                                if (targetItem != null) CaHelpers.ChooseWeaponStart(innerSelf.Owner, targetItem);
                            }
                        }
                        else if (innerSelf.Owner.HasFeat(ModData.FeatNames.CuttingHeavenL) && innerSelf.Owner.HeldItems.Count > 0)
                        {
                            CaHelpers.ChooseWeaponStart(innerSelf.Owner, innerSelf.Owner.HeldItems[0]);
                        }
                        else if (innerSelf.Owner.HasFeat(ModData.FeatNames.CuttingHeavenR) && innerSelf.Owner.HeldItems.Count > 1)
                        {
                            CaHelpers.ChooseWeaponStart(innerSelf.Owner, innerSelf.Owner.HeldItems[1]);
                        }
                    });
                }
                )
                .WithPermanentQEffect("After weapon Strikes the target is debuffed against fist Strikes, and after fist Strikes the enemy is debuffed against weapon Strikes.", delegate (QEffect self)
                 {
                     self.Name = "Crushing Earth";
                     self.AfterYouTakeActionAgainstTarget = (addingEffects, action, defender, result) =>
                     {
                         // If you hit with a weapon which is benefitting from your handwraps, you gain a buff to your Fist attack

                         if (action.HasTrait(Trait.Melee) && result >= CheckResult.Success && CaHelpers.IsItemChosenWeapon(action.Item, self.Owner) && !addingEffects.Owner.QEffects.Any(qe => qe.Id == ModData.QEffectIds.CuttingHeavenFistBuff && qe.Tag != null && qe.Tag == defender))
                         {
                                 addingEffects.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                                 {
                                     Id = ModData.QEffectIds.CuttingHeavenFistBuff,
                                     CannotExpireThisTurn = true,
                                     Tag = defender,
                                     BeforeYourActiveRoll = (rollEffect, combatAction, attackedCreature) =>
                                     {
                                         if (combatAction.HasTrait(Trait.Strike) && combatAction.HasTrait(Trait.Fist) && defender == attackedCreature)
                                         {
                                             QEffect flatFooted = QEffect.FlatFooted("Cutting Heaven, Crushing Earth");
                                             flatFooted.ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction;
                                             attackedCreature.AddQEffect(flatFooted);
                                             rollEffect.Owner.RemoveAllQEffects(qe => qe.Id == ModData.QEffectIds.CuttingHeavenFistBuff && qe.Tag != null && qe.Tag == defender);
                                         }
                                         return Task.CompletedTask;
                                     }
                                 });
                         }
                         // If you hit with your Fist you gain a buff to your weapon which qualifies for Overwhelming Combination
                         else if (action.HasTrait(Trait.Fist) && result >= CheckResult.Success && !addingEffects.Owner.QEffects.Any(qe => qe.Id == ModData.QEffectIds.CuttingHeavenWeaponBuff && qe.Tag != null && qe.Tag == defender))
                         {
                             addingEffects.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                             {
                                 Id = ModData.QEffectIds.CuttingHeavenWeaponBuff,
                                 CannotExpireThisTurn = true,
                                 Tag = defender,
                                 BeforeYourActiveRoll = (rollEffect, combatAction, attackedCreature) =>
                                 {
                                     if (combatAction.HasTrait(Trait.Strike) && !combatAction.HasTrait(Trait.Unarmed) && combatAction.HasTrait(Trait.Melee) && (!combatAction.HasTrait(Trait.TwoHanded) | combatAction.HasTrait(Trait.Finesse) | combatAction.HasTrait(Trait.Agile)) && defender == attackedCreature)
                                     {
                                         QEffect flatFooted = QEffect.FlatFooted("Cutting Heaven, Crushing Earth");
                                         flatFooted.ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction;
                                         attackedCreature.AddQEffect(flatFooted);
                                         rollEffect.Owner.RemoveAllQEffects(qe => qe.Id == ModData.QEffectIds.CuttingHeavenWeaponBuff && qe.Tag != null && qe.Tag == defender);
                                     }
                                     return Task.CompletedTask;
                                 }
                             });
                         }
                         return Task.CompletedTask;
                     };
                 });
            ModManager.AddFeat(cuttingHeaven);
            Feat swordLightWave = new TrueFeat(ModManager.RegisterFeatName("SwordLightWave", "Sword-Light Wave"), 6,
                    "You channel spiritual energy through your weapon, unleashing it as a torrent of devastating power.",
                    "Make a ranged Strike against an opponent within 60 feet using a one-handed, agile, or finesse melee weapon, or your fist unarmed attack. The attack is made at your normal proficiency with the chosen weapon or fist unarmed attack and has the same traits, damage dice, and runes, but all damage dealt by the attack is force damage.",
                    [])
                .WithAvailableAsArchetypeFeat(ModData.Traits.SpiritWarriorArchetype)
                .WithActionCost(2);
            SwFeats.CreateSwordLightWaveLogic(swordLightWave);
            ModManager.AddFeat(swordLightWave);
            Feat godsPalm = new TrueFeat(ModManager.RegisterFeatName("GodsPalm", "Gods Palm"), 8,
                    "You control your spirit energy when you attack, using it to reinforce yourself or to thrust past your enemy’s physical defenses.",
                    "Make a fist Strike; on a success, you can choose to either deal all damage from the attack as force damage, or deal damage as normal but gain a number of temporary Hit Points equal to half your level that last for 1 round.",
                    [Trait.Flourish])
                .WithAvailableAsArchetypeFeat(ModData.Traits.SpiritWarriorArchetype)
                .WithActionCost(1);
            SwFeats.CreateGodsPalmLogic(godsPalm);
            ModManager.AddFeat(godsPalm);
            Feat shelteringPulse = new TrueFeat(ModManager.RegisterFeatName("ShelteringPulse", "Sheltering Pulse"), 8,
                "You thrust your hand or weapon into the ground and release a pulse that creates a sheltering nexus of energy for you and your allies.",
                "Choose an unoccupied square within 15 feet. The nexus appears in a 15-foot emanation around that square and lasts for 3 rounds. You and your allies gain a +1 status bonus to AC while in the area.",
                [Trait.Manipulate])
                .WithAvailableAsArchetypeFeat(ModData.Traits.SpiritWarriorArchetype)
                .WithActionCost(2);
            SwFeats.CreateShelteringPulseLogic(shelteringPulse);
            ModManager.AddFeat(shelteringPulse);
        }
        private static void AddOverwhelmingCombination(Feat overwhelmingCombinationFeat)
        {
            overwhelmingCombinationFeat.WithPermanentQEffect(null, delegate (QEffect self)
            {
                self.ProvideMainAction = overwhelmingCombinationAction =>
                {
                    if (self.Owner.WieldsItem(Trait.Finesse) | self.Owner.WieldsItem(Trait.Agile) | !self.Owner.WieldsItem(Trait.TwoHanded) & !self.Owner.WieldsItem(Trait.Ranged))
                    {
                        return new ActionPossibility(SwHelpers.CreateOverwhelmingCombinationAction(overwhelmingCombinationAction.Owner));
                    }
                    return null;
                };
                self.AfterYouTakeAction = (_, action) => 
                {
                    if (action.ActionId == ModData.ActionIds.OverwhelmingCombination && self.Owner.HasEffect(ModData.QEffectIds.Oath))
                        self.Owner.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.Oath);
                    return Task.CompletedTask;
                };
            });
        }

        private static void AddParryAction(Feat parryAction)
        {
            parryAction.WithPermanentQEffect("You raise your fist to parry oncoming attacks, granting yourself a +1 circumstance bonus to AC.", delegate (QEffect self)
            {
                self.ProvideMainAction = parry => self.Owner.CanMakeBasicUnarmedAttack ? new ActionPossibility(SwHelpers.CreateParryAction(parry.Owner)) : null;
                self.Name = "Parry (fist) {icon:Action}";
            });
        }
    }
}
