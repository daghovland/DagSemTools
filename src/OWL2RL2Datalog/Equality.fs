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
        { Head = NormalHead ({Subject = ResourceOrVariable.Variable "x"
                              Predicate = ResourceOrVariable.Resource owlSameAs
                              Object = ResourceOrVariable.Variable "y"
                            })
          Body =  [PositiveTriple ({Subject = ResourceOrVariable.Variable "y"
                                    Predicate = ResourceOrVariable.Resource owlSameAs
                                    Object = ResourceOrVariable.Variable "x"
                     })]}
    
    let internal GetTransitivityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead ({Subject = ResourceOrVariable.Variable "x"
                              Predicate = ResourceOrVariable.Resource owlSameAs
                              Object = ResourceOrVariable.Variable "z"
                            })
          Body =  [PositiveTriple ({Subject = ResourceOrVariable.Variable "x"
                                    Predicate = ResourceOrVariable.Resource owlSameAs
                                    Object = ResourceOrVariable.Variable "y"
                     });
                     PositiveTriple ({Subject = ResourceOrVariable.Variable "y"
                                                Predicate = ResourceOrVariable.Resource owlSameAs
                                                Object = ResourceOrVariable.Variable "z"
                                 })
          
          ]
          }
        
        
    let internal GetSubjectEqualityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead ({Subject = ResourceOrVariable.Variable "s2"
                              Predicate = ResourceOrVariable.Variable "p"
                              Object = ResourceOrVariable.Variable "o"
                            })
          Body =  [PositiveTriple ({Subject = ResourceOrVariable.Variable "s1"
                                    Predicate = ResourceOrVariable.Resource owlSameAs
                                    Object = ResourceOrVariable.Variable "s2"
                     }) ;
                    PositiveTriple {Subject = ResourceOrVariable.Variable "s1"
                                    Predicate = ResourceOrVariable.Variable "p"
                                    Object = ResourceOrVariable.Variable "o"
                            }
                ]
          }
    let internal GetObjectEqualityAxiom (resources : GraphElementManager) =
        let owlSameAs = (resources.AddNodeResource( Iri(new IriReference (Namespaces.OwlSameAs))))
        { Head = NormalHead ({Subject = ResourceOrVariable.Variable "s"
                              Predicate = ResourceOrVariable.Variable "p"
                              Object = ResourceOrVariable.Variable "o2"
                            })
          Body =  [PositiveTriple ({Subject = ResourceOrVariable.Variable "o1"
                                    Predicate = ResourceOrVariable.Resource owlSameAs
                                    Object = ResourceOrVariable.Variable "o2"
                     }) ;
                    PositiveTriple {Subject = ResourceOrVariable.Variable "s"
                                    Predicate = ResourceOrVariable.Variable "p"
                                    Object = ResourceOrVariable.Variable "o1"
                            }
                ]
          }
        
        
    let GetEqualityAxioms (resources : GraphElementManager) =
        [GetSymmetryAxiom resources; GetSubjectEqualityAxiom resources; GetObjectEqualityAxiom resources; GetTransitivityAxiom resources]