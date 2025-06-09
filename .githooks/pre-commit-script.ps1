#!/usr/bin/env pwsh

dotnet tool restore
dotnet fantomas --check .
if ($LASTEXITCODE -ne 0) { throw "Code needs formatting." }
