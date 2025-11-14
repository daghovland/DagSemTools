/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

namespace DagSemTools.Api;
using IriTools;

/// <summary>
/// Represents a resource that is identified by an IRI.
/// </summary>
public class IriResource : Resource
{
    /// <summary>
    /// The IRI that identifies the resource.
    /// </summary>
    public IriReference Iri { get; }

    /// <inheritdoc />
    public IriResource(IriReference iri)
    {
        Iri = iri ?? throw new ArgumentNullException(nameof(iri));
    }

    /// <summary>
    /// Two Iri resources are equal if their IRIs are equal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(GraphElement? other) =>
        other != null && (ReferenceEquals(this, other) ||
                          (other is IriResource iri && iri.Iri.Equals(Iri)));


    /// <inheritdoc />
    public override string ToString()
    {
        return Iri.ToString();
    }

    /// <summary>
    /// Two Iri resources are equal if their IRIs are equal.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj) =>
        obj != null && obj is GraphElement r && Equals(r);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Iri.GetHashCode();
    }
}
