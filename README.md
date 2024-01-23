# Gaspar

Gaspar is a tool that consumes your C# domain models, types and controllers and exports them for other services and languages to consume.  For example, make your C# models available in TypeScript and your controller endpoints available in TypeScript and other C# services, removing the need to use boilerplate code and hardcoded variable names, urls, etc.

It is a C# port and enhancement of [Jonathan Svenheden's C# models to TypeScript project](https://github.com/svenheden/csharp-models-to-typescript) (written in C# and JavaScript).  It makes use of the [Roslyn (the .NET compiler platform)](https://github.com/dotnet/roslyn) to parse the source files, which removes the need to create and maintain our own parser and makes the whole process very fast.

## Supported Translations

|                         | C# Models and Types | C# Controllers |
| ----------------------- |:-------------------:|:--------------:|
| Export to TypeScript    | ✅                   | ✅              |
| Export to Angular       | ✅ *                 | ✅              |
| Export to Ocelot Config |                     | ✅              |
| Python                  |                     | ✅              |
| Export to C#            |                     | ✅              |
| Export to Proto         | ✅                   |                |

**Same as TypeScript export*

Other translations can easily be added

## Install

Gaspar is written using .NET 7 and is available on NuGet.

To install, search "WckdRzr.Gaspar" in your NuGet package manager, or visit the NuGet page: <https://www.nuget.org/packages/WckdRzr.Gaspar/>

### Older versions

The latest version of Gaspar requires .net7.0, to use with an older project you will need to use an older version of Gaspar (which may have unresolved bugs):

- For .net6.0 use version 2.6.3

- For .net5.0 use version 2.3.2

## How to use

After including the NuGet package, follow these steps:

1. Copy the file `gaspar.demo-config.json` from the root of this repo, to the root of your project and rename it `gaspar.config.json`.

2. Decorate the controllers and models you would like to export with `[ExportFor(GasparType.All)]` (and include the namespace `WCKDRZR.Gaspar`).

3. Build.
   If you didn't change the demo config, four files will be added to the root of your project:
   
   - `api.d.ts` - the TypeScript Models.
   - `My_service.cs` - The C# Service Communication interface.
   - `my_service.ts` - The Angular Service Communication interface.
   - `ocelot.my_service.json` The Ocelot config file for Service Communication.

That's all; play with the config and see what you can do...

## [ExportFor(*type*)] Attribute

In order for your models to be exported, you need to add the `[ExportFor(...)]` decorator to the classes, properties and controller actions you want to export.  For controllers, you can add to the controller class.  Properties and Actions within a decorated class can also be decorated to supplement the parent class attribute.

The attribute has one required parameter `GasparType types`. This is the type of export you want, or a group of exports you want.  Use bitwise operators for multiple types, for example:

- `[ExportFor(GasparType.All)]` to export to all known types.

- `[ExportFor(GasparType.TypeScript)]` to export to only TypeScript.

- `[ExportFor(GasparType.TypeScript | GasparType.CSharp)]` to export to TypeScript and CSharp.

- `[ExportFor(GasparType.FrontEnd)]` to export to TypeScript, Angular and Ocelot.

- `[ExportFor(GasparType.All | ~Gaspar.Ocelot)]` to export to all types, except Ocelot.

### Notes

- Feel free to use `&` or `|` to join types; they will have the same effect.

- The bitwize NOT operator `~` will remove the type; particularly useful as above, or when you want to override the parent attribute on the controller class

- Exports will only work if they are configured to output in `gaspar.config.json`

- Exports will only work for `public` objects.

### Export Everything

 If you want to export everything without adding the `[ExportFor]` decorator, you can add `"IgnoreAnnotations": true` to the root of the config file.

Gaspar will still only export `public` objects for configured types.

### Optional Parameters

These parameters are avaliable on the `[ExportFor]` attribute.  Note, if you are adding ExportFor only to selected class properties or methods, but would add the same options to each, you can use the `[ExportOptions]` attribute instead; which has the same parameters available.  This also works the other way round (ExportFor on the class and options on selected methods)

