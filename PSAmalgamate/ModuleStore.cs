namespace PSAmalgamate
{
    // contains a list of modules
    // the modules can be preloaded containing basic information
    // or containing the required information
    public class ModuleStore
    {
        internal Dictionary<string, Module> _modules = [];
        public Dictionary<string, Module> Modules { get => _modules; }

        public static async Task<ModuleStore> LoadModuleStore(FileInfo file, DirectoryInfo workingDirectory)
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
                    var currentModule = await Module.LoadModuleInfo(new FileInfo(unloadedModule), workingDirectory);
                    modulelist._modules.Add(currentModule.FilePath, currentModule);
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
            modulelist.LoadDependencyTree();
            return modulelist;
        }

        public void LoadDependencyTree()
        {
            foreach (var module in _modules)
            {
                foreach (var requiredModule in module.Value._requiredModulePaths)
                {
                    // at this point all the required modules should be loaded
                    module.Value.RequiredModules.Add(_modules[requiredModule]);
                }
            }
        }
    }
}