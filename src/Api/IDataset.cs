/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using IriTools;

namespace DagSemTools.Api;

/// <summary>
/// Represents an RDF dataset. https://www.w3.org/TR/rdf11-datasets/
/// </summary>
public interface IDataset : IGraph
{
    /// <summary>
    /// Returns the default, unnamed graph of the dataset.
    /// </summary>
    /// <returns></returns>
  public IGraph GetDefaultGraph();
/// <summary>
/// Returns the merged triples of all graphs in the dataset.
/// </summary>
/// <returns></returns>
    public IGraph GetMergedTriples();
    /// <summary>
    /// Returns all named graphs in the dataset as a dictionary where the key is the IRI of the graph and the value is the graph itself.
    /// </summary>
    /// <returns></returns>
  public Dictionary<IriReference, IGraph> GetNamedGraphs();

    /// <summary>
    /// Returns an enumerator over all triples in graphName that have the given predicate and object.
    /// Similar to the sparql query "SELECT * WHERE { ?s predicate obj }".
    /// </summary>
    /// <param name="graphName"></param>
    /// <param name="predicate"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference graphName, IriReference predicate, IriReference obj);
    /// <summary>
    /// Returns an enumerator over all triples in graphName that have the given subject and predicate.
    /// Similar to the sparql query "SELECT * WHERE { subject predicate ?o }".
    /// </summary>
    /// <param name="graphName"></param> 
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference graphName, IriReference subject, IriReference predicate);
    /// <summary>
    /// Returns an enumerator over all triples in graphName that have the given subject.
    /// Similar to the sparql query "SELECT * WHERE { subject ?p ?o }".
    /// </summary>
    /// <param name="graphName"></param> 
    /// <param name="subject"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference graphName, IriReference subject);
    /// <summary>
    /// Returns an enumerator over all triples in graphName that have the given predicate.
    /// Similar to the sparql query "SELECT * WHERE { ?s predicate ?o }".
    /// </summary>
    /// <param name="graphName"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference graphName, IriReference predicate);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given object.
    /// Similar to the sparql query "SELECT * WHERE { ?s ?p obj }".
    /// </summary>
    /// <param name="graphName"></param> 
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithObject(IriReference graphName, IriReference obj);

    /// <summary>
    /// Returns true if and only if the triple is in the Graph
    /// Similar to the sparql query "ASK WHERE { subject predicate object }".
    /// </summary>
    /// <param name="graphName"></param> 
    /// <param name="triple"></param>
    /// <returns></returns>
    public bool ContainsTriple(IriReference graphName, Triple triple);

}