**NoInheritance**    `bool`    When present on C# models (and set to true), the inherited base classes will not be included in the export.

You can also use the `[ExportWithoutInheritance]` attribute if more convenient.

**Serializer**    `string`    For C# Service Communcations, if the JSON returned by the decorated action won't deserialize through the Newtonsoft serializer, add your custom serialize class here, e.g.

- `[ExportFor(GasparType.CSharp, Serializer = nameof(MySerializer)]`

The class provided must implement a generic `Deserialize<T>` method that returns an object of type `T`.

*make sure to include the namespace to your serializer in the config (see below)*

**ReturnTypeOverride**    `string`    Allows you to override the return type name that is generated.  Particularly useful if your return type is obscured (e.g. in `ContentResult`). Use as follows:

- `[ExportFor(GasparType.Angular, ReturnTypeOverride = "MyType"]`

**Scopes**    `Array of strings`    For Ocelot - list of scopes to be used in "AllowedScopes" of the "AuthenticationOptions" section in the Ocelot config.  If set this will override scopes generated from the `DefaultScopes` and `ScopesByHttpMethod` configuration.

 **AdditionalScopes**    `Array of strings`    For Ocelot - scopes to be added to "AllowedScopes" of the "AuthenticationOptions" section in the Ocelot config. If set these scopes will be in addition to scopes generated from the `DefaultScopes` and `ScopesByHttpMethod` configuration.

## Disable Export on Build

Gaspar will export files on build.  If you would like to disable this add the following to the `PropertyGroup` of you csproj file

- `<RunGaspar>False</RunGaspar>`

This might be useful to temporarily disable the feature, or if you need to use the built in C# features (i.e. in a dependent project) but don't want the export.

You can also use this on the command line: `dotnet build /p:RunGaspar=False`

## Well Written Controller Actions

Gaspar will export all your controller actions, but they must be written in a way that Gaspar can understand.  This will improve your overall code quality, not just meet arbitrary rules.  If Gaspar can't understand the action, it may export a skeleton method marked 'depreciated' or 'obsolete' with a friendly error message.

Improve your actions as follows:

- Controller classes must be derived a class with `Controller` in the name (e.g. `Controller`, `ControllerBase`, `MyBaseController`, etc...)

- Actions should be decorated with an http method and route, e.g. `[HttpPost("[controller]/[action]")]`

Other recommendations (not required, but good practice):

- Have you controller actions return `ActionResult<T>`. This will provide a strongly-typed interface in the service communication endpoints.
  
  - If you return `ContentResult` or `JsonResult`, these will be translated to `string` and `object` respectively.

- Avoid returning `Ok()` from your actions as it will mask type errors; just return the object of the correct type; or a problem ActionResult (e.g. `NotFound()`, `Problem()`, etc...)

- Async actions are not recommended as you'll receive a Task JSON object back.  REST endpoint are synchronous by definition, so async doesn’t make sense.

## Service Communication Response

When using the exported service communication endpoints, you will receive a `ServiceResponse` object containing two objects: `Data` and `Error`; one of these will always be null or undefined, the other will be populated.

**Data** is typed to the return type of the action, (directly or in the ActionResult<>)

**Error** is an `ActionResultError` object that contains all the data returned when using an ActionResult (e.g. `NotFound()` or `Problem()`).  If the error isn't from the Action (e.g. an error in the serializer or the endpoint wasn't reachable) the ActionResultError will be populated appropriately.

*if your action returns `void`, the return object will not include Data*

### Convenience methods

ServiceResponse has the following convenience methods that you can use:

- **Success**    `bool`    True if there was no error

- **HasError**    `bool`    True if there was an error

### Usage Examples

Lets take this controller action, exported for all environments:

```csharp
using Microsoft.AspNetCore.Mvc;
using WCKDRZR.Gaspar;

namespace WCKDRZR.DataWatchdog.DNA.API.Controllers
{
    [ApiController]
    [ExportFor(GasparType.All)]
    public class MyController : ControllerBase
    {
        [HttpPost("[controller]/[action]/{id}")]
        public ActionResult<bool> MyAction([FromBody] MyObj obj, int id)
        {
            // action code...
            return true;
        }
    }
}
```

