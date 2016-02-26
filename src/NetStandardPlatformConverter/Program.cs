using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonVersionModifier
{
    public class Program
    {
        private static readonly Dictionary<string, string> TFMMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "dotnet", "netstandard1.3" },
            { "dotnet5.1", "netstandard1.0" },
            { "dotnet51", "netstandard1.0" },
            { "dotnet5.2", "netstandard1.1" },
            { "dotnet52", "netstandard1.1" },
            { "dotnet5.3", "netstandard1.2" },
            { "dotnet53", "netstandard1.2" },
            { "dotnet5.4", "netstandard1.3" },
            { "dotnet54", "netstandard1.3" },
            { "dotnet5.5", "netstandard1.4" },
            { "dotnet55", "netstandard1.4" },
            { "dotnet5.6", "netstandard1.5" },
            { "dotnet56", "netstandard1.5" },
            { "dnxcore50", "netstandardapp1.5" },
        };

        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Version Updates...");

                Console.WriteLine();
                Console.WriteLine("Updating versions for: " + Environment.NewLine + string.Join(Environment.NewLine, args));
                Console.WriteLine();

                foreach (var fileName in args)
                {
                    var projectName = Path.GetDirectoryName(fileName).Split('/', '\\').Last();
                    JObject data;
                    var updateFile = false;

                    Console.WriteLine($"Updating {projectName} project.json");
                    using (var file = File.OpenText(fileName))
                    using (var reader = new JsonTextReader(file))
                    {
                        try
                        {
                            data = (JObject)JToken.ReadFrom(reader);
                        }
                        catch
                        {
                            Console.WriteLine("Invalid project.json");
                            return;
                        }

                        var children = data["frameworks"].Children<JProperty>().ToArray();
                        foreach (var framework in children)
                        {
                            if (TFMMappings.ContainsKey(framework.Name))
                            {
                                updateFile = true;
                                var newImports = new List<string>
                                {
                                    framework.Name
                                };
                                var frameworkImports = framework.Values("imports").FirstOrDefault();
                                if (frameworkImports != null)
                                {
                                    if (frameworkImports is JArray)
                                    {
                                        newImports.AddRange(frameworkImports.Values<string>());
                                    }
                                    else // String
                                    {
                                        newImports.Add(frameworkImports.ToString());
                                    }
                                }

                                var jtokenFramework = data["frameworks"][framework.Name];

                                jtokenFramework["imports"] = JArray.FromObject(newImports);
                                jtokenFramework.Rename(TFMMappings[framework.Name]);
                            }
                        }
                    }

                    if (updateFile)
                    {
                        SaveDataTo(fileName, data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
                //Debugger.Launch();
            }
        }

        private static void SaveDataTo(string fileName, object data)
        {
            using (var fileStream = File.Open(fileName, FileMode.Truncate))
            using (var streamWriter = new StreamWriter(fileStream))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                jsonWriter.Formatting = Formatting.Indented;

                var serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, data);
            }
        }
    }

    public static class NewtonsoftExtensions
    {
        public static void Rename(this JToken token, string newName)
        {
            var parent = token.Parent;
            if (parent == null)
                throw new InvalidOperationException("The parent is missing.");
            var newToken = new JProperty(newName, token);
            parent.Replace(newToken);
        }
    }
}
