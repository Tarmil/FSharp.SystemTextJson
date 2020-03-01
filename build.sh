#!/bin/bash
set -e

dotnet tool restore
exec dotnet fake build "$@"
