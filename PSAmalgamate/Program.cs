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

var rootCommand = new RootCommand("Amalgamate powershell files into a single file");
rootCommand.AddOption(fileOption);
rootCommand.AddOption(outputOption);
int returnCode = 0;
rootCommand.SetHandler(async (file, output) =>
{
    if (file is null || output is null)
    {
        returnCode = 1;
        return;
    }


    var content = await File.ReadAllTextAsync(file.FullName);
    await File.WriteAllTextAsync(output.FullName, content);
}, fileOption, outputOption);

await rootCommand.InvokeAsync(args);

return returnCode;

