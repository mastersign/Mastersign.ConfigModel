# String Sourcing

String sourcing can be used to move long strings out of a configuration file
into separate files.
This could could be e. g. a SQL string or an HTML snippet/template.

Use the `$sources` map to specify a relative or absolute path to a text file
for string properties.
The content of the text file is then loaded with UTF-8 encoding
and assigned to the property.

This example uses the class model from the [Introduction](intro.md).

`config.yaml`:

```yaml
$sources:
  Project: resources/name.txt
  Description: resources/description.txt

Project: My Project
```

`resources/name.txt`

```txt
Default Project Name
```

`resources/description.txt`

```txt
Description from file
```

Loading the `config.yaml` yields the following model:

```yaml
Project: My Project
Description: Description from file
```

String sourcing only works on model classes derived from `ConfigModelBase`.

Explicit string values from the sourcing model supersede sourced strings.
In the example above, the value `Value B` for `B` supersede the sourced
string `B from File`.
