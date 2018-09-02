$Global:ClipCollection = @($null, $null, $null, $null, $null)

function Update {
    $clip = Get-Clipboard -Format Text -Raw

    if ($clip -ne $Global:ClipCollection[4] -and $null -ne $clip) {
        # Copy newer clipboard to older clipboard position
        for ($i = 0; $i -lt 4; $i++) {
            $Global:ClipCollection[$i] = $Global:ClipCollection[$i + 1]
        }
        # Set the newest clipboard to last position of collection
        $Global:ClipCollection[4] = $clip
    }

    for ($i = 4; $i -ge 0; $i--) {
        if ($null -eq $Global:ClipCollection[$i]) {
            break;
        }
        $meterIndex = 5 - $i
        $RmAPI.Execute("!SetOption Icon$meterIndex Text `"[\xF0E3]`"")
        $RmAPI.Execute("!SetOption Value$meterIndex Text `"`"`"$($Global:ClipCollection[$i])`"`"`"")
        $RmAPI.Execute("!SetOption Value$meterIndex TooltipText `"`"`"$($Global:ClipCollection[$i])`"`"`"")
    }
}

function SetClip {
    param (
       [int]$index = 1
    )
    $arrayIndex = 5 - $index
    if ($Global:ClipCollection[$arrayIndex]) {
        Set-Clipboard $Global:ClipCollection[$arrayIndex]
    }
}