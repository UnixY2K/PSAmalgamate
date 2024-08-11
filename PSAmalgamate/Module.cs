namespace PSAmalgamate;
public class Module
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
                    string modulePath = l["using module".Length..].Trim();
                    // check if the module is relative to the current file
                    if (modulePath.StartsWith('.'))
                    {
                        var resolvedModulePath = Path.GetFullPath(Path.Combine(workingDirectory.FullName, modulePath));
                        // check if the file exist
                        if(!File.Exists(resolvedModulePath)){
                            throw new FileNotFoundException($"module not found in the following path: {resolvedModulePath}");
                        }
                        var subModule = await LoadFile(new(resolvedModulePath), workingDirectory);
                        module.RequiredModules.Add(subModule);
                    }
                    break;
                default:
                    break;
            }
        }
        return module;
    }
}