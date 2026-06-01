namespace Fixtures;

public static class TestTreeSeeder
{
    public static void CreateSampleTree(string rootPath)
    {
        Directory.CreateDirectory(Path.Combine(rootPath, "alpha"));
        Directory.CreateDirectory(Path.Combine(rootPath, "beta", "nested"));

        File.WriteAllText(Path.Combine(rootPath, "alpha", "one.txt"), new string('a', 512));
        File.WriteAllText(Path.Combine(rootPath, "alpha", "two.txt"), new string('b', 128));
        File.WriteAllText(Path.Combine(rootPath, "beta", "nested", "three.txt"), new string('c', 1024));
    }
}
