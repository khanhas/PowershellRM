# Async Measure

Async measure is tailored for long executing time or infinite loop script. 

Script will be executed asynchronously without blocking Rainmeter main thread so other measures/meters can continue to work.

Measure value are just invocation status, not any object from script. 

[Inline function](https://eehh.com) is only be executed when script finishes.

To specify a Powershell plugin measure as Async measure, set `Async = 1`

{% code-tabs %}
{% code-tabs-item title="Example Async measure" %}
```text
[MyPSRM]
Measure = Plugin
Plugin = PowershellRM
Async = 1
Line  = while ($true) { 
Line2 =   $RmAPI.Log("Aya. Ayaya! Ayaya! A-ya-ya! Ayaya! Ayaya!");
Line3 =   Sleep -Milliseconds 500 }
```
{% endcode-tabs-item %}
{% endcode-tabs %}

## Options

### `Line`, `Line2`, `Line3`, ...

Defines script will be invoked at update line by line. Last object of this script will be measure value.

PowerShell syntax allows you to define a whole valid script in one line, but in favor of customization and readability, please do break them down to reasonable line width.

{% hint style="info" %}
 If your script is too long, it is best to write them in script file and set script file path in `ScriptFile` option.
{% endhint %}

### `ScriptFile`

Specify file name \(if script file is in same folder as skin config\) or direct path to script file.

Script files can only be used in parent measure. And only script in script file is invoked, if you both set `ScriptFile` and `Line`s, script defined in `Line`s will be ignored.

{% hint style="info" %}
`Unlike Parent measure, there's no measure value to be updated so you don't need to define Update function.`
{% endhint %}

\`\`



