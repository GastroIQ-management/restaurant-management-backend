# Review: Simplify the French expired refresh token message

**Verdict:** Ready to merge
**Build:** pass (0 errors, 6 warnings, all in files this branch does not touch)   **Tests:** full suite runs in CI (Continuous Integration) — merge only when it is green

Small change (from `/small`): no spec or plan folder, so the Completeness check does not apply.
Security matrix check (`python3 .claude/scripts/check-security-matrices.py`): passes.

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | src/RestaurantManagement.Shared/Localization/SharedResource.fr.resx:72 | No code looks up `InvalidRefreshToken`. Refresh refusals use `InvalidOrExpiredToken` (`SessionService.cs:26`), and the key has been unused since the refresh-tokens-own-table work (its review, finding 5). So no user will ever see the new text. The clean-code skill says a change removes only the unused code it created, so this branch is right to leave the key | Write a backlog item to delete `InvalidRefreshToken` from all three `.resx` files. It should also delete the `fr` case added at `SharedResourceCatalogTests.cs:104` |
| 2 | backlog/improvements/2026-10-23-review-french-canada-translations.md | The item asks for a Canadian French native speaker to review all 73 messages. This branch changes one message, and that message is unused. Deleting the item (left uncommitted for the user) means nothing tracks the native review of the other 72 | Keep the item, or narrow it to the native review, unless that review is no longer wanted |
| 3 | tests/RestaurantManagement.Shared.Tests/Localization/SharedResourceCatalogTests.cs:104 | This case pins a text that was just changed. It sits in `Localizer_ExistingMessageKey_KeepsTodaysText`, whose assertion reason reads "existing messages keep their current text". The check is correct, but the name fits a "keep unchanged" test better | No change needed now. If the key is deleted (finding 1), this case goes with it |

## Checked and fine
- Scope: 2 files, both in existing folders (`Shared/Localization`, `Shared.Tests/Localization`). No migration, package, endpoint or Shared contract change
- Tests: one `InlineData` case added; no test edited, deleted, skipped or weakened. It carries `[Trait("Rule", "SH19")]` through its theory, and SH19 already names this test
- Security matrix: SH19 ("every message exists in English, Portuguese and French") still describes the behaviour. No row needs a change
- Commit: one commit, `fix(shared): ...`, Conventional Commits, body gives the why and the backlog source, not amended
- Secrets: none in the diff
- Wording: "Veuillez vous reconnecter" matches the French `InvalidOrExpiredToken` text and keeps the English meaning ("log in again")

## Check before merging
- Finding 2: deleting the backlog item drops the native-speaker review of the French catalog. Confirm that is what you want before you commit the deletion.
- Finding 1: the edited message is unused, so this branch has no visible effect on users.

## Pull request description
**fix(shared): simplify the French expired refresh token message**

Changes the French (`fr`, also served to `fr-CA`) text of the `InvalidRefreshToken` message from
"Le jeton d'actualisation est invalide ou a expiré. Veuillez demander une nouvelle session de connexion."
to "Le jeton d'actualisation est invalide ou a expiré. Veuillez vous reconnecter.", matching the
wording of `InvalidOrExpiredToken`.

- Shared: `SharedResource.fr.resx`, one value changed
- Tests: `SharedResourceCatalogTests.Localizer_ExistingMessageKey_KeepsTodaysText` gets a `fr` case that pins the new text (rule SH19)

Risk: low. No migration, package, endpoint or contract change. No code currently looks up
`InvalidRefreshToken`, so users see no change. A follow-up should remove the unused key.

Source: backlog item `backlog/improvements/2026-10-23-review-french-canada-translations.md`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
