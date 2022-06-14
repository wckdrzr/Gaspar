# Gaspar

Gaspar is a tool that consumes your C# domain models, types and controllers and exports them for other services and languages to consume.  Your C# models are available in TypeScript and your controller endpoints are available to TypeScript and other C# services, removing the need to hard code variable names and urls.

It's built from [Jonathan Svenheden's C# models to TypeScript project](https://github.com/svenheden/csharp-models-to-typescript) which makes use of the [Roslyn (the .NET compiler platform)](https://github.com/dotnet/roslyn) to parse the source files, which removes the need to create and maintain our own parser.

## Supported Conversions

|                         | C# Models and Types | C# Controllers |
| ----------------------- |:-------------------:|:-----:|
| Export to TypeScript    | ✅ |  |
| Export to Angular       | ✅ | ✅ |
| Export to Ocelot Config |  | ✅ |
| Export to C#            |  | ✅ |

## Dependencies

* [.NET Core SDK](https://www.microsoft.com/net/download/macos)


## Install

Install from NuGet: https://www.nuget.org/packages/WckdRzr.Gaspar/


## How to use

1. Add a config file to your project named `gaspar.config.json` that contains for example (See `/Models/Configuration.cs` for the full config model)...

```
{
	"Models": {
		"Include": [
			"./Models/**/*.cs",
            "./Enums/**/*.cs"
		],
		"Exclude": [
            "./Models/foo/bar.cs"
        ],
	    "Output": [{
			"Type": "TypeScript",
	    	"Location": "./api.d.ts"
	    },{
			"Type": "CSharp",
	    	"Location": "./ServiceComms/Api.cs"
		}],
		"CamelCaseEnums": false,
		"NumericEnums": true,
		"StringLiteralTypesInsteadOfEnums": false
	},
	"Controllers": {
		"Include": [
			"./Controllers/**/*.cs",
		],
		"Exclude": [],
		"Output": [{
			"Type": "Angular",
			"Location": "./service.ts",
			"ModelPath": "./api.d"
		}],
	    "Gateway": "api",
	    "ServiceName": "all"
	},
    "OnlyWhenAttributed": "FrontEnd",
    "CustomTypeTranslations": {
        "ProductName": "string",
        "ProductNumber": "string"
    }
}
```

2. Add a NuGet script to your project.

3. Build.  The output files will be created.


## License

MIT © [Wckd Rzr](https://github.com/wckdrzr)
