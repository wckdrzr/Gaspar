# Gaspar

If you build APIs in C# and consume them elsewhere, Gaspar is your essential companion!

You C# code is the single point of truth; it is where the functionally actually is and describes the format and model of your API; Gaspar will share that information with your client services automatically at build time.  Everything stays up to date, and if things change clients will fail to compile; faster break-fix-build.

Gaspar consumes your C# domain models, enums, types and controllers and exports them for other services and languages to consume. 

- No more boilerplate code for communicating with APIs

- No more hardcoded URL strings

- No more misspelt, or misplaced variables.

- JsonPropertyKeys converted; it just works!

Using the [Roslyn (the .NET compiler platform)](https://github.com/dotnet/roslyn) to parse the source files, Gaspar quickly and reliably builds all the output files you need.

## Supported Translations

|                                  | C# Models and Types | C# Controllers |
| -------------------------------- |:-------------------:|:--------------:|
| Export to TypeScript             | ✅                   | ✅              |
| Export to Angular * <sup>1</sup> | ✅                   | ✅              |
| Export to Swift                  | ✅                   |                |
| Export to Kotlin                 | ✅                   |                |
| Export to C#                     |                     | ✅              |
| Export to Ocelot Config          |                     | ✅              |
| Python <sup>† 1</sup>            |                     | ✅              |
| Export to Proto <sup>†</sup>     | ✅                   |                |

*\* Angular model export same as TypeScript export*\
*<sup>† </sup>Not actively maintained (please contribute!)*\
*<sup>1</sup> Controllers missing JsonPropertyKey support (please contribute!)*\

Other translations can easily be added

## Install

Gaspar is written using .NET 7 and is available on NuGet.

To install, search "WckdRzr.Gaspar" in your NuGet package manager, or visit the NuGet page: <https://www.nuget.org/packages/WckdRzr.Gaspar/>

## Version 3 Breaking Changes

Version 3 breaks some existing functionally; if you're upgrading consider the following:

- `CustomTypeTranslations` has been renamed to `GlobalTypeTranslations`.  The functionally remains the same.  We have done this because these translations are applied to all output languages and wanted to make that clearer.  A new `TypeTranslations` config property has been added to allow translations at an individual language level - this is probably what you want most of the time!

- Export type `GasparType.FrontEnd` has been removed.  As part of adding Swift and Kotlin, a `GasparType.App` seemed appropriate.  However hard coding these groups seemed wrong, so you can now build your own groups as you like; unfortunately this meant that `.FrontEnd` needed to be removed!
  
  `GasparType.All` is a special case and will always be available.
  
  See `GroupTypes` below; but if you want to recreate the FrontEnd group exactly as was; add the following to the root of your config:
  
  ```json
  "GroupTypes": {
      "FrontEnd": [ "TypeScript", "Angular", "Ocelot" ]
  }
  ```
  
  Then add this static class anywhere in your code to allow use of a strongly typed property (you will also need to rename instances of `GasparType.FrontEnd` to `GasparGroupType.FrontEnd` (the actual class name isn't important):
  
  ```csharp
  public static class GasparGroupType
  {
      public const int FrontEnd = GasparType.TypeScript | GasparType.Angular | GasparType.Ocelot;
  }
  ```
  
  *If you don't want to add the class above, you can now use the type name as a string instead; i.e. `[ExportFor("FrontEnd")]`*

- The `ExportsFor()` function (which is now more reliable) no longer has `includeParent` or `anyChildrenMatch` parameters.  IncludeParent (which defaulted to true) made little sense.  Without the parentage you cannot to correctly see if an object would be exported or not; so this is always on now.  If you were using `anyChildrenMatch`, this has been moved a separate function to avoid confusion.  Use `AnyChildExportsFor()` instead.

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

The attribute has one required parameter `int types`. This is the type of export you want, or a selection of exports you want.  Use bitwise operators for multiple types, for example:

- `[ExportFor(GasparType.All)]` to export to all known types.

- `[ExportFor(GasparType.TypeScript)]` to export to only TypeScript.

- `[ExportFor(GasparType.TypeScript | GasparType.CSharp)]` to export to TypeScript and CSharp.

- `[ExportFor(GasparType.All | ~Gaspar.Ocelot)]` to export to all types, except Ocelot.

The `types` parameter is an `int`, but you should use one of the properties in the `GasparType` static class provided.  Using an int value will be unclear, and it will not work.  It is setup like this (rather than an enum) to allow the options to be extended - see `GroupTypes` below.

There is also an overload attribute that accepts a string should you want to use it like that, for example: `[ExportFor("TypeScript")]`.  This is not recommend, but some may find it useful.

### Notes

- Use `|` to join/add types.  Using `&` may work but is unpredictable.

- The bitwize NOT operator `~` will remove the type; particularly useful as above, or when you want to override the parent attribute on the class

- Exports will only work if they are configured to output in `gaspar.config.json`

- Exports will only work for `public` objects.

### Export Everything

 If you want to export everything without adding the `[ExportFor]` decorator, you can add `"IgnoreAnnotations": true` to the root of the config file.

Gaspar will still only export `public` objects for configured types.

### Optional Parameters

These parameters are available on the `[ExportFor]` attribute.  Note, if you are adding ExportFor only to selected class properties or methods, but would add the same options to each, you can use the `[ExportOptions]` attribute instead; which has the same parameters available.  This also works the other way round (ExportFor on the class and options on selected methods)

**NoInheritance**    `bool`    When present on C# models (and set to true), the inherited base classes will not be included in the export.

You can also use the `[ExportWithoutInheritance]` attribute if more convenient.

**Serializer**    `string`    For C# Service Communications, if the JSON returned by the decorated action won't deserialize through the Newtonsoft serializer, add your custom serialize class here, e.g.

- `[ExportFor(GasparType.CSharp, Serializer = nameof(MySerializer)]`

The class provided must implement a generic `Deserialize<T>` method that returns an object of type `T`.

*make sure to include the namespace to your serializer in the config (see below)*

**ReturnTypeOverride**    `string`    Allows you to override the return type name that is generated.  Particularly useful if your return type is obscured (e.g. in `ContentResult`). Use as follows:

- `[ExportFor(GasparType.Angular, ReturnTypeOverride = "MyType"]`

**Headers**    `Array of strings`    For controller actions, this allows you to specify headers that will be sent with the request.  You can already use `[FromHeader]`, but this can report missing headers back to the user.  The `Headers` options keeps header properties hidden from users calling the endpoint incorrectly; whist allowing you to see and specify the values from your calling code.

Specifying an empty array will add a `Dictionary<string, string>` property (or equivalent) to allow any number of name/values pairs, defined when calling.

Specify a list of strings for these to be required as header name properties, for which you will add string values.

**Timeout**    `long (milliseconds)`    For C# Service Communications this provides a default timeout value for calls made to this controller/action.  This can easily be overridden on any individual call.

For Ocelot, this allows you to provide a QoS Timeout value in the configuration.

**Scopes**    `Array of strings`    For Ocelot - list of scopes to be used in "AllowedScopes" of the "AuthenticationOptions" section in the Ocelot config.  If set this will override scopes generated from the `DefaultScopes` and `ScopesByHttpMethod` configuration.

 **AdditionalScopes**    `Array of strings`    For Ocelot - scopes to be added to "AllowedScopes" of the "AuthenticationOptions" section in the Ocelot config. If set these scopes will be in addition to scopes generated from the `DefaultScopes` and `ScopesByHttpMethod` configuration.

### [From*xxxx*]

Within your controller actions, you can decorate parameters with [FromBody], [FromForm], [FromHeader], [FromRoute], [FromQuery], [FromServices] or [FromKeyedServices] and they will work exactly as you expect; the later two options will not export as they are for internal data.

#### [FromFormObject]

Natively in C#, binding of complex objects from a JSON payload is only supported using [FromBody], however you can only specify one [FromBody] argument in any given function.

If you would like to send a file along with a complex object, or simply send multiple complex objects; using [FromForm] makes sense.  However [FromForm] only works with HTMLInputElement values; namely simple types (string, int, etc) and IFormFile.

In order to make this work seamlessly when using Gaspar, you can decorate complex objects with [FromFormObject]; and this will work as you would expect, for example:

```csharp
[HttpPost("[action]")]
[ExportFor(GasparType.All)]
public void Save([FromForm] IFormFile? image, [FromFormObject] ModelData model)
{
}
```

As shown above, you should continue to use [FromForm] for simple types and IFormFile.

#### 405/415 Errors

If the object you pass to a controller action is malformed in some way (number as text, or missing required properties), a 4XX error will be sent back to the browser.  The actual error is swallowed and it can sometime be really difficult to find the cause.

[FromFormObject] will write errors to the console - so you can fix them!

If your struggling with 405 or similar errors, trying changing to [FromFormObject] temporarily.

## Disable Export on Build

Gaspar will export files on build.  If you would like to disable this add the following to the `PropertyGroup` of you csproj file

- `<RunGaspar>False</RunGaspar>`

This might be useful to temporarily disable the feature, or if you need to use the built in C# features (i.e. in a dependent project) but don't want the export.

You can also use this on the command line: `dotnet build /p:RunGaspar=False`

## Well Written Controller Actions

Gaspar will export all your controller actions, but they must be written in a way that Gaspar can understand.  This will improve your overall code quality, not just meet arbitrary rules.  If Gaspar can't understand the action, it may export a skeleton method marked 'depreciated' or 'obsolete' with a friendly error message.

Improve your actions as follows:

- Controller classes must be derived from a class with `Controller` in the name (e.g. `Controller`, `ControllerBase`, `MyBaseController`, etc...)

- Actions should be decorated with an http method and route, e.g. `[HttpPost("[controller]/[action]")]`

Other recommendations (not required, but good practice):

- Have your controller actions return `ActionResult<T>`. This will provide a strongly-typed interface in the service communication endpoints.
  
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

In C# these methods are fully annotated for nullable projects; for example:

```csharp
ServiceResponse<string> response = new();
string data = response.Data; //warning here - Data may be null here
if (response.Success)
{
    data = response.Data; //Data not null here
}
```

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

        this.service.myAction(requestId, requestObj).then(response => {
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

        this.service.myAction(requestId, requestObj).subscribe(response => {
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
        ServiceResponse<bool?> response = MyService.MyAction(requestId, requestObj);

        //or call async:
        ServiceResponse<bool?> response = await MyService.MyActionAsync(requestId, requestObj);

        //you can also optionally specify a timeout TimeSpan if the request is expected to be long running, e.g.:
        //MyService.MyAction(requestId, requestObj, timeout: TimeSpan.FromMinutes(30));

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

The full specification is published to [schemastore.org](https://schemastore.org); the demo config includes the reference.

### At the root level

You must supply either Models or Controllers, but you don't need both; all other parameters are optional.

**Models**    `ConfigurationType`    Options for outputting Models and Enums

**Controllers**    `ConfigurationType`    Options for outputting service communication contracts for Controllers

**GlobalTypeTranslations**    `Array of string pairs: [{"X": "Y"}, {"A": "B"}]`    Used by TypeScript, Angular, Swift, Kotlin and Proto.  This allows you to override type names to objects the target language will understand.  e.g. `{ "IMyInterface": "Object" }`.  As this applies the same conversion to every language, it use is limited.

**TypeTranslations**    `Object. Keys are Output types.  Value is Array of string pairs: [{"X": "Y"}, {"A": "B"}]`    Used by TypeScript, Angular, Swift, Kotlin and Proto. For each exported language type, you can specify override type names the target language will understand. e.g. `{ "IMyInterface": "Object" }`.  For example:

```json
"TypeTranslations": {
    "TypeScript": {
        "IMyInterface": "any",
    },
    "Swift": {
        "IMyInterface": "AnyObject",
    },
}
```

**IgnoreMissingOutputLocations**    `bool: default false`    If the file output location cannot be found, you will get a build error; add `"IgnoreMissingOutputLocations": true` to skip the error.  This is useful when you need to build in an environment where the output may not be available (e.g. docker); although it would usually be preferable to use `dotnet build /p:RunGaspar=False` in your scripts.

**IgnoreAnnotations**    `bool: default false`    Set this to true to export all objects irrespective if they have `[ExportFor]` or not.

**GroupTypes**    `Object (see example)`   When using `[ExportFor]` you must specify one of the built-in export types (e.g. `GasparType.TypeScript`, or `.All`), however if you routinely export to several services at once you can create a group type to export to.  An example configuration might look like:

```json
"GroupTypes": {
    "FrontEnd": [ "TypeScript", "Angular", "Ocelot" ],
    "App": [ "Swift", "Kotlin" ]
}
```

*`FrontEnd` and `App` are examples, use any name you like.*

Now when using `[ExportFor]`, you can use `FrontEnd` to output TypeScript, Angular and Ocelot in one go, or use `App` to export both Swift and Kotlin in one go!

Your attribute can simply use a string: `[ExportFor("App")]`, but this is prone to error (miss-spelling, etc), so to use a strongly typed object, like you do with the native types (`GasparType.Swift`), you need to create a static class with the names as properties.

For the above example, add the following somewhere in your code and you then use, for example, `[ExportFor(GasparGroupType.App)]` to export Swift and Kotlin together.:

```csharp
public static class GasparGroupType
{
    public const int FrontEnd = GasparType.TypeScript | GasparType.Angular | GasparType.Ocelot;
    public const int App = GasparType.Swift | GasparType.Kotlin;
}
```

The values shown in the above example (e.g. `GasparType.Swift | GasparType.Kotlin`) repeat the `GasparType` properties in the config.  The actual values have no bearing on the export process (you could just set to `0`).  However if you would like to use these group types in any of the `ExportsFor` functions you must set up the groups as above.  It is therefore recommended to do this, incase you might use `ExportsFor` in the future.

*The class name above (`GasparGroupType`) is just an example, the actual name is unimportant.*

### ConfigurationType

**Include**    `Array of strings: default ["./**/*.cs"]`    List of file locations containing the models or controllers you wish to translate.  Optional, if not provided will use all .cs files in the project.

**Exclude**    `Array of strings: default []`    List of file locations to exclude from the Include list

**Output**    `Array of ConfigurationTypeOutput`    Output options.  You must supply at least one of these.

For Models:

- **UseEnumValue**    `bool: default true`    Enums will be written with their value, either set directly or from an Attribute (e.g. `[EnumMember(Value = "name")]`).

- **StringLiteralTypesInsteadOfEnums**    `bool: default false`    For TypeScript, if true will export enums as types instead of TypeScript enums.

For Controllers:

- **ServiceName**    `string`    Required.  Used to name exported items, can also be used in paths.

- **ServiceHost**    `string`    Optional.  Used by Ocelot Export, and can be used in paths.  Typically set to 'http' or 'https'

- **ServicePort**    `int`    Optional.  Used by Ocelot Export, and can be used in paths.

### ConfigurationTypeOutput

**Type**    `specific string`    Required.  Must be one of:

- For models: `"TypeScript"`, `"Angular"`, `"Swift"`, `"Kotlin"`, `"Proto"`

- For controllers: `"TypeScript"`, `"Angular"`, `"CSharp"`, `"Ocelot"` or `"Python"`

**Location**    `string`    Required.  The location to output the translated file to (relative to the project root).  For controllers, can include `{ServiceName}`, `{ServiceHost}` or `{ServicePort}` to have those placeholders replaced (see the demo file).

For controllers (optional):

**UrlPrefix**    `string`    When building the service contract, you can prefix the url with this value; e.g. `"http://myservice.com:81"`. The service url will be built from this followed by `/` then the action route.  Can include `{ServiceName}`, `{ServiceHost}` or `{ServicePort}` to have those placeholders replaced (see the demo file).

For CSharp, TypeScript and Angular controllers (optional):

- **UrlHandlerFunction**    `string`    If you would like to run your url through a function to ensure it is correct, or apply environmental prefixes;  You can provide the function name here.
  
  For CSharp, your code will need to provide a static string extension method; as follows:
  
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
  
  For Typescript and Angular, your code will need to provide an exported function; as follows:
  
  ```typescript
  export function MyUrlHelper(s: string): string {
      //manipulate s...
      return s;
  }
  ```
  
  In both the above examples, you will add `"UrlHandlerFunction": "MyUrlHelper"` to the config.
  *make sure to include the namespace/import to your function in the config (see below)*

For CSharp controllers (all optional):

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

- **ModelNamespaces**    `Array of strings`    List of namespaces to include at the top of your exported Service Communication class.  This would include the namespaces to custom types in the export, or your serializer and logging tools

For TypeScript and Angular models and controllers (all optional)

- **AddInferredNullables**    `bool: default false`    Property types that are explicitly nullable (e.g. `int?`) will always be outputed with `null` types (e.g. `number | null`).  Set this property to true to add null types to C# types that could be null if unset.
  
  In short, if your C# project doesn't enable "nullable annotation context", set this to true.

- **NullablesAlsoUndefinded**    `bool: default false`    If set to true, all nullable properties (those exported with `| null`) will additional have `| undefined` added to the type.

For TypeScript and Angular controllers (all optional):

- **ModelPath**    `string`    Path to a file containing definitions for any custom types used in your service communications (excluding the extension, as is usual for TypeScript includes). Ideally, this is the file exported by the model export part of this application.

- **HelperFile**    `string`    The service communication export requires some extra code to handle the boilerplate requests.  This is the name of the file that should be exported.  If omitted, the code will be included at the top of the exported service communications file, which may cause issues if you're exporting from multiple projects.

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

For Kotlin and Proto Models (required)

- **PackageNamespace**    `string`    Namespace to be used when generating `.kt` and `.proto` file outputs. For example, the PackageNamespace value of 'com.wckdzr' produces the following header:
  
  ```kotlin
  //kotlin file
  package com.wckdrzr;
  ```
  
  ```protobuf
  //proto file
  syntax = "proto3";
  package com.wckdrzr;
  ```

## ExportsFor

If your data is decorated with `[ExportFor]` attributes, it might sometimes be useful to see that in your code using Reflection.  There are a few functions built into Gaspar to allow this.

### ExportsFor

`ExportsFor` lets you know if it the object will export for the given `gasparType`.

```csharp
bool ExportsFor(this MemberInfo member, int gasparType)
```

*The `gasparType` parameter is an `int`, but you should use one of the properties in the static `GasparType` class (e.g. `GasparType.TypeScript`), or your group type class.*

For example `myObj.ExportsFor(GasparType.Swift)` will return true if the `myObj` class will export to Swift, via a direct attribute or it's parents.

There are a number of convenience overrides for this, you will see the `ExportsFor` function on the following types: `Type`, `TypeInfo`, `ICustomAttributeProvider`, from `Newtonsoft.Json`; `JsonProperty`, and from `System.Text`; `JsonPropertyInfo` and `JsonTypeInfo`. *(let me know if we should add others!)*

### AnyChildExportsFor

Similar to `ExportsFor` this will let you know if the object or any of it's child properties will export for the given `gasparType`.

```csharp
bool AnyChildExportsFor(this MemberInfo member, int gasparType, bool includeThis = true)
```

*The `gasparType` parameter is an `int`, but you should use one of the properties in the static `GasparType` class (e.g. `GasparType.TypeScript`), or your group type class.*

For example `myObj.AnyChildExportsFor(GasparType.Swift)` will return true if the `myObj` class or any of it's child properties will export to Swift, via a direct attribute or it's parents.

If you don't want to match the target class (`myObj`), only the children; set `includeThis` to false.

There are a number of convenience overrides for this, you will see the `ExportsFor` function on the following types: `Type`, `TypeInfo`, `ICustomAttributeProvider`, from `Newtonsoft.Json`; `JsonProperty`, and from `System.Text`; `JsonPropertyInfo` and `JsonTypeInfo`. *(let me know if we should add others!)*

### Json Serialization

This is where the power of `ExportsFor` really kicks in!

If you only export some of your model to a given platform, or exclude properties from the export, the model will only contain what you select (of course), however your actually exported data will include the values.  So you target language won't be able to access the properties, but they can be read by anyone looking at the api.  For example, take this class:

```csharp
[ExportFor(GasparType.TypeScript)]
public class User
{
    public string name { get; set; }

    [ExportFor(~GasparType.TypeScript)]    
    public string password { get; set; }

    //... etc
}
```

In the above `password` will not be available in TypeScript, but it will be in the API data.

**So, use `SerializeIfExportsFor`.**

There are lot of options here, and depends on if you use Newtonsoft or System.Text

#### System.Text.Json

When using System.Text, you should add the following `using` statement:

```csharp
using WCKDRZR.Gaspar.GasparSystemJson;
```

To export only a given type, you need to use the `IfExportsForModifer` modifier, as follows:

```csharp
string json = JsonSerializer.Serialize(
    myObject,
    new JsonSerializerOptions() {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver() {
            Modifiers = {
                GasparJson.IfExportsForModifer(GasparType.TypeScript)
            }
        }
    }
);
```

If you only have this one resolver; you can simplify your code by using the `IfExportsForResolver` resolver, as follows:

```csharp
string json = JsonSerializer.Serialize(
    myObject,
    new JsonSerializerOptions() {
        TypeInfoResolver = GasparJson.IfExportsForResolver(GasparType.TypeScript)
    }
);
```

If this is the only serialisation option you are setting, you can simplify your code by using the `IfExportsForJsonSerializerOptions` option, as follows:

```csharp
string json = JsonSerializer.Serialize(
    myObject,
    GasparJson.IfExportsForJsonSerializerOptions(GasparType.TypeScript)
);
```

...or simply:

```csharp
string json = GasparJson.SerializeIfExportsFor(myObject, GasparType.TypeScript);
```

#### Newtonsoft.Json

When using Newtonsoft, you should add the following `using` statement:

```csharp
using WCKDRZR.Gaspar.GasparNewtonsoftJson;
```

To export only a given type, you need to use the `IfExportsForResolver` resolver, as follows:

```csharp
string json = JsonConvert.SerializeObject(
    myObject,
    new JsonSerializerSettings() {
        ContractResolver = new GasparJson.IfExportsForResolver(GasparType.TypeScript)
    }
);
```

If this is the only serialisation setting you are using, you can simplify your code by using the `IfExportsForJsonSerializerSettings` setting, as follows:

```csharp
string json = JsonConvert.SerializeObject(
    myObject,
    GasparJson.IfExportsForJsonSerializerSettings(GasparType.TypeScript)
);
```

...or simply:

```csharp
string json = GasparJson.SerializeIfExportsFor(myObject, GasparType.TypeScript);
```

## Issues and Contributions

Please raise any issues or pull requests in GitHub. We will try and incorporate any changes as promptly as possible.

## Thanks

Gaspar was originally based on [Jonathan Svenheden's C# models to TypeScript project](https://github.com/svenheden/csharp-models-to-typescript) (written in C# and JavaScript).  There is still a little of Jonathan's code there, but we have moved on significantly.  Thanks for the inspiration!

© [Wckd Rzr](https://github.com/wckdrzr)
