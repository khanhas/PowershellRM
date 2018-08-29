param (
    [Parameter(Mandatory = $true)][int16]$major,
    [Parameter(Mandatory = $true)][int16]$minor,
    [Parameter(Mandatory = $true)][int16]$patch
)

$ver = "$($major).$($minor).$($patch)"

(Get-Content ".\Properties\AssemblyInfo.cs") -replace "AssemblyVersion(`"[\d\.]*`")", "AssemblyVersion(`"$($ver)`")" |
    Set-Content ".\Properties\AssemblyInfo.cs"

$skinDef = Get-Content ".\skinDefinition.json" | ConvertFrom-Json
$skinDef.version = $ver
$skinDef.output = "./dist/PowershellRM_$($ver).rmskin"
$skinDef | ConvertTo-Json | Set-Content ".\skinDefinition.json"