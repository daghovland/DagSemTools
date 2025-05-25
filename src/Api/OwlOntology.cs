/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.AlcTableau;
using DagSemTools.Datalog;
using DagSemTools.OwlOntology;
using DagSemTools.Rdf;
using DagSemTools.RdfOwlTranslator;
using Serilog;
using LanguageExt;

namespace DagSemTools.Api;

/// <summary>
/// C# representation of an OWL 2 Ontology
/// Wrapper around DagSemTools.OwlOntology (which is F#)
/// </summary>
public class OwlOntology
{
    private readonly OntologyDocument _owlOntology;
    private readonly Datastore _datastore;
    private readonly ILogger _logger;

    private OwlOntology(IGraph graph, ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        _datastore = graph.Datastore;
        var translator = new Rdf2Owl(_datastore.Triples, _datastore.Resources, _logger);
        _owlOntology = translator.extractOntology;
    }

    /// <summary>
    /// Factory method for creating an owl ontology.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static OwlOntology Create(IGraph graph, ILogger? logger = null) =>
        new(graph, logger);

    /// <summary>
    /// Returns all the axioms of the ontology. 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Axiom> GetAxioms() =>
        _owlOntology.Ontology.Axioms;

    /// <summary>
    /// Creates a reasoner (service) based on a simple Tableau-based algorithm
    /// </summary>
    /// <returns></returns>
    public Either<TableauReasoner, string> GetTableauReasoner()
    {
        var alc = OWL2ALC.Translator.translateDocument(_logger, _owlOntology);
        var (prefixes, x, (tbox, abox)) = alc.TryGetOntology();
        Tableau.ReasoningResult reasonerstate = ReasonerService.init(tbox, abox);
        return (reasonerstate) switch
        {
            Tableau.ReasoningResult.Consistent consistentState =>
                Either<TableauReasoner, string>.Left(TableauReasoner.Create(consistentState.Item, _logger)),
            Tableau.ReasoningResult.InConsistent inConsistent =>
                Either<TableauReasoner, string>.Right(inConsistent.Item.ToString()),
            _ => throw new NotImplementedException("Unknown reasoner state: " + reasonerstate.GetType().Name + "")
        };

    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Rule> GetAxiomRules() =>
        OWL2RL2Datalog.Library.owl2Datalog(_logger, _datastore.Resources, _owlOntology.Ontology);
}