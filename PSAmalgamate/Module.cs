
using PSAmalgamate.Utils;

namespace PSAmalgamate;
public class Module
{
    internal List<string> _requiredModulePaths = [];
    public required FileInfo FileInfo { get; set; }
    public string Name { get => Path.GetFileNameWithoutExtension(FileInfo.Name); }
    public string FilePath { get => FileInfo.FullName; }
    public List<Module> RequiredModules { get; } = [];
    public List<string> RequiredNamespaces { get; } = [];
    public List<string> RequiredNativeModules { get; } = [];

    public static async Task<Module> LoadModuleInfo(FileInfo file, DirectoryInfo workingDirectory)
    {
        var module = new Module()
        {
            FileInfo = file
        };
        var requiredNamespaces = await LoadRequiredNamespaces(file);
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
        module.RequiredNamespaces.AddRange(requiredNamespaces);
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
                case string l when l.StartsWith('#') || l.StartsWith("using namespace"):
                    continue;
                case string l when l.StartsWith("using module"):
                    string modulePath = l["using module".Length..].Trim();
                    // check if the module is relative to the current file
                    if (modulePath.StartsWith('.'))
                    {
                        // first resolve the module to a path relative to the file that includes it
                        // then resolve it to the specified working directory

                        var resolvedModulePath = new Uri(Path.GetFullPath(modulePath.Replace(nonNativePathSeparator, nativePathSeparator), file.DirectoryName!)).AbsolutePath;
                        resolvedModulePath = Path.GetFullPath(resolvedModulePath, workingDirectory.FullName);

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

    private static async Task<List<string>> LoadRequiredNamespaces(FileInfo file)
    {
        var requiredNamespaces = new List<string>();
        var lines = await File.ReadAllLinesAsync(file.FullName);
        foreach (var line in lines)
        {
            switch (line.Trim())
            {
                case "":
                case null:
                case string l when l.StartsWith('#') || l.StartsWith("using module"):
                    continue;
                case string l when l.StartsWith("using namespace"):
                    var namespaceLine = l["using namespace".Length..].Trim();
                    requiredNamespaces.Add(namespaceLine);
                    break;
                default:
                    break;
            }
        }
        return requiredNamespaces;
    }

    public static ModuleReader GetFilteredTextReader(Module currentModule)
    {
        return new ModuleReader(currentModule.FileInfo);
    }

    public List<Module> GetModuleHierarchy()
    {
        var addedModules = new HashSet<string>();
        return GetModuleHierarchyRecursively(ref addedModules);
    }

    private List<Module> GetModuleHierarchyRecursively(ref HashSet<string> addedModules)
    {
        List<Module> modules = [];
        foreach (var requiredModule in RequiredModules)
        {
            if (addedModules.Contains(requiredModule.FilePath))
            {
                continue;
            }
            modules.AddRange(requiredModule.GetModuleHierarchyRecursively(ref addedModules));
            // if the same module is found again just ignore it
            if (!addedModules.Contains(requiredModule.FilePath))
            {
                modules.Add(requiredModule);
                addedModules.Add(requiredModule.FilePath);
            }
        }
        return modules;
    }


    public class ModuleReader
    {
        PeekableStreamReaderAdapter peekReader;
        public bool CodeSection { get; private set; } = false;
        private bool paramSection = false;

        internal ModuleReader(FileInfo fileInfo)
        {
            var fileStream = File.OpenText(fileInfo.FullName);
            peekReader = new(fileStream);
        }

        public async IAsyncEnumerable<string> ReadLineAsync()
        {
            // read the file line by line

            while (!peekReader.EndOfStream)
            {
                var line = await peekReader.ReadLineAsync() ?? "";
                var tLine = line.TrimStart();
                if (paramSection)
                {
                    yield return line;
                    if (tLine.StartsWith(')'))
                    {
                        paramSection = false;
                        CodeSection = true;
                    }
                    continue;
                }
                if (!CodeSection)
                {
                    // peek ahead to check if the next line is a code section
                    string pLine = await peekReader.PeekLineAsync() ?? "";
                    string ptLine = pLine.TrimStart();

                    // for now we will only support params like this
                    //  param(
                    //      ***
                    //  )
                    // this way it is not required to write a parser for the parameter section
                    if (ptLine.StartsWith("param("))
                    {
                        paramSection = true;
                        continue;
                    }

                    if (!(ptLine.StartsWith("using module") || ptLine.StartsWith("using namespace") || ptLine.StartsWith('#') || ptLine.Length == 0))
                    {
                        CodeSection = true;
                    }
                }
                if (tLine.StartsWith("using module") || tLine.StartsWith("using namespace"))
                {
                    // skip modules
                    continue;
                }
                else if (tLine.StartsWith('#'))
                {
                    // skip requires section, they should be handled outside here
                    if (tLine.StartsWith("#requires"))
                    {
                        continue;
                    }
                    // return comments here as they are not part of the code section
                    yield return line;
                    continue;
                }
                yield return line;
            }
        }
    }

    private static readonly char nativePathSeparator = Path.DirectorySeparatorChar;
    private static readonly char nonNativePathSeparator = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
}