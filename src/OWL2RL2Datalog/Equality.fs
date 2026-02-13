(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.OWL2RL2Datalog

open DagSemTools.Datalog
open DagSemTools.Ingress
open DagSemTools.Rdf
open DagSemTools.Rdf.Query
open IriTools


module Equality =

    let internal GetSymmetryAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead (GetDefaultGraphPattern (Term.Variable "x") (Term.Resource owlSameAs) (Term.Variable "y"))
          Body =  [RuleAtom.PositivePattern (GetDefaultGraphPattern (Term.Variable "y") (Term.Resource owlSameAs) (Term.Variable "x"))]}
    
    let internal GetTransitivityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead (GetDefaultGraphPattern (Term.Variable "x") (Term.Resource owlSameAs) (Term.Variable "z"))
          Body =  [RuleAtom.PositivePattern (GetDefaultGraphPattern (Term.Variable "x") (Term.Resource owlSameAs) (Term.Variable "y"));
                   RuleAtom.PositivePattern (GetDefaultGraphPattern (Term.Variable "y") (Term.Resource owlSameAs) (Term.Variable "z"))]
          }
        
        
    let internal GetSubjectEqualityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead (GetDefaultGraphPattern (Term.Variable "s2") (Term.Variable "p") (Term.Variable "o"))
          Body =  [RuleAtom.PositivePattern (GetDefaultGraphPattern (Term.Variable "s1") (Term.Resource owlSameAs) (Term.Variable "s2")) ;
                   RuleAtom.PositivePattern (GetDefaultGraphPattern (Term.Variable "s1") (Term.Variable "p") (Term.Variable "o"))]
          }
    let internal GetObjectEqualityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead (GetDefaultGraphPattern (Term.Variable "s") (Term.Variable "p") (Term.Variable "o2"))
          Body =  [RuleAtom.PositivePattern (GetDefaultGraphPattern (Term.Variable "o1") (Term.Resource owlSameAs) (Term.Variable "o2")) ;
                   RuleAtom.PositivePattern (GetDefaultGraphPattern (Term.Variable "s") (Term.Variable "p") (Term.Variable "o1"))]
          }
        
        
    let GetEqualityAxioms (resources : GraphElementManager) =
        [GetSymmetryAxiom resources; GetSubjectEqualityAxiom resources; GetObjectEqualityAxiom resources; GetTransitivityAxiom resources]