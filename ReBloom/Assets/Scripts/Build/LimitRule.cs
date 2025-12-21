using System.Diagnostics;

public class LimitRule : IBuildRule
{
    private BuildManager buildManager;

    public LimitRule(BuildManager manager)
    {
        buildManager = manager;
    }

    public bool Validate(ArcContext ctx, out string errorCode)
    {
        if (ctx.Data.installLimit <= 0)
        {
            errorCode = null;
            return true;
        }

        int current = buildManager.GetCount(ctx.Data.arcId);

        if (ctx.IgnoreOccupancyInstance != null && ctx.IgnoreOccupancyInstance.ArcId == ctx.Data.arcId)
            current -= 1;

        if (current >= ctx.Data.installLimit)
        {
            errorCode = "LIMIT_REACHED";
            return false;
        }

        errorCode = null;
        return true;
    }
}
