/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/
using IriTools;
using DagSemTools.Datalog;
using DagSemTools.Rdf;

namespace DagSemTools.Api;

/// <summary>
/// Represents an RDF Graph https://www.w3.org/TR/rdf12-concepts/
/// </summary>
public interface IGraph
{
    /// <summary>
    /// Returns true if the graph is empty, that is, has no triples.
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty();
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given predicate and object.
    /// Similar to the sparql query "SELECT * WHERE { ?s predicate obj }".
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference predicate, IriReference obj);

    /// <summary>
    /// Factory method for creating an owl ontology.
    /// </summary>
    /// <returns></returns>
    public OwlOntology ParseToOntology();

    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given subject and predicate.
    /// Similar to the sparql query "SELECT * WHERE { subject predicate ?o }".
    /// </summary>
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference subject, IriReference predicate);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given subject.
    /// Similar to the sparql query "SELECT * WHERE { subject ?p ?o }".
    /// </summary>
    /// <param name="subject"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference subject);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given predicate.
    /// Similar to the sparql query "SELECT * WHERE { ?s predicate ?o }".
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference predicate);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given object.
    /// Similar to the sparql query "SELECT * WHERE { ?s ?p obj }".
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithObject(IriReference obj);

    /// <summary>
    /// Returns true if and only if the triple is in the Graph
    /// Similar to the sparql query "ASK WHERE { subject predicate object }".
    /// </summary>
    /// <param name="triple"></param>
    /// <returns></returns>
    public bool ContainsTriple(Triple triple);



}