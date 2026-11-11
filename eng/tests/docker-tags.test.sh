#!/usr/bin/env bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# Tests for eng/docker-tags.sh. Run with: bash eng/tests/docker-tags.test.sh

set -u

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPT="$HERE/../docker-tags.sh"
failures=0
OUT=""
RC=0

# run <existing tags, newline separated> <script args...>
run() {
  local existing="$1"
  shift
  OUT="$(EXISTING_TAGS="$existing" bash "$SCRIPT" "$@" --api-image api --db-image db 2>/dev/null)"
  RC=$?
}

# run_remote <remote> <script args...>: resolve tags through git ls-remote
run_remote() {
  local remote="$1"
  shift
  OUT="$(env -u EXISTING_TAGS TAGS_REMOTE="$remote" bash "$SCRIPT" "$@" --api-image api --db-image db 2>/dev/null)"
  RC=$?
}

get() {
  printf '%s\n' "$OUT" | sed -n "s/^$1=//p"
}

assert_eq() {
  local expected="$1" actual="$2" name="$3"
  if [[ "$expected" == "$actual" ]]
  then
    echo "PASS $name"
  else
    echo "FAIL $name: expected [$expected] got [$actual]"
    failures=$((failures + 1))
  fi
}

ALL_TAGS="api:v2.4.0,api:v2.4,api:v2,api:latest"

run $'v1.5.0\nv2.3.5\nv2.4.0' --ref v2.4.0
assert_eq "$ALL_TAGS" "$(get APITAGS)" "highest_final APITAGS"
assert_eq "api:v2.4,api:v2,api:latest" "$(get APITAGS_ALIASES)" "highest_final APITAGS_ALIASES"

run $'v1.5.0\nv2.3.5\nv2.4.0' --ref Pre-Release-v2.4.0
assert_eq "$ALL_TAGS" "$(get APITAGS)" "strips_prerelease_prefix APITAGS"
assert_eq "2.4.0" "$(get VERSION)" "strips_prerelease_prefix VERSION"
assert_eq "v2.4.0" "$(get VERSION_TAG)" "strips_prerelease_prefix VERSION_TAG"

run $'v2.3.5' --ref v2.4.0
assert_eq "$ALL_TAGS" "$(get APITAGS)" "self_not_in_list"

run $'v2.3.5\nPre-Release-v2.4.0\nPre-Release-v2.4.1' --ref Pre-Release-v2.4.0
assert_eq "api:v2.4.0" "$(get APITAGS)" "prefixed_newer_tag_blocks_moving_tags"
assert_eq "" "$(get APITAGS_ALIASES)" "prefixed_newer_tag_blocks_moving_tags ALIASES"

run $'v2.3.5\nPre-Release-v2.4.1-alpha.0.7' --ref v2.4.0
assert_eq "$ALL_TAGS" "$(get APITAGS)" "prefixed_alpha_tag_ignored"

run $'v2.3.4\nv2.3.5\nv2.4.0' --ref v2.3.5
assert_eq "api:v2.3.5,api:v2.3" "$(get APITAGS)" "older_patch_keeps_minor_only"

run $'v2.3.4\nv2.3.5\nv2.4.0' --ref v2.3.4
assert_eq "api:v2.3.4" "$(get APITAGS)" "older_patch_in_same_minor"

run $'v1.5.0\nv1.5.1\nv2.4.0' --ref v1.5.1
assert_eq "api:v1.5.1,api:v1.5,api:v1" "$(get APITAGS)" "older_major_patch"

run $'v2.4.0\nv3.0.0' --ref v3.0.0
assert_eq "api:v3.0.0,api:v3.0,api:v3,api:latest" "$(get APITAGS)" "new_major"

run $'v2.9.0\nv2.10.0' --ref v2.9.0
assert_eq "api:v2.9.0,api:v2.9" "$(get APITAGS)" "numeric_ordering"

run $'v2.4.0' --ref Pre-Release-v2.5.0-alpha.0.132
assert_eq "api:pre" "$(get APITAGS)" "alpha APITAGS"
assert_eq "" "$(get APITAGS_ALIASES)" "alpha APITAGS_ALIASES"
assert_eq "db:pre" "$(get DBTAGS)" "alpha DBTAGS"
assert_eq "db:pre-for-6.2" "$(get DBTAGS6x)" "alpha DBTAGS6x"
assert_eq "v2.5.0-alpha.0.132" "$(get VERSION_TAG)" "alpha VERSION_TAG"

run $'v2.4.0' --ref v2.5.0-rc.1
assert_eq "api:v2.5.0-rc.1" "$(get APITAGS)" "hyphen_prerelease"
assert_eq "" "$(get APITAGS_ALIASES)" "hyphen_prerelease APITAGS_ALIASES"

run $'v2.4.0\nv2.4.1' --ref v2.4.0-rc.1
assert_eq "api:v2.4.0-rc.1" "$(get APITAGS)" "hyphen_prerelease_keeps_stable_minor"

run $'v2.3.5\nv2.4.0' --ref v2.4.0
assert_eq "db:v2.4.0,db:v2.4" "$(get DBTAGS)" "db_unchanged_final DBTAGS"
assert_eq "db:v2.4.0-6.2,db:v2.4-6.2" "$(get DBTAGS6x)" "db_unchanged_final DBTAGS6x"

run $'v2.3.5\nv2.4.0' --ref v2.4.0 --minor-only
assert_eq "api:v2.4" "$(get APITAGS)" "minor_only APITAGS"
assert_eq "db:v2.4" "$(get DBTAGS)" "minor_only DBTAGS"
assert_eq "db:v2.4-6.2" "$(get DBTAGS6x)" "minor_only DBTAGS6x"
assert_eq "" "$(get APITAGS_ALIASES)" "minor_only APITAGS_ALIASES"

run $'v2.4.0\nv2.4.1' --ref v2.4.0 --minor-only
assert_eq "1" "$RC" "minor_only_older_patch_fails"
assert_eq "" "$OUT" "minor_only_older_patch_fails no output"

run $'v2.4.0\nv2.4.1\nv2.5.0' --ref v2.4.1 --minor-only
assert_eq "api:v2.4" "$(get APITAGS)" "minor_only_highest_of_minor_ignores_newer_minor"

run $'v2.4.0\nPre-Release-v2.4.1' --ref Pre-Release-v2.4.0 --minor-only
assert_eq "1" "$RC" "minor_only_prefixed_newer_tag_fails"

run $'v2.4.0' --ref v2.5.0-rc.1 --minor-only
assert_eq "1" "$RC" "minor_only_hyphen_prerelease_fails"

run $'v2.4.0' --ref main
assert_eq "1" "$RC" "invalid_ref_fails"

# remote lookup through git ls-remote
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
git init -q "$TMP/repo"
git -C "$TMP/repo" -c user.name=test -c user.email=test@example.com commit -q --allow-empty -m init
for t in v2.3.5 v2.4.0 Pre-Release-v2.4.0
do
  git -C "$TMP/repo" tag "$t"
done

run_remote "$TMP/repo" --ref v2.4.0
assert_eq "$ALL_TAGS" "$(get APITAGS)" "remote_lookup"

run_remote "$TMP/missing" --ref v2.4.0
assert_eq "1" "$RC" "remote_lookup_failure_fails"

if [[ $failures -gt 0 ]]
then
  echo "$failures test(s) failed"
  exit 1
fi

echo "All tests passed"
