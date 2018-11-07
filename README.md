<div align="center">
    <img src="https://github.com/khanhas/powershellrm/blob/master/assets/logo.svg">
</div>

## Install
- Download and install rmskin package in [Release page](https://github.com/khanhas/PowershellRM/releases/)  
    or download dll zip package, extract the dll corresponding to your system platform to `%appdata%\Rainmeter\Plugins`.

#### Windows 7
Before running Rainmeter, make sure you have at least PowerShell 5.1 version first by opening up PowerShell CLI and typing in:
```powershell
$psversiontable
```

It should shows: https://i.imgur.com/P6aCmr2.png

Download [PowerShell 5.1 for Windows 7](https://www.microsoft.com/en-us/download/confirmation.aspx?id=54616).

#### Windows 10
Windows 10 already comes with PowerShell 5.1 so you don't need to worry. Everything works out of the box. 

## Basic usage
There are 2 ways to input your script:

### 1. Using a PS script file

```ini
[PSRM]
Measure = Plugin
Plugin = PowershellRM
ScriptFile = myScript.ps1
```
In this script file, you need to define function `Update` with no parameter.  
`Update` function is called every time skin updating and whatever it returning will be this measure value.

### 2. Using script that are set in measure options:

```ini
[PSRM]
Measure = Plugin
Plugin = PowershellRM
Line  = Get-Process |
Line2 = Where-Object {$_.ProcessName -eq "explorer"} |
Line3 = Select -ExpandProperty "Path"
```
Last output object will be this measure value.

For more example usage, check out [example skins](https://github.com/khanhas/PowershellRM/tree/master/example-skins)

## Measure options:
PowershellRM can be used as a standalone measure or parent/children measures that share same session state.  
It means children measures can use variable or function that is defined in parent measure script.  
Only one level of parent/children is allowed.

### Parent and Child
#### `Line`, `Line2`, `Line3`, ...
Defines script will be invoked at update line by line.
Powershell syntax allows you to define a whole valid script in one line, but for sake of customization and readiblity, please do break them down to reasonable line width.
If your script is too long, it is best to write them in script file and set script file path in `ScriptFile` option.

### Parent only
#### `ScriptFile`
Script files can only be used in parent measure. And only script in script file is invoked, if you both set ScriptFile and `Line`s, script defined in `Line`s will be ignored.  
In this script file, you need to define function `Update` with no parameter.  
`Update` function is called every time skin updating and whatever it returning will be measure value.  
Optionally, you can define function `Finalize` with no parameter.  
`Finalize` function is called when reloads/unloads skin or when exits Rainmeter.   

#### `ExecutionPolicy` (Default = `default`)
Change execution policy to execute or load digitally unsigned script. Use at caution.  
Valid values:
- `allsigned`
- `bypass`
- `default`
- `remotesigned`
- `restricted`
- `undefined`
- `unrestricted`

### Child only
#### `Parent`
Set parent that this measure will be shared session state with.

## Functions
Rainmeter API is exposed to use in Powershell script by accessing variable `$RmAPI`. Followings is list of available functions you can call directly in powershell script:

`$RmAPI` | Param | Returns | Description
---|---|---|---
`.Bang` | `(bangs)` | | Execute Rainmeter bangs.
`.GetMeasureName` | `()` | string | Returns current measure name.
`.GetSkinName` | `()` | string | Returns current skin name.
`.GetSkinHandle` | `()` | int | Returns interger value of the pointer to the handle of the skin window.
`.Log` | `(message)` | | Prints notice message to Log Window.
`.Log` | `(format, ...arg[])` | | Prints formatted notice message to Log Window.
`.LogDebug` | `(message)` | | Prints debug message to Log Window. 
`.LogDebug` | `(format, ...arg[])` | | Prints formatted debug message to Log Window.
`.LogError` | `(message)` | | Prints error message to Log Window.
`.LogError` | `(format, ...arg[])` | | Prints formatted error message to Log Window.
`.LogWarning` | `(message)` | | Prints warning message to Log Window.
`.LogWarning` | `(format, ...arg[])` | | Prints formatted warning message to Log Window.
`.Measure` | `(measureName)` | double \| null | Gets number value of a measure. Returns `$null` if measure doesn't exist.
`.Measure` | `(measureName, defaultValue)` | double | Gets number value of a measure.  Returns `defaultValue` if measure doesn't exist.
`.MeasureStr` | `(measureName)` | string \| null | Gets string value of a measure. Returns `$null` if measure doesn't exist.
`.MeasureStr` | `(measureName, defaultValue)` | string \| null | Gets string value of a measure.  Returns `defaultValue` if measure doesn't exist.
`.Option` | `(option, defaultValue = 0.0)` | double | Retrieves measure option value in `double` type. 
`.OptionInt` | `(option, defaultValue = 0)` | int | Retrieves measure option value in interger. 
`.OptionStr` | `(option, defaultValue = "")` | string | Retrieves measure option value as a string. 
`.OptionPath` | `(option, defaultValue = "")` | string | Retrieves measure option defined in the skin file and converts a relative path to a absolute path.
`.ReplaceVariables` | `(input)` | double | Replaces any variables (or section variables) within the inputted string, tries to parse and returns result as double. Returns `0.0` if it cannot parse.
`.ReplaceVariablesStr` | `(input)` | string | Returns a string, replacing any variables (or section variables) within the inputted string.
`.Variable` | `(variableName)` | double \| null | Gets a skin variable value, tries to parse and returns result as double. Returns `0.0` if it cannot parse. Returns `$null` if variable doesn't exist.
`.Variable` | `(variableName, defaultValue)` | double | Gets a skin variable value, tries to parse and returns result as double. Returns `0.0` if it cannot parse. Returns `defaultValue` if variable doesn't exist.
`.VariableStr` | `(variableName)` | string \| null | Gets a skin variable value as a string. Returns `$null` if variable doesn't exist.
`.VariableStr` | `(variableName, defaultValue)` | string | Gets a skin variable value as a string. Returns `defaultValue` if variable doesn't exist.

## Log
There are 2 ways that you can use to print log into Rainmeter log windows:
### 1. Use Rainmeter API:
```powershell
# Print a warning:
$RmAPI.LogWarning("WARNING! Be careful dude.")

#Print a formatted error:
$errorSource = "an option"
$check = "Color variable"
$RmAPI.LogError("ERROR! You messed {0} up! Check {1} again!", $errorSource, $check)
```

### 2. Use `Write-` commands  
Powershell has these commands on default and I re-routed them to print to Log window:
```powershell
# Print a notice:
Write-Host
# Print a warning:
Write-Warning
```
Currently, I'm investigating why `Write-Debug` and `Write-Error` don't works.

`Write-Ouput` also works but what it writes will be used as measure value if it's the last object in the script. It won't print anything to Log window.
For example:
```powershell
$foo = "Bar"
Write-Output "Stuff"
$foo
```
Measure value is `Bar`

```powershell
$foo = "Bar"
$foo
Write-Output "Stuff"
```
Measure value is `Stuff`

## Section variable
Rainmeter supports calling function from plugin and replace whatever function retuns as variable's value.  
In this function, you can place multiple parameters, separated by commas (`,`).  
Example:
```ini
[MeterTitle]
Meter = String
Text = [MeasureName:ExampleFunction(arg_one, arg_two, arg_three)]
DynamicVariables = 1
```

I already implemented a function called `Invoke` to invoke one or more scripts:
### `Invoke(script, script, ...)`
Each script should be fitted in one parameter. Scripts will be invoked one by one.  
Last output object will be section variable value.

**Example:**
```ini
Text = [MeasureName:Invoke(Get-Process | Select-Object -Index 1 | Select-Object -ExpandProperty ProcessName)]
```

Keeps in mind that comma is for separating section variable function's parameters, if you tend to use it in script, drap your script inside double quotes and use single quotes in double quotes's place inside your script.  

**Example:**
Joins an array of string containing `Yomama`, a Rainmeter variable `Verb` and a Powershell variable `Noun`
```ini
[Rainmeter]
Update = 1000

[Variables]
Verb = is

[PSMR]
Measure = Plugin
Plugin = PowershellRM
Line = $Noun = "The Obesity"

[Meter]
Meter = String
Text = [MeasureName:Invoke("'Yomama', '#Verb#', $Noun -join ' - '")]
```

Because of this messy interface, you really should prepare a function in your PowershellRM script that only need to pass few parameters that are simple objects like string, interger or float. Then uses `:Invoke` to call that function.

## Development
Requirements:
- Visual Studio 2015/2017
- .NET Framework 4.5

```bash
git clone https://github.com/khanhas/PowershellRM
```

```bash
cd PowershellRM
.\powershell-rm.sln
```
