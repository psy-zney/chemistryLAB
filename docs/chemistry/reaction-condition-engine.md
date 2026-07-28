# Reaction Conditions, Redox, and Reusable Products

This document describes the implemented Unity/C# runtime. The numbers are
educational gameplay estimates, not laboratory operating limits.

## Runtime flow

1. Each workbench/fume-hood vessel owns a `ReactionEnvironment`: temperature in
   °C and working volume in litres.
2. `ReactionSimulator` aggregates every addition by stable chemical id.
3. Matching order is curated catalogue, reviewed redox rules, then dynamic
   ionic/acid-base/metal rules.
4. `ReactionConditionEngine` estimates molarity and pH, checks temperature,
   pH, concentration, and catalyst constraints, then estimates rate and yield.
5. A failed constraint returns `ReactionStatus.Blocked`; reactants stay in the
   vessel and the HUD tells the player which condition is missing.
6. A valid match uses limiting-reagent stoichiometry. A catalyst participant is
   excluded from the limiting-reagent calculation and is not consumed.
7. `SynthesizedInventory` can isolate the primary product into a reusable batch.

## Condition model

- Concentration is `moles / vessel volume`.
- pH is calculated from net acid/base equivalents. Strong acids and bases use
  full dissociation; acetic/phosphoric acid and ammonia use reduced educational
  dissociation factors.
- Temperature uses an Arrhenius-like exponential trend, clamped for stable
  gameplay.
- Total molarity contributes a square-root concentration factor.
- A required catalyst multiplies the estimated rate but does not change
  equilibrium or product identity.
- Rate is classified as stopped, very slow, slow, moderate, fast, or vigorous.
- The condition yield multiplier changes estimated recovered mass; purity is
  stored separately so yield is not incorrectly presented as purity.

Current explicit profiles include MnO₂-catalysed H₂O₂ decomposition, four
acidic oxidation systems, acidic Fe²⁺/permanganate, and hot concentrated
sulfuric acid with copper. Other reactions still receive pH, concentration,
temperature, rate, time, and yield estimates without an artificial hard gate.

## Redox model

Eight reviewed rules cover:

- permanganate + hydrogen peroxide in acid;
- permanganate + chloride in strong acid;
- permanganate + iodide in acid;
- hydrogen peroxide + iodide in acid;
- copper + dilute nitric acid, producing NO;
- copper + concentrated nitric acid, producing NO₂;
- copper + hot concentrated sulfuric acid, producing SO₂;
- permanganate + Fe²⁺ in acid.

Every rule stores the electron count of its oxidation and reduction
half-reactions. The engine calculates their least common multiple using the
Euclidean greatest-common-divisor algorithm and rejects invalid electron data
during Unity validation. Nitric-acid concentration selects the dilute or
concentrated branch instead of treating both equations as the same reaction.

## Product inventory

Collected products are stored as `SynthesizedBatch` records with:

- batch id and reusable chemical id;
- name, formula, phase, molar mass, appearance, and color;
- available grams and purity fraction;
- hazards, source reaction id/equation, and UTC creation time.

The save file is `chemistry-inventory.json` in
`Application.persistentDataPath`. Loading reconstructs runtime chemical
definitions. When a generated formula exists in `CompoundGenerationMatrix`,
the runtime registry also adds it to `DynamicReactionEngine`, allowing it to
participate in later precipitation, exchange, acid/base, and related rules.
Loading material from a batch subtracts the actual mass; an empty batch is
removed. Collecting clears the source vessel, preventing duplicate collection.

Gas isolation is deliberately stricter: collection requires the fume hood and
an attached gas trap. Unsafe generation itself remains possible and is handled
by the existing health/credit consequence system.

## Player controls

| Input | Action |
|---|---|
| Page Up / Page Down | Heat or cool the active vessel by 25 °C |
| F8 | Add 50 mL solvent |
| C | Collect the primary product |
| I | Cycle stored product batches |
| E on hotplate | Heat the workbench vessel by 25 °C |
| E on vessel with empty hands | Collect an available product |

The vessel HUD displays temperature, volume, pH, total molarity, catalyst,
rate class/multiplier, estimated time, limiting reagent, theoretical/recovered
mass, purity, electron count for redox, and the reason for a blocked reaction.

## Validation

Unity batch validation checks:

- acid/base equivalent neutralisation;
- missing/present catalyst behavior;
- all redox electron least-common-multiples;
- dilute versus concentrated nitric-acid branching;
- blocked cold versus active hot concentrated sulfuric-acid reaction;
- product collection and exact inventory mass subtraction;
- all previous curated, dynamic, compound-matrix, safety, UI-audio data checks.

Latest validated counts: 7 condition profiles, 8 redox rules, 9 dynamic
families, 38 curated reactions, and 155 dynamically resolved catalogue pairs.
