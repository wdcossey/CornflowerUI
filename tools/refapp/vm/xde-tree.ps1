# Windows only: runs on the reference VM, in its Windows PowerShell, not on the machine that drives it.
# Lists the UI Automation tree of the emulator's (XDE's) windows, to find the Screenshot tool's controls.
# Runs in the signed-in desktop session. -OpenTools presses the toolbar's >> to open Additional Tools, and -Tab
# selects one of its tabs, before the listing. Keep -Depth low while a file dialog is open: its tree is slow to walk.
param([string]$Out = 'C:\cornflower\grab\xde-tree.txt', [switch]$OpenTools, [string]$Tab = '', [int]$Depth = 12)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$A = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]
$C = [System.Windows.Automation.Condition]
$lines = New-Object System.Collections.Generic.List[string]

function Dump($el, [int]$level) {
    if ($level -gt $Depth) { return }
    $i = $el.Current
    $patterns = ($el.GetSupportedPatterns() | ForEach-Object { $_.ProgrammaticName -replace 'PatternIdentifiers.Pattern', '' }) -join ','
    $lines.Add(('{0}{1} name="{2}" id="{3}" class="{4}" rect={5} [{6}]' -f ('  ' * $level), $i.ControlType.ProgrammaticName, $i.Name, $i.AutomationId, $i.ClassName, $i.BoundingRectangle, $patterns))
    foreach ($child in $el.FindAll($Scope::Children, $C::TrueCondition)) { Dump $child ($level + 1) }
}

$xde = Get-Process XDE -ErrorAction SilentlyContinue
if (-not $xde) { Set-Content $Out 'no XDE process'; return }
$pidCond = New-Object System.Windows.Automation.PropertyCondition($A::ProcessIdProperty, $xde[0].Id)
$tops = $A::RootElement.FindAll($Scope::Children, $pidCond)

if ($OpenTools) {
    # The toolbar draws its own buttons, so UI Automation can't press >>; XdeToolbar posts the click.
    Add-Type -Path (Join-Path $PSScriptRoot 'XdeWin32.cs')
    $lines.Add("pressed >>: $([XdeToolbar]::OpenTools($xde[0].Id))")
    Start-Sleep 3
    $tops = $A::RootElement.FindAll($Scope::Children, $pidCond)
}

if ($Tab) {
    foreach ($top in $tops) {
        foreach ($tabItem in $top.FindAll($Scope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($A::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem)))) {
            if ($tabItem.Current.Name -eq $Tab) {
                try { $tabItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); $lines.Add("selected tab: $Tab") }
                catch { $lines.Add("select failed: $_") }
                Start-Sleep 2
            }
        }
    }
}

foreach ($top in $tops) { Dump $top 0 }
Set-Content $Out $lines
