public interface IBuildRule
{
    bool Validate(ArcContext ctx, out string errorCode);
}
