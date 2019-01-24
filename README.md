---
description: >-
  Execute a script file or just couple lines of Powershell right in Rainmeter,
  natively. Results of invocation will be measure value and be updated along
  with skin update.
---

# Home

## Install

[Download](https://github.com/khanhas/PowershellRM/releases)

Download rmskin package, with example skins and plugin bundled.

or Download dll zip package and extract the dll corresponding to your Windows platform to `%appdata%\Rainmeter\Plugins`.

### Requirements

* .NET Framework 4.5 or later.
* PowerShell 3.0 or later.

{% hint style="info" %}
 Windows 10 already shipped with PowerShell 5.1 so everything works out of the box. 
{% endhint %}

#### Windows 7

Default PowerShell shipped in Windows 7 is 2.0. If you're not sure which version you have or upgraded, open PowerShell CLI and run

```text
$PSVersionTable.PSVersion.Major
```

If value returned is `2`, download and upgrade newest PowerShell here: [https://www.microsoft.com/en-us/download/details.aspx?id=54616](https://www.microsoft.com/en-us/download/details.aspx?id=54616)

### Basic usage

There are 2 ways to input your script:

#### 1. Uses script set in `Line` options:

```text
[PSRM]
Measure = Plugin
Plugin = PowershellRM
Line  = Get-Process |
Line2 = Where-Object {$_.ProcessName -eq "explorer"} |
Line3 = Select -ExpandProperty "Path"
```

Last output object will be this measure value.

#### 2. Uses a PowerShell script file

```text
[PSRM]
Measure = Plugin
Plugin = PowershellRM
ScriptFile = myScript.ps1
```

In this script file, you need to define function `Update` with no parameter.  
`Update` function is called every time skin updating and whatever it returning will be this measure value.

For more example usage, check out [example skins](https://github.com/khanhas/PowershellRM/tree/master/example-skins)

