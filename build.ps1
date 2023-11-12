#!/usr/bin/env pwsh

if ($env:GITHUB_ACTION) {
    echo "::group::Build FAKE project"
}

dotnet run -v:m --project build $args
