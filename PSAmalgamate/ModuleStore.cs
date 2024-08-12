namespace PSAmalgamate
{
    // contains a list of modules
    // the modules can be preloaded containing basic information
    // or containing the required information
    public class ModuleStore
    {
        internal Dictionary<string, Module> _modules = [];
        public Dictionary<string, Module> Modules { get => _modules; }

        public static async Task<ModuleStore> LoadModuleList(FileInfo file, DirectoryInfo workingDirectory)
        {
            ModuleStore modulelist = new();
            HashSet<string> unloadedModules = [file.FullName];
            while (unloadedModules.Count != 0)
            {
                HashSet<string> nextUnloadedModules = [];
                foreach (var unloadedModule in unloadedModules)
                {
                    if (modulelist._modules.ContainsKey(unloadedModule))
                    {
                        // ignore the element
                        continue;
                    }
                    var currentModule = new Module()
                    {
                        FileInfo = new(unloadedModule)
                    };
                    currentModule._requiredModulePaths = await Module.LoadRequiredModules(currentModule.FileInfo, workingDirectory);
                    modulelist._modules.Add(currentModule.FileInfo.FullName, currentModule);
                    foreach (var currentNextModule in currentModule._requiredModulePaths)
                    {
                        if (!modulelist.Modules.ContainsKey(currentNextModule))
                        {
                            nextUnloadedModules.Add(currentNextModule);
                        }
                    }
                }
                unloadedModules = nextUnloadedModules;
            }
            return modulelist;
        }

    }
}