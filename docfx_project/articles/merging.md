# Merging

Every time layers or includes are merged, a couple of rules apply.
A merge is performed from a source to a target.
When merging layers, the layer with higher precedence is the source
and the layer with lower precedence is the target.

1. If an object implements the `IList<T>` interface,
   it is merged with a _list merge mode_.
   The default merge mode is _clear_.
   The merge mode is defined with a `MergeListAttribute`
   on the property holding the list.
2. If an object implements the `IDictionary<TKey, TValue>` interface
   with the type parameter `TKey` set to `string`,
   it is merges with a _dictionary merge mode_.
   The default merge mode is _replace item_.
   The merge mode is defined with a `MergeDictionaryAttribute`
   on the property holding the dictionary.
3. An object is merged automatically if its class is annotated with
   the `MergableConfigModelAttribute` attribute.
   The merge is done by iterating over all readable and writeable public properties,
   and merging the values from the source with the values on the target.
4. An object is merged by calling the `UpdateWith()` method,
   if its class implements the `IMergableConfigModel` interface.
5. If none of the rules above apply, the object is merged by
   replacing the target with the source as a unit.

## List Merging

The following list merge modes are supported:

* `ListMergeMode.Clear` (_Default_)  
  Clear the target list and add all items from the source to the target.
* `ListMergeMode.ReplaceItem`  
  Iterate over the indices of the source list.
  Replace every item in the target list with the item from the source list.
  If the source list holds more items then the target, add them to the end.
* `ListMergeMode.MergeItem`  
  Iterate over the indices of the source list.
  Merge every item in the target list with the item from the source list.
  If the source list holds more items then the target, add them to the end.
* `ListMergeMode.Append`  
  Add all items from the source list to the end of the target list.
* `ListMergeMode.Prepend`  
  Insert all items from the source list at the beginning of the target list.
  Keep the order from the source list.
* `ListMergeMode.AppendDistinct`  
  The same as `Append`, but skip items from the source list,
  if an equal item already exists in the target list.
* `ListMergeMode.PrependDistinct`  
  The same as `Prepend`, but skip items from the source list,
  if an equal item already exists in the target list.

The following example demonstrates merging a list with the merge mode `MergeItem`:

```cs
[MergableConfigModel]
class RootModel
{
    [MergeList(ListMergeMode.MergeItem)]
    public IList<ChildModel> Children { get; set; }
}

[MergableConfigModel]
class ChildModel
{
    public int? X { get; set; }
    public int? Y { get; set; }
}
```

Using the following two layers

`layer1.yaml`:

```yaml
Children:
  - X: 11
    Y: 12
  - X: 21
    Y: 22
```

`layer2.yaml`:

```yaml
Children:
  - X: 31
  - Y: 42
  - X: 51
    Y: 52
```

The result is:

```yaml
Children:
  - X: 31
    Y: 12
  - X: 21
    Y: 42
  - X: 51
    Y: 52
```

## Dictionary Merging

The following dictionary merge modes are supported:

* `DictionaryMergeMode.Clear`  
  Clear the target dictionary and add all items from the source to the target.
* `DictionaryMergeMode.ReplaceItem`  (_Default_)  
  Iterate over the keys of the source dictionary.
  Replace every item in the target dictionary having the same key
  with the item from the source dictionary.
  If the source key does not exist in the target dictionary, add the item.
* `DictionaryMergeMode.MergeItem`  
  Iterate over the keys of the source list.
  Merge every item in the target dictionary having the same key
  with the item from the source dictionary.
  If the source key does not exist in the target dictionary, add the item.

The following example demonstrates merging a dictionary with the merge mode `MergeItem`:

```cs
[MergableConfigModel]
class RootModel
{
    [MergeList(DictionaryMergeMode.MergeItem)]
    public IDictionary<string, ChildModel> Children { get; set; }
}

[MergableConfigModel]
class ChildModel
{
    public int? X { get; set; }
    public int? Y { get; set; }
}
```

Using the following two layers

`layer1.yaml`:

```yaml
Children:
  a:
    X: 11
    Y: 12
  b:
    X: 21
    Y: 22
```

`layer2.yaml`:

```yaml
Children:
  a:
    X: 31
  b:
    Y: 42
  c:
    X: 51
    Y: 52
```

the result is:

```yaml
Children:
  a:
    X: 31
    Y: 12
  b:
    X: 21
    Y: 42
  c:
    X: 51
    Y: 52
```
