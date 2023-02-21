using Backup.Shared;

using Microsoft.AspNetCore.Components;

using Radzen;
using Radzen.Blazor;

using System.Security;

namespace BackupRestore.CustomValidator;

public class PathValidator : ValidatorBase
{
    [Parameter]
    public override string Text { get; set; } = "One or more paths are non existent";

    [Parameter]
    public bool CheckForExisting { get; set; } = true;
    [Parameter]
    public bool MultiLine { get; set; } = true;
    [Parameter]
    public bool CheckForDirectory { get; set; } = true;

    protected override bool Validate(IRadzenFormComponent component)
    {
        var value = component.GetValue();
        var valueAsString = value as string;

        if (string.IsNullOrEmpty(valueAsString))
        {
            return true;
        }

        if (MultiLine)
        {
            var paths = valueAsString.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths)
            {
                if (!CheckPath(path))
                    return false;
            }
        }
        else
            return CheckPath(valueAsString);
        return true;
    }

    private bool CheckPath(string path)
    {
        FileSystemInfo? info = null;
        try
        {
            if (CheckForDirectory)
                info = new DirectoryInfo(path);
            else
                info = new System.IO.FileInfo(path);
        }
        catch (ArgumentException)
        {
            Text = $"Invalid path: {path}";
        }
        catch (System.IO.PathTooLongException)
        {
            Text = $"Path too long: {path}";
        }
        catch (NotSupportedException)
        {
            Text = $"Invalid path: {path}";
        }
        catch (SecurityException)
        {
            Text = $"Path can not be accessed: {path}";
        }
        if (info is null)
        {
            return false;
        }

        if (CheckForExisting && !info.Exists)
        {
            Text = $"The Path \"{path}\" does not exist";
            return false;
        }
        if (!Path.IsPathFullyQualified(path) || !Path.IsPathRooted(path))
        {
            Text = $"The Path \"{path}\" is invalid or not absolute";
            return false;
        }
        return true;
    }
}
