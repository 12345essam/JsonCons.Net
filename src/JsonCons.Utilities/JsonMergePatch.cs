using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;

namespace JsonCons.Utilities
{
    /// <summary>
    /// Provides functionality for applying a JSON Merge Patch as 
    /// defined in <see href="https://datatracker.ietf.org/doc/html/rfc7396">RFC 7396</see>
    /// to a JSON value.
    /// </summary>
    /// <example>
    /// The following example borrowed from [RFC 7396](https://datatracker.ietf.org/doc/html/rfc7396) shows how to apply a JSON Merge Patch to a JSON value
    /// <code>
    /// using System;
    /// using System.Diagnostics;
    /// using System.Text.Json;
    /// using JsonCons.Utilities;
    /// 
    /// public class Example
    /// {
    ///    public static void Main()
    ///    {
    ///     using var doc = JsonDocument.Parse(@"
    /// {
    ///      ""title"": ""Goodbye!"",
    ///      ""author"" : {
    ///    ""givenName"" : ""John"",
    ///    ""familyName"" : ""Doe""
    ///      },
    ///      ""tags"":[ ""example"", ""sample"" ],
    ///      ""content"": ""This will be unchanged""
    /// }
    ///     ");
    /// 
    ///     using var patch = JsonDocument.Parse(@"
    /// {
    ///      ""title"": ""Hello!"",
    ///      ""phoneNumber"": ""+01-123-456-7890"",
    ///      ""author"": {
    ///    ""familyName"": null
    ///      },
    ///      ""tags"": [ ""example"" ]
    /// }
    ///         ");
    /// 
    ///     using JsonDocument result = JsonMergePatch.ApplyMergePatch(doc.RootElement, patch.RootElement);
    /// 
    ///     var options = new JsonSerializerOptions() { WriteIndented = true };
    /// 
    ///     Console.WriteLine("The original document:\n");
    ///     Console.WriteLine($"{JsonSerializer.Serialize(doc.RootElement, options)}\n");
    ///     Console.WriteLine("The patch:\n");
    ///     Console.WriteLine($"{JsonSerializer.Serialize(patch.RootElement, options)}\n");
    ///     Console.WriteLine("The result:\n");
    ///     Console.WriteLine($"{JsonSerializer.Serialize(result, options)}\n");
    ///        ");
    ///     }
    /// }
    /// </code>
    /// The original document:
    /// 
    /// {
    ///   "title": "Goodbye!",
    ///   "author": {
    ///     "givenName": "John",
    ///     "familyName": "Doe"
    ///   },
    ///   "tags": [
    ///     "example",
    ///     "sample"
    ///   ],
    ///   "content": "This will be unchanged"
    /// }
    /// 
    /// The patch:
    /// 
    /// {
    ///   "title": "Hello!",
    ///   "phoneNumber": "\u002B01-123-456-7890",
    ///   "author": {
    ///     "familyName": null
    ///   },
    ///   "tags": [
    ///     "example"
    ///   ]
    /// }
    /// 
    /// The result:
    /// 
    /// {
    ///   "title": "Hello!",
    ///   "author": {
    ///     "givenName": "John"
    ///   },
    ///   "tags": [
    ///     "example"
    ///   ],
    ///   "content": "This will be unchanged",
    ///   "phoneNumber": "\u002B01-123-456-7890"
    /// }    
    /// </example>

    public static class JsonMergePatch
    {
        /// <summary>
        /// Applies a JSON Merge Patch as defined in <see href="https://datatracker.ietf.org/doc/html/rfc7396">RFC 7396</see> 
        /// to a source JSON value.
        /// </summary>
        /// <remarks>
        /// It is the users responsibilty to properly Dispose the returned JSONDocument value
        /// </remarks>
        /// <param name="source">The source JSON value.</param>
        /// <param name="patch">The JSON merge patch to be applied to the source JSON value.</param>
        /// <returns>The patched JSON value</returns>
        public static JsonDocument ApplyMergePatch(JsonElement source, JsonElement patch)
        {
            var documentBuilder = new JsonDocumentBuilder(source);
            var builder = ApplyMergePatch(ref documentBuilder, patch);
            return builder.ToJsonDocument();
        }

        static JsonDocumentBuilder ApplyMergePatch(ref JsonDocumentBuilder target, JsonElement patch)
        {
            if (patch.ValueKind == JsonValueKind.Object)
            {
                if (target.ValueKind != JsonValueKind.Object)
                {
                    target = new JsonDocumentBuilder(JsonValueKind.Object);
                }
                foreach (var property in patch.EnumerateObject())
                {
                    JsonDocumentBuilder item;
                    if (target.TryGetProperty(property.Name, out item))
                    {
                        target.RemoveProperty(property.Name);
                        if (property.Value.ValueKind != JsonValueKind.Null)
                        {
                            target.AddProperty(property.Name, ApplyMergePatch(ref item, property.Value));
                        }
                    }
                    else if (property.Value.ValueKind != JsonValueKind.Null)
                    {
                        item = new JsonDocumentBuilder(JsonValueKind.Object);
                        target.AddProperty(property.Name, ApplyMergePatch(ref item, property.Value));
                    }
                }
                return target;
            }
            else
            {
                return new JsonDocumentBuilder(patch);
            }
        }
    }


} // namespace JsonCons.Utilities
