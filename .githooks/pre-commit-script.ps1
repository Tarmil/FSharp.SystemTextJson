#!/usr/bin/env pwsh

dotnet tool restore
dotnet fantomas --check -r .
if ($LASTEXITCODE -ne 0) { throw "Code needs formatting." }
