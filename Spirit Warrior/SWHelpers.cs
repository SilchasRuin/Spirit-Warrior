using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;

namespace Spirit_Warrior
{
    public static class SwHelpers
    {
        public static CombatAction CreateOverwhelmingCombinationAction(Creature owner)
        {
            CombatAction overwhelmingCombinationAction = new CombatAction(
                owner,
                new ModdedIllustration("TXAssets/OCAction.png"),
                "Overwhelming Combination",
                [Trait.Flourish],
                "Make two Strikes against a target within your reach, one with the required weapon and one with your fist unarmed attack. If both hit the same target, combine their damage for the purposes of its resistances and weaknesses. Apply your multiple attack penalty to each Strike normally.",
                Target.Self().WithAdditionalRestriction(_ =>
                {
                    if (!owner.CanMakeBasicUnarmedAttack)
                        return "You must be able to make a basic unarmed attack to use Overwhelming Combination.";
                    if (owner.HeldItems.Count == 0 || (!IsItemOcWeapon(owner.HeldItems[0]) && !IsItemOcWeapon(owner.HeldItems[1])))
                        return "You must be wielding a proper weapon to use Overwhelming Combination.";
                    return (owner.Weapons.Any(weapon => weapon.HasTrait(Trait.Fist) && CommonRulesConditions.CouldMakeStrike(owner, weapon)) ? null : "There is no nearby enemy or you can't make attacks.")!;
                }))
            {
                ShortDescription = "Make two Strikes against a target within your reach."
            }
            .WithActionCost(1)
            .WithEffectOnSelf(async (action, innerSelf) =>
             {
                 List<Creature>? chosenCreatures = [];
                 var hpBefore = -1;
                 // A helper method to handle the strikes and steps
                 async Task HandleStrikes(Item weapon)
                 {
                     // Adds the weapon to usage
                     CombatAction strike = innerSelf.CreateStrike(weapon).WithActionCost(0);
                     List<Option> possibilities = [];
                     GameLoop.AddDirectUsageOnCreatureOptions(strike, possibilities, true);

                     // Creates the possibilities and prompts for user selection
                     if (possibilities.Count > 0)
                     {
                         Option chosenOption;
                         if (possibilities.Count >= 2)
                         {
                             var result = await innerSelf.Battle.SendRequest(new AdvancedRequest(innerSelf, "Choose a creature to Strike.", possibilities)
                             {
                                 TopBarText = "Choose a creature to Strike.",
                                 TopBarIcon = weapon.Illustration
                             });
                             chosenOption = result.ChosenOption;
                         }
                         else
                             chosenOption = possibilities[0];
                         Item ocWeapon = IsItemOcWeapon(owner.HeldItems[0]) ? owner.HeldItems[0] : owner.HeldItems[1];
                         QEffect oathBonus = new QEffect()
                         {
                             BonusToDamage = (Func<QEffect, CombatAction, Creature, Bonus>)((_, _, _) => new Bonus(innerSelf.Proficiencies.Get(ocWeapon.Traits) >= Proficiency.Master ? 4 : 2, BonusType.Circumstance, "Oath"))
                         };
                             if (chosenOption is CreatureOption creatureOption2)
                             {
                                 if (hpBefore == -1)
                                     hpBefore = creatureOption2.Creature.HP;
                                 chosenCreatures.Add(creatureOption2.Creature);
                             }
                             if (chosenOption is CancelOption)
                             {
                                 action.RevertRequested = true;
                                 chosenCreatures = null;
                                 return;
                             }
                             if (chosenOption is CreatureOption chosenCreature)
                                 if (innerSelf.FindQEffect(ModData.QEffectIds.UnholyBane) != null)
                                 {
                                     Creature creature = chosenCreature.Creature;
                                     if (OathAgainst(innerSelf, creature))
                                         innerSelf.AddQEffect(oathBonus);
                                 }
                             _ = await chosenOption.Action() ? 1 : 0;
                     }
                 }
                 {
                     Item ocWeapon = IsItemOcWeapon(owner.HeldItems[0]) ? owner.HeldItems[0] : owner.HeldItems[1];
                     if (owner.HeldItems.Count == 2 && IsItemOcWeapon(owner.HeldItems[0]) && IsItemOcWeapon(owner.HeldItems[1]))
                     {
                         Item ocWeapon2 = owner.HeldItems[1];
                         Item fist = owner.UnarmedStrike.WithMainTrait(Trait.Fist);
                         ChoiceButtonOption choice = await innerSelf.AskForChoiceAmongButtons(IllustrationName.QuestionMark, "Choose which Strike to use first.", ocWeapon.BaseItemName.HumanizeTitleCase2(), ocWeapon2.BaseItemName.HumanizeTitleCase2(), fist.BaseItemName.HumanizeTitleCase2(), "Cancel");
                         if (choice.Index != 3)
                         {
                             Item remainingWeapon;
                             if (choice.Index == 0 || choice.Index == 2)
                             {
                                 Item weapon = (choice.Index == 0) ? ocWeapon : fist;
                                 remainingWeapon = (choice.Index == 0) ? fist : ocWeapon;
                                 await HandleStrikes(weapon);
                                 await HandleStrikes(remainingWeapon);
                             }
                             if (choice.Index == 1)
                             {
                                 Item weapon = (choice.Index == 1) ? ocWeapon2 : fist;
                                 remainingWeapon = fist;
                                 await HandleStrikes(weapon);
                                 await HandleStrikes(remainingWeapon);
                             }
                         }
                         if (choice.Index == 3)
                         {
                             action.RevertRequested = true;
                         }
                     }
                     else
                     {
                         Item fist = owner.UnarmedStrike.WithMainTrait(Trait.Fist);
                         ChoiceButtonOption choice = await innerSelf.AskForChoiceAmongButtons(IllustrationName.QuestionMark, "Choose which Strike to use first.", ocWeapon.BaseItemName.HumanizeTitleCase2(), fist.BaseItemName.HumanizeTitleCase2(), "Cancel");
                         if (choice.Index != 2)
                         {
                             Item weapon = (choice.Index == 0) ? ocWeapon : fist;
                             Item remainingWeapon = (choice.Index == 0) ? fist : ocWeapon;
                             await HandleStrikes(weapon);
                             await HandleStrikes(remainingWeapon);
                         }
                         if (choice.Index == 2)
                         {
                             action.RevertRequested = true;
                         }
                     }
                 }
            });
            return overwhelmingCombinationAction;
        }
        public static bool IsItemOcWeapon(Item item)
        {
            return (item.HasTrait(Trait.Finesse) || item.HasTrait(Trait.Agile) || !item.HasTrait(Trait.TwoHanded)) && !item.HasTrait(Trait.Ranged);
        }
        public static CombatAction CreateParryAction(Creature owner)
        {
            CombatAction parryAction = new CombatAction(owner, new ModdedIllustration("TXAssets/FistParry.png"), "Parry Fist", [], "You raise your fist to parry oncoming attacks, granting yourself a +1 circumstance bonus to AC.", Target.Self())
                    .WithSoundEffect(SfxName.RaiseShield)
                    .WithActionCost(1)
                    .WithEffectOnSelf(you =>
                    {
                        you.AddQEffect(new QEffect("Parrying with fist", "You have a +1 circumstance bonus to AC.", ExpirationCondition.ExpiresAtStartOfYourTurn, you, new ModdedIllustration("TXAssets/FistParry.png"))
                            {
                                BonusToDefenses = delegate (QEffect _, CombatAction? _, Defense defense)
                                {
                                    QEffect? qEffect = you.FindQEffect(ModData.QEffectIds.FlowingPalm);
                                    if (defense != Defense.AC) return null;
                                    var amount = 1;
                                    if (qEffect != null)
                                        amount = 2;
                                    return new Bonus(amount, BonusType.Circumstance, "parry");
                                }
                            });
                    }
                    );
            return parryAction;
        }
        private static bool OathAgainst(Creature spiritWarrior, Creature attacker)
        {
            QEffect? qEffect = spiritWarrior.FindQEffect(ModData.QEffectIds.UnholyBane);
            return qEffect != null && attacker.Traits.Contains((Trait.Undead)) || attacker.Traits.Contains((Trait.Demon));
        }

