
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Text;
using Humanizer;
using System.Text.RegularExpressions;

// Config

var typeAlias = new Dictionary<string, string>
{
    ["StorageInformation"] = "MyJdStorageInformation",
    ["SystemInformation"] = "MyJdSystemInformation",
    ["UrlDisplayTypeStorable"] = "MyJdUrlDisplayTypeStorable",
    ["LogFolderStorable"] = "MyJdLogFolderStorable",
    ["DirectConnectionInfos"] = "MyJdDirectConnectionInfos",
};

var knownWords = new[] { "Crawler", "Collector", "Grabber", "Captcha", "Events", "Controller", "Forward", "V2", "UUID", "List" };

var typeRefNameOverrides = new Dictionary<int, string>
{
    [333] = "MyJdMenuStructureType",
    [25] = "MyJdBasicAuthenticationType",
};

// Generator

var config = Configuration.Default.WithDefaultLoader();
var context = BrowsingContext.New(config);
var document = await context.OpenAsync("https://my.jdownloader.org/developers/index.html");

var mainContent = document.QuerySelector("#main-content");

var endpoints = new List<MyJdEndpoint>();
var enums = new Dictionary<int, MyJdEnum>();
var types = new Dictionary<int, MyJdType>();
var docPart = MyJdDocumentationPart.Unknown;
var @namespace = "";
var endpoint = "";

var enumName = "";
var enumAnchor = -1;
var enumClassNamespace = "";

var typeName = "";
var typeAnchor = -1;
var typeClassNamespace = "";

foreach (var child in mainContent!.Children)
{
    if (child is IHtmlHeadingElement { NodeName: "H1", TextContent: "Methods" })
    {
        docPart = MyJdDocumentationPart.Methods;
        continue;
    }

    if (child is IHtmlDivElement { ClassName: "header1", Children: [_, IHtmlHeadingElement { NodeName: "H1" } heading] })
    {
        var headingContent = NormalizeString(heading.TextContent);

        if (headingContent == "Enums & Constants")
        {
            docPart = MyJdDocumentationPart.EnumsConstants;
            continue;
        }

        if (headingContent == "Structures & Objects")
        {
            docPart = MyJdDocumentationPart.StructuresObjects;
            continue;
        }
    }

    switch (docPart)
    {
        case MyJdDocumentationPart.Methods:
            ReadMethodsElement(child);
            break;
        case MyJdDocumentationPart.EnumsConstants:
            ReadEnumsConstantsElement(child);
            break;
        case MyJdDocumentationPart.StructuresObjects:
            ReadStructuresObjectsElement(child);
            break;
        default:
            break;
    }
}



