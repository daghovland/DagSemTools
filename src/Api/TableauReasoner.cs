/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.AlcTableau;
using DagSemTools.Rdf;
using IriTools;
using LanguageExt;
using Microsoft.FSharp.Collections;
using Serilog;


namespace DagSemTools.Api;

/// <summary>
/// C# representation of the simplest DL Tableau Reasoner 
/// </summary>
public class TableauReasoner
{
    private readonly ILogger _logger;
    private readonly Tableau.ReasonerState _reasoningState;
    private TableauReasoner(Tableau.ReasonerState reasonerState, ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        _reasoningState = reasonerState;
    }

    internal static TableauReasoner Create(Tableau.ReasonerState reasonerState, ILogger logger) =>
        new(reasonerState, logger);

    internal static Func<ALC.Concept, IriResource> GetConceptResource(GraphElementManager elementManager) =>
        (ALC.Concept concept) =>
            concept switch
            {
                ALC.Concept.ConceptName cName => new IriResource(elementManager, cName.Item),
                ALC.Concept.Conjunction conjunction => throw new NotImplementedException(),
                ALC.Concept.Disjunction disjunction => throw new NotImplementedException(),
                ALC.Concept.Existential existential => throw new NotImplementedException(),
                ALC.Concept.Negation negation => throw new NotImplementedException(),
                ALC.Concept.Universal universal => throw new NotImplementedException(),
                _ => throw new NotImplementedException("Unknown concept type: " + concept.GetType().Name + "")
            };

    /// <summary>
    /// Get iris of all types of the individual
    /// </summary>
    /// <param name="elementManager"></param>
    /// <param name="individual"></param>
    /// <returns></returns>
    public IEnumerable<IriResource> GetTypes(GraphElementManager elementManager, IriReference individual) =>
        SeqModule.ToList(ReasonerService
            .get_individual_types(_reasoningState, individual)
            .Select(GetConceptResource(elementManager)));
}