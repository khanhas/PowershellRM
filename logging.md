# Logging

There are 2 ways that you can use to print log into Rainmeter log windows

## Rainmeter API

| Function | Parameters | Description |
| :--- | :--- | :--- |
| `$RmAPI.Log` | `(message)` | Prints notice message |
| `$RmAPI.Log` | `(format, ...arg[])` | Prints formatted notice message |
| `$RmAPI.LogDebug` | `(message)` | Prints debug message |
| `$RmAPI.LogDebug` | `(format, ...arg[])` | Prints formatted debug message |
| `$RmAPI.LogError` | `(message)` |  Prints error message |
| `$RmAPI.LogError` | `(format, ...arg[])` | Prints formatted error message |
| `$RmAPI.LogWarning` | `(message)` | Prints warning message |
| `$RmAPI.LogWarning` | `(format, ...arg[])` | Prints formatted warning message |

{% code-tabs %}
{% code-tabs-item title="Example using Rainmeter API to log" %}
```php
# Print a warning:
$RmAPI.LogWarning("WARNING! Be careful dude.")

# Print a formatted error:
$errorSource = "an option"
$check = "Color variable"
$RmAPI.LogError("ERROR! You messed {0} up! Check {1} again!", $errorSource, $check)
```
{% endcode-tabs-item %}
{% endcode-tabs %}

### `Write-` cmdlets

Powershell has these commands on default and they are re-routed to print to Log window:

| Cmdlet | Description |
| :--- | :--- |
| `Write-Host` | Print notice message |
| `Write-Warning` | Print warning message |
| `Write-Debug` | Print debug message |
| `Write-Verbose` | Print notice message |

Message from `Write-Debug` is only printed when you manually set in your script variable:

```text
$DebugPreference = "Continue"
```

Message from `Write-Verbose` is only printed when you manually set in your script variable:

```text
$VerbosePreference = "Continue"
```

