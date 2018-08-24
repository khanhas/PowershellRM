# PowershellRM
Invokes Powershell script in Rainmeter

## Example
```ini
[Rainmeter]
Update = 100

[Variables]
WeatherLink = https://query.yahooapis.com/v1/public/yql?q=select item.condition from weather.forecast where woeid in (select woeid from geo.places(1) where text="hochiminh, vn")&format=json

[ShapeMeter]
Meter = Shape
Shape = Rectangle 0,0,300,300,10 | StrokeWidth 0

[psrm]
Measure = plugin
plugin = PowershellRM
Line  = $request = Invoke-WebRequest '#WeatherLink#'
Line2 = $request = $request.Content | ConvertFrom-Json
Line3 = $weather = $request.query.results.channel.item.condition

[child_1]
Measure = plugin
Plugin = PowershellRM
ParentName = psrm
Line = $weather.temp

[child_2]
Measure = plugin
Plugin = PowershellRM
ParentName = psrm
Line = $weather.text

[Temp]
Meter=String
MeasureName = child_1
Text = %1[\xBA]F
FontSize = 40
X = 10
Y = 10

[Condition]
Meter=String
MeasureName = child_2
FontSize = 30
X = r
Y = 5R
```
