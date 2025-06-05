using Dawnsbury;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using System.Runtime.Serialization;

namespace Spirit_Warrior
{
    public static class SWHelpers
    {
        public static CombatAction CreateOverwhelmingCombinationAction(Creature owner)
        {
            CombatAction overwhelmingCombinationAction = new CombatAction(
                owner,
                new ModdedIllustration("TXAssets/OCAction.png"),
                "Overwhelming Combination",
                [Trait.Flourish],
                "Make two Strikes against a target within your reach, one with the required weapon and one with your fist unarmed attack. If both hit the same target, combine their damage for the purposes of its resistances and weaknesses. Apply your multiple attack penalty to each Strike normally.",
                (Target)Target.Self().WithAdditionalRestriction((Func<Creature, string>)(Self =>
                {
                    if (!(owner.CanMakeBasicUnarmedAttack))
                        return "You must be able to make a basic unarmed attack to use Overwhelming Combination.";
                    return owner.Weapons.Any<Item>((Func<Item, bool>)(weapon => weapon.HasTrait(Trait.Fist) && CommonRulesConditions.CouldMakeStrike(owner, weapon))) ? (string)null : "There is no nearby enemy or you can't make attacks.";
                })))
            {
                ShortDescription = "Make two Strikes against a target within your reach."
            }
            .WithActionCost(1)
            .WithEffectOnSelf(async (CombatAction action, Creature innerSelf) =>
             {
                 List<Creature> chosenCreatures = new List<Creature>();
                 int hpBefore = -1;
                 // A helper method to handle the strikes and steps
                 async Task HandleStrikes(Item weapon)
                 {
                     // Adds the weapon to useage
                     CombatAction strike = innerSelf.CreateStrike(weapon).WithActionCost(0);
                     List<Option> possibilities = new List<Option>();
                     GameLoop.AddDirectUsageOnCreatureOptions(strike, possibilities, true);

                     // Creates the possibilites and prompts for user selection
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
                         Item ocWeapon = IsItemOCWeapon(owner.HeldItems[0]) ? owner.HeldItems[0] : owner.HeldItems[1];
                         QEffect oathBonus = new QEffect()
                         {
                             BonusToDamage = (Func<QEffect, CombatAction, Creature, Bonus>)((effect, action, theDefender) => new Bonus(innerSelf.Proficiencies.Get(ocWeapon.Traits) >= Proficiency.Master ? 4 : 2, BonusType.Circumstance, "Oath"))
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
                                 chosenCreatures = (List<Creature>)null;
                                 return;
                             }
                             if (chosenOption is CreatureOption chosenCreature)
                             {
                                 Creature creature = chosenCreature.Creature;
                                 if (OathAgainst(innerSelf, creature))
                                     innerSelf.AddQEffect(oathBonus);
                             }
                             int num = await chosenOption.Action() ? 1 : 0;
                         }
                         
                     }
             {
                 Item ocWeapon = IsItemOCWeapon(owner.HeldItems[0]) ? owner.HeldItems[0] : owner.HeldItems[1];
                     if (owner.HeldItems.Count == 2 && IsItemOCWeapon(owner.HeldItems[0]) && IsItemOCWeapon(owner.HeldItems[1]))
                     {
                         Item ocWeapon2 = owner.HeldItems[1];
                         Item fist = owner.UnarmedStrike.WithMainTrait(Trait.Fist);
                         ChoiceButtonOption choice = await innerSelf.AskForChoiceAmongButtons(IllustrationName.QuestionMark, "Choose which Strike to use first.", ocWeapon.BaseItemName.HumanizeTitleCase2(), ocWeapon2.BaseItemName.HumanizeTitleCase2(), fist.BaseItemName.HumanizeTitleCase2(), "Cancel");
                         Item remainingWeapon = ocWeapon;
                         if (choice.Index != 3)
                         {
                             if (choice.Index == 0 || choice.Index == 2)
                             {
                                 Item weapon = (choice.Index == 0) ? ocWeapon : fist;
                                 remainingWeapon = (choice.Index == 0) ? fist : ocWeapon;
                                 await HandleStrikes(weapon);
                                 if (await innerSelf.AskForConfirmation(remainingWeapon.Illustration, "Make a strike?", "Yes"))
                                 {
                                     await HandleStrikes(remainingWeapon);
                                 }
                             }
                             if (choice.Index == 1)
                             {
                                 Item weapon = (choice.Index == 1) ? ocWeapon2 : fist;
                                 remainingWeapon = fist;
                                 await HandleStrikes(weapon);
                                 if (await innerSelf.AskForConfirmation(remainingWeapon.Illustration, "Make a strike?", "Yes"))
                                 {
                                     await HandleStrikes(remainingWeapon);
                                 }
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
                         Item remainingWeapon = ocWeapon;
                         if (choice.Index != 2)
                         {
                             Item weapon = (choice.Index == 0) ? ocWeapon : fist;
                             remainingWeapon = (choice.Index == 0) ? fist : ocWeapon;
                             await HandleStrikes(weapon);
                             if (await innerSelf.AskForConfirmation(remainingWeapon.Illustration, "Make a strike?", "Yes"))
                             {
                                 await HandleStrikes(remainingWeapon);
                             }
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

        public static bool IsItemOCWeapon(Item item)
        {
            if (item.HasTrait(Trait.Finesse) | item.HasTrait(Trait.Agile) | !item.HasTrait(Trait.TwoHanded) & !item.HasTrait(Trait.Ranged))
            {
                return true;
            }
            return false;
        }
        public static CombatAction CreateParryAction(Creature owner)
        {
            CombatAction parryaction = new CombatAction(owner, new ModdedIllustration("TXAssets/FistParry.png"), "Parry Fist", [], "You raise your fist to parry oncoming attacks, granting yourself a +1 circumstance bonus to AC.", Target.Self())
                    .WithSoundEffect(SfxName.RaiseShield)
                    .WithActionCost(1)
                    .WithEffectOnSelf(you =>
                    {
                        you.AddQEffect(new QEffect("Parrying with fist", "You have a +1 circumstance bonus to AC.", ExpirationCondition.ExpiresAtStartOfYourTurn, you, new ModdedIllustration("TXAssets/FistParry.png"))
                            {
                                BonusToDefenses = delegate (QEffect parrying, CombatAction? bonk, Defense defense)
                                {
                                    QEffect qeffect = you.FindQEffect(ModData.QEffectIds.FlowingPalm);
                                    if (defense == Defense.AC)
                                    {
                                        int amount = 1;
                                        if (qeffect != null)
                                            amount = 2;
                                        return new Bonus(amount, BonusType.Circumstance, "parry");
                                    }
                                    else return null;
                                },

                            });
                    }
                    );
            return parryaction;
        }
        private static bool OathAgainst(Creature spiritwarrior, Creature attacker)
        {
            QEffect qeffect = spiritwarrior.FindQEffect(ModData.QEffectIds.UnholyBane);
            return qeffect != null && attacker.Traits.Contains((Trait.Undead)) || attacker.Traits.Contains((Trait.Demon));
        }
    }
}