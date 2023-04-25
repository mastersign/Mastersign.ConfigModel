# Naming Convention

The property naming convention dictates the writing of the model properties in the YAML files.
The naming convention applies to all model properties, but not to dictionary keys.

The following conventions are supported directly.

* `PropertyNameHandling.PascalCase` (_Default_)  
  Example: `ConfigModelManager`
* `PropertyNameHandling.CamelCase`  
  Example: `configModelManager`
* `PropertyNameHandling.LowerCase`  
  Example: `configmodelmanager`
* `PropertyNameHandling.Hyphenated`  
  Example: `config-model-manager`
* `PropertyNameHandling.Underscored`  
  Example: `config_model_manager`

You can specify the naming convention, when creating the `ConfigModelManager`:

```cs
var manager = new ConfigModelManager<RootModel>(
    propertyNameHandling: PropertyNameHandling.CamelCase);
```

Using an alternative constructor for `ConfigModelManager`,
a custom implementation of `YamlDotNet.Code.INamingConvention` can be provided.
