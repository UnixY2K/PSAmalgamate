using System.CommandLine;
using PSAmalgamate;

static void WriteError(object? error)
{
    // get the current console color
    var originalColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(error);
    Console.ForegroundColor = originalColor;
}

static void WriteModuleDeps(Module module, string indent = "", bool isLast = true)
{
    var marker = isLast ? "└──" : "├──";
    var originalColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write(indent);
    Console.Write(marker);
    Console.ForegroundColor = originalColor;
    Console.Write(module.Name);
    Console.WriteLine();


    indent += isLast ? "   " : "│  ";

    var lastChild = module.RequiredModules.LastOrDefault();

    foreach (var child in module.RequiredModules)
        WriteModuleDeps(child, indent, child == lastChild);
}
static void WriteModuleHierarchy(Module module)
{
    var moduleHierarchy = module.GetModuleHierarchy();
    foreach (var currentModule in moduleHierarchy)
    {
        Console.WriteLine($" - {currentModule.Name}");
    }
}

var fileOption = new Option<FileInfo?>(
    name: "--file",
    description: "The file to read and amalgamate.",
    isDefault: false,
    parseArgument: result =>
    {
        switch (result.Tokens.Count)
        {
            case 0:
                result.ErrorMessage = "no input file specified";
                return null;
            case > 1:
                result.ErrorMessage = "only one input file can be specified";
                return null;
        }
        string? filePath = result.Tokens.Single().Value;
        if (!File.Exists(filePath))
        {
            result.ErrorMessage = "the specified input file does not exist";
            return null;
        }
        return new FileInfo(filePath);
    }
    )
{ IsRequired = true };
fileOption.AddAlias("-f");


var outputOption = new Option<FileInfo?>(
    name: "--output",
    description: "The file to write the output to.",
    isDefault: false,
    parseArgument: result =>
    {
        switch (result.Tokens.Count)
        {
            case 0:
                result.ErrorMessage = "no output file specified";
                return null;
            case > 1:
                result.ErrorMessage = "only one output file can be specified";
                return null;
        }
        string? filePath = result.Tokens.Single().Value;
        return new FileInfo(filePath);
    }
    )
{ IsRequired = true };
outputOption.AddAlias("-o");

var directoryOption = new Option<DirectoryInfo?>(
    name: "--directory",
    description: "working directory where the script search will resolve to",
    isDefault: true,
    parseArgument: result =>
    {
        switch (result.Tokens.Count)
        {
            case 0:
                result.ErrorMessage = "no working directory specified";
                return null;
            case > 1:
                result.ErrorMessage = "Only one working directory can be specified";
                return null;
        }
        string workingDirectory = result.Tokens.Single().Value;
        if (!Directory.Exists(workingDirectory))
        {
            result.ErrorMessage = "the specified working directory is not a valid directory";
            return null;
        }

        return new DirectoryInfo(Path.GetFullPath(workingDirectory));
    }
    );
directoryOption.AddAlias("-d");
directoryOption.SetDefaultValue(new DirectoryInfo(Directory.GetCurrentDirectory()));

// write module deps switch
var showModuleDepsSwitch = new Option<bool>(
    name: "--show-module-deps",
    description: "Show the module dependencies",
    parseArgument: _ => true
    )
{
    AllowMultipleArgumentsPerToken = false,
    Arity = ArgumentArity.Zero
};


// write module hierarchy switch
var showModuleHierarchySwitch = new Option<bool>(
    name: "--show-module-hierarchy",
    description: "Show the module hierarchy",
        parseArgument: _ => true
    )
{
    AllowMultipleArgumentsPerToken = false,
    Arity = ArgumentArity.Zero
};


var rootCommand = new RootCommand("Amalgamate powershell files into a single file");
rootCommand.AddOption(fileOption);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(directoryOption);
rootCommand.AddOption(showModuleDepsSwitch);
rootCommand.AddOption(showModuleHierarchySwitch);
int returnCode = 0;
rootCommand.SetHandler(async (file, output, directory, showModuleDeps, showModuleHierarchy) =>
{
    if (file is null || output is null || directory is null)
    {
        returnCode = 1;
        return;
    }

    // modules requires to be the first call for the script to work
    // we will only resolve the modules that refer to an specific path
    // for example the current directory (./)
    // iterate over the file and find the modules
    var modules = new List<string>();

    Module rootFile;
    try
    {
        var moduleList = await ModuleStore.LoadModuleStore(file, directory);
        rootFile = moduleList.Modules[file.FullName];
    }
    catch (AggregateException exs)
    {
        WriteError("the following errors ocurred:");
        foreach (var ex in exs.InnerExceptions)
        {
            WriteError($" - {ex.Message}");
        }
        returnCode = 1;
        return;
    }
    catch (Exception ex)
    {
        WriteError("an error ocurred while loading the file");
        WriteError($"{ex.GetType().FullName}: {ex.Message}");
        returnCode = 1;
        return;
    }

    if (showModuleDeps)
    {
        WriteModuleDeps(rootFile);
    }
    if (showModuleHierarchy)
    {
        WriteModuleHierarchy(rootFile);
    }
    // truncate the file
    try
    {
        await File.WriteAllTextAsync(output.FullName, "");
    }
    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
    {
        WriteError($"An error occurred while truncating output file.\n{ex.Message}");
        returnCode = -1;
    }
    catch (Exception ex)
    {
        WriteError($"An unexpected error occurred.\n{ex.Message}");
        returnCode = -1;
    }
    if (returnCode != 0)
    {
        return;
    }
    using (FileStream fs = File.OpenWrite(output.FullName))
    {
        var moduleHierarchy = rootFile.GetModuleHierarchy();

        await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes("### main file pre code section ###\n"));

        await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes("### begin namespace injection ###\n"));
        var namespaces = new HashSet<string>();
        foreach (var currentModule in moduleHierarchy)
        {
            await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"### module {currentModule.Name} ###\n"));
            foreach (var moduleNamespace in currentModule.RequiredNamespaces)
            {
                if (!namespaces.Add(moduleNamespace))
                {
                    await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes("#"));
                }
                await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"using namespace {moduleNamespace}\n"));
            }
        }
        await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes("### end namespace injection ###\n"));

        var rootModuleStream = Module.GetFilteredTextReader(rootFile);
        // iterate until reach code section
        await foreach (var line in rootModuleStream.ReadLineAsync())
        {
            await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes(line + '\n'));
            if (rootModuleStream.CodeSection)
            {
                break;
            }
        }

        await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes("### begin module injection ###\n"));
        foreach (var currentModule in moduleHierarchy)
        {
            await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"### module {currentModule.Name} ###\n"));
            var moduleStream = Module.GetFilteredTextReader(currentModule);
            await foreach (var line in moduleStream.ReadLineAsync())
            {
                await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes(line + '\n'));
            }
        }
        await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes("### end module injection ###\n"));

        await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes("### main file code section ###\n"));

        // now continue with the remaining code section
        await foreach (var line in rootModuleStream.ReadLineAsync())
        {
            await fs.WriteAsync(System.Text.Encoding.UTF8.GetBytes(line + '\n'));
        }

    }

}, fileOption, outputOption, directoryOption, showModuleDepsSwitch, showModuleHierarchySwitch);
await rootCommand.InvokeAsync(args);

return returnCode;