        /*public static void AlternateOcAction(TrueFeat overwhelmingCombinationAction)
        {
            overwhelmingCombinationAction.WithPermanentQEffect(
                "Make two Strikes against a target within your reach, one with the required weapon and one with your fist unarmed attack. If both hit the same target, combine their damage for the purposes of its resistances and weaknesses. Apply your multiple attack penalty to each Strike normally.",
                qfInner =>
                {
                    qfInner.ProvideStrikeModifier = item =>
                    {
                        if (IsItemOcWeapon(item) != true && !item.HasTrait(Trait.Fist))
                            return null;
                        if (IsItemOcWeapon(item))
                        {
                            CombatAction ocAction = qfInner.Owner.CreateStrike(item,
                                qfInner.Owner.Actions.AttackedThisManyTimesThisTurn);
                            ocAction.Description = StrikeRules.CreateBasicStrikeDescription4(
                                ocAction.StrikeModifiers,
                                additionalAttackRollText: "Make an attack with your fist after making this attack");
                            ocAction.EffectOnOneTarget = async (innerAction, self, target, result) =>
                            {
                                await innerAction.AllExecute();
                                self.CreateStrike(self.UnarmedStrike.WithMainTrait(Trait.Fist), -1, null).WithActionCost(0);
                            };
                            return ocAction;
                        }

                        if (item.HasTrait(Trait.Fist))
                        {
                            CombatAction ocAction = qfInner.Owner.CreateStrike(item,
                                qfInner.Owner.Actions.AttackedThisManyTimesThisTurn);
                            ocAction.Description = StrikeRules.CreateBasicStrikeDescription4(
                                ocAction.StrikeModifiers,
                                additionalAttackRollText: "Make an attack with a qualifying weapon after this");
                            ocAction.EffectOnOneTarget = async (innerAction, self, target, result) =>
                            {
                                foreach (Item weapon in self.HeldItems)
                                {
                                    
                                }
                                await innerAction.AllExecute();
                                self.CreateStrike(self.UnarmedStrike.WithMainTrait(Trait.Fist), -1, null).WithActionCost(0);
                            };
                        }
                        
                    };
                }
            );
        }*/
    }
}