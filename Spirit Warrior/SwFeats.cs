using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Spirit_Warrior;

public abstract class SwFeats
{
    private static readonly TileQEffectId Shelter = ModManager.RegisterEnumMember<TileQEffectId>("Shelter");
    internal static void CreateSwordLightWaveLogic(Feat swordLight)
    {
        swordLight.WithPermanentQEffect(null, effect =>
        {
            Creature self = effect.Owner;
            effect.ProvideStrikeModifier = item =>
            {
                if (SwHelpers.IsItemOcWeapon(item) || item.HasTrait(Trait.Fist))
                {
                    Item dupe = !item.HasTrait(Trait.Fist) ? item.Duplicate() : Item.ImprovedFist();
                    if (item.Name == "Fleshgem Fist")
                    {
                        dupe.Traits.Remove(Trait.Agile);
                        dupe.WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing));
                    }
                    dupe.Traits.Remove(Trait.Melee);
                    dupe.Traits.Add(Trait.Ranged);
                    dupe.WithAdditionalWeaponProperties(properties =>
                        properties.WithRangeIncrement(12).WithMaximumRange(12));
                    Item? handwraps = StrikeRules.GetBestHandwraps(self);
                    QEffect? chosenWeaponQf = self.FindQEffect(ModData.QEffectIds.CrushingEarthWeapon);
                    if ((chosenWeaponQf?.Tag is Item tag && tag == item || item.HasTrait(Trait.Fist)) && handwraps != null)
                    {
                        CrushingHeavenLogic.ResetWeapons(dupe);
                        CrushingHeavenLogic.HandleRune(handwraps, dupe, RuneKind.WeaponPotency);
                        CrushingHeavenLogic.HandleRune(handwraps, dupe, RuneKind.WeaponStriking);
                        CrushingHeavenLogic.HandleRune(handwraps, dupe, RuneKind.WeaponProperty);
                    }
                    CombatAction slWave = self.CreateStrike(dupe);
                    slWave.Description = StrikeRules.CreateBasicStrikeDescription4(slWave.StrikeModifiers, additionalAttackRollText: "This attack will deal force damage.");
                    slWave.StrikeModifiers = new StrikeModifiers()
                        {
                            ReplacementDamageKind = DamageKind.Force,
                        };
                    slWave.WithActionCost(2);
                    slWave.Illustration = new SideBySideIllustration(item.Illustration ,IllustrationName.RadiantBeam);
                    if (slWave.HasTrait(Trait.Melee)) slWave.Traits.Remove(Trait.Melee);
                    slWave.WithActionId(ModData.ActionIds.SwordLightWave);
                    slWave.WithProjectileCone(IllustrationName.RadiantBeam, 15, ProjectileKind.Ray).WithSoundEffect(SfxName.PhaseBolt);
                    slWave.Name = "Sword-light Wave";
                    return slWave;
                }
                return null;
            };
        });
    }
    internal static void CreateGodsPalmLogic(Feat godsPalm)
    {
        godsPalm.WithPermanentQEffect(null,
            effect =>
            {
                Creature self = effect.Owner;
                effect.ProvideStrikeModifierAsPossibilities = (_,item) => item.HasTrait(Trait.Fist) ? GodsPalm(self, item) : [];
            });
    }

    internal static void CreateShelteringPulseLogic(Feat shelteringPulse)
    {
        shelteringPulse.WithPermanentQEffect(null, qf =>
            {
                Creature self = qf.Owner;
                qf.ProvideMainAction = _ =>
                {
                    CombatAction shelter = new CombatAction(self, IllustrationName.CircleOfProtection,
                            "Sheltering Pulse", [Trait.Manipulate],
                            "Choose an unoccupied square within 15 feet. The nexus appears in a 15-foot emanation around that square and lasts for 3 rounds. You and your allies gain a +1 status bonus to AC while in the area.",
                            Target.RangedEmptyTileForSummoning(3))
                        .WithActionCost(2)
                        .WithEffectOnSelf((action,_) =>
                        {
                            QEffect pulseCd = new()
                            {
                                WhenExpires = _ =>
                                {
                                    foreach (Tile tile in self.Battle.Map.AllTiles.Where(tile => tile.HasEffect(Shelter)))
                                        tile.RemoveAllQEffects(qEffect => qEffect.TileQEffectId == Shelter);
                                },
                                StateCheckWithVisibleChanges = _ =>
                                {
                                    List<Tile> pulse = [action.ChosenTargets.ChosenTile!];
                                    foreach (Tile tile in self.Battle.Map.AllTiles.Where(tile =>
                                                 tile.DistanceTo(action.ChosenTargets.ChosenTile!) <= 2))
                                    {
                                        pulse.Add(tile);
                                    }
                                    foreach (Tile tile in self.Battle.Map.AllTiles.Where(tile =>
                                                 tile.DistanceTo(action.ChosenTargets.ChosenTile!) == 3))
                                    {
                                        TileQEffect tileEf = new()
                                        {
                                            TileQEffectId = Shelter, 
                                            Illustration = IllustrationName.BlessCircle,
                                            VisibleDescription = "Sheltering Pulse",
                                        };
                                        if (!tile.HasEffect(Shelter))
                                        {
                                            tile.AddQEffect(tileEf.WithGrantsEphemeralEffectToOwner(qEffect =>
                                            {
                                                if (tile.PrimaryOccupant == null ||
                                                    !tile.PrimaryOccupant.FriendOf(self)
                                                    || tile.PrimaryOccupant.HasEffect(ModData.QEffectIds.Pulse)) return;
                                                qEffect.BonusToDefenses = (_, _, defense) =>
                                                    defense == Defense.AC
                                                        ? new Bonus(1, BonusType.Status, "Sheltering Pulse")
                                                        : null;
                                                qEffect.Illustration = IllustrationName.CircleOfProtection;
                                                qEffect.Name = "Sheltering Pulse";
                                                qEffect.Id = ModData.QEffectIds.Pulse;
                                                qEffect.Description = "As long as you remain within the circle, you gain a +1 status bonus to AC.";
                                            }));
                                        }
                                    }
                                    foreach (Tile tile in pulse)
                                    {
                                        TileQEffect tileQf = new()
                                        {
                                            TileQEffectId = Shelter,
                                            VisibleDescription = "Sheltering Pulse",
                                        };
                                        if (!tile.HasEffect(Shelter))
                                        {
                                            tile.AddQEffect(tileQf.WithGrantsEphemeralEffectToOwner(qEffect =>
                                            {
                                                if (tile.PrimaryOccupant == null ||
                                                    !tile.PrimaryOccupant.FriendOf(self)
                                                    || tile.PrimaryOccupant.HasEffect(ModData.QEffectIds.Pulse)) return;
                                                qEffect.BonusToDefenses = (_, _, defense) =>
                                                    defense == Defense.AC
                                                        ? new Bonus(1, BonusType.Status, "Sheltering Pulse")
                                                        : null;
                                                qEffect.Illustration = IllustrationName.CircleOfProtection;
                                                qEffect.Name = "Sheltering Pulse";
                                                qEffect.Id = ModData.QEffectIds.Pulse;
                                                qEffect.Description = "As long as you remain within the circle, you gain a +1 status bonus to AC.";
                                            }));
                                        }
                                    }
                                    return Task.CompletedTask;
                                }
                            };
                            pulseCd.WithExpirationAtStartOfSourcesTurn(self, 3);
                            self.AddQEffect(pulseCd);
                            return Task.CompletedTask;
                        });
                    return new ActionPossibility(shelter);
                };
            }
            );
    }
    private static IEnumerable<Possibility> GodsPalm(Creature self, Item item)
    {
        CombatAction forcePalm = self.CreateStrike(item);
        forcePalm.Traits.Add(Trait.Flourish);
        forcePalm.Traits.Add(Trait.Basic);
        forcePalm.Description = StrikeRules.CreateBasicStrikeDescription4(forcePalm.StrikeModifiers,
            additionalAttackRollText: "This attack will deal force damage.");
        forcePalm.StrikeModifiers = new StrikeModifiers()
        {
            ReplacementDamageKind = DamageKind.Force,
        };
        forcePalm.Name = "Gods Palm - Force";
        forcePalm.ContextMenuName = "Gods Palm - Force";
        yield return new ActionPossibility(forcePalm);
        CombatAction barrierPalm = self.CreateStrike(item);
        barrierPalm.Traits.Add(Trait.Flourish);
        barrierPalm.Traits.Add(Trait.Basic);
        barrierPalm.Description = StrikeRules.CreateBasicStrikeDescription4(barrierPalm.StrikeModifiers,
            additionalAttackRollText:
            "On a success, gain a number of temporary hit points equal to half your level that last for 1 round.");
        barrierPalm.WithEffectOnSelf((action, _) =>
        {
            if (action.CheckResult >= CheckResult.Success)
            {
                var barrier = self.Level / 2;
                self.GainTemporaryHP(barrier);
                self.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                    {
                        WhenExpires = qf => self.TemporaryHP = 0,
                    }
                );
            }
            return Task.CompletedTask;
        });
        barrierPalm.ContextMenuName = "Gods Palm - Temp HP";
        barrierPalm.Name = "Gods Palm - Temp HP";
        
        yield return new ActionPossibility(barrierPalm);
    }
}