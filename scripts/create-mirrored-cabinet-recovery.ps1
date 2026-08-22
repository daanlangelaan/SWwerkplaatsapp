param(
    [string]$SourceFolder = 'H:\cnc\CABINET TEST\SW-20260617-204859',
    [string]$OutputFolder = '',
    [switch]$SingleCabinet,
    [switch]$OverwriteGeneratedOutput
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$culture = [System.Globalization.CultureInfo]::InvariantCulture
$sourceTapPath = Join-Path $SourceFolder 'Betonplex_18mm_NestPlaat_01.tap'
$sourceCamPath = Join-Path $SourceFolder 'CAM-operaties.csv'
$sourceNestPath = Join-Path $SourceFolder 'NestPlan.csv'

foreach ($requiredPath in @($sourceTapPath, $sourceCamPath, $sourceNestPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Bronbestand ontbreekt: $requiredPath"
    }
}

$jobCode = if ($SingleCabinet) { 'HERSTEL_1_KAST' } else { 'HERSTEL_GESPIEGELD' }
$jobTapBaseName = $jobCode + '_NestPlaat_01'
$jobPartSummary = if ($SingleCabinet) { '1 kastzijde en 1 ladezijde' } else { '2 kastzijden en 2 ladezijden' }
$jobPartCount = if ($SingleCabinet) { 2 } else { 4 }
if ([string]::IsNullOrWhiteSpace($OutputFolder)) {
    $folderSuffix = if ($SingleCabinet) { '-HERSTEL-1-KAST' } else { '-HERSTEL-GESPIEGELD' }
    $OutputFolder = $SourceFolder + $folderSuffix
}

if (Test-Path -LiteralPath $OutputFolder) {
    if (-not $OverwriteGeneratedOutput) {
        throw "Uitvoermap bestaat al; gebruik -OverwriteGeneratedOutput om uitsluitend de bekende gegenereerde bestanden te vernieuwen: $OutputFolder"
    }
}

$sourceNest = @(Import-Csv -LiteralPath $sourceNestPath -Delimiter ';')
$sourceCam = @(Import-Csv -LiteralPath $sourceCamPath -Delimiter ';')
$sourceTapLines = [System.IO.File]::ReadAllLines($sourceTapPath)

$targetDefinitions = if ($SingleCabinet) {
    @(
        [pscustomobject]@{
            SourceName = 'Zijwand links'
            OutputName = 'Kastzijde tegenhand'
        },
        [pscustomobject]@{
            SourceName = 'Ladezijde links U1-1'
            OutputName = 'Ladezijde tegenhand'
        }
    )
}
else {
    @(
        [pscustomobject]@{
            SourceName = 'Zijwand links'
            OutputName = 'Kastzijde tegenhand 1'
        },
        [pscustomobject]@{
            SourceName = 'Zijwand rechts'
            OutputName = 'Kastzijde tegenhand 2'
        },
        [pscustomobject]@{
            SourceName = 'Ladezijde links U1-1'
            OutputName = 'Ladezijde tegenhand 1'
        },
        [pscustomobject]@{
            SourceName = 'Ladezijde rechts U1-1'
            OutputName = 'Ladezijde tegenhand 2'
        }
    )
}

$translationXmm = -593.0
$targetMap = @{}
$targetPlacements = @()

foreach ($definition in $targetDefinitions) {
    $placement = @($sourceNest | Where-Object { $_.Onderdeel -eq $definition.SourceName })
    if ($placement.Count -ne 1) {
        throw "Verwacht precies 1 nestingplaatsing voor '$($definition.SourceName)', gevonden: $($placement.Count)"
    }

    if ($placement[0].Geroteerd -ne 'nee') {
        throw "Herstelscript verwacht een niet-geroteerde bronplaatsing voor '$($definition.SourceName)'."
    }

    $length = [double]::Parse($placement[0].Lengte_mm, $culture)
    $width = [double]::Parse($placement[0].Breedte_mm, $culture)
    $oldX = [double]::Parse($placement[0].X_links_mm, $culture)
    $oldY = [double]::Parse($placement[0].Y_onder_mm, $culture)
    $newX = $oldX + $translationXmm
    $newLabel = "$($definition.OutputName) $([int]$length)x$([int]$width)mm #1"

    $item = [pscustomobject]@{
        SourceName = $definition.SourceName
        SourceLabel = $placement[0].Label
        OutputName = $definition.OutputName
        OutputLabel = $newLabel
        LengthMm = $length
        WidthMm = $width
        OldXmm = $oldX
        OldYmm = $oldY
        NewXmm = $newX
        NewYmm = $oldY
        MirrorGlobalAxisXmm = $oldX + $length / 2.0
        TranslateXmm = $translationXmm
    }

    $targetMap[$definition.SourceName] = $item
    $targetPlacements += $item
}

$allPlacementLabels = @{}
foreach ($placement in $sourceNest) {
    $allPlacementLabels[$placement.Label] = $placement.Onderdeel
}

function Format-Number {
    param([double]$Value)

    if ([Math]::Abs($Value) -lt 0.0005) {
        $Value = 0
    }

    return $Value.ToString('0.###', $culture)
}

function Mirror-GlobalX {
    param(
        [double]$X,
        [pscustomobject]$Target
    )

    return 2.0 * $Target.OldXmm + $Target.LengthMm - $X + $Target.TranslateXmm
}

function Transform-TargetLine {
    param(
        [string]$Line,
        [pscustomobject]$Target
    )

    $result = $Line.Replace($Target.SourceLabel, $Target.OutputLabel)

    $result = [regex]::Replace(
        $result,
        '(?<![A-Za-z])X(-?\d+(?:\.\d+)?)',
        {
            param($match)
            $x = [double]::Parse($match.Groups[1].Value, $culture)
            return 'X' + (Format-Number (Mirror-GlobalX -X $x -Target $Target))
        })

    $result = [regex]::Replace(
        $result,
        '(?<![A-Za-z])I(-?\d+(?:\.\d+)?)',
        {
            param($match)
            $i = [double]::Parse($match.Groups[1].Value, $culture)
            return 'I' + (Format-Number (-$i))
        })

    return $result
}

function Find-PlacementByOperationComment {
    param([string]$Line)

    if (-not $Line.StartsWith('(')) {
        return $null
    }

    foreach ($label in $allPlacementLabels.Keys) {
        if ($Line.StartsWith('(' + $label, [System.StringComparison]::Ordinal)) {
            return $allPlacementLabels[$label]
        }
    }

    return $null
}

function Is-CommonBoundaryLine {
    param([string]$Line)

    return $Line.StartsWith('(Laad tool ', [System.StringComparison]::Ordinal) -or
        $Line.StartsWith('(--- BEWERKING ', [System.StringComparison]::Ordinal) -or
        $Line.StartsWith('(Einde programma:', [System.StringComparison]::Ordinal) -or
        $Line -eq 'M9'
}

$outputTapLines = [System.Collections.Generic.List[string]]::new()
$motionPairs = [System.Collections.Generic.List[object]]::new()
$activePartName = $null
$activeTarget = $null
$sourceTargetG2Count = 0

foreach ($line in $sourceTapLines) {
    $operationPartName = Find-PlacementByOperationComment -Line $line
    if ($null -ne $operationPartName) {
        $activePartName = $operationPartName
        $activeTarget = if ($targetMap.ContainsKey($operationPartName)) { $targetMap[$operationPartName] } else { $null }
        if ($null -ne $activeTarget) {
            $outputTapLines.Add((Transform-TargetLine -Line $line -Target $activeTarget))
        }
        continue
    }

    if (Is-CommonBoundaryLine -Line $line) {
        $activePartName = $null
        $activeTarget = $null
    }

    if ($null -ne $activePartName) {
        if ($null -ne $activeTarget) {
            if ($line -match '^G2(?:\s|$)') {
                $sourceTargetG2Count++
            }
            $transformedTargetLine = Transform-TargetLine -Line $line -Target $activeTarget
            if ($line -match '^G[0123](?:\s|$)') {
                $motionPairs.Add([pscustomobject]@{
                    Target = $activeTarget
                    SourceLine = $line
                    OutputLine = $transformedTargetLine
                })
            }
            $outputTapLines.Add($transformedTargetLine)
        }
        continue
    }

    if ($line -in @(
        '(Machine staat op home. Plaats de volgende voorraadplaat.)',
        '(Start daarna bestand: Betonplex_18mm_NestPlaat_02.tap)',
        '(Controleer opspanning en zet/controleer Z0 op bovenzijde materiaal.)'
    )) {
        continue
    }

    $rewritten = $line
    if ($rewritten -eq '(Project: Betonplex_18mm_NestPlaat_01)') {
        $rewritten = "(Project: $jobTapBaseName)"
    }
    elseif ($rewritten -eq '(Plaat: 1 van 2)') {
        $rewritten = '(Plaat: 1 van 1)'
    }
    elseif ($rewritten -eq '(PLAAT 1 VAN 2 KLAAR)') {
        $rewritten = '(LAATSTE PLAAT KLAAR - machine staat op home)'
    }

    $outputTapLines.Add($rewritten)
}

function Csv-Field {
    param([object]$Value)

    $text = if ($null -eq $Value) { '' } else { [string]$Value }
    return '"' + $text.Replace('"', '""') + '"'
}

function Cam-X {
    param(
        [pscustomobject]$Row,
        [pscustomobject]$Target
    )

    $x = [double]::Parse($Row.X_mm, $culture)
    $length = [double]::Parse($Row.Lengte_mm, $culture)
    if ($length -gt 0.0001) {
        return $Target.LengthMm - ($x + $length)
    }

    return $Target.LengthMm - $x
}

$outputCamRows = @()
foreach ($definition in $targetDefinitions) {
    $target = $targetMap[$definition.SourceName]
    $rows = @($sourceCam | Where-Object { $_.Plaat -eq $definition.SourceName })
    foreach ($row in $rows) {
        $outputCamRows += [pscustomobject]@{
            Plaat = $target.OutputName
            Volgorde = $row.Volgorde
            Bewerking = $row.Bewerking
            Tool = $row.Tool
            X_mm = Format-Number (Cam-X -Row $row -Target $target)
            Y_mm = $row.Y_mm
            Diameter_mm = $row.Diameter_mm
            Lengte_mm = $row.Lengte_mm
            Breedte_mm = $row.Breedte_mm
            Diepte_mm = $row.Diepte_mm
            Opmerking = '[TEGENHAND] ' + $row.Opmerking
        }
    }
}

$camHeaders = @('Plaat', 'Volgorde', 'Bewerking', 'Tool', 'X_mm', 'Y_mm', 'Diameter_mm', 'Lengte_mm', 'Breedte_mm', 'Diepte_mm', 'Opmerking')
$camLines = [System.Collections.Generic.List[string]]::new()
$camLines.Add(($camHeaders -join ';'))
foreach ($row in $outputCamRows) {
    $camLines.Add((@($camHeaders | ForEach-Object { Csv-Field $row.$_ }) -join ';'))
}

$nestLines = [System.Collections.Generic.List[string]]::new()
$nestLines.Add('Nestplaat;Materiaal;Voorraadmaat_mm;Onderdeel;Instantie;X_links_mm;Y_onder_mm;Lengte_mm;Breedte_mm;Geroteerd;Nesting_hand;Label')
foreach ($target in $targetPlacements) {
    $fields = @(
        $jobTapBaseName,
        'Betonplex 18mm',
        '2500 x 1250',
        $target.OutputName,
        '1',
        (Format-Number $target.NewXmm),
        (Format-Number $target.NewYmm),
        (Format-Number $target.LengthMm),
        (Format-Number $target.WidthMm),
        'nee',
        'gespiegeld-x',
        $target.OutputLabel
    )
    $nestLines.Add((@($fields | ForEach-Object { Csv-Field $_ }) -join ';'))
}

$bomLines = [System.Collections.Generic.List[string]]::new()
$bomLines.Add('Type;Naam;Artikelnummer;Aantal;Eenheid;Materiaal;Maat;Opmerking')
$bomQuantity = if ($SingleCabinet) { 1 } else { 2 }
$bomLines.Add("Plaat;`"Kastzijde tegenhand`";;$bomQuantity;st;`"Betonplex 18mm`";`"590 x 882 x 18 mm`";`"Lokaal X-gespiegeld t.o.v. de oude gelijke kastzijden`"")
$bomLines.Add("Plaat;`"Ladezijde tegenhand`";;$bomQuantity;st;`"Betonplex 18mm`";`"545 x 158 x 18 mm`";`"Lokaal X-gespiegeld t.o.v. de oude gelijke ladezijden`"")

function New-NestingSvg {
    $scale = 0.44
    $margin = 45.0
    $stockWidth = 2500.0
    $stockHeight = 1250.0
    $svgWidth = $stockWidth * $scale + 2 * $margin
    $svgHeight = $stockHeight * $scale + 150
    $sb = [System.Text.StringBuilder]::new()

    [void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
    [void]$sb.AppendLine('<svg xmlns="http://www.w3.org/2000/svg" width="' + (Format-Number $svgWidth) + '" height="' + (Format-Number $svgHeight) + '" viewBox="0 0 ' + (Format-Number $svgWidth) + ' ' + (Format-Number $svgHeight) + '">')
    [void]$sb.AppendLine('<style>text{font-family:Arial,sans-serif;fill:#172033}.title{font-size:22px;font-weight:700}.label{font-size:17px;font-weight:700}.dim{font-size:13px}.small{font-size:11px}.stock{fill:#f5f7fa;stroke:#111827;stroke-width:2}.part{fill:#dbeafe;stroke:#2563eb;stroke-width:2}.pocket{fill:#fed7aa;fill-opacity:.5;stroke:#ea580c;stroke-width:1.5;stroke-dasharray:6 4}.hole{fill:#fff;stroke:#374151;stroke-width:1.4}.notch{fill:#f5f7fa;stroke:#2563eb;stroke-width:2}.axis{stroke:#16a34a;stroke-width:1.5;stroke-dasharray:5 4}.note{font-size:14px;fill:#166534;font-weight:700}</style>')
    [void]$sb.AppendLine('<text class="title" x="' + (Format-Number $margin) + '" y="30">' + $jobTapBaseName + ' - Betonplex 18mm - 2500 x 1250 mm</text>')
    [void]$sb.AppendLine('<text class="note" x="' + (Format-Number $margin) + '" y="53">Alleen ' + $jobPartSummary + '; ieder deel is lokaal X-gespiegeld.</text>')
    [void]$sb.AppendLine('<g transform="translate(' + (Format-Number $margin) + ',70)">')
    [void]$sb.AppendLine('<rect class="stock" x="0" y="0" width="' + (Format-Number ($stockWidth * $scale)) + '" height="' + (Format-Number ($stockHeight * $scale)) + '"/>')

    foreach ($target in $targetPlacements) {
        $x = $target.NewXmm * $scale
        $y = ($stockHeight - $target.NewYmm - $target.WidthMm) * $scale
        $w = $target.LengthMm * $scale
        $h = $target.WidthMm * $scale

        if ($target.SourceName -like 'Zijwand*') {
            $notchDepth = 80.0
            $notchHeight = 100.0
            $points = @(
                "$x,$y",
                "$($x + $w),$y",
                "$($x + $w),$($y + ($target.WidthMm - $notchHeight) * $scale)",
                "$($x + ($target.LengthMm - $notchDepth) * $scale),$($y + ($target.WidthMm - $notchHeight) * $scale)",
                "$($x + ($target.LengthMm - $notchDepth) * $scale),$($y + $h)",
                "$x,$($y + $h)"
            ) -join ' '
            [void]$sb.AppendLine('<polygon class="part" points="' + $points + '"/>')
        }
        else {
            [void]$sb.AppendLine('<rect class="part" x="' + (Format-Number $x) + '" y="' + (Format-Number $y) + '" width="' + (Format-Number $w) + '" height="' + (Format-Number $h) + '"/>')
        }

        $axisX = $x + $w / 2.0
        [void]$sb.AppendLine('<line class="axis" x1="' + (Format-Number $axisX) + '" y1="' + (Format-Number $y) + '" x2="' + (Format-Number $axisX) + '" y2="' + (Format-Number ($y + $h)) + '"><title>Lokale spiegelas</title></line>')

        $partRows = @($outputCamRows | Where-Object { $_.Plaat -eq $target.OutputName })
        foreach ($row in $partRows) {
            $localX = [double]::Parse($row.X_mm, $culture)
            $localY = [double]::Parse($row.Y_mm, $culture)
            $diameter = [double]::Parse($row.Diameter_mm, $culture)
            $pocketLength = [double]::Parse($row.Lengte_mm, $culture)
            $pocketWidth = [double]::Parse($row.Breedte_mm, $culture)
            if ($diameter -gt 0 -and $row.Bewerking -notlike 'Buitencontour*') {
                $cx = $x + $localX * $scale
                $cy = $y + ($target.WidthMm - $localY) * $scale
                $radius = [Math]::Max(1.8, $diameter * $scale / 2.0)
                [void]$sb.AppendLine('<circle class="hole" cx="' + (Format-Number $cx) + '" cy="' + (Format-Number $cy) + '" r="' + (Format-Number $radius) + '"><title>' + [System.Security.SecurityElement]::Escape($row.Opmerking) + '</title></circle>')
            }
            elseif ($pocketLength -gt 0 -and $pocketWidth -gt 0) {
                $px = $x + $localX * $scale
                $py = $y + ($target.WidthMm - $localY - $pocketWidth) * $scale
                [void]$sb.AppendLine('<rect class="pocket" x="' + (Format-Number $px) + '" y="' + (Format-Number $py) + '" width="' + (Format-Number ($pocketLength * $scale)) + '" height="' + (Format-Number ($pocketWidth * $scale)) + '"><title>' + [System.Security.SecurityElement]::Escape($row.Opmerking) + '</title></rect>')
            }
        }

        [void]$sb.AppendLine('<text class="label" x="' + (Format-Number ($x + 8)) + '" y="' + (Format-Number ($y + 23)) + '">' + [System.Security.SecurityElement]::Escape($target.OutputName) + '</text>')
        [void]$sb.AppendLine('<text class="dim" x="' + (Format-Number ($x + 8)) + '" y="' + (Format-Number ($y + 43)) + '">' + (Format-Number $target.LengthMm) + ' x ' + (Format-Number $target.WidthMm) + ' x 18 mm - TEGENHAND</text>')
        [void]$sb.AppendLine('<text class="small" x="' + (Format-Number ($x + 8)) + '" y="' + (Format-Number ($y + $h - 10)) + '">X' + (Format-Number $target.NewXmm) + ' Y' + (Format-Number $target.NewYmm) + ' - gespiegeld-x</text>')
    }

    [void]$sb.AppendLine('</g></svg>')
    return $sb.ToString()
}

function Get-MotionBounds {
    param([string[]]$Lines)

    $xs = [System.Collections.Generic.List[double]]::new()
    $ys = [System.Collections.Generic.List[double]]::new()
    foreach ($line in $Lines) {
        if ($line -notmatch '^G[0123](?:\s|$)' -or $line -match '^G28(?:\s|$)') {
            continue
        }

        if ($line -match '(?:^|\s)X(-?\d+(?:\.\d+)?)') {
            $xs.Add([double]::Parse($Matches[1], $culture))
        }
        if ($line -match '(?:^|\s)Y(-?\d+(?:\.\d+)?)') {
            $ys.Add([double]::Parse($Matches[1], $culture))
        }
    }

    return [pscustomobject]@{
        MinX = ($xs | Measure-Object -Minimum).Minimum
        MaxX = ($xs | Measure-Object -Maximum).Maximum
        MinY = ($ys | Measure-Object -Minimum).Minimum
        MaxY = ($ys | Measure-Object -Maximum).Maximum
    }
}

function Get-GcodeCommand {
    param([string]$Line)

    if ($Line -match '^(G[0123])(?:\s|$)') {
        return $Matches[1]
    }

    return ''
}

function Get-GcodeWord {
    param(
        [string]$Line,
        [char]$Letter
    )

    if ($Line -match ('(?:^|\s)' + [regex]::Escape([string]$Letter) + '(-?\d+(?:\.\d+)?)')) {
        return [pscustomobject]@{
            Present = $true
            Value = [double]::Parse($Matches[1], $culture)
        }
    }

    return [pscustomobject]@{
        Present = $false
        Value = 0.0
    }
}

function Assert-EqualWord {
    param(
        [pscustomobject]$Pair,
        [char]$Letter
    )

    $sourceWord = Get-GcodeWord -Line $Pair.SourceLine -Letter $Letter
    $outputWord = Get-GcodeWord -Line $Pair.OutputLine -Letter $Letter
    if ($sourceWord.Present -ne $outputWord.Present) {
        throw "G-codewoord $Letter ontbreekt aan een zijde: '$($Pair.SourceLine)' -> '$($Pair.OutputLine)'"
    }
    if ($sourceWord.Present -and [Math]::Abs($sourceWord.Value - $outputWord.Value) -gt 0.0005) {
        throw "G-codewoord $Letter veranderde onverwacht: '$($Pair.SourceLine)' -> '$($Pair.OutputLine)'"
    }
}

foreach ($pair in $motionPairs) {
    $sourceCommand = Get-GcodeCommand -Line $pair.SourceLine
    $outputCommand = Get-GcodeCommand -Line $pair.OutputLine
    $expectedCommand = $sourceCommand
    if ($outputCommand -ne $expectedCommand) {
        throw "Onjuiste bewegingscode na spiegeling: '$($pair.SourceLine)' -> '$($pair.OutputLine)'"
    }

    $sourceX = Get-GcodeWord -Line $pair.SourceLine -Letter 'X'
    $outputX = Get-GcodeWord -Line $pair.OutputLine -Letter 'X'
    if ($sourceX.Present -ne $outputX.Present) {
        throw "X-woord ontbreekt aan een zijde: '$($pair.SourceLine)' -> '$($pair.OutputLine)'"
    }
    if ($sourceX.Present) {
        $expectedSum = 2.0 * $pair.Target.OldXmm + $pair.Target.LengthMm + $pair.Target.TranslateXmm
        if ([Math]::Abs(($sourceX.Value + $outputX.Value) - $expectedSum) -gt 0.0005) {
            throw "X-spiegelinvariant faalt: '$($pair.SourceLine)' -> '$($pair.OutputLine)'"
        }
    }

    $sourceI = Get-GcodeWord -Line $pair.SourceLine -Letter 'I'
    $outputI = Get-GcodeWord -Line $pair.OutputLine -Letter 'I'
    if ($sourceI.Present -ne $outputI.Present) {
        throw "I-woord ontbreekt aan een zijde: '$($pair.SourceLine)' -> '$($pair.OutputLine)'"
    }
    if ($sourceI.Present -and [Math]::Abs($sourceI.Value + $outputI.Value) -gt 0.0005) {
        throw "I-spiegelinvariant faalt: '$($pair.SourceLine)' -> '$($pair.OutputLine)'"
    }

    foreach ($unchangedLetter in @('Y', 'Z', 'J', 'F')) {
        Assert-EqualWord -Pair $pair -Letter $unchangedLetter
    }
}

$bounds = Get-MotionBounds -Lines $outputTapLines.ToArray()
if ($bounds.MinX -lt 0 -or $bounds.MaxX -gt 2500 -or $bounds.MinY -lt 0 -or $bounds.MaxY -gt 1250) {
    throw "G-code buiten voorraadgrenzen: X $($bounds.MinX)..$($bounds.MaxX), Y $($bounds.MinY)..$($bounds.MaxY)"
}

$unexpectedLabels = @()
foreach ($placement in $sourceNest) {
    if ($targetMap.ContainsKey($placement.Onderdeel)) {
        continue
    }
    if ($outputTapLines -match [regex]::Escape($placement.Label)) {
        $unexpectedLabels += $placement.Label
    }
}
if ($unexpectedLabels.Count -gt 0) {
    throw "Onbedoelde onderdelen aangetroffen in herstel-G-code: $($unexpectedLabels -join ', ')"
}

$expectedCamCount = @($sourceCam | Where-Object { $targetMap.ContainsKey($_.Plaat) }).Count
if ($outputCamRows.Count -ne $expectedCamCount) {
    throw "Onverwacht aantal CAM-operaties: $($outputCamRows.Count), verwacht $expectedCamCount"
}

$operationCommentCount = 0
foreach ($line in $outputTapLines) {
    foreach ($target in $targetPlacements) {
        if ($line.StartsWith('(' + $target.OutputLabel, [System.StringComparison]::Ordinal)) {
            $operationCommentCount++
            break
        }
    }
}
if ($operationCommentCount -ne $expectedCamCount) {
    throw "Aantal G-codebewerkingen ($operationCommentCount) wijkt af van CAM-overzicht ($expectedCamCount)."
}

$outputG2Count = @($outputTapLines | Where-Object { $_ -match '^G2(?:\s|$)' }).Count
$outputG3Count = @($outputTapLines | Where-Object { $_ -match '^G3(?:\s|$)' }).Count
if ($outputG2Count -ne $sourceTargetG2Count) {
    throw "Aantal behouden G2-bogen ($outputG2Count) wijkt af van geselecteerde G2-bronbogen ($sourceTargetG2Count)."
}
if ($outputG3Count -ne 0) {
    throw "De originele job bevat geen G3-bogen; in de hersteljob zijn er onverwacht $outputG3Count."
}

[System.IO.Directory]::CreateDirectory($OutputFolder) | Out-Null

$utf8Bom = [System.Text.UTF8Encoding]::new($true)
$tapAscii = [System.Text.Encoding]::ASCII
$outputTapPath = Join-Path $OutputFolder ($jobTapBaseName + '.tap')
$outputCamPath = Join-Path $OutputFolder 'CAM-operaties.csv'
$outputNestPath = Join-Path $OutputFolder 'NestPlan.csv'
$outputBomPath = Join-Path $OutputFolder 'BOM.csv'
$outputSvgPath = Join-Path $OutputFolder 'NestVisualisatie.svg'
$readmePath = Join-Path $OutputFolder 'LEESMIJ-EERST.txt'
$validationPath = Join-Path $OutputFolder 'VALIDATIE.txt'

[System.IO.File]::WriteAllLines($outputTapPath, $outputTapLines, $tapAscii)
[System.IO.File]::WriteAllLines($outputCamPath, $camLines, $utf8Bom)
[System.IO.File]::WriteAllLines($outputNestPath, $nestLines, $utf8Bom)
[System.IO.File]::WriteAllLines($outputBomPath, $bomLines, $utf8Bom)
[System.IO.File]::WriteAllText($outputSvgPath, (New-NestingSvg), $utf8Bom)

$readmePartList = if ($SingleCabinet) {
    @"
- 1 x Kastzijde tegenhand, 590 x 882 x 18 mm
- 1 x Ladezijde tegenhand, 545 x 158 x 18 mm
"@
}
else {
    @"
- 2 x Kastzijde tegenhand, 590 x 882 x 18 mm
- 2 x Ladezijde tegenhand, 545 x 158 x 18 mm
"@
}
$readmePairing = if ($SingleCabinet) {
    'Met deze twee nieuwe tegenhanden kan eerst een compleet kastje met de oude delen worden opgebouwd.'
}
else {
    'Met deze vier nieuwe tegenhanden kunnen de oude delen per paar over twee kastjes worden verdeeld.'
}

$readme = @"
HERSTELJOB SW-20260617-204859 - LEES DIT VOOR HET FREZEN

Doel
----
Deze job freest uitsluitend $jobPartCount nieuwe tegenhand-delen:
$readmePartList

Elk nieuw deel is lokaal over de X-as gespiegeld ten opzichte van het overeenkomstige
oude deel uit SW-20260617-204859. De twee oude kastzijden waren onderling gelijk en
de twee oude ladezijden waren onderling geometrisch gelijk. $readmePairing

Machine-instelling
------------------
- Verwachte voorraadplaat: Betonplex 18 mm, 2500 x 1250 mm
- G54 X0/Y0: links-onder van de voorraadplaat
- Z0: bovenzijde materiaal
- De $jobPartCount delen liggen binnen de linker 1250 mm van de plaat.
- Gebruik niet de oude labels "links/rechts" om te paren; controleer de fysieke tegenhand.

Bestanden
---------
- $jobTapBaseName.tap : uit te voeren Mach3-programma
- NestVisualisatie.svg : visuele controle
- NestPlan.csv         : plaatsingen en gespiegeld-x-markering
- CAM-operaties.csv    : lokale, gespiegeld berekende CAM-coordinaten
- BOM.csv              : uitsluitend de $jobPartCount herstelonderdelen
- VALIDATIE.txt        : automatische technische controles

Belangrijk
----------
De geometrie, Z-dieptes, voedingen, tabs en gereedschappen zijn afkomstig uit de
oude freesrun. Alleen de $jobPartCount geselecteerde onderdelen zijn behouden. Hun X-geometrie
is per onderdeel gespiegeld. De volledige G2-cirkels blijven G2; hun I-offsets zijn
gespiegeld zodat de cirkelgeometrie correct blijft. Het TAP-bestand gebruikt exact zoals de bron ASCII zonder BOM,
CRLF-regeleindes en dezelfde kop-, toolwissel- en eindstructuur.
"@
[System.IO.File]::WriteAllText($readmePath, $readme, $utf8Bom)

$sourceTapHash = (Get-FileHash -LiteralPath $sourceTapPath -Algorithm SHA256).Hash
$sourceCamHash = (Get-FileHash -LiteralPath $sourceCamPath -Algorithm SHA256).Hash
$validation = @"
VALIDATIE HERSTELJOB

Bronmap: $SourceFolder
Bron TAP SHA256: $sourceTapHash
Bron CAM SHA256: $sourceCamHash

Geselecteerde delen: $jobPartCount
CAM-operaties: $($outputCamRows.Count) (verwacht $expectedCamCount)
G-codebewerkingsblokken: $operationCommentCount (verwacht $expectedCamCount)
Paarsgewijs gecontroleerde freesbewegingen: $($motionPairs.Count)
G-codegrenzen: X $(Format-Number $bounds.MinX) t/m $(Format-Number $bounds.MaxX) mm; Y $(Format-Number $bounds.MinY) t/m $(Format-Number $bounds.MaxY) mm
Voorraadgrenzen: X 0 t/m 2500 mm; Y 0 t/m 1250 mm
Onbedoelde onderdeellabels: geen
G2-bogen: $outputG2Count (gelijk aan $sourceTargetG2Count geselecteerde G2-bronbogen)
G3-bogen: $outputG3Count (gelijk aan de originele opbouw)
TAP-encoding: ASCII zonder BOM
Regeleindes: CRLF
Extra regels t.o.v. originele kop/toolwisselstructuur: geen

Spiegel- en verplaatsformule per onderdeel:
X_nieuw_machine = 2 * X_oud_plaatsing + lengte - X_oud_machine - 593
Y_nieuw_machine = Y_oud_machine
I_nieuw = -I_oud
G2 blijft G2

Uitkomst: automatische controles geslaagd.
"@
[System.IO.File]::WriteAllText($validationPath, $validation, $utf8Bom)

Write-Output "Hersteljob gemaakt: $OutputFolder"
Write-Output "G-codegrenzen: X $(Format-Number $bounds.MinX)..$(Format-Number $bounds.MaxX), Y $(Format-Number $bounds.MinY)..$(Format-Number $bounds.MaxY)"
Write-Output "CAM-operaties: $($outputCamRows.Count); G-codebewerkingen: $operationCommentCount; G2-bogen: $outputG2Count; G3-bogen: $outputG3Count"
