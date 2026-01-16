using System.Diagnostics;

public class LimitRule : IBuildRule
{
    private BuildManager buildManager;

    public LimitRule(BuildManager manager)
    {
        buildManager = manager;
    }

    public bool Validate(ArcContext ctx, out BuildError errorCode)
    {
        if (ctx.Data.installLimit <= 0)
        {
            errorCode = BuildError.None;
            return true;
        }

        int current = buildManager.GetCount(ctx.Data.arcId);

        if (ctx.IgnoreOccupancyInstance != null && ctx.IgnoreOccupancyInstance.ArcId == ctx.Data.arcId)
            current -= 1;

        if (current >= ctx.Data.installLimit)
        {
            errorCode = BuildError.LimitReached;
            return false;
        }

        errorCode = BuildError.None;
        return true;
    }
}
