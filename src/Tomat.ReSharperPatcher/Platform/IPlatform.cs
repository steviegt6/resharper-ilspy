namespace Tomat.ReSharperPatcher.Platform;

/// <summary>
///     Represents a platform.
/// </summary>
public interface IPlatform
{
    /// <summary>
    ///     Gets the directory in which local data for this application is
    ///     stored.
    /// </summary>
    string GetRspDataDirectory();
}