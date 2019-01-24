# Rainmeter API

Rainmeter API is exposed and can be accessed via `$RmAPI` in your script.

Following is complete list of all available functions:

| `$RmAPI` | Param | Returns | Description |
| :--- | :--- | :--- | :--- |
| `.Bang` | `(bangs)` |  | Execute Rainmeter bangs. |
| `.GetMeasureName` | `()` | string | Returns current measure name. |
| `.GetSkinName` | `()` | string | Returns current skin name. |
| `.GetSkinHandle` | `()` | int | Returns interger value of the pointer to the handle of the skin window. |
| `.Log` | `(message)` |  | Prints notice message to Log Window. |
| `.Log` | `(format, ...arg[])` |  | Prints formatted notice message to Log Window. |
| `.LogDebug` | `(message)` |  | Prints debug message to Log Window. |
| `.LogDebug` | `(format, ...arg[])` |  | Prints formatted debug message to Log Window. |
| `.LogError` | `(message)` |  | Prints error message to Log Window. |
| `.LogError` | `(format, ...arg[])` |  | Prints formatted error message to Log Window. |
| `.LogWarning` | `(message)` |  | Prints warning message to Log Window. |
| `.LogWarning` | `(format, ...arg[])` |  | Prints formatted warning message to Log Window. |
| `.Measure` | `(measureName)` | double \| null | Gets number value of a measure. Returns `$null` if measure doesn't exist. |
| `.Measure` | `(measureName, defaultValue)` | double | Gets number value of a measure.  Returns `defaultValue` if measure doesn't exist. |
| `.MeasureStr` | `(measureName)` | string \| null | Gets string value of a measure. Returns `$null` if measure doesn't exist. |
| `.MeasureStr` | `(measureName, defaultValue)` | string \| null | Gets string value of a measure.  Returns `defaultValue` if measure doesn't exist. |
| `.Option` | `(option, defaultValue = 0.0)` | double | Retrieves measure option value in `double` type. |
| `.OptionInt` | `(option, defaultValue = 0)` | int | Retrieves measure option value in integer. |
| `.OptionStr` | `(option, defaultValue = "")` | string | Retrieves measure option value as a string. |
| `.OptionPath` | `(option, defaultValue = "")` | string | Retrieves measure option defined in the skin file and converts a relative path to a absolute path. |
| `.ReplaceVariables` | `(input)` | double | Replaces any variables \(or section variables\) within the inputted string, tries to parse and returns result as double. Returns `0.0` if it cannot parse. |
| `.ReplaceVariablesStr` | `(input)` | string | Returns a string, replacing any variables \(or section variables\) within the inputted string. |
| `.Variable` | `(variableName)` | double \| null | Gets a skin variable value, tries to parse and returns result as double. Returns `0.0` if it cannot parse. Returns `$null` if variable doesn't exist. |
| `.Variable` | `(variableName, defaultValue)` | double | Gets a skin variable value, tries to parse and returns result as double. Returns `0.0` if it cannot parse. Returns `defaultValue` if variable doesn't exist. |
| `.VariableStr` | `(variableName)` | string \| null | Gets a skin variable value as a string. Returns `$null` if variable doesn't exist. |
| `.VariableStr` | `(variableName, defaultValue)` | string | Gets a skin variable value as a string. Returns `defaultValue` if variable doesn't exist. |

