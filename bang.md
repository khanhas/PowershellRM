# Bang

You can invoke functions, cmdlets or a script block with `!CommandMeasure` bang.

{% code-tabs %}
{% code-tabs-item title="Example Bang usage" %}
```php
[PSRM]
Measure = Plugin
Plugin = PowershellRM
Line = function FirstFile() { $f = Get-ChildItem .\; $RmAPI.Log($f[0].FullName) }

[Meter]
Meter = Shape
Shape = Rectangle 0,0,200,200
LeftMouseUpAction = !CommandMeasure PSRM "FirstFile"
```
{% endcode-tabs-item %}
{% endcode-tabs %}

