# PowershellRM
Invokes Powershell script in Rainmeter

## Install
- Download and install rmskin package in [Release page](https://github.com/khanhas/PowershellRM/releases/)  
    or download dll zip package, extract the dll corresponding to your system platform to `%appdata%\Rainmeter\Plugins`.

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

#### `ExecutionPolicy` (Default = `default`)
Change execution policy to execute or load digitally unsigned script. Use at caution.  
Valid values:
- `unrestricted`
- `remotesigned`
- `allsigned`
- `restricted`
- `bypass`
- `undefined`

### Child only
#### `ParentName`
Set parent that this measure will be shared session state with.

## Functions
Rainmeter API is exposed to use in Powershell script by accessing variable `$RmAPI`. Followings is list of available functions you can call directly in powershell script:

`$RmAPI` | Param | Description
---|---|---
`.Execute` | `(bangs)` | Execute Rainmeter bangs.
`.GetMeasureName` | `()` | Returns current measure name.
`.GetSkin` | `()` | Retrieves interger value of the internal pointer to the current skin.
`.GetSkinName` | `()` | Returns current skin name.
`.GetSkinWindow` | `()` | Returns interger value of the pointer to the handle of the skin window.
`.Log` | `(logType, message)` | Prints message to Log Window.
`.LogF` | `(logType, format, ...args[])` | Prints formated string to Log Window
`.ReadDouble` | `(option, defaultValue)` | Retrieves measure option value in `double` type. 
`.ReadInt` | `(option, defaultValue)` | Retrieves measure option value in interger. 
`.ReadPath` | `(option, defaultValue)` | Retrieves measure option defined in the skin file and converts a relative path to a absolute path.
`.ReadString` | `(option, defaultValue)` | Retrieves the option defined in the skin file as a string.
`.ReplaceVariables` | `(input)` | Returns a string, replacing any variables (or section variables) within the inputted string.
 
Valid `logType`:
- `1`: Error
- `2`: Warning
- `3`: Notice
- `4`: Debug

## Log
There are 2 ways that you can use to print log into Rainmeter log windows:
### 1. Use Rainmeter API:
```powershell
# Print a warning:
$RmAPI.Log(2, "WARNING! Be careful dude.")

#Print a formatted error:
$errorSource = "an option"
$check = "Color variable"
$RmAPI.LogF(1, "ERROR! You messed {0} up! Check {1} again!", $errorSource, $check)
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
