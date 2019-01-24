# Parent Measure

Parent measure can be used as:

* A parent with some child measures referenced to
* A standalone measure 

## Options

### `Line`, `Line2`, `Line3`, ...

Defines script will be invoked at update line by line. Last object of this script will be measure value.

PowerShell syntax allows you to define a whole valid script in one line, but in favor of customization and readability, please do break them down to reasonable line width.

{% hint style="info" %}
If your script is too long, it is best to write them in script file and set script file path in `ScriptFile` option.
{% endhint %}

### **`ScriptFile`**

Specify file name \(if script file is in same folder as skin config\) or direct path to script file.

Script files can only be used in parent measure. And only script in script file is invoked, if you both set `ScriptFile` and `Line`s, script defined in `Line`s will be ignored.

To have measure value, you need to define function `Update` with no parameter.  
`Update` function is called every time skin updating and whatever it returning will be measure value.

{% code-tabs %}
{% code-tabs-item title="Example" %}
```text
function Update {
    return "Example value from PowershellRM measure"
}
```
{% endcode-tabs-item %}
{% endcode-tabs %}

Optionally, you can define function `Finalize` with no parameter.  
`Finalize` function is called when reloads/unloads skin or exits Rainmeter. It's particularly useful for script to clean up or destroy objects or kill spawned processes.

### `ExecutionPolicy` 

**Default:** `default`

Change execution policy to execute or load unsigned script. Use at caution.  
Valid values:

* `allsigned`
* `bypass`
* `default`
* `remotesigned`
* `restricted`
* `undefined`
* `unrestricted`

