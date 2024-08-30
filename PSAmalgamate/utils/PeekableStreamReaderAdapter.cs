namespace PSAmalgamate.Utils;

public class PeekableStreamReaderAdapter(StreamReader underlying)
{
    private readonly StreamReader Underlying = underlying;
    private readonly Queue<string> BufferedLines = new();

    public bool EndOfStream { get => Underlying.EndOfStream && BufferedLines.Count == 0; }

    public async Task<string?> PeekLineAsync()
    {
        string? line = await Underlying.ReadLineAsync();
        if (line == null)
            return null;
        BufferedLines.Enqueue(line);
        return line;
    }


    public async Task<string?> ReadLineAsync()
    {
        if (BufferedLines.Count > 0)
            return BufferedLines.Dequeue();
        return await Underlying.ReadLineAsync();
    }
}