# Toolbox for Unity

A collection of utilities and reusable code I use in my prototypes and other projects. Amassed over time via tutorials and guides.

Requires Unity **6000.1+**.

# Installation

In the Package Manager (`Window > Package Manager`), click **+** and choose **Install package from git URL…** ([Unity manual](https://docs.unity3d.com/Manual/upm-ui-giturl.html)), then paste:

```
https://github.com/mactinite/UnityToolbox.git#main
```

Or add it to `Packages/manifest.json` directly:

```json
"toolbox": "https://github.com/mactinite/UnityToolbox.git#main"
```

# Modules

## Ability System (`toolbox.AbilitySystem`)

A generalized gameplay ability system in the spirit of Unreal's GAS: **tags** (ScriptableObject
assets with parent hierarchy), **attributes** (SO identities + per-owner values with
Add/PercentAdd/PercentMult/Override modifier channels), **gameplay effects**
(Instant/Duration/Infinite, periodic execution, stacking, tag granting/gating), and **abilities**
(cost + cooldown expressed as effects, tick-based lifecycle). All logic lives in plain C#
(`AbilitySystemCore`) behind a thin `AbilitySystemComponent`, so it is fully edit-mode testable —
the test suite in `Tests/` doubles as usage examples.

Games extend it through one pattern — subclass the definition with data, subclass the spec with behaviour:

```csharp
[CreateAssetMenu(menuName = "MyGame/Abilities/Fireball")]
public class FireballDefinition : GameplayAbilityDefinition
{
    public GameObject projectilePrefab;
    public override AbilitySpec CreateSpec(AbilitySystemCore owner) => new FireballSpec(this, owner);
}

public class FireballSpec : AbilitySpec
{
    public FireballSpec(FireballDefinition definition, AbilitySystemCore owner) : base(definition, owner) { }

    protected override void OnActivate()
    {
        var host = (AbilitySystemComponent)Owner.Context; // reach the scene
        // spawn projectile, then finish:
        End();
    }
}
```

Activation runs `CanActivate` (tag requirements, blocks, cooldown tag, full cost simulation),
cancels conflicting abilities, grants activation tags, commits cost + cooldown, then `OnActivate`.
Ongoing abilities get `OnTick(dt)` until they call `End()` or something `Cancel()`s them.

**Demo:** open `Samples/AbilitySystemDemo/AbilitySystemDemo.unity` and press Play — WASD to move,
Space to jump (stamina cost + cooldown), hold Shift to sprint (speed buff + stamina drain, ends on
exhaustion). The on-screen overlay shows live attributes, tags, active effects and cooldowns.
The demo needs the Input System package; the ability system itself has no dependencies.

## Options (`toolbox.Options`)

Player-facing settings: a typed model, a versioned JSON file, and ready-made display and quality
packs. The game declares its options once as static fields and reacts to them with `Bind`. The
core has no dependencies; a uGUI menu module (`toolbox.Options.UI`) that builds a settings screen
from the registered options is the next step.

```csharp
[OptionsCatalog]
public static class MyOptions
{
    public static readonly FloatOption MasterVolume =
        new FloatOption("audio.master", 1f) { Category = "Audio", Format = OptionFormat.Percent };

    public static readonly BoolOption ScreenShake = new BoolOption("gameplay.screenShake", true);

    public static readonly EnumOption<TextSpeed> TextSpeed = new EnumOption<TextSpeed>("gameplay.textSpeed", TextSpeed.Normal);
}

// At startup (AfterAssembliesLoaded or later):
var store = OptionsStore.Default;
store.RegisterCatalog(typeof(MyOptions));
store.AddDisplay();      // display.mode / resolution / monitor / vsync / fpsCap, with their applier
store.AddQuality();      // graphics.quality from the project's quality levels
store.Load();

// Appliers: called now (or once loaded) and after every change.
MyOptions.MasterVolume.Bind(v => mixer.SetFloat("Master", ToDecibels(v)));
```

- **Options:** `BoolOption`, `IntOption`, `FloatOption` (range, step, `Percent` formatting),
  `ChoiceOption` (keyed choices, replaceable at runtime), `EnumOption<T>` (stored by name),
  `StringOption` (blobs such as input binding overrides). Metadata: `Label`, `Description`,
  `Category`, `Order`, `Flags` (`Hidden`, `DevOnly`, `Confirm`, `RequiresRestart`),
  `Presentation`, `VisibleWhen` / `EnabledWhen` / `AvailableWhen`, `LegacyIds`.
- **Store:** `OptionsStore` registers options (several catalogues can share one), loads and saves
  through an `IOptionsStorage` (`options.json` in the persistent data path by default,
  `PlayerPrefs`, or in-memory), keeps unknown keys, reads legacy ids, runs `AddMigration` steps
  up to `SchemaVersion`, and saves unsaved changes on quit.
- **Transactions:** `OptionsTransaction` stages `Confirm` options (display mode, resolution),
  applies them together, then `Commit`s or `Revert`s; appliers undo through the same `Bind`.
- **Compatibility promises:** ids are contracts, values are self-describing text (choice keys,
  enum names, never indices), missing keys keep defaults, unknown choices fall back to the
  default, unknown keys survive, renames go through `LegacyIds`, shape changes through migrations.

The edit-mode tests in `Tests/Options` double as the usage reference.

## Other modules (`toolbox.Runtime`)

- **ServiceLocator** — hierarchical service container (component parents → scene → global) with bootstrappers.
- **EventBus** — static generic `EventBus<T>` with auto-discovered `IEvent` buses.
- **DamageSystem** — `DamageReceiver<T>` with health, i-frames and a damage-mitigation pipeline.
- **Singleton** — `SingletonBehaviour`, persistent and regulator variants.
- **State Machine** — coroutine-based `StateMachine<T>`.
- **Extensions** — `OrNull()`, transform/vector/layer-mask/rigidbody helpers and more.
- **Attributes** — `[Layer]`, `[Scene]`, `[SortingLayer]` property attributes with drawers.

# Development

Add as an embedded package to a Unity project using git submodules:

```
git submodule add https://github.com/mactinite/UnityToolbox.git Packages/Toolbox
```

Edits made in `Packages/Toolbox` are live in the host project; commit and push from the submodule
to publish them. The edit-mode test suite (`toolbox.Tests`) appears in the Test Runner
automatically for embedded installs; for git-URL installs add `"testables": ["toolbox"]` to the
host project's `Packages/manifest.json` to see the tests.
