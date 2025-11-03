public static class ExternalId
{
    public static string ForGitHub(long githubUserId)
        => $"github:{githubUserId}";
}