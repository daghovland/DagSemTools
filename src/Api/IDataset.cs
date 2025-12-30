/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Datalog;
using IriTools;

namespace DagSemTools.Api;

/// <summary>
/// Represents an RDF dataset. https://www.w3.org/TR/rdf11-datasets/
/// </summary>
public interface IDataset 
{
    /// <summary>
    /// Returns the default, unnamed graph of the dataset.
    /// </summary>
    /// <returns></returns>
  public IGraph GetDefaultGraph();
    /// <summary>
    /// Returns the merged triples of all graphs in the dataset.
    /// Since blank nodes are scoped by graph, this operation will change the blank node names
    /// </summary>
    /// <returns>The RDF graph consisting of all triples in all the graphs in the dataset</returns>
    public IGraph GetMergedTriples();
    /// <summary>
    /// Returns all named graphs in the dataset as a dictionary where the key is the IRI of the graph and the value is the graph itself.
    /// </summary>
    /// <returns></returns>
  public Dictionary<IriReference, IGraph> GetNamedGraphs();

    /// <summary>
    /// Returns an enumerator over all triples in graphName that have the given predicate and object.
    /// Similar to the sparql query "SELECT * WHERE {GRAPH graphName { ?s predicate obj } }".
    /// </summary>
    /// <param name="graphName"></param>
    /// <param name="predicate"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithPredicateObject(IriReference graphName, IriReference predicate, IriReference obj);
    /// <summary>
    /// Returns an enumerator over all triples in graphName that have the given subject and predicate.
    /// Similar to the sparql query "SELECT * WHERE { GRAPH graphName { subject predicate ?o }".
    /// </summary>
    /// <param name="graphName"></param> 
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithSubjectPredicate(IriReference graphName, IriReference subject, IriReference predicate);
    /// <summary>
    /// Returns an enumerator over all triples in graphName that have the given subject.
    /// Similar to the sparql query "SELECT * WHERE { GRAPH graphName { subject ?p ?o } }".
    /// </summary>
    /// <param name="graphName"></param> 
    /// <param name="subject"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithSubject(IriReference graphName, IriReference subject);
    /// <summary>
    /// Returns an enumerator over all triples in graphName that have the given predicate.
    /// Similar to the sparql query "SELECT * WHERE { GRAPH graphName { ?s predicate ?o } }".
    /// </summary>
    /// <param name="graphName"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithPredicate(IriReference graphName, IriReference predicate);
    /// <summary>
    /// Returns an enumerator over all triples in the graph that have the given object.
    /// Similar to the sparql query "SELECT * WHERE { GRAPH graphName { ?s ?p obj } }".
    /// </summary>
    /// <param name="graphName"></param> 
    /// <param name="obj"></param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriplesWithObject(IriReference graphName, IriReference obj);

    /// <summary>
    /// Returns true if and only if the triple is in the Graph
    /// Similar to the sparql query "ASK WHERE { GRAPH graphName { subject predicate object } }".
    /// </summary>
    /// <param name="graphName"></param> 
    /// <param name="triple"></param>
    /// <returns></returns>
    public bool ContainsTriple(IriReference graphName, Triple triple);
    
    /// <summary>
    /// Answers a SPARQL SELECT query over the whole dataset
    /// </summary>
    /// <param name="query"></param>
    /// <returns>An enumerable of solutions. Each solution is a dictionary of the bindings</returns>
    public IEnumerable<Dictionary<string, GraphElement>> AnswerSelectQuery(string query);


    /// <summary>
    /// Loads and runs datalog rules from the file
    /// Note that this materializes new triples in the datastore
    /// </summary>
    /// <param name="datalog">The file with the datalog program</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void LoadDatalog(FileInfo datalog);

    /// <summary>
    /// Loads and runs datalog rules from the file
    /// The rules are added to (not replacing) the existing rules
    /// Note that this adds new triples to the datastore (materialises)
    /// </summary>
    /// <param name="newRules">The new rules to be added</param>
    public void LoadDatalog(IEnumerable<Rule> newRules);

    /// <summary>
    /// Enables OWL 2 RL Reasoning
    /// https://www.w3.org/TR/owl2-profiles/#Reasoning_in_OWL_2_RL_and_RDF_Graphs_using_Rules
    /// Note that this adds new triples to the datastore
    /// </summary>
    public void EnableOwlReasoning();
    

    /// <summary>
    /// Experimental: Enables owl:sameAs reasoning 
    /// https://www.w3.org/TR/owl2-profiles/#Reasoning_in_OWL_2_RL_and_RDF_Graphs_using_Rules
    /// Note that this adds new triples to the datastore
    /// Also it limits the reasoners functionality on negation, since very few programs are stratifiable after these axioms are added
    /// </summary>
    public void EnableEqualityReasoning();


}