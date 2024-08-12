namespace PSAmalgamate;
public class Module
{
    private List<Module> _requiredModules = [];
    internal List<String> _requiredModulePaths = [];
    public required FileInfo FileInfo { get; set; }
    public string Name { get => Path.GetFileNameWithoutExtension(FileInfo.Name); }
    public List<Module> RequiredModules { get => _requiredModules; }

    public static async Task<Module> LoadFile(FileInfo file, DirectoryInfo workingDirectory)
    {
        Module module = new()
        {
            FileInfo = file
        };
        var lines = await LoadRequiredModules(file, workingDirectory);
        foreach (var modulePath in lines)
        {
            var subModule = await LoadFile(new(modulePath), workingDirectory);
            module.RequiredModules.Add(subModule);
        }
        return module;
    }
    public static async Task<List<string>> LoadRequiredModules(FileInfo file, DirectoryInfo workingDirectory, bool onlyFiles = true)
    {
        List<string> modulelist = [];
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
                    string modulePath = l["using module".Length..].Trim();
                    // check if the module is relative to the current file
                    if (modulePath.StartsWith('.'))
                    {
                        var resolvedModulePath = Path.GetFullPath(Path.Combine(workingDirectory.FullName, modulePath));
                        // check if the file exist
                        if (!File.Exists(resolvedModulePath))
                        {
                            throw new FileNotFoundException($"module not found in the following path: {resolvedModulePath}");
                        }
                        modulePath = resolvedModulePath;
                        modulelist.Add(modulePath);
                    }
                    else if (!onlyFiles)
                    {
                        modulelist.Add(modulePath);
                    }
                    break;
                default:
                    break;
            }
        }
        return modulelist;
    }
}