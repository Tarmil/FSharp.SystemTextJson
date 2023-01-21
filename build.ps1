#!/usr/bin/env pwsh

dotnet tool restore
dotnet run --project build $args