#### To use in TypeScript

```typescript
import { Service } from 'src/app/interfaces/services'; //Service will be prefixed with ServiceName from config

export class MyTypeScriptPage {

    service = new Service.MyController();

    constructor() {

        requestId = 1;
        requestObj = {};

        this.service.myAction(requestObj, requestId).then(response => {
            if (response.data) {
                //use the data
            } else {
                // handle response.error if appropriate
            }
        });

        //if you have a custom error handler definded in config, you could also:
        //with: import { ServiceErrorMessage } from 'src/app/interfaces/service-helper';
        //this.service.myAction(requestId, requestObj, ServiceErrorMessage.ServerResponse).then...
    }
}
```

#### To use in Angular

```typescript
import { Service } from 'src/app/interfaces/services'; //Service will be prefixed with ServiceName from config

export class MyAngularPage {

    constructor(private service: Service.MyController) {

        requestId = 1;
        requestObj = {};

        this.service.myAction(requestObj, requestId).subscribe(response => {
            if (response.data) {
                //use the data
            } else {
                // handle response.error if appropriate
            }
        });

        //if you have a custom error handler definded in config, you could also:
        //with: import { ServiceErrorMessage } from 'src/app/interfaces/service-helper';
        //this.service.myAction(requestId, requestObj, ServiceErrorMessage.ServerResponse).sub...
    }
}
```

#### To use in C#

```csharp
using WCKDRZR.Gaspar.Models;
using WCKDRZR.Gaspar.ServiceCommunciation.Service; //Service will be prefixed with ServiceName from config

namespace MyProject
{
    class MyClass
    {
        int requestId = 1;
        MyObj requestObj = new();

        //MyController becomes MyService; MyAction method name is intact
        ServiceResponse<bool?> response = MyService.MyAction(requestObj, requestId);

        //or call async:
        ServiceResponse<bool?> response = await MyService.MyActionAsync(requestObj, requestId);

        if (response.Data != null)
        {
            // use the data    
        }
        else
        {
            // handle response.Error if appropriate
        }
    }
}
```

#### To use in Python

```python
# example to follow
```

## Configuration

The demo config provided only includes the basics to make Gaspar work; here is a full list of the options available (feel free to have a look in the `Models/Configuration.cs` file):

### At the root level

You must supply either Models or Controllers, but you don't need both; all other parameters are optional.

**Models**    `ConfigurationType`    Options for outputting Models and Enums

**Controllers**    `ConfigurationType`    Options for outputting service communication contracts for Controllers

**CustomTypeTranslations**    `Array of string pairs: [{"X": "Y"}, {"A": "B"}]`    For TypeScript and Angular, you can override type names to objects TypeScript will understand.  e.g. `{ "IMyInterface": "Object" }`

**IgnoreMissingOutputLocations**    `bool: default false`    If the file output location cannot be found, you will get a build error; add `"IgnoreMissingOutputLocations": true` to skip the error.  This is useful when you need to build in an environment where the output may not be available (e.g. docker); although it would usually be preferable to use `dotnet build /p:RunGaspar=False` in your scripts.

**IgnoreAnnotations**    `bool: default false`    Set this to true to export all objects irrespective if they have `[ExportFor]` or not.

### ConfigurationType

**Include**    `Array of strings: default ["./**/*.cs"]`    List of file locations containing the models or controllers you wish to translate.  Optional, if not provided will use all .cs files in the project.

**Exclude**    `Array of strings: default []`    List of file locations to exclude from the Include list

**Output**    `Array of ConfigurationTypeOutput`    Output options.  You must supply at least one of these.

For Models:

- **UseEnumValue**    `bool: default true`    Enums will be written with thier value, either set directly or from an Atrribute (e.g. `[EnumMember(Value = "name")]`).

- **StringLiteralTypesInsteadOfEnums**    `bool: default false`    For TypeScript, if true will export enums as types instead of TS enums.

For Controllers:

