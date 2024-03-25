namespace CodeEvaluator.Data;

public static class Paths
{
    public static readonly string ApplicationDataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeEvaluator");
}