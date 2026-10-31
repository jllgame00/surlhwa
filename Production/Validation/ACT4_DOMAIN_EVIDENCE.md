# ACT 4 Shake preparation domain evidence

Baseline: `4790c33`. Scope: the three existing domain files and
`French75PreparationTests.cs` with its Unity metadata. No content IDs, saved
snapshot fields, tuning values, scenes or prefabs change in this task.

Authority: Project Vision; Final Canon BART-001–003, PREP-001–005, DRINK-08,
SUPPLY-002/005; Architecture 03/04/05/20; ACT4 implementation plan task 1.

The recipe owns `ShakeFinishPolicy`: existing constructors default to
`PourThenSplash`; the new `StrainThenPourAndGentleStir` requires Shake and an
actual recipe late ingredient. Both direct and held Shaker Strain retain the
existing lid, capacity and actual-liquid transfer checks.

An internal, live-only provenance flag permits unshaken Jigger additions to
preserve an actual Shaker-strained batch's existing Shake stamp and shaken
amounts. The added liquid is never included in those shaken amounts. Different
stamps cannot merge; an unshaken base addition cannot borrow quality. The flag
does not appear in submitted snapshots or saves. Discard already creates a
fresh `BrewingSession`, so previous actions and technique cannot survive it.

The new recipe finish reads accepted ingredient components and action order:
Shaker Strain, then measured late Pour or Jigger transfer, then a GentleStir
fresh for the final serving composition. It does not use final `Strained` as
proof because an actual later Pour legitimately clears that value. Recognition,
quality and guest FIT remain their existing independent evaluations. Gin Fizz
retains its PourFromContainer plus late Splash contract.

## Standalone validation

Evidence is under ignored `Validation/Evidence/act4-domain/`; output/obj paths
are isolated from other agents. This tier does **not** claim Unity execution,
Player controls, visuals or subjective bartending feel.

| Run | Observed result |
| --- | --- |
| Initial focused `red-strain-focused.log` | 49 pass / 2 expected failures: direct and held Shaker Strain rejected |
| Policy/provenance `red-finish.log` | 65 pass / 11 failures: new French finish and Jigger preservation absent |
| Focused `green-finish.log` | 76 pass / 0 failures across Stir, Build, Shake, Float and French preparation |
| Final focused `green-final.log` | 77 pass / 0 failures; original Gin Fizz reason order retained and non-Shake/null-late policy guard checked |
| Initial full sandbox `red-strain.log` | 199 pass / 37 failures: the two expected Strain failures plus 35 Windows file-publication restrictions |
| Full normal-filesystem `full-green.log` | 285 pass / 26 failures, all ACT4 continuation/retry work still in progress when compiled; prior publication failures pass |

The full run's outstanding cases are owned by the parallel continuation/content
tasks and require the final integrated rerun. No failing assertion was weakened:

- `ActualCampaignReadsFrenchFactsRetriesMissingFinishAndAwardsFailedHamletMemoryNaturally`
- `OwnedOptionalRetryReadingRoundTripsAfterFailedDiscoveryWithoutLearningTheRecipe`
- `ActFourContinuationPreservesEverySourcePhaseAndReceipt` (three source ACTs)
- `ActFourImportPreservesBothOriginalGenerationsAndIsIdempotent` (three source ACTs)
- `ActThreeToFourPublicationFaultRetryKeepsOneImportAndOriginalHistory`
  (`before_write`, `after_write`, `after_flush`, `after_validate`, `after_replace`, `after_final_read`)
- `ActFourPrefixAllowsOnlyAbsentOrExplicitNullOptionalRetryMetadata` (three source ACTs)
- `ActFourImportNeverRollsBackFutureSourceOrDestinationHistory` (both source/destination variants)
- `ExistingInvalidActFourGenerationPreventsOlderImport` (`current.json`, `previous.json`)
- `ActThreeCorruptionRequiresExplicitRecoveryWithoutTouchingEitherSourceGeneration`
- `FrenchRetryAndHamletDepartureRoundTripEveryActualCheckpointWithoutChangingEvidence`
- `FrenchRetryCannotAdvanceWithoutItsConsumedReadingEvenWithAValidChecksum`
  (`Brewing`, `Reaction`, `Complete`)

The focused cases cover actual source/destination conservation, held and partial
Strain, repeated Jigger portions, direct Pour clearing `Strained`, unshaken
additions, mixed batches, wrong/early/Splash finishing actions, stale/missing
GentleStir, closed lids, discard, recipe construction guards, independent guest
FIT and poor technique quality, plus accepted Gin Fizz regressions. Final
runtime/input and save integration validation is performed by the root task.
