namespace ScorecardGenerator.Checks.PendingRenovateAzurePRs;

public class InfoGenerator
{
    public interface IInfo
    {
        static bool FromURL(string url, out IInfo? info)
        {
            info = null;
            return false;
        }
    }

    private class AzureInfo : IInfo
    {
        private string _organization;
        private string _project;
        private string _repo;

        private AzureInfo(string organization, string project, string repo)
        {
            _organization = organization;
            _project = project;
            _repo = repo;
        }

        static bool FromURL(string url, out IInfo? info)
        {
            if (url.Contains("visualstudio"))
            {
                var pathSplit = url.Split('/');
                var gitIndex = Array.IndexOf(pathSplit, "_git");
                info = new AzureInfo
                (
                    pathSplit[gitIndex - 2], 
                    pathSplit[gitIndex - 1], 
                    pathSplit[gitIndex + 1]
                );
                return true;
            }
            
            if (url.Contains("dev.azure"))
            {
                var pathSplit = url.Split('/');
                var gitIndex = Array.IndexOf(pathSplit, "_git");
                info = new AzureInfo
                (
                    pathSplit[gitIndex - 2], 
                    pathSplit[gitIndex - 1], 
                    pathSplit[gitIndex + 1]
                );
                return true;
            }
            
            info = null;
            return false;
        }
    };

    private class GitHubInfo : IInfo
    {
        private string _organization;
        private string _repo;

        private GitHubInfo(string organization, string repo)
        {
            _organization = organization;
            _repo = repo;
        }

        public static bool FromURL(string url, out IInfo? info)
        {
            if (!url.Contains("github"))
            {
                info = null;
                return false;
            }
            var pathSplit = url.Split('/');
            var gitIndex = Array.IndexOf(pathSplit, "github.com");
            info = new GitHubInfo
            (
                pathSplit[gitIndex + 1], 
                pathSplit[gitIndex + 2]
            );
            return true;

        }
    }

    public static IInfo FromURL(string url)
    {
        // find all private classes deriving from IInfo and call FromURL on them; return the first non-null result
        var types = typeof(IInfo).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IInfo)));
        foreach (var type in types)
        {
            var method = type.GetMethod("FromURL");
            if (method == null)
            {
                continue;
            }
            var result = method.Invoke(null, new object[] {url});
            if (result != null)
            {
                return (IInfo)result;
            }
        }
        throw new Exception("No IInfo implementation found for URL: " + url);
    }
}