using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace PaintTranslator.Input
{
    /// <summary>
    /// A file that exists only inside a drag-and-drop payload rather than on disk.
    /// </summary>
    public sealed class VirtualFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualFile"/> class.
        /// </summary>
        /// <param name="name">The file name the source application supplied.</param>
        /// <param name="content">The complete bytes of the file.</param>
        public VirtualFile(string name, byte[] content)
        {
            Name = name;
            Content = content;
        }

        /// <summary>
        /// Gets the file name the source application supplied.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the complete bytes of the file.
        /// </summary>
        public byte[] Content { get; }
    }

    /// <summary>
    /// Pulls file bytes out of a drag-and-drop payload that carries its content in memory
    /// instead of pointing at a path on disk. Browsers use this to hand over an image
    /// dragged off a web page, which is what makes such a drop work without re-fetching
    /// the image and without the URL even being resolvable.
    /// </summary>
    /// <remarks>
    /// The managed <see cref="IDataObject"/> wrapper cannot express the index needed to
    /// address one file among several, so this has to go through the COM interface
    /// directly. The interop is confined to this file.
    /// </remarks>
    public static class VirtualFileDataReader
    {
        /// <summary>
        /// The clipboard format naming the files in the payload and their metadata.
        /// </summary>
        private const string FileGroupDescriptorFormat = "FileGroupDescriptorW";

        /// <summary>
        /// The clipboard format holding the bytes of one file from the payload.
        /// </summary>
        private const string FileContentsFormat = "FileContents";

        /// <summary>
        /// The byte length of a single FILEDESCRIPTORW record: flags, class id, extents,
        /// attributes, three timestamps, the 64-bit size, then a fixed 260-character name.
        /// </summary>
        private const int FileDescriptorSize = 592;

        /// <summary>
        /// The offset of the file name within a FILEDESCRIPTORW record.
        /// </summary>
        private const int FileDescriptorNameOffset = 72;

        /// <summary>
        /// The maximum number of characters in a FILEDESCRIPTORW file name.
        /// </summary>
        private const int FileDescriptorNameLength = 260;

        /// <summary>
        /// Tells the caller whether a payload carries virtual files at all, so a drag can
        /// be accepted or rejected without doing the work of extracting anything.
        /// </summary>
        /// <param name="data">The drag-and-drop or clipboard payload.</param>
        /// <returns>True when the payload describes at least one virtual file.</returns>
        public static bool IsPresent(IDataObject data)
        {
            return data != null
                && data.GetDataPresent(FileGroupDescriptorFormat)
                && data.GetDataPresent(FileContentsFormat);
        }

        /// <summary>
        /// Extracts the first virtual file from a payload.
        /// </summary>
        /// <param name="data">The drag-and-drop or clipboard payload.</param>
        /// <returns>The first file in the payload, or null when the payload carries no
        /// virtual files or its content could not be read.</returns>
        public static VirtualFile TryReadFirst(IDataObject data)
        {
            if (!IsPresent(data))
            {
                return null;
            }

            string[] names = ReadFileNames(data);
            if (names == null || names.Length == 0)
            {
                return null;
            }

            byte[] content = TryReadContent(data, 0);
            return content == null ? null : new VirtualFile(names[0], content);
        }

        /// <summary>
        /// Reads the file names out of the payload's group descriptor.
        /// </summary>
        /// <param name="data">The drag-and-drop or clipboard payload.</param>
        /// <returns>The declared file names, or null when the descriptor is missing or
        /// malformed.</returns>
        private static string[] ReadFileNames(IDataObject data)
        {
            if (!(data.GetData(FileGroupDescriptorFormat) is MemoryStream stream))
            {
                return null;
            }

            using (stream)
            {
                byte[] buffer = stream.ToArray();

                // The descriptor opens with a 4-byte count, then one fixed-size record
                // per file.
                if (buffer.Length < 4)
                {
                    return null;
                }

                int count = BitConverter.ToInt32(buffer, 0);
                if (count <= 0 || buffer.Length < 4 + (count * FileDescriptorSize))
                {
                    return null;
                }

                var names = new string[count];
                for (int i = 0; i < count; i++)
                {
                    int nameStart = 4 + (i * FileDescriptorSize) + FileDescriptorNameOffset;

                    // The name field is a fixed-width UTF-16 buffer padded with nulls, so
                    // the real name ends at the first null rather than at the field end.
                    string padded = Encoding.Unicode.GetString(buffer, nameStart, FileDescriptorNameLength * 2);
                    int end = padded.IndexOf('\0');
                    names[i] = end >= 0 ? padded.Substring(0, end) : padded;
                }

                return names;
            }
        }

        /// <summary>
        /// Reads the bytes of one file from the payload.
        /// </summary>
        /// <param name="data">The drag-and-drop or clipboard payload.</param>
        /// <param name="index">The zero-based position of the file within the payload.</param>
        /// <returns>The file's bytes, or null when the source refused to provide them.</returns>
        private static byte[] TryReadContent(IDataObject data, int index)
        {
            if (!(data is ComTypes.IDataObject comData))
            {
                return null;
            }

            var request = new ComTypes.FORMATETC
            {
                cfFormat = (short)DataFormats.GetFormat(FileContentsFormat).Id,
                dwAspect = ComTypes.DVASPECT.DVASPECT_CONTENT,
                lindex = index,
                ptd = IntPtr.Zero,

                // Sources choose their own storage; browsers hand back a stream, while
                // some other applications allocate global memory instead.
                tymed = ComTypes.TYMED.TYMED_ISTREAM | ComTypes.TYMED.TYMED_HGLOBAL,
            };

            ComTypes.STGMEDIUM medium = default;
            try
            {
                comData.GetData(ref request, out medium);
            }
            catch (Exception)
            {
                // A source that cannot satisfy the request throws rather than returning a
                // failure code; the caller falls through to the next payload format.
                return null;
            }

            try
            {
                switch (medium.tymed)
                {
                    case ComTypes.TYMED.TYMED_ISTREAM:
                        return ReadStream(medium.unionmember);

                    case ComTypes.TYMED.TYMED_HGLOBAL:
                        return ReadGlobalMemory(medium.unionmember);

                    default:
                        return null;
                }
            }
            finally
            {
                ReleaseStgMedium(ref medium);
            }
        }

        /// <summary>
        /// Copies the full contents of a COM stream into an array.
        /// </summary>
        /// <param name="streamPointer">An unmanaged pointer to the stream.</param>
        /// <returns>The stream's bytes, or null when the pointer is null.</returns>
        private static byte[] ReadStream(IntPtr streamPointer)
        {
            if (streamPointer == IntPtr.Zero)
            {
                return null;
            }

            var stream = (ComTypes.IStream)Marshal.GetObjectForIUnknown(streamPointer);
            try
            {
                stream.Stat(out ComTypes.STATSTG stat, StatFlagNoName);

                // A drag payload has to fit in memory to be decoded anyway, so anything
                // claiming to exceed the array limit is rejected rather than truncated.
                if (stat.cbSize <= 0 || stat.cbSize > int.MaxValue)
                {
                    return null;
                }

                // The source may hand back a stream that has already been read from.
                stream.Seek(0, StreamSeekSet, IntPtr.Zero);

                var buffer = new byte[stat.cbSize];
                IntPtr bytesRead = Marshal.AllocCoTaskMem(sizeof(int));
                try
                {
                    // Streams are free to satisfy a read partially, so keep going until
                    // the buffer is full or the source stops producing bytes.
                    int total = 0;
                    while (total < buffer.Length)
                    {
                        var chunk = new byte[buffer.Length - total];
                        stream.Read(chunk, chunk.Length, bytesRead);

                        int count = Marshal.ReadInt32(bytesRead);
                        if (count <= 0)
                        {
                            break;
                        }

                        Buffer.BlockCopy(chunk, 0, buffer, total, count);
                        total += count;
                    }

                    if (total == 0)
                    {
                        return null;
                    }

                    if (total < buffer.Length)
                    {
                        Array.Resize(ref buffer, total);
                    }

                    return buffer;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(bytesRead);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(stream);
            }
        }

        /// <summary>
        /// Copies the contents of a global memory block into an array.
        /// </summary>
        /// <param name="handle">The global memory handle.</param>
        /// <returns>The block's bytes, or null when the handle is null or cannot be locked.</returns>
        private static byte[] ReadGlobalMemory(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            IntPtr pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                int size = (int)GlobalSize(handle);
                if (size <= 0)
                {
                    return null;
                }

                var buffer = new byte[size];
                Marshal.Copy(pointer, buffer, 0, size);
                return buffer;
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }

        /// <summary>
        /// The STATFLAG_NONAME value, which skips allocating the stream's name.
        /// </summary>
        private const int StatFlagNoName = 1;

        /// <summary>
        /// The STREAM_SEEK_SET value, meaning an offset from the start of the stream.
        /// </summary>
        private const int StreamSeekSet = 0;

        /// <summary>
        /// Frees the storage a completed <c>GetData</c> call handed back.
        /// </summary>
        /// <param name="medium">The medium to release.</param>
        [DllImport("ole32.dll")]
        private static extern void ReleaseStgMedium(ref ComTypes.STGMEDIUM medium);

        /// <summary>
        /// Locks a global memory block and returns a pointer to its contents.
        /// </summary>
        /// <param name="handle">The global memory handle.</param>
        /// <returns>A pointer to the block, or zero on failure.</returns>
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr handle);

        /// <summary>
        /// Releases a lock taken by <see cref="GlobalLock"/>.
        /// </summary>
        /// <param name="handle">The global memory handle.</param>
        /// <returns>True while the block remains locked by another caller.</returns>
        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr handle);

        /// <summary>
        /// Reports the byte size of a global memory block.
        /// </summary>
        /// <param name="handle">The global memory handle.</param>
        /// <returns>The size in bytes, or zero on failure.</returns>
        [DllImport("kernel32.dll")]
        private static extern UIntPtr GlobalSize(IntPtr handle);
    }
}
