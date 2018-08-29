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
If your script is too long, it is best to define script file and set its path in `ScriptFile`.

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

### Development
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
