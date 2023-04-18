# Test Notes

## Dimensions

- [ ] Property Name Casing
- [ ] String Sourcing with `$sources`
- [ ] Type Descriminator
	+ [ ] Property Existence
	+ [ ] Property Value
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
