using System;
using System.IO;

namespace Tomat.ReSharperPatcher.Platform;

internal sealed class WindowsPlatform : IPlatform
{
    private const string rsp_data_directory_name = "resharper-patcher";

    string IPlatform.GetRspDataDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), rsp_data_directory_name);
    }
}