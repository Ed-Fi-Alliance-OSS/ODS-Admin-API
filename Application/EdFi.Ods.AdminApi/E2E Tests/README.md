# Admin API End-to-End (E2E) Tests

These end-to-end tests are written for [Bruno](https://www.usebruno.com/), an
open-source, git-friendly API client (an alternative to Postman). Test
collections are stored as plain `.bru` files, so they can be reviewed and
diffed like any other source file.

## Installing Bruno

Download the Bruno desktop application from
<https://www.usebruno.com/downloads>.

(For running tests headlessly, e.g. in CI, see the `@usebruno/cli` package
instead — covered in each collection's own README, linked below.)

## Directory Layout

- **`V1/`** — E2E tests for the v1 Admin API.
- **`V2/`** — E2E tests for the v2 Admin API.

Each version folder contains a Bruno collection (identified by its
`bruno.json` file) plus a `gh-action-setup` folder used by CI. See the
`README.md` inside each collection folder for detailed setup, authentication,
and test-running instructions:

- [V1/Bruno Admin API E2E refactor/README.md](V1/Bruno%20Admin%20API%20E2E%20refactor/README.md)
- [V2/Bruno Admin API E2E 2.0 refactor/README.md](V2/Bruno%20Admin%20API%20E2E%202.0%20refactor/README.md)

## Opening a Collection in Bruno

1. Open the Bruno desktop application.
2. Click **Open Collection**.
3. Navigate to and select the collection folder for the version you want to
   test, e.g. `E2E Tests/V1/Bruno Admin API E2E refactor` or
   `E2E Tests/V2/Bruno Admin API E2E 2.0 refactor` (the folder containing
   `bruno.json`, not the `V1`/`V2` parent folder).
4. Bruno will load the collection's requests and environments. Select the
   `local` environment before running requests against a local Admin API
   instance.

For running the full suite from the command line (used in CI), see
`eng/run-e2e-bruno.ps1` and `docs/developer.md`.