- **ServiceName**    `string`    Required.  Used to name exported items, can also be used in paths.

- **ServiceHost**    `string`    Optional.  Used by Ocelot Export, and can be used in paths.  Typically set to 'http' or 'https'

- **ServicePort**    `int`    Optional.  Used by Ocelot Export, and can be used in paths.

### ConfigurationTypeOutput

**Type**    `specific string`    Required.  Must be one of: `"CSharp"`, `"Angular"`, `"Ocelot"` or `"TypeScript"`

**Location**    `string`    Required.  The location to output the translated file to (relative to the project root).  For controllers, can include `{ServiceName}`, `{ServiceHost}` or `{ServicePort}` to have those placeholders replaced (see the demo file).

For controllers (optional):

**UrlPrefix**    `string`    When building the service contract, you can prefix the url with this value; e.g. `"http://myservice.com:81"`. The service url will be built from this followed by `/` then the action route.  Can include `{ServiceName}`, `{ServiceHost}` or `{ServicePort}` to have those placeholders replaced (see the demo file).

For CSharp controllers (all optional):

- **UrlHandlerFunction**    `string`    If you would like to run your url through a function to ensure it is correct, you can provide the function name here.  Your code will need to provide a static string extension method; as follows:
  
  ```csharp
  internal static class StringExtensions
  {
      public static string MyUrlHelper(this string s)
      {
          //manipulate s...
          return s;
      }
  }
  ```
  
  In the above example, you will add `"UrlHandlerFunction": "MyUrlHelper"` to the config.
  *make sure to include the namespace to your function in the config (see below)*

