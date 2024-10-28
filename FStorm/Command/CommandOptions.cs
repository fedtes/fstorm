using System;

namespace FStorm;

public class CommandOptions
{
    /// <summary>
    /// Default command execution timeout
    /// </summary>
    internal uint? CommandTimeout { get; set; }

    /// <summary>
    /// Default $top value if not speficied in the request
    /// </summary>
    internal uint? DefaultTopRequest { get; set; }

    /// <summary>
    /// If true ignore the <see cref="DefaultTopRequest"/> parameters. If $top is specified in the request then it is NOT ignored. 
    /// </summary>
    internal bool? BypassDefaultTopRequest { get; set; }
}