void ReadMethodsElement(IElement element)
{
    if (element is IHtmlDivElement { Children: [_, IHtmlHeadingElement { NodeName: "H1", TextContent: ['N', 'a', 'm', 'e', 's', 'p', 'a', 'c', 'e', ..] } namespaceHeader] })
    {
        var match = Regex.Match(namespaceHeader.InnerHtml, "Namespace&nbsp;/(?<namespaceName>\\w*)<span");
        if (!match.Success)
            throw new InvalidOperationException("Unknown namespace name");

        @namespace = match.Groups["namespaceName"].Value;
    }

    if (!string.IsNullOrEmpty(@namespace) && element is IHtmlHeadingElement { NodeName: "H3", ClassName: "main-type-method" })
    {
        if (element.FirstChild is null)
            throw new InvalidOperationException("Endpoint definition did not contain endpoint name");

        endpoint = KnownWords(element.FirstChild!.TextContent);
    }

    if (!string.IsNullOrEmpty(@namespace) && element is IHtmlUnorderedListElement { ClassName: "keyvalue" })
    {
        var detailsPart = MyJdEndpointDetailsPart.Unknown;
        var path = "";
        string? returnType = null;
        var parameters = new List<MyJdEndpointParameter>();
        var deprecated = element.ChildNodes.Any(x => x is IHtmlParagraphElement);
        var description = "";
        var possibleErrors = "";

        foreach (var detailsChild in element.ChildNodes.OfType<IHtmlListItemElement>())
        {
            if (detailsChild is not { ClassName: "keyvalueentry" })
            {
                if (detailsChild is { Children: [IHtmlParagraphElement { ClassName: "deprecated" }] })
                {
                    deprecated = true;
                    continue;
                }

                throw new InvalidOperationException("Unknown endpoint details format");
            }

            var valueNode = detailsChild.ChildNodes.First(c => c is IHtmlSpanElement { ClassName: "value" });

            var key = NormalizeString(detailsChild.ChildNodes.First(c => c is IHtmlSpanElement { ClassName: "key" }).TextContent);
            var value = NormalizeString(valueNode.TextContent);

            if (Enum.TryParse<MyJdEndpointDetailsPart>(key.Replace(" ", "").Replace("(", "").Replace(")", ""), true, out var part))
                detailsPart = part;

            var typRefs = valueNode.ChildNodes.OfType<IHtmlAnchorElement>().ToDictionary(a => a.TextContent, a => int.Parse(a.Href!.Split('_')[1]));

            if (detailsPart == MyJdEndpointDetailsPart.Parameter)
            {
                var namedParameterMatch = new Regex("""\d - (?<parameterName>\w+) \((?<parameterType>(\w|[\[\]<>\|])+)\)""").Match(value);
                var unnamedParameterMatch = new Regex("""\d - \(?(?<parameterType>\w+)\)?""").Match(value);

                if (namedParameterMatch.Success)
                {
                    var parameterName = namedParameterMatch.Groups["parameterName"].Value;
                    var parameterType = namedParameterMatch.Groups["parameterType"].Value;
                    parameters.Add(new MyJdEndpointParameter(parameterName, ParseType(parameterType, typRefs)));
                    continue;

                }
                else if (unnamedParameterMatch.Success)
                {
                    var parameterType = unnamedParameterMatch.Groups["parameterType"].Value;
                    var parameterName = $"param{parameters.Count + 1}";
                    parameters.Add(new MyJdEndpointParameter(parameterName, ParseType(parameterType, typRefs)));
                    continue;
                }

                throw new InvalidOperationException("Unknown parameter format");
            }

            if (detailsPart == MyJdEndpointDetailsPart.Call)
            {
                path = value.Split('?')[0];
                continue;
            }

            if (detailsPart == MyJdEndpointDetailsPart.ReturnType)
            {
                returnType = ParseType(value, typRefs);
                continue;
            }

            if (detailsPart == MyJdEndpointDetailsPart.Description)
            {
                description = value;
                continue;
            }

            if (detailsPart == MyJdEndpointDetailsPart.PossibleErrors)
            {
                possibleErrors = value;
                continue;
            }

            throw new InvalidOperationException("Unknown endpoint details part");
        }

        if (!string.IsNullOrEmpty(endpoint))
            endpoints.Add(new MyJdEndpoint(@namespace, endpoint, parameters, path, returnType, description, possibleErrors, deprecated));
    }
}

void ReadEnumsConstantsElement(IElement element)
{
    if (element is IHtmlDivElement { ClassName: "header3", ChildNodes: [IHtmlAnchorElement { ClassName: "anchor" } anchor, IHtmlHeadingElement { NodeName: "H3", ClassName: "tooltip" } header] })
    {
        enumName = header.InnerHtml.Split('<')[0];
        enumAnchor = int.Parse(anchor.Id!.Split('_')[1]);
        enumClassNamespace = header.ChildNodes.OfType<IHtmlSpanElement>().FirstOrDefault(n => n.ClassName == "tooltiptext")?.TextContent
            ?? throw new InvalidOperationException("Class namespace not found");
        return;
    }

    if (element is IHtmlUnorderedListElement { ClassName: "enums" })
    {
        if (string.IsNullOrEmpty(enumName))
            throw new InvalidOperationException("Empty enum name not set");
        if (enumAnchor == -1)
            throw new InvalidOperationException("Enum anchor not set");

        var enumValues = new List<MyJdEnumValue>();


        foreach (var enumEntryElement in element.ChildNodes)
        {
            if (enumEntryElement is not IHtmlListItemElement)
                continue;

            if (enumEntryElement.ChildNodes is [IText text])
            {
                enumValues.Add(new(NormalizeString(text.Text).Trim(), ""));
                continue;
            }

            var toolTipContainer = enumEntryElement.ChildNodes.OfType<IHtmlSpanElement>().SingleOrDefault(n => n is { ClassName: "tooltip" })
                ?? throw new InvalidOperationException($"Unable to find tooltip container for enum value.\r\n{enumEntryElement.ToHtml()}");
            var toolTipText = toolTipContainer.ChildNodes.SingleOrDefault(n => n is IHtmlSpanElement { ClassName: "tooltiptext" })
                ?? throw new InvalidOperationException($"Unable to find tooltiptext container for enum value.\r\n{enumEntryElement.ToHtml()}");

            enumValues.Add(new(NormalizeString(toolTipContainer.InnerHtml.Split('<')[0]).Trim(), NormalizeString(toolTipText.TextContent)));
        }

        var finalName = $"MyJd{enumName.Humanize(LetterCasing.LowerCase).Dehumanize()}";

        if (typeRefNameOverrides.TryGetValue(enumAnchor, out var overrideName))
            finalName = overrideName;

        enums.Add(enumAnchor, new(finalName, enumClassNamespace, enumValues));
    }
}

