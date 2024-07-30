using System;

namespace WCKDRZR.Gaspar
{
    public class ExportForAttribute : ExportOptionsAttribute
    {
        /// <summary>
        /// Mark this property for Gaspar Export.
        /// </summary>
        /// <param name="types">Types to export for.  Use propeties in the GasparTypes class.  Select multiple with bitwise `|` operator.  Deselect type with bitwise `~` operator</param>
        public ExportForAttribute(int types) { }

        /// <summary>
        /// Mark this property for Gaspar Export.
        /// </summary>
        /// <param name="type">String name of type or group to export.  Not recommended, use GasparTypes class properties instead; and extend for custom group types</param>
        public ExportForAttribute(string type) { }
    }

    public class ExportOptionsAttribute : Attribute
    {
        /// <summary>
        /// Set to true to removed the inherited base classes from the export.
        /// </summary>
        public bool NoInheritance { get; set; }

        /// <summary>
        /// Allows you to override the return type name that is generated.  Particularly useful if your return type is obscured (e.g. in `ContentResult`).
        /// </summary>
        public string? ReturnTypeOverride { get; set; }

        /// <summary>
        /// For C# Service Communications, if the JSON returned by the decorated action won't deserialize through the Newtonsoft serializer, add the name of your custom serialize class (use nameof()).
        /// </summary>
        public string? Serializer { get; set; }
    
        /// <summary>
        /// For Ocelot export, list of scopes to be used in "AllowedScopes" of the "AuthenticationOptions" section in the Ocelot config.  If set this will override scopes generated from the `DefaultScopes` and `ScopesByHttpMethod` configuration.
        /// </summary>
        public string[]? Scopes { get; set; }

        /// <summary>
        /// For Ocelot - scopes to be added to "AllowedScopes" of the "AuthenticationOptions" section in the Ocelot config. If set these scopes will be in addition to scopes generated from the `DefaultScopes` and `ScopesByHttpMethod` configuration.
        /// </summary>
        public string[]? AdditionalScopes { get; set; }

        /// <summary>
        /// For C# Service Communications this provides a default timeout value in milliseconds for calls made to this controller/action.
        /// For Ocelot, this allows you to provide a QoS Timeout value in the configuration.
        /// </summary>
        public long Timeout { get; set; }

        /// <summary>
        /// Specify headers that will be sent with the request.  Specifying an empty array will add a `Dictionary<string, string>` property (or equivalent) to allow any number of name/values pairs, defined when calling.  Specify a list of strings for these to be required as header name properties, for which you will add string values.
        /// </summary>
        public string[]? Headers { get; set; }
    }

    public class ExportWithoutInheritance : Attribute
    {
    }

    public static class GasparType
    {
        public const int All =
            Angular | CSharp | Ocelot | TypeScript | Proto | Python | Swift | Kotlin
        ;

        public const int Angular = 1 << 0;
        public const int CSharp = 1 << 1;
        public const int Ocelot = 1 << 2;
        public const int TypeScript = 1 << 3;
        public const int Proto = 1 << 4;
        public const int Python = 1 << 5;
        public const int Swift = 1 << 6;
        public const int Kotlin = 1 << 7;
    }
}