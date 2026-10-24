# AGENTS.md: Larje Purchases Unity IAP

Instructions for coding agents working on this package.

## Package layout

- `Runtime/`: `Larje.Purchases.UnityIap`.
  - `UnityPurchasesService`: the `IPurchasesService` implementation on top of `PurchasesServiceBase`.
- `Zenject/`: `Larje.Purchases.UnityIap.Zenject` with the scriptable object installer. It calls the `BindLarje*Provider` extensions from `Larje.Core.Zenject`, which replace the simulated core service regardless of installer order.
- `Editor/Defines/`: registers the SDK assembly with `OptionalDependencyDefines` from core.

## Notes

- SDK code lives only in assemblies constrained by `LARJE_UNITY_IAP`. The define comes from `versionDefines` (UPM install) or from `OptionalDependencyDefines.Register` in `Editor/Defines` (DLL import).
- Keep the provider behind the core interface. Game code must not reference this package directly; new capabilities go into the core interface and its simulated implementation first.
- SDK callbacks can arrive on background threads: switch with `UniTask.SwitchToMainThread()` before touching Unity APIs or raising events.
- Compiles against com.unity.purchasing 5.4.4 in Unity 6000.4.9f1. Not tested against a store yet.

## Code style (C#)

- Do not use `var`. Write explicit types.
- Do not write comments in code.
- Do not use `continue`. Restructure the loop or extract a method.
- Seal classes when they are not designed for inheritance.
- Use block bodies `{ }` for methods and `if` statements. Expression-bodied members are fine for simple properties when the surrounding code already uses them.
- Do not name variables `value` except in property setters.
- Make private methods `static` only when a static context requires it.
- Order members as: constants, static fields, serialized fields, fields, properties, events, methods, nested enums, nested classes. Within each group order by access: public, protected, internal, private.
- Private fields use `_camelCase`, constants use `UPPER_SNAKE_CASE`.
- Serialized fields: never use `[Tooltip]`. Group related fields with `[Header]` and avoid single-field headers.

## Architecture rules

- Services are plain C# classes with constructor injection. Do not add a service locator, singletons or a custom DI container.
- Use UniTask with a `CancellationToken` for loops, delays and async work. Do not use `Update` loops or coroutines in services.
- Ids are ScriptableObject assets. Persistent data lives in save sections owned by the feature.
- Zenject code goes only into the `*.Zenject` assembly. It compiles under `LARJE_ZENJECT`, which comes from `versionDefines` on `com.larje.zenject`.
- Unity AI MCP tools go only into the `*.AI.Editor` assembly. It compiles under `LARJE_UNITY_AI`. Tool names use the `Larje.` prefix and the `larje` group. Put logic that tests need into the regular editor assembly so the tests do not depend on Unity AI.
- Runtime assemblies must not reference editor-only APIs outside `#if UNITY_EDITOR`.

## Working in a Unity project

- Do not edit `.prefab`, `.unity`, `.asset` or `.mat` files as text. Change them through the Unity Editor or MCP tools.
- Commit the `.meta` file for every new file and folder. Git packages are immutable, so missing metas break consumers.
- After every C# change, refresh the editor, wait until compilation is idle, and check that the console has no errors or warnings before moving on.
- Run EditMode tests through the Test Runner. List the package in the project manifest `testables` first.

## Commits

- Write short technical commit messages only. Do not push unless explicitly asked.

