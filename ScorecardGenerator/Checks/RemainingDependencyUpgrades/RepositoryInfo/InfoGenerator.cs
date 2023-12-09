namespace ScorecardGenerator.Checks.RemainingDependencyUpgrades.RepositoryInfo;

public static class InfoGenerator
{
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