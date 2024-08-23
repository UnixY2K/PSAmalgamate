#requires -Version 7.0
using module ./Cat.psm1
using module ./Dachshund.psm1
using module ./Dog.psm1

param(
    [string]$DogName = "Rex",
    [string]$CatName = "Whiskers"
)

$ErrorActionPreference = "Stop"

[Animal]$ADog = [Daschund]::new()
[Animal]$ACat = [Cat]::new()
if ($DogName) {
    $ADog.name = $DogName
}
if ($CatName) {
    $ACat.name = $CatName
}
Write-Host "$($ADog.name) says: $($ADog.Speak())"
Write-Host "$($ACat.name) says: $($ACat.Speak())"