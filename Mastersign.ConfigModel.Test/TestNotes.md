# Test Notes

## Dimensions

- [X] Property Naming Convention
- [ ] String Sourcing with `$sources`
    + [X] Simple string sourcing
    + [X] Explicit properties have precedence over file sources
    + [X] Sourcing in children
    + [ ] Sourcing in includes
- [X] Type Discriminator
    + [X] Property Existence
    + [X] Property Value
- [X] Collection Merge
    + [X] List Merge Clear
    + [X] List Merge Append
    + [X] List Merge Prepend
    + [X] List Merge Item Replace
    + [X] List Merge Item Merge
    + [X] Dictionary Merge Clear
    + [X] Dictionary Merge Item Replace
    + [X] Dictionary Merge Item Merge
- [X] Root Layer Combination
    + [X] Attribute
    + [X] Interface
    + [X] POCO Replacement
    + [X] Layer Source Filenames
- [ ] Layer Combination from `$includes`
    + [X] Without Globbing
    + [X] With Globbing
    + [ ] Layer Source Filenames
    + [X] Cycle Detection
- [ ] Exceptions
    + [ ] If include does not exist
    + [ ] If include can not be read
    + [ ] If include can not be parsed
    + [ ] If include can not be deserialized
    + [X] If includes build a cycle
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
