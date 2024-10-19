using System.Runtime.InteropServices;
using System.Text;

class UnixRawStdinReader
{
    private const int STDIN_FILENO = 0;

    private const int EAGAIN = 11;
    
    private const int BytesToBeRead = 1024;

    private Encoding encoding;

    private byte[] bytesBufferToBeRead;

    private char[] unprocessedBuffer;    

    private int unprocessedBufferStartIndex;

    private int unprocessedBufferEndIndex;

    public UnixRawStdinReader(Encoding encoding)
    {
        this.encoding = encoding;

        bytesBufferToBeRead = new byte[BytesToBeRead];
        unprocessedBuffer = new char[encoding.GetMaxCharCount(BytesToBeRead)];
        unprocessedBufferStartIndex = 0;
        unprocessedBufferEndIndex = 0;
    }    

    public char ReadChar() 
    {
        if (unprocessedBufferStartIndex >= unprocessedBufferEndIndex)
        {
            while (true)
            {
                int bytesRead = read(STDIN_FILENO, bytesBufferToBeRead, BytesToBeRead);
                if (bytesRead == -1)
                {
                    var errorCode = Marshal.GetLastSystemError();
                    if (errorCode != EAGAIN)
                    {
                        throw new Exception($"read failed with code: {errorCode}.");
                    }

                    continue;
                }

                if (bytesRead > 0)
                {
                    unprocessedBufferStartIndex = 0;
                    unprocessedBufferEndIndex = encoding.GetChars(bytesBufferToBeRead, 0, bytesRead, unprocessedBuffer, 0);
                    break;
                }
            }
        }

        return unprocessedBuffer[unprocessedBufferStartIndex++];
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int read(int fd, byte[] buf, int count);
}