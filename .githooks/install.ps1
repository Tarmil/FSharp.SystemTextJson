#!/usr/bin/env pwsh

Copy-Item -Force $PSScriptRoot/hooks/* $PSScriptRoot/../.git/hooks/

git config blame.ignoreRevsFile .git-blame-ignore-revs
