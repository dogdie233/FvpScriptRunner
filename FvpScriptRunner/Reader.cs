using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FvpScriptRunner;

public class Reader
{
    public Stream Input { get; init; }
    private Encoding Encoding { get; init; }
    private long StartPosition { get; init; }
    public long Position => Input.Position - StartPosition;

    public Reader(Stream stream, Encoding encoding)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Stream is not readable", nameof(stream));

        Input = stream;
        Encoding = encoding;
        StartPosition = stream.Position;
    }

    public T Read<T>() where T : unmanaged
    {
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
        var read = Input.Read(buffer);
        if (read != buffer.Length)
            throw EndOfStream();
        if (!BitConverter.IsLittleEndian)
            buffer.Reverse();
        return MemoryMarshal.Read<T>(buffer);
    }

    public string ReadString()
    {
        var length = (int)Read<byte>();
        if (length == 0)
            throw new InvalidDataException("String length cannot be zero");

        if (--length == 0)
        {
            Advance(1);
            return string.Empty;
        }

        var buffer = new byte[length];
        var read = Input.Read(buffer);
        Advance(1);
        if (read != length)
            throw EndOfStream();

        return Encoding.GetString(buffer);
    }

    public void Advance(long count)
    {
        Input.Seek(count, SeekOrigin.Current);
    }

    public void Advance<T>() where T : unmanaged
    {
        Advance(Unsafe.SizeOf<T>());
    }

    public void SeekTo(long position)
    {
        Input.Seek(StartPosition + position, SeekOrigin.Begin);
    }

    private static EndOfStreamException EndOfStream() => new();
}
