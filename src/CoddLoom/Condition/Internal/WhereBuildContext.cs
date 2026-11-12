using CoddLoom.Params;
using CoddLoom.Sql;
using System.Collections.Generic;

namespace CoddLoom.Condition.Internal;

/// <summary>
/// Carries the parameter materialization of a single WHERE render: every
/// emitted parameter gets a deterministic name and a snapshot value. The
/// caller's <see cref="WhereConditions"/> and <see cref="ValueParam"/>
/// instances are read only; they are never renamed or rewritten, so a
/// condition object can safely be rendered or shared concurrently.
/// </summary>
internal sealed class WhereBuildContext(SqlBuilder builder)
{
    private readonly ParameterNameGenerator _generator = new();
    private readonly List<ValueParam> _parameters = [];

    internal IReadOnlyList<ValueParam> Parameters => _parameters;

    internal ValueParam Snapshot(ValueParam source, bool wrapLikeValue)
    {
        var value = source.Value;
        if (wrapLikeValue)
        {
            // LIKE wrapping is applied to the snapshot handed to the command,
            // never to the caller's original value.
            value = builder.GetLikeParamValue(source.Value.ToString());
        }

        var snapshot = new ValueParam(source.Column, value, _generator.Get(source.Column), source.ForceParameter);
        _parameters.Add(snapshot);
        return snapshot;
    }
}
