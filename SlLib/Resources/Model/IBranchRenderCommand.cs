namespace SlLib.Resources.Model;

public interface IBranchRenderCommand : IRenderCommand
{
    public int BranchOffset { get; set; }
}