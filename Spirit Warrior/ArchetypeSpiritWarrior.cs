using Dawnsbury;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks.Monsters.L3;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using static Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.BarbarianFeatsDb.AnimalInstinctFeat;
using static System.Net.Mime.MediaTypeNames;

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
                    "You’re a warrior who trains your spirit and body to work in perfect harmony, enhancing your attacks with your spiritual energy while fighting with a ferocious martial technique that combines blade and fist.",
                    "The damage die for your fist changes to 1d6 instead of 1d4, and your fist gains the parry trait. You don’t take the normal –2 circumstance penalty when making a lethal attack with your fist or any other unarmed attacks. You gain the Overwhelming Combination action. " +
                    "\n\n Activate—Overwhelming Combination [one-action] (flourish); Requirements You’re wielding a one-handed melee weapon or a melee weapon with the agile or finesse trait; Effect Make two Strikes against a target within your reach, one with the required weapon and one with your fist unarmed attack. If both hit the same target, combine their damage for the purposes of its resistances and weaknesses. Apply your multiple attack penalty to each Strike normally.")

            .WithOnCreature(self =>
            {
                self.WithUnarmedStrike(Item.ImprovedFist());
                self.AddQEffect(new QEffect()
                {
                    Id = QEffectId.PowerfulFist
                });

            })
            ;
            AddOverwhelmingCombination(spiritWarriorDedication);
            AddParryAction(spiritWarriorDedication);
            ModManager.AddFeat(spiritWarriorDedication);
            Feat unholyBane = new TrueFeat(
                    ModData.FeatNames.UnholyBane,
                    4,
                    "You’ve sworn an oath to defend the helpless from dangerous unholy creatures, specifically the undead and demons that threaten the Points of Light.",
                    "Attacks made as part of your Overwhelming Combination ability gain a circumstance bonus to damage against undead and demons equal to 2 times the number of weapon damage dice. You also gain a +2 circumstance bonus to saves against spells from demons and undead.",
                    [Trait.Archetype, Trait.Homebrew])
                .WithAvailableAsArchetypeFeat(ModData.Traits.SpiritWarriorArchetype)
                .WithOnCreature(self =>
                {
                    self.AddQEffect(new QEffect()
                    {
                        Id = ModData.QEffectIds.UnholyBane
                    });
                }
                )

                .WithPermanentQEffect("Attacks made as part of your Overwhelming Combination ability gain a +2 circumstance bonus to damage against undead and demons, or +4 if you have master proficiency with the weapon you used. You also gain a +2 circumstance bonus to saves against spells from demons and undead.",
                (Action<QEffect>)(qf => qf.BonusToDefenses = (Func<QEffect, CombatAction, Defense, Bonus>)((_, action, defense) =>
                    {
                        if (action == null || !action.HasTrait(Trait.Spell) || !defense.IsSavingThrow() && !action.Owner.HasTrait(Trait.Undead | Trait.Demon))
                            return (Bonus)null;
                        int amount = 2;
                        return new Bonus(amount, BonusType.Circumstance, "Unholy Bane Oath");
                    }
                )));

            ModManager.AddFeat(unholyBane);
            Feat flowingPalm = new TrueFeat(
                ModData.FeatNames.FlowingPalm,
                6,
                "The simple and precise movements of your hands allow you to deflect blows with the same efficacy as a raised shield.",
                "When you parry with your fist, increase the circumstance bonus to AC it grants from +1 to +2.",
                [Trait.Archetype])
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
            Feat cuttingHeaven = new TrueFeat(
                ModData.FeatNames.CuttingHeaven,
                6,
                "Your skill in combining fist and blade has grown into a seamless art where each attack makes an opponent more vulnerable to the next.",
                "As long as you have invested and are wearing a set of handwraps of mighty blows, you also apply their runes to a single weapon you’re wielding that can be used with your Overwhelming Combination ability.You gain the following benefits. When you successfully Strike an opponent with this weapon, it’s off - guard to the next Strike you make against it with a fist unarmed attack before the end of your next turn. When you successfully Strike an opponent with your fist unarmed attack, it’s off - guard to the next Strike you make against it with a one - handed, agile, or finesse melee weapon before the end of your next turn.",
                [Trait.Archetype])
                .WithAvailableAsArchetypeFeat(ModData.Traits.SpiritWarriorArchetype)
                .WithOnCreature(self => {self.AddQEffect(CrushingHeavenLogic.CuttingHeavenWeaponQEffect());})
                .WithOnCreature(self => { self.AddQEffect(CrushingHeavenLogic.SWInvestedWeaponQEffect()); })
                .WithPermanentQEffect("You apply runes from your handwraps to your weapon", qfFeat =>
                {
                    qfFeat.Innate = false;
                    qfFeat.StartOfCombat = (Func<QEffect, Task>)(async innerSelf =>
                    {
                        List<string> itemOptionsString = new List<string>();
                        List<Item> itemOptions = new List<Item>();
                        foreach (Item item in innerSelf.Owner.HeldItems.Where(Item => SWHelpers.IsItemOCWeapon(Item) == true))
                        {
                            itemOptionsString.Add(item.Name);
                            itemOptions.Add(item);
                        }
                        itemOptionsString.Add("none");
                        ChoiceButtonOption chosenOption = await innerSelf.Owner.AskForChoiceAmongButtons(
                            IllustrationName.QuestionMark,
                            "Choose a weapon to apply your handwraps runes to.",
                            itemOptionsString.ToArray()
                            );
                        if (itemOptionsString[chosenOption.Index] != "none")
                        {
                            Item targetItem = itemOptions[chosenOption.Index];
                            CAHelpers.ChooseWeaponStart(innerSelf.Owner, targetItem);
                        }
 
                    });
                }
                )
                .WithPermanentQEffect("After weapon Strikes the target is debuffed against fist Strikes, and after fist Strikes the enemy is debuffed against weapon Strikes.", delegate (QEffect self)
                 {
                     self.AfterYouTakeActionAgainstTarget = async (QEffect addingEffects, CombatAction action, Creature defender, CheckResult result) =>
                     {
                         // If you attack with a weapon which qualifies for Overwhelming Combination, you gain a buff to your Fist attack
                         if (action.HasTrait(Trait.Melee) && result >= CheckResult.Success  && !action.HasTrait(Trait.Unarmed) && (!action.HasTrait(Trait.TwoHanded) | action.HasTrait(Trait.Finesse) | action.HasTrait(Trait.Agile)) && !addingEffects.Owner.QEffects.Any(qe => qe.Id == ModData.QEffectIds.CuttingHeavenFistBuff && qe.Tag != null && qe.Tag == defender))
                         {
                             addingEffects.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                             {
                                 Id = ModData.QEffectIds.CuttingHeavenFistBuff,
                                 CannotExpireThisTurn = true,
                                 Tag = defender,
                                 BeforeYourActiveRoll = async (QEffect rollEffect, CombatAction action, Creature attackedCreature) =>
                                 {
                                     if (action.HasTrait(Trait.Strike) && action.HasTrait(Trait.Fist) && defender == attackedCreature)
                                     {
                                         QEffect flatFooted = QEffect.FlatFooted("Cutting Heaven, Crushing Earth");
                                         flatFooted.ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction;
                                         attackedCreature.AddQEffect(flatFooted);
                                         rollEffect.Owner.RemoveAllQEffects(qe => qe.Id == ModData.QEffectIds.CuttingHeavenFistBuff && qe.Tag != null && qe.Tag == defender);
                                     }
                                 }
                             });
                         }
                         // If you attack with your Fist you gain a buff to your weapon which qualifies for Overwhelming Combination
                         else if (action.HasTrait(Trait.Fist) && result >= CheckResult.Success && !addingEffects.Owner.QEffects.Any(qe => qe.Id == ModData.QEffectIds.CuttingHeavenWeaponBuff && qe.Tag != null && qe.Tag == defender))
                         {
                             addingEffects.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                             {
                                 Id = ModData.QEffectIds.CuttingHeavenWeaponBuff,
                                 CannotExpireThisTurn = true,
                                 Tag = defender,
                                 BeforeYourActiveRoll = async (QEffect rollEffect, CombatAction action, Creature attackedCreature) =>
                                 {
                                     if (action.HasTrait(Trait.Strike) && !action.HasTrait(Trait.Unarmed) && action.HasTrait(Trait.Melee) && (!action.HasTrait(Trait.TwoHanded) | action.HasTrait(Trait.Finesse) | action.HasTrait(Trait.Agile)) && defender == attackedCreature)
                                     {
                                         QEffect flatFooted = QEffect.FlatFooted("Cutting Heaven, Crushing Earth");
                                         flatFooted.ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction;
                                         attackedCreature.AddQEffect(flatFooted);
                                         rollEffect.Owner.RemoveAllQEffects(qe => qe.Id == ModData.QEffectIds.CuttingHeavenWeaponBuff && qe.Tag != null && qe.Tag == defender);
                                     }
                                 }
                             });
                         }
                     };
                 });
            ModManager.AddFeat(cuttingHeaven);

        }
        public static void AddOverwhelmingCombination(Feat overwhelmingcombinationFeat)
        {
            overwhelmingcombinationFeat.WithPermanentQEffect(null, delegate (QEffect self)
            {
                self.ProvideMainAction = (QEffect overwhelmingCombinationAction) =>
                {
                    if (self.Owner.WieldsItem(Trait.Finesse) | self.Owner.WieldsItem(Trait.Agile) | !self.Owner.WieldsItem(Trait.TwoHanded) & !self.Owner.WieldsItem(Trait.Ranged))
                    {
                        return new ActionPossibility(SWHelpers.CreateOverwhelmingCombinationAction(overwhelmingCombinationAction.Owner));
                    }
                    return null;
                };
            });
        }
        public static void AddParryAction(Feat parryaction)
        {
            parryaction.WithPermanentQEffect(null, delegate (QEffect self)
            {
                self.ProvideMainAction = (QEffect parryaction) =>
                {
                    if (self.Owner.CanMakeBasicUnarmedAttack)
                    {
                        return new ActionPossibility(SWHelpers.CreateParryAction(parryaction.Owner));
                    }
                    else return null;
                };
            });
        }
    }
}
