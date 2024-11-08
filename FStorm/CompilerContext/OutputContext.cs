using System;

namespace FStorm;

public class OutputContext : IOutputContext
{
    private OutputKind outputKind;
    private EdmPath? resourcePath = null;
    private EdmEntityType? resourceEdmType = null;
    public void SetOutputKind(OutputKind OutputType) { outputKind = OutputType; }
    public OutputKind GetOutputKind() => outputKind;
    public void SetOutputPath(EdmPath ResourcePath)
    {
        resourcePath = ResourcePath;
        SetOutputType(ResourcePath.GetEdmEntityType());
    }
    public EdmPath? GetOutputPath() => resourcePath;
    public EdmEntityType? GetOutputType() => resourceEdmType;
    public void SetOutputType(EdmEntityType? ResourceEdmType) { resourceEdmType = ResourceEdmType; }
}
