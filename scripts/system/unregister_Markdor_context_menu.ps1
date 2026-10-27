[CmdletBinding(SupportsShouldProcess = $true)]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$supportedExtensions = @(".md", ".markdown", ".mdown", ".txt")
$menuKeyName = "OpenWithMarkdor"

if ($env:OS -ne "Windows_NT") {
    throw "This script can only unregister Windows Explorer context menus on Windows."
}

foreach ($extension in $supportedExtensions) {
    $verbPath = "HKCU:\Software\Classes\SystemFileAssociations\$extension\shell\$menuKeyName"
    if (-not (Test-Path -LiteralPath $verbPath)) {
        continue
    }

    if ($PSCmdlet.ShouldProcess($extension, "Unregister Markdor context menu")) {
        Remove-Item -LiteralPath $verbPath -Recurse -Force
    }
}

# 只删除本应用的候选值和独立键，不删除扩展名本身及其他程序的关联。
foreach ($extension in @(".md", ".markdown", ".mdown")) {
    $candidatePath = "HKCU:\Software\Classes\$extension\OpenWithProgids"
    if ((Test-Path -LiteralPath $candidatePath) -and $PSCmdlet.ShouldProcess($extension, "Remove Markdor opening candidate")) {
        (Get-Item -LiteralPath $candidatePath).DeleteValue("Markdor.Markdown", $false)
    }
}
foreach ($path in @("HKCU:\Software\Classes\Markdor.Markdown", "HKCU:\Software\Classes\Applications\Markdor.exe")) {
    if ((Test-Path -LiteralPath $path) -and $PSCmdlet.ShouldProcess($path, "Remove Markdor registration")) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

Write-Host "Markdor context menu targets: $($supportedExtensions -join ', ')"
