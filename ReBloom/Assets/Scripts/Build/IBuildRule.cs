public interface IBuildRule
{
    bool Validate(ArcContext ctx, out BuildError errorCode);
}
