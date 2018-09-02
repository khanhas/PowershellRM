Add-Type @"
public struct Timer {
    public string Name;
    public uint TotalSec;
    public System.Diagnostics.Stopwatch SW;
}
"@
[void][Reflection.Assembly]::LoadWithPartialName('Microsoft.VisualBasic')

# Create an array to store timers
$Global:timerList = New-Object System.Collections.Generic.List[Timer]
Function AddTimer {
    param(
        [string]$name = "Timer",
        [uint32]$hour = 0,
        [uint32]$minute = 0
    )

    if ($hour -eq 0 -and $minute -eq 0) {return}

    $item = New-Object -TypeName Timer -Property @{
        Name = $name
        TotalSec = ($hour * 60 * 60 + $minute * 60)
        SW = New-Object System.Diagnostics.StopWatch
    }

    $item.SW.Start()

    $Global:timerList.Add($item)
}

$Global:Times = ""

Function Popup {
    param(
        [string]$message = ""
    )

    # Create another powershell for popup to prevent locking thread
    # so multiple popups can pop at same time
    $newPS = [System.Management.Automation.PowerShell]::Create()
    $newPS.Runspace.SessionStateProxy.SetVariable("message",$message)
    $newPS.AddScript({
        (New-Object -ComObject Wscript.Shell).Popup($message, 0, "Ding dong", 0x1)
    }).BeginInvoke()
}

Function EnterTimerNameAndAdd {
    param(
        [uint32]$hour = 0,
        [uint32]$minute = 0
    )

    $name = [Microsoft.VisualBasic.Interaction]::InputBox('Enter your timer name:', 'Timer')

    # If user entered nothing or hit canle, $name will be blank string
    if ($name -ne "")
    {
        AddTimer $name $hour $minute
    }
}

Function Update {
    $Global:Times = ""

    if ($Global:timerList.Count -eq 0) {
        return ""
    }

    $names = ""

    $Global:timerList.ToArray() | ForEach-Object {
        $remainingSecond = $_.TotalSec - $_.SW.Elapsed.TotalSeconds

        $remainingMinute = [Math]::Floor($remainingSecond / 60)
        $remainingHour = [Math]::Floor($remainingMinute / 60)

        $Global:Times += "- $($remainingHour):$($remainingMinute):$(60 - $_.SW.Elapsed.Seconds)`n"
        $names += "$($_.Name)`n"

        if ($remainingSecond -le 0) {
            $_.SW.Stop()
            Popup $_.Name
            $Global:timerList.Remove($_)
        }
    }
    return $names
}
