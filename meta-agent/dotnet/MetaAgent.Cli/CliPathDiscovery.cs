using System;
using System.IO;

static class CliPathDiscovery
{
    public static string FindAgentsDir()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 12; i++)
        {
            var direct = Path.Combine(dir.FullName, "agents");
            if (Directory.Exists(direct))
            {
                return direct;
            }

            var nested = Path.Combine(dir.FullName, "meta-agent", "agents");
            if (Directory.Exists(nested))
            {
                return nested;
            }

            if (dir.Parent == null)
            {
                break;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("agents directory not found in repository tree");
    }
}
