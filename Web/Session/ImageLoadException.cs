namespace PaintTranslator.Web.Session;

/// <summary>A user-facing failure: the message is shown as-is, so it must be plain English.</summary>
public sealed class ImageLoadException : Exception
{
    public ImageLoadException(string message, Exception? inner = null) : base(message, inner) { }
}
