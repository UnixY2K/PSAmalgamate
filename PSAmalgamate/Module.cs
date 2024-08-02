using System.Reflection;
using System.Threading.Tasks.Dataflow;

class Module
{
    private List<Module> _requiredModules = [];
    public required FileInfo FileInfo { get; set; }
    public string Name { get => Path.GetFileNameWithoutExtension(FileInfo.Name); }
    public List<Module> RequiredModules { get => _requiredModules; }

    public static async Task<Module> LoadFile(FileInfo file, DirectoryInfo workingDirectory)
    {
        Module module = new()
        {
            FileInfo = file
        };
        var lines = await File.ReadAllLinesAsync(file.FullName);
        foreach (var line in lines)
        {
            switch (line.Trim())
            {
                case "":
                case null:
                case string l when l.StartsWith('#'):
                    continue;
                case string l when l.StartsWith("using module"):
                    string modulePath = l.Substring("using module".Length);
                    Console.WriteLine(modulePath);
                    break;
                default:
                    break;
            }
        }
        return module;
    }
}