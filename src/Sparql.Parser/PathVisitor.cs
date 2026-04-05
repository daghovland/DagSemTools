/*
 Copyright (C) 2025 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*/

using DagSemTools.Ingress;
using DagSemTools.Rdf;
using static DagSemTools.Rdf.Ingress;

namespace DagSemTools.Sparql.Parser;

internal class PathVisitor(TermVisitor termVisitor) : SparqlBaseVisitor<Query.Term>
{
    public override Query.Term VisitIri(
        SparqlParser.IriContext context)
        => termVisitor.VisitIri(context);

    // path → pathAlternative (just delegate)
    public override Query.Term VisitPath(SparqlParser.PathContext context)
        => Visit(context.pathAlternative());

    // pathAlternative: pathSequence ( '|' pathSequence )*
    // For now, return the first alternative (simplified — full support requires PropertyPath type)
    public override Query.Term VisitPathAlternative(SparqlParser.PathAlternativeContext context)
        => Visit(context.pathSequence(0));

    // pathSequence: pathEltOrInverse ( '/' pathEltOrInverse )*
    // For now, return the first element (simplified)
    public override Query.Term VisitPathSequence(SparqlParser.PathSequenceContext context)
        => Visit(context.pathEltOrInverse(0));

    // pathEltOrInverse: pathElt | '^' pathElt
    public override Query.Term VisitPathEltOrInverse(SparqlParser.PathEltOrInverseContext context)
        => Visit(context.pathElt());

    // pathElt: pathPrimary pathMod?
    public override Query.Term VisitPathElt(SparqlParser.PathEltContext context)
        => Visit(context.pathPrimary());

    // pathPrimary '(' path ')' #PathGroup
    public override Query.Term VisitPathGroup(SparqlParser.PathGroupContext context)
        => Visit(context.path());
}