void ReadStructuresObjectsElement(IElement element)
{
    if (element is IHtmlDivElement { ClassName: "header3", ChildNodes: [IHtmlAnchorElement { ClassName: "anchor" } anchor, IHtmlHeadingElement { NodeName: "H3", ClassName: "tooltip" } header] })
    {
        typeName = header.InnerHtml.Split('<')[0];
        typeAnchor = int.Parse(anchor.Id!.Split('_')[1]);
        typeClassNamespace = header.ChildNodes.OfType<IHtmlSpanElement>().FirstOrDefault(n => n.ClassName == "tooltiptext")?.TextContent 
            ?? throw new InvalidOperationException("Class namespace not found");
        return;
    }

    var properties = new List<MyJdTypeProperty>();

    if (element is IHtmlPreElement { ChildNodes: [{ NodeName: "CODE" } codeElement] } && !string.IsNullOrEmpty(typeName))
    {
        if (string.IsNullOrEmpty(typeName))
            throw new InvalidOperationException("Type name not set");
        if (enumAnchor == -1)
            throw new InvalidOperationException("Type anchor not set");

        var anchorLookup = element.QuerySelectorAll<IHtmlAnchorElement>("a").ToDictionary(a => a.TextContent, a => int.Parse(a.Href!.Split('_')[1]));

        var code = NormalizeString(codeElement.TextContent);

        if (code.Count(c => c == '=') > 1)
        {
            var match = new Regex("""(?:(?<propertyDescription>\/\*\*(?:[^*]|\*(?!\/))*\*\/)\s*)?\s*(?<propertyName>"\w+")\s*=\s*(?<propertyType>\([^\)]+\))""").Matches(codeElement.TextContent);

            if (match.Count == 0)
                throw new InvalidOperationException("Unable to parse properties");

            foreach (Match m in match)
            {
                var propertyType = m.Groups["propertyType"].Value.Trim('(', ')');
                var propertyName = m.Groups["propertyName"].Value;
                var propertyDescription = "";

                if (!string.IsNullOrEmpty(m.Groups["propertyDescription"].Value))
                {
                    propertyDescription = string.Join('\n', m.Groups["propertyDescription"].Value.Replace("/**", "").Replace("/*", "").Replace("* ", "").Replace("*/", "").Split('\n').Select(x => x.Trim())).Trim();

                }

                properties.Add(new(propertyName, ParseType(propertyType, anchorLookup), propertyDescription));
            }
        }

        var finalTypeName = $"MyJd{typeName.Humanize(LetterCasing.LowerCase).Dehumanize()}";

        if(typeClassNamespace.Contains("v2", StringComparison.InvariantCultureIgnoreCase))
            finalTypeName += "V2";

        types.Add(typeAnchor, new(finalTypeName, typeClassNamespace, properties));
    }
}

static string NormalizeString(string s) => Regex.Replace(s, @"[^\x20-\x7F]", " ");


// Generate endpoint classes

foreach (var endpointGroup in endpoints.GroupBy(x => x.Namespace))
{
    if (endpointGroup.Key == "")
        continue;

    var endpointGroupName = $"{CapitalizeFirstLetter(endpointGroup.Key)}Endpoint";
    endpointGroupName = KnownWords(endpointGroupName);

    var classCode = $$"""
// <auto-generated/>
using hhnl.My.JDownloader.Api;
using hhnl.My.JDownloader.Api.Utils;
using hhnl.My.JDownloader.Api.Models;
using hhnl.My.JDownloader.Api.Enums;
using System.Text.Json;

namespace hhnl.My.JDownloader.Api.Endpoints;

public class {{endpointGroupName}}(MyJDownloaderDevice device)
{
    {{string.Join('\n', endpointGroup.Select(GenerateEndpointMethodCode))}}
}

""";

    var filePath = Path.Combine("..", "..", "..", "..", "hhnl.My.JDownloader.Api", "Endpoints", "Generated", $"{endpointGroupName}.cs");
    File.WriteAllText(filePath, classCode.NormalizeLineEndings());

}

