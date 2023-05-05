# Merge Keys

[Merge keys](https://ktomk.github.io/writing/yaml-anchor-alias-and-merge-key.html) —
even thou they are currently not specified well —
are supported in many YAML parser implementations.
And _YamlDotNet_ is no exception. That is why _Mastersign.ConfigModel_ can support them too.
And they have proven to be very helpful in removing repetition from config files,
and in turn making them more [DRY](https://en.wikipedia.org/wiki/Don%27t_repeat_yourself).

Support for merge keys is currently optional and needs to be activated
with a parameter in the @Mastersign.ConfigModel.ConfigModelManager`1 constructor.

## Example

Consider the following model:

```cs
using Mastersign.ConfigModel;

class ProjectModel
{
    public string? Caption { get; set; }

    public Dictionary<string, DataModel>? Data { get; set; }
}

class DataModel
{
    public int? X { get; set; }
    public int? Y { get; set; }
    public int? Z { get; set; }
}
```

Without merge keys a config file could look like this:

```yaml
Caption: No Merge Keys Yet
Data:
  a:
    X: 1
    Y: 1
    Z: 1
  b:
    X: 1
    Y: 1
    Z: 2
  c:
    X: 1
    Y: 1
    Z: 3
```

To reduce the repetition in the file merge keys can be used.
At first, one or more maps need to be marked with an anchor:

```yaml
...
Data:
  a: &default-data
    X: 1
    Y: 1
    Z: 1
...
```

Then an alias can be used in an merge key `<<:` to paste an anchored map into the current map:

```yaml
...
Data:
  ...
  b:
    <<: *default-data
    Z: 2
```

Leading to this "dried" config file:

```yaml
Caption: Merge Key Demo
Data:
  a: &default-data
    X: 1
    Y: 1
    Z: 1
  b:
    <<: *default-data
    Z: 2
  c:
    <<: *default-data
    Z: 3
```

**Important:** To load a model with merge keys, you need to pass `true` for the `withMergeKeys`
parameter in the constructor of @Mastersign.ConfigModel.ConfigModelManager`1.

If you want to have multiple maps as reusable templates/presets in your file,
you need to prepare a place for them in the model;
otherwise the maps are not deserialized and can therefore not be found,
when referring to them with an alias.

```cs
class ProjectModel
{
    public string? Caption { get; set; }

    [NoMerge]
    public Dictionary<string, DataModel>? Presets { get; set; }

    public Dictionary<string, DataModel>? Data { get; set; }
}
```

Now you can refer to them throughout your config file.
You can even merge multiple aliases into one target map.

```yaml
Caption: Merge Key Demo
Presets:
  ones: &default-data
    X: 1
    Y: 1
    Z: 1
  layer2: &layer-2
    Y: 2
Data:
  a:
    <<: *default-data
  b:
    <<: [*default-data, *layer-2]
    Z: 2
  c:
    <<:
      - *default-data
      - *layer-2
    Z: 3
```

The order of normal keys and merge keys is important here.
If you first specify normal keys and place a merge key behind them,
the maps referenced in the merge key will overwrite the values of the normal keys.
Consider the following config file:

```yaml
Caption: Importance Of Order
Presets:
  preset: &preset
    X: 99
Data:
  a:
    X: 100
    <<: *preset
```

The value of `Data["a"].X` will be `99` here.

## Limitations

Aliases can not refer to anchors in another file.
Therefore, if [includes](includes.md) are used, aliases can not refer to anchors in included files.
Consequently, merge keys can only be used in the context of one file.

Values with an anchor need to be represented in the model to be available
for alias look-up during deserialization.
Meaning, you can not use a non-existent property to store an anchored value,
with the intention to refer to it with an alias later in the file.
The property, holding the anchored value must be present in the model.
