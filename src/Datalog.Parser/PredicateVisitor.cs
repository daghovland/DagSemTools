/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

namespace DagSemTools.Datalog.Parser;

using DagSemTools;
using DagSemTools.Rdf;
using DagSemTools.Datalog.Parser;
using static DatalogParser;

/// <inheritdoc />
internal class PredicateVisitor : DatalogBaseVisitor<Term>
{
    internal ResourceVisitor ResourceVisitor { get; }

    /// <inheritdoc />
    internal PredicateVisitor(ResourceVisitor resourceVisitor)
    {
        ResourceVisitor = resourceVisitor;
    }

    /// <inheritdoc />
    public override Term VisitPredicateVerb(DatalogParser.PredicateVerbContext context)
    {
        var resource = ResourceVisitor.Visit(context);
        return Term.NewResource(resource);
    }


    /// <summary>
    /// Visits the abbreviation 'a' for rdf:type
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Term VisitRdfTypeAbbrVerb(RdfTypeAbbrVerbContext context) =>
        Term.NewResource(ResourceVisitor.Visit(context));


    /// <inheritdoc />
    public override Term VisitRdfobject(DatalogParser.RdfobjectContext context)
    {
        return Term.NewResource(ResourceVisitor.Visit(context));
    }

    /// <inheritdoc />
    public override Term VisitVariable(DatalogParser.VariableContext context)
    {
        return Term.NewVariable(context.GetText());
    }
}