// Generate models

foreach (var type in types.Values)
{
    var typeCode = $$"""
    // <auto-generated/>
    using System.Text.Json.Serialization;
    using System.Text.Json;
    using hhnl.My.JDownloader.Api.Enums;

    namespace hhnl.My.JDownloader.Api.Models;

    public partial class {{type.Name}}
    {
        {{string.Join('\n', type.Properties.Select(p => $$"""
{{FormatPropertyDescription(p)}}
        [JsonPropertyName({{p.Name}})]
        public {{FormatType(p.Type)}} {{KnownWords(CapitalizeFirstLetter(p.Name.Trim('"')))}} { get; set; }
"""))}}
    }


    """;

    var filePath = Path.Combine("..", "..", "..", "..", "hhnl.My.JDownloader.Api", "Models", "Generated", $"{type.Name}.cs");
    File.WriteAllText(filePath, typeCode.NormalizeLineEndings());
}

//Generate enums

foreach (var enm in enums.Values)
{
    var enumCode = $$"""
    // <auto-generated/>
    using System.Text.Json.Serialization;
    using System.Text.Json;

    namespace hhnl.My.JDownloader.Api.Enums;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum {{enm.Name}}
    {
        /// <summary>
        /// DO NOT USE. This value is used to not send default values in requests.
        /// </summary>
        Default_Value,
        {{string.Join(",\n", enm.Values.Select(v => $$"""
    {{FormatEnumValueDescription(v)}}
        [JsonStringEnumMemberName("{{v.Name}}")]
        {{v.Name.Humanize(LetterCasing.LowerCase).Dehumanize()}}
    """))}}
    }
    """;

    var filePath = Path.Combine("..", "..", "..", "..", "hhnl.My.JDownloader.Api", "Enums", "Generated", $"{enm.Name}.cs");
    File.WriteAllText(filePath, enumCode.NormalizeLineEndings());
}


string KnownWords(string input)
{
    var result = new System.Text.StringBuilder(input);
    var orderedKnownWords = knownWords.OrderByDescending(w => w.Length);

    foreach (var word in knownWords)
    {
        for (var i = 0; i <= result.Length - word.Length; i++)
        {
            if (string.Compare(result.ToString(i, word.Length), word, StringComparison.OrdinalIgnoreCase) == 0)
            {
                for (var j = 0; j < word.Length; j++)
                {
                    result[i + j] = word[j];
                }
            }
        }
    }

    return result.ToString();
}

static string FormatPropertyDescription(MyJdTypeProperty property)
{
    if (string.IsNullOrEmpty(property.Description))
        return "";
    return $$"""

        /// <summary>
{{string.Join('\n', property.Description.Split('\n').Select(l => $"""
        /// {l}
"""))}}
        /// </summary>
""";
}

static string FormatEnumValueDescription(MyJdEnumValue value)
{
    if (string.IsNullOrEmpty(value.Description))
        return "";
    return $$"""

        /// <summary>
    {{string.Join('\n', value.Description.Split('\n').Select(l => $"""
        /// {l}
"""))}}
        /// </summary>
    """;
}

string GenerateEndpointMethodCode(MyJdEndpoint endpoint)
{

    return $$"""
{{GenerateEndpointMethodHeader(endpoint)}}        
        public virtual async Task{{FormatReturnType(endpoint.ReturnType)}} {{CapitalizeFirstLetter(endpoint.Name)}}Async({{string.Join(", ", endpoint.Parameters.Select(p => $"{FormatType(p.Type)} {p.Name}"))}}{{FormatCancellationTokenParameter(endpoint)}})
            => await device.RequestAsync{{FormatReturnType(endpoint.ReturnType)}}("{{endpoint.Path}}", {{FormatParameterPassing(endpoint)}}, cancellationToken);
""";
}

static string FormatParameterPassing(MyJdEndpoint endpoint)
    => endpoint.Parameters.Count == 0 ? "null" : $"[{string.Join(", ", endpoint.Parameters.Select(x => x.Name))}]";

static string FormatCancellationTokenParameter(MyJdEndpoint endpoint)
    => (endpoint.Parameters.Count == 0 ? "" : ", ") + "CancellationToken cancellationToken = default";

