[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$MarkdorPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$supportedExtensions = @(".md", ".markdown", ".mdown", ".txt")
$menuKeyName = "OpenWithMarkdor"
$menuText = -join @([char]0x7528, " Markdor ", [char]0x6253, [char]0x5F00)

function Resolve-MarkdorExecutable([string]$InputPath) {
    if ([string]::IsNullOrWhiteSpace($InputPath)) {
        $InputPath = Join-Path $PSScriptRoot "Markdor.exe"
    }

    if (-not (Test-Path -LiteralPath $InputPath -PathType Leaf)) {
        throw "Markdor.exe was not found. Run this script from the Markdor publish folder, or pass -MarkdorPath."
    }

    return (Resolve-Path -LiteralPath $InputPath).Path
}

function Set-RegistryDefaultValue([string]$Path, [string]$Value) {
    New-Item -Path $Path -Force | Out-Null
    $key = Get-Item -LiteralPath $Path
    $key.SetValue("", $Value, [Microsoft.Win32.RegistryValueKind]::String)
}

function Set-RegistryStringValue([string]$Path, [string]$Name, [string]$Value) {
    New-Item -Path $Path -Force | Out-Null
    New-ItemProperty -LiteralPath $Path -Name $Name -Value $Value -PropertyType String -Force | Out-Null
}

if ($env:OS -ne "Windows_NT") {
    throw "This script can only register Windows Explorer context menus on Windows."
}

$markdorExecutable = Resolve-MarkdorExecutable $MarkdorPath
$command = '"{0}" "%1"' -f $markdorExecutable
$progId = "Markdor.Markdown"
$classesRoot = "HKCU:\Software\Classes"

if ($PSCmdlet.ShouldProcess($markdorExecutable, "Register Markdor Markdown opening candidate and icon")) {
    Set-RegistryDefaultValue "$classesRoot\$progId" "Markdown"
    Set-RegistryDefaultValue "$classesRoot\$progId\DefaultIcon" ('"{0}",0' -f $markdorExecutable)
    Set-RegistryDefaultValue "$classesRoot\$progId\shell\open\command" $command
    Set-RegistryStringValue "$classesRoot\Applications\Markdor.exe" "FriendlyAppName" "Markdor"
    Set-RegistryDefaultValue "$classesRoot\Applications\Markdor.exe\DefaultIcon" ('"{0}",0' -f $markdorExecutable)
    Set-RegistryDefaultValue "$classesRoot\Applications\Markdor.exe\shell\open\command" $command
    foreach ($extension in @(".md", ".markdown", ".mdown")) {
        Set-RegistryStringValue "$classesRoot\Applications\Markdor.exe\SupportedTypes" $extension ""
        $candidatePath = "$classesRoot\$extension\OpenWithProgids"
        New-Item -Path $candidatePath -Force | Out-Null
        (Get-Item -LiteralPath $candidatePath).SetValue($progId, [byte[]]@(), [Microsoft.Win32.RegistryValueKind]::None)
    }
}

foreach ($extension in $supportedExtensions) {
    $verbPath = "HKCU:\Software\Classes\SystemFileAssociations\$extension\shell\$menuKeyName"
    $commandPath = "$verbPath\command"

    if ($PSCmdlet.ShouldProcess($extension, "Register Markdor context menu")) {
        Set-RegistryDefaultValue $verbPath $menuText
        Set-RegistryStringValue $verbPath "MUIVerb" $menuText
        Set-RegistryStringValue $verbPath "Icon" $markdorExecutable
        Set-RegistryStringValue $verbPath "MultiSelectModel" "Single"
        Set-RegistryDefaultValue $commandPath $command
    }
}

Write-Host "Markdor context menu targets: $($supportedExtensions -join ', ')"
Write-Host "Command: $command"
