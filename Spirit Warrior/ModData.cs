using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Spirit_Warrior
{
    public static class ModData
    {
        public static class Traits
        {
            public static readonly Trait SpiritWarriorArchetype = ModManager.RegisterTrait("SpiritWarrior", new TraitProperties("Spirit Warrior", true));
            public static readonly Trait SpiritWarrior = ModManager.RegisterTrait("SpiritWarrior", new TraitProperties("Spirit Warrior", true));
        }
        public static class FeatNames
        {
            public static readonly FeatName UnholyBane = ModManager.RegisterFeatName("Archetype.SpiritWarrior.UnholyBane", "Unholy Bane Oath");
            public static readonly FeatName MageBreaker = ModManager.RegisterFeatName("Archetype.SpiritWarrior.MageBreaker", "Mage Breaker Oath");
            public static readonly FeatName FlowingPalm = ModManager.RegisterFeatName("Archetype.SpiritWarrior.FlowingPalm", "Flowing Palm Deflection");
            public static readonly FeatName CuttingHeaven = ModManager.RegisterFeatName("Archetype.SpiritWarrior.CuttingHeaven", "Cutting Heaven, Crushing Earth");
            public static readonly FeatName CuttingHeavenL = ModManager.RegisterFeatName("Archetype.SpiritWarrior.CuttingHeavenL", "Apply to Left Hand");
            public static readonly FeatName CuttingHeavenR = ModManager.RegisterFeatName("Archetype.SpiritWarrior.CuttingHeavenR", "Apply to Right Hand");
        }
        internal static class QEffectIds
        {
            internal static QEffectId UnholyBane { get; } = ModManager.RegisterEnumMember<QEffectId>("Unholy Bane Oath");
            internal static QEffectId FlowingPalm { get; } = ModManager.RegisterEnumMember<QEffectId>("Flowing Palm Deflection");
            internal static QEffectId CuttingHeavenFistBuff { get; } = ModManager.RegisterEnumMember<QEffectId>("Cutting Heaven Fist");
            internal static QEffectId CuttingHeavenWeaponBuff { get; } = ModManager.RegisterEnumMember<QEffectId>("Cutting Heaven Weapon");
            internal static QEffectId CrushingEarthWeapon { get; } = ModManager.RegisterEnumMember<QEffectId>("Crushing Earth Weapon");
            internal static QEffectId OldPotency { get; } = ModManager.RegisterEnumMember<QEffectId>("Old Potency");
            internal static QEffectId OldStriking { get; } = ModManager.RegisterEnumMember<QEffectId>("Old Striking");
            internal static QEffectId OldProperty1 { get; } = ModManager.RegisterEnumMember<QEffectId>("Old Property1");
            internal static QEffectId OldProperty2 { get; } = ModManager.RegisterEnumMember<QEffectId>("Old Property2");
            internal static QEffectId OldProperty3 { get; } = ModManager.RegisterEnumMember<QEffectId>("Old Property3");
            internal static QEffectId OldName { get; } = ModManager.RegisterEnumMember<QEffectId>("SW Old Name");
            internal static QEffectId Pulse { get; } = ModManager.RegisterEnumMember<QEffectId>("Pulse");
            internal static QEffectId Oath { get; } = ModManager.RegisterEnumMember<QEffectId>("Oath");

        }
        internal static class ActionIds
        {
            internal static readonly ActionId SwordLightWave = ModManager.RegisterEnumMember<ActionId>("SwordLightWave");
            internal static readonly ActionId OverwhelmingCombination = ModManager.RegisterEnumMember<ActionId>("OverwhelmingCombination");
        }

        internal static class MIllustrations
        {
            internal static readonly Illustration OverwhelmingCombination =
                new ModdedIllustration("TXAssets/OCAction.png");
        }
    }
}
