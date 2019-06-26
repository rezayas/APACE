﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APACElib;
using ComputationLib;
using RandomVariateLib;

namespace APACElib
{
    public class GonoModel : ModelInstruction
    {
        enum Comparts { I, W, U }; // infection, waiting for treatment, waiting for retreatment 
        enum Drugs { A1, B1, B2}; // 1st line treatment with A, 1st line treatment with B, and 2nd line treatment with B
        enum Ms { M1, M2};  // 1st line treatment with M, and 2nd line treatment with M
        enum SymStates { Sym, Asym };
        enum ResistStates { G_0, G_A, G_B, G_AB }
        enum Interventions { A1 = 2, B1, M1, B2_A, M2_A, M2_B_AB} // A1:    A is used for 1st line treatment
                                                                  // B1:    B is used for 1st line treatment
                                                                  // M1:    M is used for 1st line treatment
                                                                  // B2_A:  retreating those infected with G_A with B after 1st line treatment failure
                                                                  // M2_A:  retreating those infected with G_A with M after 1st line treatment failure
                                                                  // M2_B_AB: retreating those infected with G_B or G_AB with M after 1st line treatment failure
        enum DummyParam { D_0, D_1, D_Minus1, D_Inf, T_Prev, T_DeltaPrev} // 0, 1, 2, 3, 4, 5
        enum SpecialStat { PopSize=0, Prev, FirstTx, SuccessAOrB, SuccessAOrBOrM, PercFirstTxAndResist}
        enum Features { Time, PercResist, ChangeInPercResist, IfEverUsed}
        enum Conditions {AOut, BOut, ABOut, AOk, BOk, ABOk, BNeverUsed, MNeverUsed, AOn, AOff, BOn, BOff, MOn, MOff};

        private List<int> _specialStatIDs = new List<int>(new int[Enum.GetValues(typeof(SpecialStat)).Length]);
        private List<int> _featureIDs = new List<int>(new int[Enum.GetValues(typeof(Features)).Length]);
        private List<string> _infProfiles = new List<string>();

        public GonoModel() : base()
        {
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    _infProfiles.Add(s.ToString() + " | " + r.ToString());
                }
        }

        public override void BuildModel()
        {
            // add parameters 
            AddGonoParameters("MSM");
            // add classes
            AddGonoClasses("MSM");
            // add events
            AddGonoEvents();
            // add interventions
            AddGonoInterventions();
            // add summation statistics
            AddGonoSumStats();
            // add ratio statistics
            AddGonoRatioStatistics();
            // add features
            AddGonoFeatures();
            // add conditions
            AddGonoConditions();
            // add connections
            AddGonoConnections();
        }

        private void AddGonoParameters(string region)
        {
            // add the parameters from the parameter sheet
            AddParameters();
            int parID = _paramManager.Parameters.Count;

            // initial size of S
            AddGonoParamSize_S(region, ref parID);

            // initial size of I compartments
            int infProfileID = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    AddGonoParamSize_I(region, s, r, infProfileID++, ref parID);

        }

        private void AddGonoParamSize_S(string region, ref int id)
        {
            _paramManager.Add(new ProductParameter(
                ID: id++,
                name: "Initial size of " + region + " | S",
                parameters: GetParamList(new List<string>() { "Initial population size | " + region, "1-Initial prevalence | " + region }))
                );
        }
        
        private void AddGonoParamSize_I(string region, SymStates s, ResistStates r, int infProfileID, ref int id)
        {
            string name = "Initial size of " + region + " | I | " + _infProfiles[infProfileID];
            List<string> paramNames = new List<string>() { "Initial population size | " + region, "Initial prevalence | " + region };

            if (s == SymStates.Sym)
                paramNames.Add("Initial symptomatic | " + region);
            else
                paramNames.Add("1-Initial symptomatic | " + region);

            switch (r)
            {
                case ResistStates.G_0:
                    paramNames.Add("1-Initial resistant to A or B | " + region);
                    break;
                case ResistStates.G_A:
                    paramNames.Add("Initial resistant to A | " + region);
                    break;
                case ResistStates.G_B:
                    paramNames.Add("Initial resistant to B | " + region);
                    break;
                case ResistStates.G_AB:
                    paramNames.Add("Initial resistant to AB | " + region);
                    break;
            }

            if (r == ResistStates.G_AB)
                _paramManager.Add(new IndependetParameter(
                    ID: id++,
                    name: name,
                    enumRandomVariateGenerator: RandomVariateLib.SupportProcedures.ConvertToEnumRVG("Constant"),
                    par1: 0, par2: 0, par3: 0, par4: 0)
                    );
            else
                _paramManager.Add(new ProductParameter(
                    ID: id++,
                    name: name,
                    parameters: GetParamList(paramNames))
                    );
        }

        private void AddGonoClasses(string region)
        {
            int id = 0;
            int infProfile = 0; // infection profile

            // add S
            Class_Normal S = Get_S(id, region);
            _classes.Add(S);
            _dicClasses[S.Name] = id++;

            // add classes to count the treatment outcomes
            // Success with A1, B1, or B2
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
            {
                Class_Normal c = Get_Success(id, region, d.ToString());
                _classes.Add(c);
                _dicClasses[c.Name] = id++;
            }

            // Success with M1 or M2
            foreach (Ms m in Enum.GetValues(typeof(Ms)))
            {
                Class_Normal c = Get_Success(id, region, m.ToString());
                _classes.Add(c);
                _dicClasses[c.Name] = id++;
            }

            // add death
            Class_Death D = Get_D(id, region);
            _classes.Add(D);
            _dicClasses[D.Name] = id++;

            // add I's, W's, and U's, 
            // example: "I | Sym | G_0"     
            int infParID = _paramManager.Dic["Infectivity of | I | " + _infProfiles[0]];
            int size0ParID = _paramManager.Dic["Initial size of " + region + " | I | " + _infProfiles[0]];
            foreach (Comparts c in Enum.GetValues(typeof(Comparts)))
            {
                infProfile = 0;
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        Class_Normal C = Get_I_W_U(id: id, region: region, 
                            infProfileID: infProfile,
                            c: c, 
                            r: r, 
                            size0ParID: (c == Comparts.I) ? size0ParID++ : (int)DummyParam.D_0, 
                            infectivityParID: infParID + infProfile);
                        _classes.Add(C);
                        _dicClasses[C.Name] = id++;
                        ++infProfile;
                    }
            }

