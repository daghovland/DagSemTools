/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.AlcTableau;
using DagSemTools.AlcTableau;
using Serilog;


namespace DagSemTools.Api;

public class TableauReasoner
{
        private readonly ILogger _logger;
        private readonly ALC.OntologyDocument _ontologyDocument;
        private TableauReasoner(ALC.OntologyDocument ontologyDocument, ILogger? logger = null)
        {
            _logger = logger ?? new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            _ontologyDocument = ontologyDocument;
        }

        internal static TableauReasoner Create(ALC.OntologyDocument ontologyDocument, ILogger logger) =>
            new (ontologyDocument, logger);
        

}