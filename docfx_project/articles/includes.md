# Includes

With includes, you can spread a large configuration model over multiple files.
Includes can complement each other e. g. for different parts of the model,
or build overlays with default values and more specific layered on top.

Use the `$includes` array to specify one or more
relative or absolute paths — or glob patterns — to other YAML files.

Glob patterns support the `*` for any characters but a path separator,
and `**` for any characters including path separators.

This example uses the class model from the [Introduction](intro.md).

`config.yaml`:

```yaml
$includes:
  - defaults.yaml
  - config.d/**/*.yaml

A: Value A
Child:
  Ys:
    b: 20
```

The included files are used as base for the including model in the given order.
Globbed files are sorted alphabetically.
Values from the including file supersede the values from included files.

`defaults.yaml`:

```yaml
A: Default A
B: Default B
Child:
  X: 1
  Ys:
    a: 1
    b: 2
```

`config.d/10-example.yaml`:

```yaml
Child:
  Ys:
    c: 99
```

The loaded model equates to the following:

```yaml
A: Value A
B: Default B
Child:
  X: 1
  Ys:
    a: 1
    b: 20
    x: 99
```

Includes only work on model classes derived from `ConfigModelBase`.

Includes can be used on every level of the model tree.
Nested includes are supported.
Meaning, an included YAML files can use includes themselves.
