using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// A magnitude read from an attribute on the effect's source or target, scaled by a coefficient:
    /// <c>coefficient(level) * attributeValue</c>. The classic "damage scales with Strength".
    /// </summary>
    [CreateAssetMenu(menuName = "Toolbox/Ability System/Attribute-Backed Magnitude", fileName = "Mag_New")]
    public sealed class AttributeBackedMagnitude : MagnitudeCalculation
    {
        [SerializeField] internal AttributeDefinition attribute;
        [SerializeField] internal CaptureSource captureFrom = CaptureSource.Source;
        [SerializeField] internal CaptureTiming captureWhen = CaptureTiming.OnApplication;
        [SerializeField] internal ScalableFloat coefficient = new(1f);

        public override void OnApplication(GameplayEffectSpec spec)
        {
            if (captureWhen == CaptureTiming.OnApplication)
                spec.CaptureAttribute(attribute, captureFrom);
        }

        public override float Evaluate(GameplayEffectSpec spec) =>
            coefficient.Evaluate(spec.Level) *
            spec.ResolveAttributeValue(attribute, captureFrom, preferCaptured: captureWhen == CaptureTiming.OnApplication);
    }
}
