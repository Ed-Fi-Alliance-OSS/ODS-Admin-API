#!/usr/bin/env bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# Computes the Docker Hub tags for a release.
#
# Usage:
#   docker-tags.sh --ref <ref> --api-image <name> --db-image <name> [--minor-only]
#
# Environment:
#   EXISTING_TAGS  Newline-separated git tag names. When non-empty, no remote lookup is done.
#   TAGS_REMOTE    Git remote used to list tags. Defaults to
#                  https://github.com/${GITHUB_REPOSITORY}.git
#
# Output (stdout, one KEY=VALUE per line):
#   VERSION          Version without the leading "v"
#   VERSION_TAG      Version with the leading "v" (no "Pre-Release-" prefix)
#   APITAGS          Comma-separated image:tag list for the Admin API image
#   APITAGS_ALIASES  Tags that repoint to <api-image>:<VERSION_TAG>: APITAGS without that
#                    entry, or the minor tag with --minor-only (empty if none)
#   DBTAGS           Comma-separated image:tag list for the database image (7.x)
#   DBTAGS6x         Comma-separated image:tag list for the database image (6.x)
#
# The API image gets the moving tags vX.Y, vX and latest only when the release is
# the highest of its group. Database image tags are not affected by that rule.

set -euo pipefail

fail() {
  echo "$*" >&2
  exit 1
}

REF=""
API_IMAGE=""
DB_IMAGE=""
MINOR_ONLY=false

while [[ $# -gt 0 ]]
do
  case "$1" in
    --ref) REF="${2:-}"; shift 2 ;;
    --api-image) API_IMAGE="${2:-}"; shift 2 ;;
    --db-image) DB_IMAGE="${2:-}"; shift 2 ;;
    --minor-only) MINOR_ONLY=true; shift ;;
    *) fail "Unknown argument: $1" ;;
  esac
done

[[ -n "$REF" && -n "$API_IMAGE" && -n "$DB_IMAGE" ]] || fail "--ref, --api-image and --db-image are required"

REF_PATTERN='^(Pre-Release-)?v[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$'
[[ "$REF" =~ $REF_PATTERN ]] || fail "Invalid release tag: $REF"

VERSION_TAG="${REF#Pre-Release-}"
VERSION="${VERSION_TAG#v}"

# Joins image:tag pairs with commas: join_tags <image> <tag>...
join_tags() {
  local image="$1" out="" tag
  shift
  for tag in "$@"
  do
    out+="${out:+,}${image}:${tag}"
  done
  printf '%s' "$out"
}

emit() {
  printf '%s=%s\n' "$1" "$2"
}

if [[ "$VERSION_TAG" == *alpha* ]]
then
  # Pre-releases get the tag "pre"
  emit VERSION "$VERSION"
  emit VERSION_TAG "$VERSION_TAG"
  emit APITAGS "${API_IMAGE}:pre"
  emit APITAGS_ALIASES ""
  emit DBTAGS "${DB_IMAGE}:pre"
  emit DBTAGS6x "${DB_IMAGE}:pre-for-6.2"
  exit 0
fi

IFS=. read -r MAJOR MINOR_NUMBER _ <<< "$VERSION_TAG"
MINOR="${MAJOR}.${MINOR_NUMBER}"

PLAIN_PATTERN='^v[0-9]+\.[0-9]+\.[0-9]+$'
api_tags=()

# Lists the final releases (vX.Y.Z) known to git, plus the version being
# published, into $final_tags. The "Pre-Release-" prefix is stripped so
# final-shaped prefixed tags (e.g. Pre-Release-v2.4.1) count the same as the
# refs being published.
load_final_tags() {
  local raw_tags remote listing

  if [[ -n "${EXISTING_TAGS:-}" ]]
  then
    raw_tags="$EXISTING_TAGS"
  else
    remote="${TAGS_REMOTE:-}"
    if [[ -z "$remote" && -n "${GITHUB_REPOSITORY:-}" ]]
    then
      remote="https://github.com/${GITHUB_REPOSITORY}.git"
    fi
    [[ -n "$remote" ]] || fail "Set EXISTING_TAGS, TAGS_REMOTE or GITHUB_REPOSITORY to look up release tags"

    if ! listing="$(git ls-remote --tags --refs "$remote" 2>&1)"
    then
      fail "Unable to list tags from ${remote}: ${listing}"
    fi
    [[ -n "$listing" ]] || fail "No tags returned from ${remote}"

    raw_tags="$(printf '%s\n' "$listing" | sed 's#.*refs/tags/##')"
  fi

  final_tags="$(printf '%s\n%s\n' "$raw_tags" "$VERSION_TAG" | sed 's/^Pre-Release-//' | grep -E "$PLAIN_PATTERN" || true)"
}

# is_highest <ERE>: the published version is the highest among final tags matching ERE
is_highest() {
  local top
  top="$(printf '%s\n' "$final_tags" | { grep -E "$1" || true; } | sort -V | tail -n 1)"
  [[ "$top" == "$VERSION_TAG" ]]
}

MINOR_PATTERN="^${MINOR//./\\.}\\."

if [[ ! "$VERSION_TAG" =~ $PLAIN_PATTERN ]]
then
  # Other pre-releases (e.g. rc) are published by version only: they never move
  # the minor, major or latest tags.
  [[ "$MINOR_ONLY" != "true" ]] || fail "--minor-only requires a final release, got ${VERSION_TAG}"
  api_tags=("$VERSION_TAG")
elif [[ "$MINOR_ONLY" == "true" ]]
then
  load_final_tags
  is_highest "$MINOR_PATTERN" || fail "${VERSION_TAG} is not the highest release of ${MINOR}; publishing ${MINOR} would roll it back"
  api_tags=("$MINOR")
else
  load_final_tags

  api_tags=("$VERSION_TAG")
  if is_highest "$MINOR_PATTERN"
  then
    api_tags+=("$MINOR")
  fi
  if is_highest "^${MAJOR}\\."
  then
    api_tags+=("$MAJOR")
  fi
  if is_highest '^v'
  then
    api_tags+=("latest")
  fi
fi

aliases=""
if [[ "$MINOR_ONLY" == "true" ]]
then
  # The minor tag is the only tag published, and it is an alias of vX.Y.Z
  aliases="$(join_tags "$API_IMAGE" "$MINOR")"
else
  aliases="$(join_tags "$API_IMAGE" "${api_tags[@]:1}")"
fi

if [[ "$MINOR_ONLY" == "true" ]]
then
  db_tags=("$MINOR")
  db6x_tags=("${MINOR}-6.2")
elif [[ ! "$VERSION_TAG" =~ $PLAIN_PATTERN ]]
then
  # Non-final refs (e.g. rc) never replace the stable minor database image
  db_tags=("$VERSION_TAG")
  db6x_tags=("${VERSION_TAG}-6.2")
else
  db_tags=("$VERSION_TAG" "$MINOR")
  db6x_tags=("${VERSION_TAG}-6.2" "${MINOR}-6.2")
fi

emit VERSION "$VERSION"
emit VERSION_TAG "$VERSION_TAG"
emit APITAGS "$(join_tags "$API_IMAGE" "${api_tags[@]}")"
emit APITAGS_ALIASES "$aliases"
emit DBTAGS "$(join_tags "$DB_IMAGE" "${db_tags[@]}")"
emit DBTAGS6x "$(join_tags "$DB_IMAGE" "${db6x_tags[@]}")"
