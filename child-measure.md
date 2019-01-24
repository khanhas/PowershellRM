# Child Measure

Parent and Child measures shares same runspace and session state, it means Child measure can access and change variables, functions from Parent and other previous child.

Only one level of parent/children is allowed.

## Options

### `Parent`

Set parent that this measure will be shared session state with.

### `Line`, `Line2`, `Line3`, ... 

Defines script will be invoked at update line by line.

Powershell syntax allows you to define a whole valid script in one line, but in favor of customization and readiblity, please do break them down to reasonable line width.

