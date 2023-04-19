# Test Notes

## Dimensions

- [X] Property Naming Convention
- [ ] String Sourcing with `$sources`
    + [X] Simple string sourcing
    + [X] Explicit properties have precedence over file sources
    + [ ] Sourcing in children
    + [ ] Sourcing in includes
- [ ] Type Discriminator
    + [ ] Property Existence
    + [ ] Property Value
- [ ] Root Layer Combination
    + [ ] List Merge Append
    + [ ] List Merge Prepend
    + [ ] List Merge Item Replace
    + [ ] List Merge Item Merge
    + [ ] Dictionary Item Replace
    + [ ] Dictionary Item Merge
    + [ ] Layer Source Filenames
- [ ] Layer Combination from `$includes`
    + [ ] Without Globbing
    + [ ] With Globbing
    + [ ] Layer Source Filenames
    + [ ] Cycle Detection
- [ ] Non-Mergablility
    + [ ] POCO
    + [ ] `ConfigModelBase`
- [ ] Mergability with Attribute
    + [ ] POCO
    + [ ] `ConfigModelBase`
- [ ] Mergability with Interface
    + [ ] POCO
    + [ ] `ConfigModelBase`
- [ ] Exceptions
    + [ ] If include does not exist
    + [ ] If include can not be read
    + [ ] If include can not be parsed
    + [ ] If include can not be deserialized
    + [ ] If string source does not exist
    + [ ] If string source can not be read
- [ ] Change Trigger
    + [ ] Without delay
    + [ ] With delay
    + [ ] With locked file
    + [ ] On root
    + [ ] On string source
    + [ ] On include
    + [ ] On nested include
