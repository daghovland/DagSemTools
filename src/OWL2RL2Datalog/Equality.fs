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
open IriTools


module Equality =

    let internal GetSymmetryAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead ({Subject = Term.Variable "x"
                              Predicate = Term.Resource owlSameAs
                              Object = Term.Variable "y"
                            })
          Body =  [PositiveTriple ({Subject = Term.Variable "y"
                                    Predicate = Term.Resource owlSameAs
                                    Object = Term.Variable "x"
                     })]}
    
    let internal GetTransitivityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead ({Subject = Term.Variable "x"
                              Predicate = Term.Resource owlSameAs
                              Object = Term.Variable "z"
                            })
          Body =  [PositiveTriple ({Subject = Term.Variable "x"
                                    Predicate = Term.Resource owlSameAs
                                    Object = Term.Variable "y"
                     });
                     PositiveTriple ({Subject = Term.Variable "y"
                                                Predicate = Term.Resource owlSameAs
                                                Object = Term.Variable "z"
                                 })
          
          ]
          }
        
        
    let internal GetSubjectEqualityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead ({Subject = Term.Variable "s2"
                              Predicate = Term.Variable "p"
                              Object = Term.Variable "o"
                            })
          Body =  [PositiveTriple ({Subject = Term.Variable "s1"
                                    Predicate = Term.Resource owlSameAs
                                    Object = Term.Variable "s2"
                     }) ;
                    PositiveTriple {Subject = Term.Variable "s1"
                                    Predicate = Term.Variable "p"
                                    Object = Term.Variable "o"
                            }
                ]
          }
    let internal GetObjectEqualityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead ({Subject = Term.Variable "s"
                              Predicate = Term.Variable "p"
                              Object = Term.Variable "o2"
                            })
          Body =  [PositiveTriple ({Subject = Term.Variable "o1"
                                    Predicate = Term.Resource owlSameAs
                                    Object = Term.Variable "o2"
                     }) ;
                    PositiveTriple {Subject = Term.Variable "s"
                                    Predicate = Term.Variable "p"
                                    Object = Term.Variable "o1"
                            }
                ]
          }
        
        
    let GetEqualityAxioms (resources : GraphElementManager) =
        [GetSymmetryAxiom resources; GetSubjectEqualityAxiom resources; GetObjectEqualityAxiom resources; GetTransitivityAxiom resources]