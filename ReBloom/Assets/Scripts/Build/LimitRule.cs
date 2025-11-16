public class LimitRule : IBuildRule
{
    private BuildManager buildManager;

    public LimitRule(BuildManager manager)
    {
        buildManager = manager;
    }

    public bool Validate(ArcContext ctx, out string errorCode)
    {
        int current = buildManager.GetCount(ctx.Data.arcId);
        if (current >= ctx.Data.installLimit)
        {
            errorCode = "LIMIT_REACHED";
            return false;
        }

        errorCode = null;
        return true;
    }
}
