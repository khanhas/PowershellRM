$Global:a = 1

function Update
{
    $Global:a = $Global:a + 1
    return $Global:a
}