static string GenerateEndpointMethodHeader(MyJdEndpoint endpoint)
{
    var headerParts = new List<string>();

    if (!string.IsNullOrEmpty(endpoint.Description))
    {
        headerParts.Add($"""
        /// <summary>
{string.Join('\n', endpoint.Description.Split('\n').Select(l => $"""
        /// {l}
"""))}
        /// </summary>
""");
    }

    if (!string.IsNullOrEmpty(endpoint.PossibleErrors))
    {
        headerParts.Add($"""
        /// <remarks>
        /// Possible Errors: {endpoint.PossibleErrors}
        /// </remarks>
""");
    }

    if (endpoint.Deprecated)
    {
        headerParts.Add("""
        [Obsolete("This endpoint is deprecated.")]
""");
    }

    if (headerParts.Count > 0)
        headerParts.Insert(0, "");

    return string.Join('\n', headerParts);
}

string FormatReturnType(string? s)
    => s switch
    {
        null => "",
        _ => $"<{FormatType(s)}>"
    };

string FormatType(ReadOnlySpan<char> s)
{
    if (typeAlias.TryGetValue(s.ToString(), out var alias))
        return alias;

    return s.Trim() switch
    {
        "boolean" => "bool",
        "String" => "string",
        "Object" => "object",
        "Long" => "long",
        "Integer" => "int",
        "List" => "IReadOnlyCollection<long>",
        "JsonMap" => "IReadOnlyDictionary<string, JsonElement>",
        "V" => "object",

        [.. var innerType, '|', 'n', 'u', 'l', 'l'] => $"{FormatType(innerType)}?",
        [.. var innerType, '[', ']'] => $"IReadOnlyCollection<{FormatType(innerType)}>",
        ['L', 'i', 's', 't', '<', .. var innerType, '>'] => $"IReadOnlyCollection<{FormatType(innerType)}>",
        ['U', 'n', 'o', 'r', 'd', 'e', 'r', 'e', 'd', 'L', 'i', 's', 't', '<', .. var innerType, '>'] => $"IReadOnlyCollection<{FormatType(innerType)}>",
        ['M', 'a', 'p', '<', .. var innerTypes, '>'] => $"IReadOnlyDictionary<{FormatType(innerTypes.ToString().Split(',')[0])}, {FormatType(innerTypes.ToString().Split(',')[1])}>",
        ['M', 'a', 'p'] => $"IReadOnlyDictionary<object, object>",
        ['t', 'y', 'p', 'e', 'R', 'e', 'f', '_', .. var typeRefId] => GetTypeName(typeRefId.ToString()),
        _ => s.ToString(),
    };
}

string GetTypeName(string typeRefIdString)
{
    var typeRef = int.Parse(typeRefIdString);

    if (types.TryGetValue(typeRef, out var type))
        return type.Name;

    if (enums.TryGetValue(typeRef, out var enm))
        return enm.Name;

    throw new InvalidOperationException($"Unknown typeRef_{typeRef}");
}

static string CapitalizeFirstLetter(string s) => char.ToUpper(s[0]) + s[1..];

static string TypeNameOrRef(string name, int? typeRef)
    => typeRef is not null ? $"typeRef_{typeRef}" : name;

static string ParseType(string s, IReadOnlyDictionary<string, int> typeRefLookup)
{
    s = s.Trim();

    if (typeRefLookup.TryGetValue(s, out var typeRef))
        return TypeNameOrRef(s, typeRef);

    if (s.EndsWith("[]"))
    {
        return $"{ParseType(s[..^2], typeRefLookup)}[]";
    }

    if (s.EndsWith("|null"))
    {
        return $"{ParseType(s[..^5], typeRefLookup)}|null";
    }

    var genericMarkerIndex = s.IndexOf('<');
    if (genericMarkerIndex > 0 && s.EndsWith(">"))
    {
        var typeName = s[..genericMarkerIndex];
        var innerTypesString = s.Substring(genericMarkerIndex + 1, s.Length - genericMarkerIndex - 2);

        var parts = new List<string>();
        var depth = 0;
        var currentPart = new System.Text.StringBuilder();

        foreach (var c in innerTypesString)
        {
            if (c == '<') depth++;
            if (c == '>') depth--;

            if (c == ',' && depth == 0)
            {
                parts.Add(currentPart.ToString());
                currentPart.Clear();
            }
            else
            {
                currentPart.Append(c);
            }
        }
        parts.Add(currentPart.ToString());

        var parsedParts = parts.Select(p => ParseType(p, typeRefLookup));
        return $"{typeName}<{string.Join(", ", parsedParts)}>";
    }

    return s;
}

