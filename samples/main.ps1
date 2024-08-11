#requires -Version 7.0
using module ./Cat.psm1
using module ./Dachshund.psm1
using module ./Dog.psm1

$ErrorActionPreference = "Stop"

[Animal]$ADog = [Daschund]::new()
[Animal]$ACat = [Cat]::new()
Write-Host "$($ADog.name) says: $($ADog.Speak())"
Write-Host "$($ACat.name) says: $($ACat.Speak())"