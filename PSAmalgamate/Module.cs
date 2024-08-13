namespace PSAmalgamate;
public class Module
{
    internal List<string> _requiredModulePaths = [];
    public required FileInfo FileInfo { get; set; }
    public string Name { get => Path.GetFileNameWithoutExtension(FileInfo.Name); }
    public List<Module> RequiredModules { get; } = [];
    public List<string> RequiredNativeModules { get; } = [];

    public static async Task<Module> LoadModuleInfo(FileInfo file, DirectoryInfo workingDirectory)
    {
        var module = new Module()
        {
            FileInfo = file
        };
        var requiredModules = await LoadRequiredModules(file, workingDirectory, false);
        foreach (var requiredModule in requiredModules)
        {
            // check if the module is a native module or a script module
            if (File.Exists(requiredModule))
            {
                module.RequiredModules.Add(await LoadModuleInfo(new FileInfo(requiredModule), workingDirectory));
            }
            else
            {
                module.RequiredNativeModules.Add(requiredModule);
            }

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