//foreach (var namespaceSection in namespaceSections)
//{
//    var namespaceName = namespaceSection.QuerySelector("h3")?.TextContent.Trim();
//    if (string.IsNullOrEmpty(namespaceName))
//    {
//        continue;
//    }

//    var className = namespaceName.Replace(".", string.Empty) + "Api";
//    var filePath = Path.Combine("..", "hhnl.My.JDownloader.Api", "Generated", $"{className}.cs");

//    var methods = new StringBuilder();

//    var endpointSections = namespaceSection.QuerySelectorAll("div > div > div > div.panel-body");

//    foreach (var endpointSection in endpointSections)
//    {
//        var endpointName = endpointSection.QuerySelector("span")?.TextContent.Trim();
//        if (string.IsNullOrEmpty(endpointName))
//        {
//            continue;
//        }

//        var methodName = endpointName.Split('/').Last();
//        methodName = string.Concat(methodName.Split('.').Select(s => char.ToUpper(s[0]) + s.Substring(1)));

//        var parameterList = new List<string>();
//        var parameterDescriptions = new StringBuilder();
//        var parameterTypes = new List<string>();

//        var parameterRows = endpointSection.QuerySelectorAll("table > tbody > tr");

//        var parameterNames = new List<string>();

//        foreach (var row in parameterRows)
//        {
//            var cells = row.QuerySelectorAll("td");
//            if (cells.Length < 3)
//            {
//                continue;
//            }

//            var parameterName = cells[0].TextContent.Trim();
//            var description = cells[2].TextContent.Trim();

//            parameterList.Add($"object {parameterName}");
//            parameterNames.Add(parameterName);
//            parameterDescriptions.AppendLine($"        /// <param name=\"{parameterName}\">{description}</param>");
//            parameterTypes.Add($"typeof({GetCSharpType(cells[1].TextContent.Trim())})");
//        }

//        var returnType = GetCSharpType(endpointSection.QuerySelector("p:nth-child(6)")?.TextContent.Trim() ?? "void");

//        methods.Append($$"""
//        /// <summary>
//        /// {{endpointSection.QuerySelector("p")?.TextContent.Trim()}}
//        /// </summary>
//        {{parameterDescriptions.ToString().TrimEnd()}}
//        public Task<{{returnType}}> {{methodName}}Async({{string.Join(", ", parameterList)}})
//        {
//            var parameters = new object[] { {{string.Join(", ", parameterNames)}} };
//            var parameterTypes = new Type[] { {{string.Join(", ", parameterTypes)}} };
//            return _client.CallAction<{{returnType}}>("{{endpointName}}", parameters, parameterTypes);
//        }

//        """);
//    }

//    var fileContent = $$"""
//// <auto-generated />
//#nullable enable
//using System.Text.Json.Serialization;
//using hhnl.My.JDownloader.Api.Models;

//namespace hhnl.My.JDownloader.Api.Generated
//{
//    public class {{className}}
//    {
//        private readonly MyJDownloaderClient _client;

//        public {{className}}(MyJDownloaderClient client)
//        {
//            _client = client;
//        }

//{{methods}}
//    }
//}
//""";

//    File.WriteAllText(filePath, fileContent);
//}

//string GetCSharpType(string input)
//{
//    return input switch
//    {
//        "String" => "string",
//        "Boolean" => "bool",
//        "Long" => "long",
//        "Integer" => "int",
//        "Object" => "object",
//        "String[]" => "string[]",
//        "Boolean[]" => "bool[]",
//        "Long[]" => "long[]",
//        "Integer[]" => "int[]",
//        "Object[]" => "object[]",
//        _ => "object"
//    };
//}




enum MyJdDocumentationPart
{
    Unknown,
    Methods,
    EnumsConstants,
    StructuresObjects,
}

record MyJdEndpoint(string @Namespace, string Name, List<MyJdEndpointParameter> Parameters, string Path, string? ReturnType, string Description, string PossibleErrors, bool Deprecated);
record MyJdEndpointParameter(string Name, string Type);
enum MyJdEndpointDetailsPart
{
    Unknown,
    Parameter,
    Call,
    ReturnType,
    Description,
    PossibleErrors,
}

record MyJdEnum(string Name, string ClassNamespace, List<MyJdEnumValue> Values);
record MyJdEnumValue(string Name, string Description);
record MyJdType(string Name, string ClassNamespace, List<MyJdTypeProperty> Properties);
record MyJdTypeProperty(string Name, string Type, string Description);
