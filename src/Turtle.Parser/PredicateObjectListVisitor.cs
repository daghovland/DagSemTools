/*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*/

using static DagSemTools.Rdf.Ingress;

namespace DagSemTools.Turtle.Parser;

internal class PredicateObjectListVisitor : TriGDocBaseVisitor<Func<uint, List<Triple>>>
{
    private ResourceVisitor _resourceVisitor;

    internal PredicateObjectListVisitor(ResourceVisitor resourceVisitor)
    {
        _resourceVisitor = resourceVisitor;
    }

    /// <summary>
    /// Visits the grammar rule predicateObjectList used in https://www.w3.org/TR/rdf12-turtle/#grammar-production-blankNodePropertyList
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Func<uint, List<Triple>> VisitPredicateObjectList(
        TriGDocParser.PredicateObjectListContext context) =>
        (node) =>
            context.verbObjectList()
                .SelectMany(vo => VisitVerbObjectList(vo)(node))
                .ToList();

    /// <summary>
    /// Used for the aggregate calculation of a annotation. 
    /// </summary>
    /// <param name="_reifierNodeId"></param>
    /// <param name="_triples"></param>
    /// <param name="_tripleIds"></param>
    private struct AnnotationStatus(uint _reifierNodeId, IEnumerable<Triple> _triples, IEnumerable<uint> _tripleIds)
    {
        internal uint reifierNodeId = _reifierNodeId;
        internal IEnumerable<Triple> triples = _triples;
        internal IEnumerable<uint> tripleIds = _tripleIds;
    }

    private void HandleAnnotation((TriGDocParser.AnnotationContext? annot, Triple triple) rdfobj)
    {
        if (rdfobj.annot != null && rdfobj.annot.children != null)
        {
            var reifications = rdfobj.annot.children
                .Aggregate(seed: new AnnotationStatus(_resourceVisitor.Datastore.NewAnonymousBlankNode(), new List<Triple>(), new List<uint>()),
                    func: (aggr, child) => child switch
                    {
                        TriGDocParser.PredicateObjectListContext predobj => HandlePredicateObjectReification(predobj, aggr),
                        TriGDocParser.ReifierContext reif => HandleReification(reif, rdfobj.triple, aggr),
                        _ => aggr
                    });
            reifications.triples.ToList().ForEach(_resourceVisitor.Datastore.AddTriple);
            reifications.tripleIds.ToList().ForEach(tripleId => _resourceVisitor.Datastore.AddReifiedTriple(rdfobj.triple, tripleId));
        }
    }
    private AnnotationStatus HandlePredicateObjectReification(TriGDocParser.PredicateObjectListContext predobj, AnnotationStatus aggr)
    {
        var newTriples = VisitPredicateObjectList(predobj)(aggr.reifierNodeId);
        return new AnnotationStatus(aggr.reifierNodeId, aggr.triples.Concat(newTriples), aggr.tripleIds);
    }
    private AnnotationStatus HandleReification(TriGDocParser.ReifierContext reif, Triple triple, AnnotationStatus aggr)
    {
        var reifier = _resourceVisitor.Visit(reif);
        return new AnnotationStatus(reifier, aggr.triples, aggr.tripleIds.Append(reifier));
    }


    /// <summary>
    /// Visits the grammar rule verbObjectList which is a merging of predicateObjectList and objectLists in https://www.w3.org/TR/rdf12-turtle
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Func<uint, List<Triple>> VisitVerbObjectList(
        TriGDocParser.VerbObjectListContext context) =>
        (node) =>
        {
            var predicate = _resourceVisitor.Visit(context.verb());
            var tripes = context.annotatedObject()
                .Select(rdfObj => (annot: rdfObj.annotation(), obj: _resourceVisitor.Visit(rdfObj.rdfobject())))
                .Select(obj => (annot: obj.annot, triple: new Triple(node, predicate, obj.obj)))
                .ToList();
            tripes
                .ForEach(HandleAnnotation);
            return tripes.Select(triple => triple.triple).ToList();
        };

}