using System.Collections.Generic;

namespace CoddLoom.Condition.Internal;

/// <summary>
/// Generates deterministic parameter names for one WHERE render. Names are a
/// pure function of the emission order, so independent walks of the same
/// condition tree (SQL rendering and parameter materialization) always agree.
/// </summary>
internal sealed class ParameterNameGenerator
{
    private readonly Dictionary<string, int> _paramNameIndex = new();

    public string Get(string paramName)
    {
        if (paramName.Contains("."))
        {
            paramName = paramName.Replace('.', '_');
        }

        if (!_paramNameIndex.ContainsKey(paramName))
        {
            _paramNameIndex.Add(paramName, 0);
        }
        else
        {
            _paramNameIndex[paramName] += 1;
        }

        return $"{paramName}{_paramNameIndex[paramName]}";
    }
}
