---
description: Functions to be used in measure section variable.
---

# Inline Functions

**Requirement:** Rainmeter 4.1 or later.

**Syntax of inline functions:**

```text
[&Measure:Function(parameter, parameter, ...)]
```

{% hint style="info" %}
Comma is used for separating inline function parameters. If your script or string have comma , draps them inside double quotes.
{% endhint %}

 `DynamicVariables = 1` MUST be set on any measures or meters where inline function is used.



**Example usage:**

```php
[Psrm]
Measure = Plugin
Plugin = PowershellRM
Line = function Nuzzle($num) { return "Rawr x$($num)!" }
Line2 = $weedNumber = 420

[TextMeter]
Meter = String
Text = [&Psrm:Invoke(Nuzzle 3)]
W = [&Psrm:Variable(weedNumber)]
SolidColor = 000000
FontColor = FFFFFF
FontSize = 30
DynamicVariables = 1
```

![](.gitbook/assets/image.png)

## `Invoke`

**Syntax: `[&Measure:Invoke(script)]`**

invokes **`script`** and last output object will be section variable value.

## `Variable`

**Syntax:** `[&Measure:Variable(variableName[, defaultValue])`

Get a PowerShell variable value. 

Optionally, you can set `defaultValue` to fallback to when runspace is not ready or variable is not available. Default `defaultValue` is blank string.

## `Expand`

**Syntax:** `[&Measure:Expand(input)]`

Returns `input` string with all of the variable and expression substitutions done.

{% code-tabs %}
{% code-tabs-item title="Example Expand usage" %}
```text
[&Weather:Expand(Today is $($condition)`nTemp: $($tempC * 9 / 5 + 32) F)]
```
{% endcode-tabs-item %}
{% endcode-tabs %}

