param(
    [Parameter()]
    [String]$arch = "x64",
    [String]$os = "windows"
)
$sharpos = ""
switch ($os)
{
    "windows" {$sharpos = "win"}
    "macos" {$sharpos = "osx"}
    "ubuntu" {$sharpos = "linux"}
}
$sharpr = @("$sharpos-$arch")

Set-Location ".."
$callparam = "-r", $sharpr, "-c", "Release", "-o", ".\build", "--self-contained", "true"
if (Test-Path -Path ".\build" -PathType Container) {
    Remove-Item ".\build" -Force -Recurse
}
New-Item ".\build" -ItemType "directory"

&"dotnet" "publish" $callparam
Copy-Item -Path @("build") -Destination ".." -Recurse -Exclude "*.pdb" -Force

switch ($os)
{
    "windows" {
        
    }
    "macos" {
        
    }
    "ubuntu" {

    }
}