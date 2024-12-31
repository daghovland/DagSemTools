/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Datalog;
using DagSemTools.OwlOntology;
using DagSemTools.Rdf;
using DagSemTools.RdfOwlTranslator;
using Serilog;

namespace DagSemTools.Api;

/// <summary>
/// C# representation of an OWL 2 Ontology
/// Wrapper around DagSemTools.OwlOntology (which is F#)
/// </summary>
public class OwlOntology
{
    private DagSemTools.OwlOntology.Ontology _owlOntology;
    private Datastore _datastore;
    private ILogger _logger;

    internal OwlOntology(IGraph graph, ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        _datastore = graph.Datastore;
        var translator = new Rdf2Owl(_datastore.Triples, _datastore.Resources);
        _owlOntology = translator.extractOntology;
    }

    /// <summary>
    /// Factory method for creating an owl ontology.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static OwlOntology Create(IGraph graph, ILogger? logger = null)
    {
        return new OwlOntology(graph, logger);
    }

    /// <summary>
    /// Returns all the axioms of the ontology. 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Axiom> GetAxioms() =>
        _owlOntology.Axioms;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Rule> GetAxiomRules() =>
        DagSemTools.OWL2RL2Datalog.Library.owl2Datalog(_logger, _datastore.Resources, _owlOntology);
}