            // Prob symptomatic after infection
            int classIdIfSymp = _dicClasses[region + " | I | Sym | G_0"];
            int classIDIfAsymp = _dicClasses[region + " | I | Asym | G_0"];
            int ifSymParID = _paramManager.Dic["Prob sym | G_0"];
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
            {
                Class_Splitting ifSym = Get_IfSym(
                    id: id, 
                    region: region, 
                    r: r,
                    ifSymParID: ifSymParID + (int)r, 
                    classIDIfSym: classIdIfSymp + (int)r,
                    classIDIfAsym: classIDIfAsymp + (int)r);
                _classes.Add(ifSym);
                _dicClasses[ifSym.Name] = id++;
            }

            // if seeking retreatment after resistance or failure
            // examples "If retreat A | A --> I | Sym | G_0"
            //          "If retreat F | A --> I | Sym | G_A"
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs)))   // A1, B1, B2
                // assume that failure after B2 will always seek retreatment 
                if (drug == Drugs.A1 || drug == Drugs.B1)  
                {
                    infProfile = 0;
                    foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                        foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        {
                            Class_Splitting ifRetreat = Get_IfRetreat(
                                id: id,
                                region: region,
                                r: r,
                                s: s, 
                                drug: drug, 
                                infProfile: infProfile);
                            _classes.Add(ifRetreat);
                            _dicClasses[ifRetreat.Name] = id++;
                            ++infProfile;
                        }
                }

            // TODO: starts here

            // if symptomatic after the emergence of resistance
            // example: "If symp | A | A --> I | Asym | G_0"
            //          true    -> "If retreat A | A --> I | Sym | G_0"
            //          false   -> "I | Asym | G_A"         
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs))) // A1, B1, or B2
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                    // if developed resistance
                    if (resistOrFail != "F")
                    {                        
                        string className = "If sym | " + resistOrFail + " | " + drug.ToString() + " --> I | Asym | " + r.ToString();
                        string classIfSym = "", classIfAsym = "";

                        // assuming that failure after B2 will receive M2
                        if (drug == Drugs.A1 || drug == Drugs.B1)
                            classIfSym = "If retreat " + resistOrFail + " | " + drug.ToString() + " --> I | Sym | " + r.ToString();
                        else
                            classIfSym = "Success with M2";
                        classIfAsym = "I | Asym | G_" + resistOrFail;

                        Class_Splitting ifSymp = new Class_Splitting(id, className);
                        ifSymp.SetUp(
                            parOfProbSucess: GetParam("Prob sym | G_" + resistOrFail),
                            destinationClassIDIfSuccess: _dicClasses[classIfSym],
                            destinationClassIDIfFailure: _dicClasses[classIfAsym]
                            );
                        SetupClassStatsAndTimeSeries(thisClass: ifSymp);
                        _classes.Add(ifSymp);
                        _dicClasses[className] = id++;   
                    }
                }

            // treatment outcomes (resistance)    
            // example: "If A | A --> I | Sym | G_0"
            //          true: "If retreat A | A --> I | Sym | G_0"
            //          false: "Success A1"
            // example: "If A | A --> I | Asym | G_0"
            //          true: "If sym | A | A --> I | Asym | G_0"
            //          false: "Success A1"
            int classIDSuccessA1 = _dicClasses["Success with " + Drugs.A1.ToString()];
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs))) // A1, B1, B2
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        // considering only resistance outcome
                        string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                        if (resistOrFail == "F")
                            continue;

                        string strInfProfile = "I | " + s.ToString() + " | " + r.ToString();  // "I | Sym | G_0"                
                        string treatmentProfile = resistOrFail + " | " + drug.ToString() + " --> " + strInfProfile; // "A | A --> I | Sym | G_0"
                        string className = "If " + treatmentProfile; // "If A | A --> I | Sym | G_0"
                        string classIfResist = "";

                        // find the destination classes
                        if (drug == Drugs.A1 || drug == Drugs.B1)
                        {
                            // if already symptomatic 
                            if (s == SymStates.Sym)
                                classIfResist = "If retreat " + treatmentProfile;
                            else // if not symtomatic
                                classIfResist = "If sym | " + treatmentProfile;
                        }
                        else // if already received B2
                        {
                            //if (r != ResistStates.G_A)
                            //    continue;
                            classIfResist = "U | " + s.ToString() + " | G_" + resistOrFail;
                        }

                        // parameter name
                        string paramName = (drug == Drugs.A1) ? "Prob resistance | Drug A" : "Prob resistance | Drug B";

                        // make the splitting class
                        Class_Splitting ifResist = new Class_Splitting(id, className);
                        ifResist.SetUp(
                            parOfProbSucess: GetParam(paramName),
                            destinationClassIDIfSuccess: _dicClasses[classIfResist],
                            destinationClassIDIfFailure: classIDSuccessA1 + (int)drug
                            );
                        SetupClassStatsAndTimeSeries(thisClass: ifResist, showIncidence:true);
                        _classes.Add(ifResist);
                        _dicClasses[className] = id++;
                    }
        }

        private Class_Normal Get_S(int id, string region)
        {
            Class_Normal S = new Class_Normal(id, region + " | S");
            S.SetupInitialAndStoppingConditions(
                initialMembersPar: GetParam("Initial size of " + region + " | S"));
            S.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_1, repeat: 4),
                infectivityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(
                thisClass: S,
                showPrevalence: true);
            return S;
        }
        private Class_Normal Get_Success(int id, string region, string drug)
        {
            Class_Normal c = new Class_Normal(id, region + " | Success with " + drug);
            c.SetupInitialAndStoppingConditions(
                initialMembersPar: _paramManager.Parameters[(int)DummyParam.D_0]);
            c.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                infectivityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(
                thisClass: c,
                showIncidence: true);
            return c;
        }
        private Class_Death Get_D(int id, string region)
        {
            Class_Death D = new Class_Death(id, region + " | Death");
            SetupClassStatsAndTimeSeries(
                    thisClass: D,
                    showIncidence: true);
            return D;
        }
        private Class_Normal Get_I_W_U(int id, string region, int infProfileID, Comparts c, ResistStates r, int size0ParID, int infectivityParID)
        {
            Class_Normal C = new Class_Normal(id, region + " | " + c.ToString() + " | " + _infProfiles[infProfileID]);
            C.SetupInitialAndStoppingConditions(
                initialMembersPar: _paramManager.Parameters[size0ParID],
                ifShouldBeEmptyForEradication: false);  // to simulate until the end of the simulation hirozon
            C.SetupTransmissionDynamicsProperties(
                susceptibilityParams: GetParamList(dummyParam: DummyParam.D_0, repeat: 4), // no reinfection in I, W, or U
                infectivityParams: GetParamList(
                    parID: infectivityParID,
                    pos: (int)r,
                    size: 4,
                    dummyParam: DummyParam.D_0),
                rowIndexInContactMatrix: 0);
            SetupClassStatsAndTimeSeries(
                thisClass: C,
                showPrevalence: (c == Comparts.I || c == Comparts.U) ? true : false,
                showIncidence: (c == Comparts.W) ? true : false);
            return C;
        }
        private Class_Splitting Get_IfSym(int id, string region, ResistStates r, int ifSymParID, int classIDIfSym, int classIDIfAsym)
        {
            Class_Splitting ifSym = new Class_Splitting(id, region + " | If Sym | " + r.ToString());
            ifSym.SetUp(
                parOfProbSucess: _paramManager.Parameters[ifSymParID],
                destinationClassIDIfSuccess: classIDIfSym,
                destinationClassIDIfFailure: classIDIfAsym);
            SetupClassStatsAndTimeSeries(thisClass: ifSym);
            return ifSym;
        }
        private Class_Splitting Get_IfRetreat(int id, string region, ResistStates r, SymStates s, Drugs drug, int infProfile)
        {
            string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
            string className = region +  " | If retreat " + resistOrFail + " | " + drug.ToString() + " --> I | " + _infProfiles[infProfile];

            string classIfSeekTreatment = "", classIfNotSeekTreatment = "";
            // if failed
            if (resistOrFail == "F")
            {
                // and seeks treatment -> waiting for retreatment
                classIfSeekTreatment = region + " | U | " + _infProfiles[infProfile];
                // and does not seek treatment -> the infectious state 
                classIfNotSeekTreatment = region + " | I | " + _infProfiles[infProfile];
            }
            else // if developed resistance
            {
                // update the infection profile
                string newInfProfile = s.ToString() + " | G_" + resistOrFail;
                // and seeks treatment -> waiting for retreatment
                classIfSeekTreatment = region + " | U | " + newInfProfile;
                // and does not seek treatment -> the infectious state
                classIfNotSeekTreatment = region + " | I | " + newInfProfile;
            }

            Class_Splitting ifRetreat = new Class_Splitting(id, className);
            ifRetreat.SetUp(
                parOfProbSucess: GetParam("Prob retreatment | " + s.ToString()),
                destinationClassIDIfSuccess: _dicClasses[classIfSeekTreatment],
                destinationClassIDIfFailure: _dicClasses[classIfNotSeekTreatment]
                );
            SetupClassStatsAndTimeSeries(thisClass: ifRetreat);
            return ifRetreat;
        }

        private void AddGonoEvents()
        {
            int id = 0;
            int inf = 0;
            string eventName = "";
            // rates
            int infRate = _paramManager.Dic["Dummy Inf"];
            int birthRate = _paramManager.Dic["Annual birth rate (per pop member)"];
            int deathRate = _paramManager.Dic["Annual death rate"];
            int naturalRecoveryRate = _paramManager.Dic["Natural recovery"];
            int screeningRate = _paramManager.Dic["Annual screening rate"];
            int seekingTreatmentRate = _paramManager.Dic["Annual rate of seeking treatment (symptomatic)"];
            int seekingReTreatmentRate = _paramManager.Dic["Annual rate of retreatment"];
            
            int idS = _dicClasses["S"];
            int idDeath = _dicClasses["Death"];
            int idSuccessM1 = _dicClasses["Success with " + Ms.M1.ToString()];
            int idSuccessM2 = _dicClasses["Success with " + Ms.M2.ToString()];

            // main compartments: S, I
            List<string> mainComp = new List<string>();
            mainComp.Add("S");
            for (inf = 0; inf <_infProfiles.Count; inf ++)
                mainComp.Add("I | " + _infProfiles[inf]);

            // add Birth events
            foreach (string comp in mainComp)
            {
                eventName = "Birth | " + comp;
                _events.Add(new Event_Birth(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0,
                    rateParameter: _paramManager.Parameters[birthRate],
                    IDOfDestinationClass: idS)
                    );
                _dicEvents[eventName] = id++;
            }
            // add Death events
            foreach (string comp in mainComp)
            {
                eventName = "Death | " + comp;
                _events.Add(new Event_EpidemicIndependent(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0,
                    rateParameter: _paramManager.Parameters[deathRate],
                    IDOfDestinationClass: idDeath)
                    );
                _dicEvents[eventName] = id++;
            }
            

            // add Infection events
            int idIfSympG_0 = _dicClasses["If Sym | G_0"];
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
            {
                eventName = "Infection | " + r.ToString();
                _events.Add(new Event_EpidemicDependent(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0,
                    IDOfPathogenToGenerate: (int)r,
                    IDOfDestinationClass: idIfSympG_0 + (int)r)
                    );
                _dicEvents[eventName] = id++;
            }

            // add Natual Recovery events
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Natural Recovery | I | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        rateParameter: _paramManager.Parameters[naturalRecoveryRate],
                        IDOfDestinationClass: idS)
                    );
                    _dicEvents[eventName] = id++;
                }

            // add Seeking Treatment events
            int idWSymG_0 = _dicClasses["W | Sym | G_0"];
            inf = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Seeking Treatment | I | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        rateParameter: (s == SymStates.Sym) ? _paramManager.Parameters[seekingTreatmentRate] : _paramManager.Parameters[(int)DummyParam.D_0],
                        IDOfDestinationClass: idWSymG_0 + inf++)
                    );
                    _dicEvents[eventName] = id++;
                }

            // add Screening events
            inf = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Screening | I | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: 0,
                        rateParameter: _paramManager.Parameters[screeningRate],
                        IDOfDestinationClass: idWSymG_0 + inf++)
                    );
                    _dicEvents[eventName] = id++;
                }

            // add First-Line Treatment with A1 and B1
            foreach (Drugs d in Enum.GetValues(typeof(SymStates)))
                if (d == Drugs.A1 || d == Drugs.B1)
                    foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                        foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        {
                            string resistOrFail = GetResistOrFail(resistStat: r, drug: d);
                            string infProfile = s.ToString() + " | " + r.ToString();
                            string treatmentProfile = resistOrFail + " | " + d.ToString() + " --> I | " + infProfile;
                            eventName = "Tx_" + d.ToString() + " | W | " + infProfile;
                            string destClassName = "";

                            if (resistOrFail == "F")
                                destClassName = "If retreat " + treatmentProfile;
                            else
                                destClassName = "If " + treatmentProfile;

                            _events.Add(new Event_EpidemicIndependent(
                                name: eventName,
                                ID: id,
                                IDOfActivatingIntervention: (d == Drugs.A1) ? (int)Interventions.A1 : (int)Interventions.B1,
                                rateParameter: _paramManager.Parameters[infRate],
                                IDOfDestinationClass: _dicClasses[destClassName])
                                );
                            _dicEvents[eventName] = id++;
                        }

            // add First-Line Treatment with M1
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Tx_M1 | W | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: (int)Interventions.M1,
                        rateParameter: _paramManager.Parameters[infRate],
                        IDOfDestinationClass: idSuccessM1)
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Second-Line Treatment with B2            
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string resistOrFail = GetResistOrFail(resistStat: r, drug: Drugs.B2);
                    string infProfile = s.ToString() + " | " + r.ToString();
                    string treatmentProfile = resistOrFail + " | B2 --> I | " + infProfile;
                    eventName = "Tx_B2 | U | " + infProfile;

                    string destClassName = "";
                    if (resistOrFail == "F")
                        // if treatment failure occurs, the patient will receive M2 
                        destClassName = "Success with M2";
                    else
                        destClassName = "If " + treatmentProfile;

                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: (r == ResistStates.G_A) ? (int)Interventions.B2_A : 1,
                        rateParameter: _paramManager.Parameters[seekingReTreatmentRate],
                        IDOfDestinationClass: _dicClasses[destClassName])
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Second-Line Treatment with M2              
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    string infProfile = s.ToString() + " | " + r.ToString();
                    eventName = "Tx_M2 | U | " + infProfile;
                    _events.Add(new Event_EpidemicIndependent(
                        name: eventName,
                        ID: id,
                        IDOfActivatingIntervention: (r == ResistStates.G_A) ? (int)Interventions.M2_A : (int)Interventions.M2_B_AB,
                        rateParameter: (r == ResistStates.G_A) ? _paramManager.Parameters[(int)DummyParam.D_0] : _paramManager.Parameters[seekingReTreatmentRate],
                        IDOfDestinationClass: idSuccessM2)
                        );
                    _dicEvents[eventName] = id++;
                }

            // add Leaving Success with A1, B1, or B2
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
            {
                eventName = "Leaving Success with " + d.ToString();
                _events.Add(new Event_EpidemicIndependent(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0, // always on
                    rateParameter: _paramManager.Parameters[(int)DummyParam.D_Inf],
                    IDOfDestinationClass: 0) // back to S
                    );
                _dicEvents[eventName] = id++;
            }
            // add Leaving Success with M1 or M2
            foreach (Ms m in Enum.GetValues(typeof(Ms)))
            {
                eventName = "Leaving Success with " + m.ToString();
                _events.Add(new Event_EpidemicIndependent(
                    name: eventName,
                    ID: id,
                    IDOfActivatingIntervention: 0, // always on
                    rateParameter: _paramManager.Parameters[(int)DummyParam.D_Inf],
                    IDOfDestinationClass: 0) // back to S
                    );
                _dicEvents[eventName] = id++;
            }
        }

        private void AddGonoInterventions()
        {
            AddInterventions();

            int id = _decisionMaker.Interventions.Count();
            int i = 0;

            foreach (Interventions intrv in Enum.GetValues(typeof(Interventions)))
            {
                int conditionIDToTurnOn = 0, conditionIDToTurnOff = 0;
                switch (intrv)
                {
                    case Interventions.A1:
                        {
                            conditionIDToTurnOn = (int)Conditions.AOn;
                            conditionIDToTurnOff = (int)Conditions.AOff;                           
                        }
                        break;
                    case Interventions.B1:
                        {
                            conditionIDToTurnOn = (int)Conditions.BOn;
                            conditionIDToTurnOff = (int)Conditions.BOff;
                        }
                        break;
                    case Interventions.M1:
                        {
                            conditionIDToTurnOn = (int)Conditions.MOn;
                            conditionIDToTurnOff = (int)Conditions.MOff;
                        }
                        break;
                    case Interventions.B2_A:
                        {
                            conditionIDToTurnOn = (int)Conditions.AOn;
                            conditionIDToTurnOff = (int)Conditions.BOff;
                        }
                        break;
                    case Interventions.M2_A:
                        {
                            conditionIDToTurnOn = (int)Conditions.MOn;
                            conditionIDToTurnOff = (int)Conditions.MOff;
                        }
                        break;
                }

                // decision rule 
                DecisionRule simDecisionRule = null;
                if (intrv == Interventions.M2_B_AB)
                    simDecisionRule = new DecionRule_Predetermined(predeterminedSwitchValue: 1);
                else
                    simDecisionRule = new DecisionRule_ConditionBased(
                        conditions: _epiHist.Conditions,
                        conditionIDToTurnOn: conditionIDToTurnOn,
                        conditionIDToTurnOff: conditionIDToTurnOff);

                // intervention
                _decisionMaker.AddAnIntervention(
                    new Intervention(
                        index: id++,
                        name: intrv.ToString(),
                        actionType: EnumInterventionType.Additive,
                        affectingContactPattern: false,
                        timeIndexBecomesAvailable: 0,
                        timeIndexBecomesUnavailable: _modelSets.TimeIndexToStop,
                        parIDDelayToGoIntoEffectOnceTurnedOn: 0,
                        decisionRule: simDecisionRule));

                i++;
            }
        }

        private void AddGonoSumStats()
        {
            int id = 0;
            string formula = "";

            // population size
            formula = "";
            foreach (Class c in _classes.Where(c => c is Class_Normal))
                formula += c.ID + "+";

            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Population size",
                    strType: "Prevalence",
                    sumFormula: formula,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatIDs[(int)SpecialStat.PopSize] = id - 1;

            // gonorrhea prevalence formulas
            string pFormula = "";
            List<string> pSymFormula = new List<string>() { "", "" }; // Sym, Asym
            List<string> pResistFormula = new List<string>() { "", "", "" }; // A, B, AB
            foreach (Class c in _classes.Where(c => c is Class_Normal ))
            {
                if (c.Name.First() == 'I' || c.Name.First() == 'W')
                {
                    pFormula += c.ID + "+";
                    if (c.Name.Contains("Sym"))
                        pSymFormula[0] += c.ID + "+";
                    else
                        pSymFormula[1] += c.ID + "+";

                    if (c.Name.Substring(c.Name.Length - 2) == "_A")
                        pResistFormula[0] += c.ID + "+";
                    else if (c.Name.Substring(c.Name.Length-2) == "_B")
                        pResistFormula[1] += c.ID + "+";
                    else if (c.Name.Substring(c.Name.Length - 2) == "AB")
                        pResistFormula[2] += c.ID + "+";
                }
            }

            // gonorrhea prevalence
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Prevalence",
                    strType: "Prevalence",
                    sumFormula: pFormula,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatIDs[(int)SpecialStat.Prev] = id - 1;

            // gonorrhea prevalence by symptom status
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
            {
                if (s == SymStates.Sym)
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Prevalence | " + s.ToString(),
                            strType: "Prevalence",
                            sumFormula: pSymFormula[(int)s],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );
            }

            // gonorrhea prevalence by resistance 
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
                if (r != ResistStates.G_0)
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Prevalence | " + r.ToString(),
                            strType: "Prevalence",
                            sumFormula: pResistFormula[(int)r-1],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );

            // first-line treatment 
            string treatedFormula = "", treatedAndSymFormula = "";
            List<string> treatedResistFormula = new List<string>() { "", "", "" }; // A, B, AB
            foreach (Class c in _classes.Where(c => c is Class_Normal))
            {
                if (c.Name.Length > 1 && c.Name.Substring(0, 2) == "W ")
                {
                    treatedFormula += c.ID + "+";
                    if (c.Name.Contains("Sym"))
                        treatedAndSymFormula += c.ID + "+";

                    if (c.Name.Substring(c.Name.Length - 2) == "_A")
                        treatedResistFormula[0] += c.ID + "+";
                    else if (c.Name.Substring(c.Name.Length - 2) == "_B")
                        treatedResistFormula[1] += c.ID + "+";
                    else if (c.Name.Substring(c.Name.Length - 2) == "AB")
                        treatedResistFormula[2] += c.ID + "+";
                }
            }

            // received first-line treatment (= number of cases)
            SumClassesTrajectory t1st = new SumClassesTrajectory(
                ID: id++,
                name: "Received 1st Tx",
                strType: "Incidence",
                sumFormula: treatedFormula,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            UpdateClassTimeSeries(t1st);
            t1st.DeltaCostHealthCollector =
                new DeltaTCostHealth(
                    deltaT: _modelSets.DeltaT, 
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    DALYPerNewMember: _paramManager.Parameters[(int)DummyParam.D_1],
                    costPerNewMember: _paramManager.Parameters[(int)DummyParam.D_0]
                    );
            _epiHist.SumTrajs.Add(t1st);
            _specialStatIDs[(int)SpecialStat.FirstTx] = id - 1;

            // received first-line treatment and symptomatic 
            _epiHist.SumTrajs.Add(new SumClassesTrajectory(
                ID: id++,
                name: "Received 1st Tx & Symptomatic",
                strType: "Incidence",
                sumFormula: treatedAndSymFormula,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                );

            // received first-line treatment by resistance status
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
                if (r != ResistStates.G_0)
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Received 1st Tx & Resistant to " + r.ToString(),
                            strType: "Incidence",
                            sumFormula: treatedResistFormula[(int)r-1],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );

            // sucessful treatment
            string treatedA1 = _classes[_dicClasses["Success with A1"]].ID.ToString();
            string treatedB1 = _classes[_dicClasses["Success with B1"]].ID.ToString();
            string treatedB2 = _classes[_dicClasses["Success with B2"]].ID.ToString();
            string treatedM1 = _classes[_dicClasses["Success with M1"]].ID.ToString();
            string treatedM2 = _classes[_dicClasses["Success with M2"]].ID.ToString();

            string success1st = treatedA1 + "+" + treatedB1 + "+" + treatedM1;
            string successAorB = treatedA1 + "+" + treatedB1 + "+" + treatedB2;
            string successM = treatedM1 + "+" + treatedM2;
            string successAll = successAorB + "+" + successM;

            // # sucessfully treated with 1st line treatment 
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Success 1st",
                    strType: "Incidence",
                    sumFormula: success1st,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            // # sucessfully treated with A or B 
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Success A or B",
                    strType: "Incidence",
                    sumFormula: successAorB,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatIDs[(int)SpecialStat.SuccessAOrB] = id - 1;

            // # sucessfully treated with M
            SumClassesTrajectory tM = new SumClassesTrajectory(
               ID: id++,
               name: "Success M",
               strType: "Incidence",
               sumFormula: successM,
               displayInSimOutput: true,
               warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
               nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            UpdateClassTimeSeries(tM);
            tM.DeltaCostHealthCollector =
                new DeltaTCostHealth(
                    deltaT: _modelSets.DeltaT,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    DALYPerNewMember: _paramManager.Parameters[(int)DummyParam.D_0],
                    costPerNewMember: _paramManager.Parameters[(int)DummyParam.D_1]
                    );
            _epiHist.SumTrajs.Add(tM);

            // # sucessfully treated 
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Success All",
                    strType: "Incidence",
                    sumFormula: successAll,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatIDs[(int)SpecialStat.SuccessAOrBOrM] = id - 1;

            // update times series of summation statistics
            UpdateSumStatTimeSeries();
        }

        private void AddGonoRatioStatistics()
        {
            int id = _epiHist.SumTrajs.Count();
            int idPopSize = _specialStatIDs[(int)SpecialStat.PopSize];
            int idPrevalence = _specialStatIDs[(int)SpecialStat.Prev];
            int idFirstTx = _specialStatIDs[(int)SpecialStat.FirstTx];
            int idSuccessAOrB = _specialStatIDs[(int)SpecialStat.SuccessAOrB];
            int idSuccessAOrBOrM = _specialStatIDs[(int)SpecialStat.SuccessAOrBOrM];
            Parameter nIsolateTested = GetParam("Number of isolates tested");

            // gonorrhea prevalence
            RatioTrajectory prevalence = new RatioTrajectory(
                id: id,
                name: "Prevalence",
                strType: "Prevalence/Prevalence",
                ratioFormula: idPrevalence + "/" + idPopSize,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            if (_modelSets.ModelUse == EnumModelUse.Calibration)
                prevalence.CalibInfo = new SpecialStatCalibrInfo(
                    measureOfFit: "Likelihood", 
                    likelihoodFunction: "Binomial",
                    likelihoodParam: "", 
                    ifCheckWithinFeasibleRange:true,
                    lowFeasibleBound: 0.005,
                    upFeasibleBound: 0.04,
                    minThresholdToHit: 0);
            _epiHist.RatioTrajs.Add(prevalence);           
            id++;

            // % infection symptomatic (prevalence)
            RatioTrajectory prevalenceSym = new RatioTrajectory(
                id: id++,
                name: "% infection symptomatic",
                strType: "Prevalence/Prevalence",
                ratioFormula: (idPrevalence + 1) + "/" + idPrevalence,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            _epiHist.RatioTrajs.Add(prevalenceSym);

            // % infection resistant to A, B, or AB (prevalence)
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    RatioTrajectory prev = new RatioTrajectory(
                        id: id++,
                        name: "% infection resistant to " + r.ToString(),
                        strType: "Prevalence/Prevalence",
                        ratioFormula: (idPrevalence + 1 + (int)r) + "/" + idPrevalence,
                        displayInSimOutput: true,
                        warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                        nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
                    _epiHist.RatioTrajs.Add(prev);
                }
            }

            // % received 1st Tx and symptomatic (incidence)            
            RatioTrajectory firstTxSym = new RatioTrajectory(
                id: id++,
                name: "% received 1st Tx & symptomatic ",
                strType: "Incidence/Incidence",
                ratioFormula: (idFirstTx + 1) + "/" + idFirstTx,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            if (_modelSets.ModelUse == EnumModelUse.Calibration)
                firstTxSym.CalibInfo = new SpecialStatCalibrInfo(
                    measureOfFit: "Likelihood",
                    likelihoodFunction: "Binomial",
                    likelihoodParam: "",
                    ifCheckWithinFeasibleRange: true,
                    lowFeasibleBound: 0.5,
                    upFeasibleBound: 0.8,
                    minThresholdToHit: 0);
            _epiHist.RatioTrajs.Add(firstTxSym);

            // % received 1st Tx and resistant to A, B, or AB (incidence)
            _specialStatIDs[(int)SpecialStat.PercFirstTxAndResist] = id;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    RatioTrajectory firstTx = new RatioTrajectory(
                    id: id,
                    name: "% received 1st Tx & resistant to " + r.ToString(),
                    strType: "Incidence/Incidence",
                    ratioFormula: (idFirstTx + 1 + (int)r) + "/" + idFirstTx,
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
                    if (_modelSets.ModelUse == EnumModelUse.Calibration && r != ResistStates.G_AB)
                        firstTxSym.CalibInfo = new SpecialStatCalibrInfo(
                            measureOfFit: "Feasible Range Only",
                            likelihoodFunction: "",
                            likelihoodParam: "",
                            ifCheckWithinFeasibleRange: true,
                            lowFeasibleBound: 0,
                            upFeasibleBound: 1,
                            minThresholdToHit: 0.05);
                    _epiHist.SurveyedIncidenceTrajs.Add(
                       new SurveyedIncidenceTrajectory(
                           id: id,
                           name: "% received 1st Tx & resistant to " + r.ToString(),
                           displayInSimOutput: true,
                           firstObsMarksStartOfEpidemic: false,
                           sumClassesTrajectory: null,
                           sumEventTrajectory: null,
                           ratioTrajectory: firstTx,
                           nDeltaTsObsPeriod: _modelSets.NumOfDeltaT_inObservationPeriod,
                           nDeltaTsDelayed: 0,
                           noise_nOfDemoninatorSampled: nIsolateTested)
                           );
                    _epiHist.RatioTrajs.Add(firstTx);
                    id++;
                }
            }
            // annual rate of gonorrhea cases
            RatioTrajectory rate = new RatioTrajectory(
                id: id++,
                name: "Annual rate of gonorrhea cases",
                strType: "Incidence/Prevalence",
                ratioFormula: idFirstTx + "/" + idPopSize,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            if (_modelSets.ModelUse == EnumModelUse.Calibration)
                rate.CalibInfo = new SpecialStatCalibrInfo(
                    measureOfFit: "Likelihood",
                    likelihoodFunction: "Binomial",
                    likelihoodParam: "",
                    ifCheckWithinFeasibleRange: true,
                    lowFeasibleBound: 0.02,
                    upFeasibleBound: 0.08,
                    minThresholdToHit: 0);
            _epiHist.RatioTrajs.Add(rate);

            // effective life of drugs A and B
            RatioTrajectory effLifeAandB = new RatioTrajectory(
                id: id++,
                name: "Effective life of A and B",
                strType: "Incidence/Incidence",
                ratioFormula: idSuccessAOrB + "/" + idSuccessAOrBOrM,
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
            _epiHist.RatioTrajs.Add(effLifeAandB);

            // update times series of ratio statistics
            UpdateRatioStatTimeSeries();
        }

        private void AddGonoConnections()
        {
            int i = 0;           
            int birthID = _dicEvents["Birth | S"];
            int deathID = _dicEvents["Death | S"];
            int infectionID = _dicEvents["Infection | G_0"];
            int naturalRecoveryID = _dicEvents["Natural Recovery | I | Sym | G_0"];
            int seekingTreatmentID = _dicEvents["Seeking Treatment | I | Sym | G_0"];
            int screeningID = _dicEvents["Screening | I | Sym | G_0"];
            int txA = _dicEvents["Tx_A1 | W | " + _infProfiles[0]];
            int txB = _dicEvents["Tx_B1 | W | " + _infProfiles[0]]; 
            int txM = _dicEvents["Tx_M1 | W | " + _infProfiles[0]]; 
            int txB2 = _dicEvents["Tx_B2 | U | " + _infProfiles[0]];
            int txM2 = _dicEvents["Tx_M2 | U | " + _infProfiles[0]];
            int leaveSuccess = _dicEvents["Leaving Success with A1"];
            int success = _dicClasses["Success with A1"];

            // ----------------
            // add events for S
            Class_Normal S = (Class_Normal)_classes[_dicClasses["S"]];
            // birth and death
            S.AddAnEvent(_events[birthID]);
            S.AddAnEvent(_events[deathID]);
            // infections
            i = 0;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                S.AddAnEvent(_events[infectionID + i++]);

            // ----------------
            // add events for I, W, U
            i = 0;
            int w = 0, u = 0;
            foreach (Class c in _classes.Where(c => (c is Class_Normal)))
            {
                // for I
                if (c.Name.StartsWith("I"))
                {                    
                    ((Class_Normal)c).AddAnEvent(_events[birthID + i + 1]);
                    ((Class_Normal)c).AddAnEvent(_events[deathID + i + 1]);
                    ((Class_Normal)c).AddAnEvent(_events[naturalRecoveryID + i]);
                    ((Class_Normal)c).AddAnEvent(_events[seekingTreatmentID + i]);
                    ((Class_Normal)c).AddAnEvent(_events[screeningID + i]);
                    ++i;
                }
                // for W
                else if (c.Name.StartsWith("W "))
                {
                    ((Class_Normal)c).AddAnEvent(_events[txA + w]);
                    ((Class_Normal)c).AddAnEvent(_events[txB+ w]);
                    ((Class_Normal)c).AddAnEvent(_events[txM+ w]);
                    ++w;
                }
                else if (c.Name.StartsWith("U"))
                {
                    ((Class_Normal)c).AddAnEvent(_events[txB2 + u]);
                    ((Class_Normal)c).AddAnEvent(_events[txM2 + u]);
                    ++u;
                }
            }

            // add leaving success with A1, B1, B2, M1, M2
            for (int j = 0; j < 5; j++)
                ((Class_Normal)_classes[success + j]).AddAnEvent(_events[leaveSuccess + j]);
        }

        private void AddGonoFeatures()
        {
            int id = 0;
            int idPercFirstTxAndResist = _specialStatIDs[(int)SpecialStat.PercFirstTxAndResist];

            // add time
            _featureIDs[(int)Features.Time] = id;
            _epiHist.Features.Add(new Feature_EpidemicTime("Epidemic Time", id++));

            // % receieved 1st Tx and resistant to A, B, or AB
            _featureIDs[(int)Features.PercResist] = id;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    _epiHist.AddASpecialStatisticsFeature(
                        name: "% received 1st Tx & resistant to " + r.ToString(),
                        featureID: id++,
                        specialStatID: idPercFirstTxAndResist + (int)r - 1,
                        strFeatureType: "Current Observed Value",
                        par: 0);
                }
            }

            // change in % receieved 1st Tx and resistant to A, B, or AB
            _featureIDs[(int)Features.ChangeInPercResist] = id;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    _epiHist.AddASpecialStatisticsFeature(
                        name: "Change in % received 1st Tx & resistant to " + r.ToString(),
                        featureID: id++,
                        specialStatID: idPercFirstTxAndResist + (int)r - 1,
                        strFeatureType: "Slope",
                        par: 0);
                }
            }

            // if A1 and B1 ever switched off 
            _featureIDs[(int)Features.IfEverUsed] = id;
            _epiHist.Features.Add(new Feature_Intervention(
                name: "If A1 ever switched off",
                featureID: id++, 
                featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOff,
                intervention: _decisionMaker.Interventions[(int)Interventions.A1])
                );
            _epiHist.Features.Add(new Feature_Intervention(
                name: "If B1 ever switched off",
                featureID: id++, 
                featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOff,
                intervention: _decisionMaker.Interventions[(int)Interventions.B1])
                );

            // if M ever switched on
            _epiHist.Features.Add(new Feature_Intervention(
                name: "If M1 ever switched on",
                featureID: id++,
                featureType: Feature_Intervention.EnumFeatureType.IfEverSwitchedOn,
                intervention: _decisionMaker.Interventions[(int)Interventions.M1])
                );
        }

        private void AddGonoConditions()
        {
            int id = 0;
            EnumSign[] signs;
            List<Parameter> thresholdParams = new  List<Parameter>{
                _paramManager.Parameters[(int)DummyParam.T_Prev],
                _paramManager.Parameters[(int)DummyParam.T_DeltaPrev]};
            List<Parameter> thresholdParams0 = new List<Parameter> {
                _paramManager.Parameters[(int)DummyParam.D_0] };
            List<Parameter> thresholdParams00 = new List<Parameter> {
                _paramManager.Parameters[(int)DummyParam.D_0],
                _paramManager.Parameters[(int)DummyParam.D_0] };

            // out condition for A, B, or both
            signs = new EnumSign[2] { EnumSign.q, EnumSign.q };
            for (int i = 0; i < 3; i++)
                _epiHist.Conditions.Add(new Condition_OnFeatures(
                    id: id++,
                    features: new List<Feature> {
                        _epiHist.Features[_featureIDs[(int)Features.PercResist] + i],
                        _epiHist.Features[_featureIDs[(int)Features.ChangeInPercResist] + i] } ,
                    thresholdParams: thresholdParams,
                    signs: signs,
                    conclusion: EnumAndOr.Or));

            // ok condition for A, B, or both
            signs = new EnumSign[2] { EnumSign.le, EnumSign.le };
            for (int i = 0; i < 3; i++)
                _epiHist.Conditions.Add(new Condition_OnFeatures(
                    id: id++,
                    features: new List<Feature> {
                        _epiHist.Features[_featureIDs[(int)Features.PercResist] + i],
                        _epiHist.Features[_featureIDs[(int)Features.ChangeInPercResist] + i] },
                    thresholdParams: thresholdParams,
                    signs: signs,                    
                    conclusion: EnumAndOr.And));

            // B is never used
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                features: new List<Feature> {
                        _epiHist.Features[_featureIDs[(int)Features.IfEverUsed] + 1] },
                signs: new EnumSign[1] { EnumSign.e },
                thresholdParams: thresholdParams0,
                conclusion: EnumAndOr.And));

            // M1 is neer used
            thresholdParams = new List<Parameter>{_paramManager.Parameters[(int)DummyParam.D_0]};
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                features: new List<Feature> {
                        _epiHist.Features[_featureIDs[(int)Features.IfEverUsed] + 2] },
                signs: new EnumSign[1] { EnumSign.e },
                thresholdParams: thresholdParams0,
                conclusion: EnumAndOr.And));

            // turn on A
            _epiHist.Conditions.Add(new Condition_OnFeatures(
                id: id++,
                features: new List<Feature> {
                    _epiHist.Features[_featureIDs[(int)Features.Time]],
                    _epiHist.Features[_featureIDs[(int)Features.IfEverUsed]] },
                signs: new EnumSign[2] { EnumSign.qe, EnumSign.e },
                thresholdParams: thresholdParams00,
                conclusion: EnumAndOr.And));

            // turn off A
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.AOut],
                    _epiHist.Conditions[(int)Conditions.ABOut] },
                conclusion: EnumAndOr.Or));

            // turn on B
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.AOut],
                    _epiHist.Conditions[(int)Conditions.BOk],
                    _epiHist.Conditions[(int)Conditions.ABOk],
                    _epiHist.Conditions[(int)Conditions.BNeverUsed],
                    _epiHist.Conditions[(int)Conditions.MNeverUsed]},
                conclusion: EnumAndOr.And));

            // turn off B
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id++,
                conditions: new List<Condition> {
                    _epiHist.Conditions[(int)Conditions.BOut],
                    _epiHist.Conditions[(int)Conditions.ABOut] },
                conclusion: EnumAndOr.Or));

            // turn on M
            _epiHist.Conditions.Add(new Condition_OnConditions(
                id: id,
                conditions: new List<Condition> {
                    _epiHist.Conditions[id - 1] },
                conclusion: EnumAndOr.And));
            id++;

            // turn off M
            _epiHist.Conditions.Add(new Condition_AlwaysFalse(id:id++));

        }

        private List<Parameter> GetParamList(string paramName)
        {            
            return new List<Parameter>() { _paramManager.GetParameter(paramName) };
        }
        private List<Parameter> GetParamList(List<string> paramNames)
        {
            List<Parameter> list = new List<Parameter>();
            foreach (string name in paramNames)
                list.Add(GetParam(name));

            return list;
        }
        private List<Parameter> GetParamList(DummyParam dummyParam, int repeat)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < repeat; i++)
                list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        private List<Parameter> GetParamList(string paramName, int pos, int size, DummyParam dummyParam)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < size; i++)
                if (i == pos)
                    list.Add(GetParam(paramName));
                else
                    list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        private List<Parameter> GetParamList(int parID, int pos, int size, DummyParam dummyParam)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < size; i++)
                if (i == pos)
                    list.Add(_paramManager.Parameters[parID]);
                else
                    list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        private Parameter GetParam(string paramName)
        {
            return  _paramManager.GetParameter(paramName);
        }

        private string GetResistOrFail(ResistStates resistStat, Drugs drug)
        {
            string resistOrFail = "";
            switch (resistStat)
            {
                case ResistStates.G_0:
                    resistOrFail = (drug == Drugs.A1) ? "A" : "B";                   
                    break;
                case ResistStates.G_A:
                    resistOrFail = (drug == Drugs.A1) ? "F" : "AB";
                    break;
                case ResistStates.G_B:
                    resistOrFail = (drug == Drugs.A1) ? "AB" : "F";
                    break;
                case ResistStates.G_AB:
                    resistOrFail = "F";
                    break;
            }
            return resistOrFail;
        }
    }
}
