using System.CommandLine;


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
    ){ IsRequired = true };
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
    ){ IsRequired = true };
outputOption.AddAlias("-o");

var directoryOption = new Option<DirectoryInfo?>(
    name: "--directory", 
    description: "working directory where the script search will resolve to",
    isDefault: true,
    parseArgument: result => {
        switch(result.Tokens.Count){
            case 0: 
                result.ErrorMessage = "no working directory specified";
                return null;
            case > 1:
                result.ErrorMessage = "Only one working directory can be specified";
                return null;
        }
        string workingDirectory = result.Tokens.Single().Value;
        if(!Directory.Exists(workingDirectory)){
            result.ErrorMessage = "the specified working directory is not a valid directory";
            return null;
        }
        
        return new DirectoryInfo(Path.GetFullPath(workingDirectory));
    }
    );
directoryOption.AddAlias("-d");
directoryOption.SetDefaultValue(new DirectoryInfo(Directory.GetCurrentDirectory()));

var rootCommand = new RootCommand("Amalgamate powershell files into a single file");
rootCommand.AddOption(fileOption);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(directoryOption);
int returnCode = 0;
rootCommand.SetHandler(async (file, output, directory) =>
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

    Console.WriteLine("included modules");
    Module rootFile = await Module.LoadFile(file, directory);
    Console.WriteLine(rootFile.Name);

    // truncate the file
    await File.WriteAllTextAsync(output.FullName, "");

}, fileOption, outputOption, directoryOption);

await rootCommand.InvokeAsync(args);

return returnCode;