- **LoggingReceiver**    `string`    When using your exported service communication endpoint, if there's an error receiving the response or deserializing it, the error will be logged to the console (using `Console.WriteLine`).  If you would like the error to be absorbed by your own logging system you can provide a static logging class name here, for example:
  
  ```csharp
  internal static class MyLogger
  {
      public static void GasparError(string message)
      {
          //handle error
      }
  }
  ```
  
  In the above example, you will add `"LoggingReceiver": "MyLogger"` to the config.  Alternatively you can add a `GasparError()` method (as above) to your existing logging class (provided it's a static class).
  
  If your logging class throws an error, that error will be written to the console, followed by the original error.
  
  *make sure to include the namespace to your class in the config (see below)*

- **ModelNamespaces**    `Array of strings`    List of namespaces to include at the top of your exported Service Communication class.  This would inculde the namespaces to custom types in the export, or your serializer and logging tools

For TypeScript and Angular models and controllers (all optional)

- **AddInferredNullables**    `boolean`    Property types that are explicitly nullable (e.g. `int?`) will always be outputed with `null` types (e.g. `number | null`).  Set this property to true to add null types to C# types that could be null if unset.
  
  In short, if your C# project doesn't enable "nullable annotation context", set this to true.

- **NullablesAlsoUndefinded**    `boolean`    If set to true, all nullable properties (those exported with `| null`) will additional have `| undefined` added to the type.

For TypeScript and Angular controllers (all optional):

- **HelperFile**    `string`    The service communication export requires some extra code to handle the boilerplate requests.  This is the name of the file that should be exported.  If omitted, the code will be included at the top of the exported service communications file, which may cause issues if you're exporting from multiple projects.

- **ModelPath**    `string`    Path to a file containing definitions for any custom types used in your service communications (excluding the extension, as is usual for TypeScript includes).  Ideally, this is the file exported by the model export part of this application.

- **ErrorHandlerPath**    `string`    If an error is received from the requested endpoint, it will be absorbed (although it will always be seen in the browser console).  The response will include the error details if you want to handle it from the calling class, but if you always want to show a message to the user (e.g. using a SnackBar), you can provide an error handler, as below:
  
  In TypeScript:
  
  ```typescript
  export class ServiceErrorHandler {
      showError(message: string | null): void {
          message = message ?? 'An unknown error occurred');
          //show error to user
      }
  }
  ```
  
  In Angular:
  
  ```typescript
  import { Injectable } from "@angular/core";
  
  @Injectable({ providedIn: 'root' })
  export class ServiceErrorHandler {
  {
      showError(message: string | null): void {
          message = message ?? "An unknown error has occurred";
          //show error to user
      }
  }
  ```
  
  The class and function names must be as above.  In the config provide the path to the above file (without extension) e.g. `"ErrorHandlerPath": "./service-error-handler"`

- **DefaultErrorMessage**    `specific string`    When using the ErrorHandler (above), this controls what the message parameter contains; there are three possible values:
  
  - `None` will not call the error handler
  - `ServerResponse` will pass the message from the server to the handler
  - `Generic` will pass `null` to the handler.  You can then show a generic message of your choice (as demonstrated above).
  
  All the endpoints created in the service communication export provide an additional `showError` parameter that allows you to override this default directly in the calling class.  It is likely that you will set this to `Generic`, and then for particular routes and pages you can override to `None` or `ServerResponse` as required.

- **Imports**    `Dictionary<string, string>`    List of imports to include at the top of your exported Service Communication class. Will be written in the form
  
  ```typescript
  import { key } from "value";
  ```

For Python controllers (all optional):

- **HelperFile**    `string`    The service communication export requires some extra code to handle the boilerplate requests. This is the name of the file that should be exported. If omitted, the code will be included at the top of the exported service communications file, which may cause issues if you're exporting from multiple projects.

- **Imports**    `Dictionary<string, string>`    List of imports to include at the top of your exported Service Communication class.  Will be written in the form
  
  ```python
  import key from value
  ```

For Ocelot controllers (all optional):

- **DefaultScopes**    `string[]`    If specified, the scopes listed here will be outputed into the "AllowedScopes" section of "AuthenticationOptions" for all routes.  Scopes can include `{ServiceName}`, `{ServiceHost}` or `{ServicePort}` to have those placeholders replaced (see the demo file).

- **ScopesByHttpMethod**    `Dictionary<string, string[]>`    Dictionary keyed on scope names.  Each element can contain an array of Http Methods; if the route matches a listed method, the key will be added to the "AllowedScopes" section of "AuthenticationOptions".  For example:
  
  ```json
  "ScopesByHttpMethod": {
      "service.write": [ "POST", "PUT", "GET" ],
      "service.read": [ "GET" ]
  }
  ```
  
  Dictionary keys can include `{ServiceName}`, `{ServiceHost}` or `{ServicePort}` to have those placeholders replaced (see the demo file).

- **NoAuth**    `bool`    If true, the "AuthenticationOptions" section of the Ocelot config will not be outputted.

- **ExcludeScopes**    `bool`    If true, the "AllowedScopes" section of the Ocelot config will not be outputted.

For Proto Models (required)

- **PackageNamespace**    `string`    Namespace to be used when generating .proto file outputs. For example, the PackageNamespace value of 'com.wckdzr' produces the following header for .proto files:
  
  ```protobuf
  syntax = "proto3";
  package com.wckdrzr;
  // ... proto definitions
  ```

## C# Extension Methods

Provided for convenience when using Gaspar tagged data:

```csharp
bool ExportsFor(this TypeInfo member, GasparType type, bool includeParent = true, bool anyChildrenMatch = false)
bool ExportsFor(this Type member, GasparType type, bool includeParent = true, bool anyChildrenMatch = false)
bool ExportsFor(this MemberInfo member, GasparType type, bool includeParent = true, bool anyChildrenMatch = false)
```

These methods will let you know if it the object (TypeInfo, System.Type or MemberInfo) is tagged with a given GasparType.

For example `myclass.ExportsFor(GasparType.FrontEnd)` will return true if the myclass has the `[ExportsFor(GasparType.FrontEnd)]` attribute

Set `includeParent` to false to only look at the passed object, and not it's parent

Set `anyChildrenMatch` to true to return true if any of the child members have the ExportFor attribute.

## Issues and Contributions

Please raise any issues or pull requests in GitHub. We will try and incorporate any changes as promptly as possible.

© [Wckd Rzr](https://github.com/wckdrzr)
