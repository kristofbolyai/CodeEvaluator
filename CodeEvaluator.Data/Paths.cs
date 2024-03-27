namespace CodeEvaluator.Data;

public static class Paths
{
    public static readonly string ApplicationDataPath =
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeEvaluator");

    public static readonly string SubmissionsPath = Path.Join(Paths.ApplicationDataPath, "submissions");

    public static readonly string ContainerMountPath = Path.Join(Paths.ApplicationDataPath, "mnt");
}