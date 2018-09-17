 $ClipCollection = 0..4 | ForEach-Object { $null }

function Update {
    $clip = Get-Clipboard -Format Text -Raw

    if ($clip -ne $ClipCollection[0] -and $null -ne $clip) {
        # Copy newer clipboard to older clipboard position
        for ($i = 4; $i -ge 1; $i--) {
            $ClipCollection[$i] = $ClipCollection[$i - 1]
        }

        # Set the newest clipboard to first position of collection
        $ClipCollection[0] = $clip

        for ($i = 0; $i -le 4; $i++) {
            $item = $ClipCollection[$i]

            if ($null -eq $item) {
                break;
            }

            $RmAPI.Bang("!ShowMeter Icon$i")

            $strippedDownline = $item -replace "`n", "" -replace "`r", ""
            $RmAPI.Bang("!SetOption Value$i Text `"`"`"$strippedDownline`"`"`"")
            $RmAPI.Bang("!SetOption Value$i TooltipText `"`"`"$item`"`"`"")
        }
    }
}

function SetClip {
    param (
       [int]$index = 0
    )

    if ($ClipCollection[$index]) {
        Set-Clipboard $ClipCollection[$index]
    }
}