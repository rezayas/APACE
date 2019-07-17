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
    enum GonoSpecialStatIDs { PopSize = 0, Prev, FirstTx, SuccessAOrB, SuccessAOrBOrM, PercFirstTxAndResist }

    public class GonoSpecialStatInfo
    {        
        public List<int> SpecialStatIDs { get; set; }
        public List<string> Prev { get; set; }
        public List<List<string>> PrevSym { get; set; } // Sym, Asym
        public List<List<string>> PrevResist { get; set; } // 0, A, B, AB

        public List<string> Treated { get; set; }
        public List<string> TreatedAndSym { get; set; } // Sym, Asym
        public List<List<string>> TreatedResist { get; set; } // 0, A, B, AB

        public List<string> TreatedA1B1B2 { get; set; } // A1, B1, B2
        public List<string> TreatedM1M2 { get; set; } // M1, M2

        public List<int> SumStatIDsTxResist { get; set; } 
        public List<int> RatioStatIDsTxResist { get; set; } 

        public void Reset(int nRegions)
        {
            SpecialStatIDs = new List<int>(new int[Enum.GetValues(typeof(GonoSpecialStatIDs)).Length]);
            Prev = new List<string>();
            PrevSym = new List<List<string>>();
            PrevResist = new List<List<string>>();
            Treated = new List<string>();
            TreatedAndSym = new List<string>();
            TreatedResist = new List<List<string>>();

            SumStatIDsTxResist = new List<int>(new int[4]); // 4 for 0, A, B, AB
            RatioStatIDsTxResist = new List<int>(new int[4]); // 4 for 0, A, B, AB

            for (int i = 0; i < nRegions; i++)
            {
                Prev.Add("");
                PrevSym.Add(new List<string>() { "", "" }); // Sym, Asym
                PrevResist.Add(new List<string> { "", "", "", "" }); // 0, A, B, AB
                Treated.Add("");
                TreatedAndSym.Add("");
                TreatedResist.Add(new List<string> { "", "", "", "" }); // 0, A, B, AB
            }

            TreatedA1B1B2 = new List<string>() { "", "", "" };
            TreatedM1M2 = new List<string>() { "", ""};
        }
    }


    public abstract class GonoModel : ModelInstruction
    {
        protected enum Comparts { I, W, U }; // infection, waiting for treatment, waiting for retreatment 
        protected enum Drugs { A1, B1, B2 }; // 1st line treatment with A, 1st line treatment with B, and 2nd line treatment with B
        protected enum Ms { M1, M2 };  // 1st line treatment with M, and 2nd line treatment with M
        protected enum SymStates { Sym, Asym };
        protected enum ResistStates { G_0, G_A, G_B, G_AB }
        protected enum Interventions { A1 = 2, B1, M1, B2_A, M2_A, M2_B_AB } // A1:    A is used for 1st line treatment
                                                                             // B1:    B is used for 1st line treatment
                                                                             // M1:    M is used for 1st line treatment
                                                                             // B2_A:  retreating those infected with G_A with B after 1st line treatment failure
                                                                             // M2_A:  retreating those infected with G_A with M after 1st line treatment failure
                                                                             // M2_B_AB: retreating those infected with G_B or G_AB with M after 1st line treatment failure
        protected enum DummyParam { D_0, D_1, D_Minus1, D_Inf, T_Prev, T_DeltaPrev } // 0, 1, 2, 3, 4, 5

        protected enum Features { Time, PercResist, ChangeInPercResist, IfEverUsed }
        protected enum Conditions { AOut, BOut, ABOut, AOk, BOk, ABOk, BNeverUsed, MNeverUsed, AOn, AOff, BOn, BOff, MOn, MOff };
        protected List<int> _featureIDs = new List<int>(new int[Enum.GetValues(typeof(Features)).Length]);
        protected List<string> _infProfiles = new List<string>();
        protected GonoSpecialStatInfo _specialStatInfo = new GonoSpecialStatInfo();

        public GonoModel()
        {
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                {
                    _infProfiles.Add(s.ToString() + " | " + r.ToString());
                }
        }

        protected void AddGonoParameters(List<string> regions)
        {
            int parIDInitialPop = _paramManager.Dic["Initial population size | " + regions[0]];
            int parID1MinusPrev = _paramManager.Dic["1-Initial prevalence | " + regions[0]];

            // initial size of S
            for (int i = 0; i < regions.Count; i++)
                AddGonoParamSize_S(
                    region: regions[i], 
                    parIDInitialPop: parIDInitialPop + i, 
                    parID1MinusPrev: parID1MinusPrev + i);

            // initial size of I compartments
            int infProfileID = 0;
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    AddGonoParamSize_I(regions, s, r, infProfileID++);

        }

        private void AddGonoParamSize_S(string region, int parIDInitialPop, int parID1MinusPrev)
        {
            _paramManager.Add(new ProductParameter(
                ID: _paramManager.Parameters.Count,
                name: "Initial size of " + region + " | S",
                parameters: GetParamList(new List<int>() { parIDInitialPop, parID1MinusPrev }))
                );
        }

        private void AddGonoParamSize_I(List<string> regions, SymStates s, ResistStates r, int infProfileID)
        {
            List<int> paramIDs = new List<int>();
            int parID = _paramManager.Parameters.Count;
            int parIDInitialPopSize = _paramManager.Dic["Initial population size | " + regions[0]];
            int parIDInitialPrevalence = _paramManager.Dic["Initial prevalence | " + regions[0]];
            int parIDInitialSym = _paramManager.Dic["Initial symptomatic | " + regions[0]];
            int parIDInitialAsym = _paramManager.Dic["1-Initial symptomatic | " + regions[0]];
            int parIDInitialResistToA = _paramManager.Dic["Initial resistant to A | " + regions[0]];
            int parIDInitialResistToB = _paramManager.Dic["Initial resistant to B | " + regions[0]];
            int parIDInitialResistToAorB = _paramManager.Dic["1-Initial resistant to A or B | " + regions[0]];

            for (int regionID = 0; regionID < regions.Count; regionID++)
            {
                string parName = "Initial size of " + regions[regionID] + " | I | " + _infProfiles[infProfileID];

                // par ID for population size of this region
                paramIDs.Add(parIDInitialPopSize + regionID);
                // par ID for prevalence of gonorrhea in this region
                paramIDs.Add(parIDInitialPrevalence + regionID);

                // par ID for proportion symptomatic or asymptomatic 
                if (s == SymStates.Sym)
                    paramIDs.Add(parIDInitialSym + regionID);
                else
                    paramIDs.Add(parIDInitialAsym + regionID);

                // par ID for prevalence of resistance to A, B, or AB
                switch (r)
                {
                    case ResistStates.G_0:
                        paramIDs.Add(parIDInitialResistToAorB + regionID);
                        break;
                    case ResistStates.G_A:
                        paramIDs.Add(parIDInitialResistToA + regionID);
                        break;
                    case ResistStates.G_B:
                        paramIDs.Add(parIDInitialResistToB + regionID);
                        break;
                    case ResistStates.G_AB:
                        paramIDs.Add(0);
                        break;
                }

                if (r == ResistStates.G_AB)
                    _paramManager.Add(new IndependetParameter(
                        ID: parID++,
                        name: parName,
                        enumRandomVariateGenerator: RandomVariateLib.SupportProcedures.ConvertToEnumRVG("Constant"),
                        par1: 0, par2: 0, par3: 0, par4: 0)
                        );
                else
                    _paramManager.Add(new ProductParameter(
                        ID: parID++,
                        name: parName,
                        parameters: GetParamList(paramIDs))
                        );
            }
        }

        protected List<Parameter> GetParamList(string paramName)
        {
            return new List<Parameter>() { _paramManager.GetParameter(paramName) };
        }
        protected List<Parameter> GetParamList(List<int> paramIDs)
        {
            List<Parameter> list = new List<Parameter>();
            foreach (int i in paramIDs)
                list.Add(_paramManager.Parameters[i]);

            return list;
        }
        protected List<Parameter> GetParamList(List<string> paramNames)
        {
            List<Parameter> list = new List<Parameter>();
            foreach (string name in paramNames)
                list.Add(GetParam(name));

            return list;
        }
        protected List<Parameter> GetParamList(DummyParam dummyParam, int repeat)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < repeat; i++)
                list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        protected List<Parameter> GetParamList(string paramName, int pos, int size, DummyParam dummyParam)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < size; i++)
                if (i == pos)
                    list.Add(GetParam(paramName));
                else
                    list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        protected List<Parameter> GetParamList(int parID, int pos, int size, DummyParam dummyParam)
        {
            List<Parameter> list = new List<Parameter>();
            for (int i = 0; i < size; i++)
                if (i == pos)
                    list.Add(_paramManager.Parameters[parID]);
                else
                    list.Add(_paramManager.Parameters[(int)dummyParam]);
            return list;
        }
        protected Parameter GetParam(string paramName)
        {
            return _paramManager.GetParameter(paramName);
        }

        protected string GetResistOrFail(ResistStates resistStat, Drugs drug)
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

        protected void AddGonoClasses(List<string> regions)
        {
            int classID = 0;
            int regionID = 0;
            int infProfile = 0; // infection profile
            int parIDSize = 0;

            // add S's
            parIDSize = _paramManager.Dic["Initial size of " + regions[0] + " | S"];
            for (regionID = 0; regionID < regions.Count; regionID++)
            {
                Class_Normal S = Get_S(
                    id: classID,
                    region: regions[regionID],
                    parInitialSizeID: parIDSize + regionID);
                _classes.Add(S);
                _dicClasses[S.Name] = classID++;
            }

            // add classes to count the treatment outcomes
            // Success with A1, B1, or B2
            foreach (Drugs d in Enum.GetValues(typeof(Drugs)))
            {
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    Class_Normal c = Get_Success(id: classID, region: regions[regionID], drug: d.ToString());
                    _classes.Add(c);
                    _dicClasses[c.Name] = classID++;
                    _specialStatInfo.TreatedA1B1B2[(int)d] += c.ID + "+";
                }
            }

            // Success with M1 or M2
            foreach (Ms m in Enum.GetValues(typeof(Ms)))
            {
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    Class_Normal c = Get_Success(id: classID, region: regions[regionID], drug: m.ToString());
                    _classes.Add(c);
                    _dicClasses[c.Name] = classID++;
                    _specialStatInfo.TreatedM1M2[(int)m] += c.ID + "+";
                }
            }

            // add death
            for (regionID = 0; regionID < regions.Count; regionID++)
            {
                Class_Death D = Get_D(id: classID, region: regions[regionID]);
                _classes.Add(D);
                _dicClasses[D.Name] = classID++;
            }

            // add I's, W's, and U's, 
            // example: "I | Sym | G_0"     
            int infictivityParID = _paramManager.Dic["Infectivity of | I | " + _infProfiles[0]];
            parIDSize = _paramManager.Dic["Initial size of " + regions[0] + " | I | " + _infProfiles[0]];
            foreach (Comparts c in Enum.GetValues(typeof(Comparts)))
            {
                infProfile = 0;
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    {
                        for (regionID = 0; regionID < regions.Count; regionID++)
                        {
                            Class_Normal C = Get_I_W_U(
                                id: classID,
                                region: regions[regionID],
                                infProfileID: infProfile,
                                c: c,
                                r: r,
                                parIDSize: (c == Comparts.I) ? parIDSize++ : (int)DummyParam.D_0,
                                infectivityParID: infictivityParID + infProfile);
                            _classes.Add(C);
                            _dicClasses[C.Name] = classID++;

                            // update formulas of special statistics 
                            _specialStatInfo.Prev[0] += C.ID + "+";
                            if (s == SymStates.Sym)
                                _specialStatInfo.PrevSym[0][0] += C.ID + "+";
                            else
                                _specialStatInfo.PrevSym[0][1] += C.ID + "+";

                            if (r != ResistStates.G_0)
                                _specialStatInfo.PrevResist[0][(int)r] += C.ID + "+";
                            
                            // special statics on treatment
                            if (c == Comparts.W)
                            {
                                _specialStatInfo.Treated[0] += C.ID + "+";
                                if (s == SymStates.Sym)
                                    _specialStatInfo.TreatedAndSym[0] += C.ID + "+";

                                if (r != ResistStates.G_0)
                                {
                                    _specialStatInfo.TreatedResist[0][(int)r] += C.ID + "+";
                                    if (regions.Count > 1)
                                        _specialStatInfo.TreatedResist[regionID+1][(int)r] += C.ID + "+";
                                }
                                
                            }
                        }
                        ++infProfile;
                    }
            }

            // Prob symptomatic after infection
            int classIDIfSymp = _dicClasses[regions[0] + " | I | Sym | G_0"];
            int classIDIfAsymp = _dicClasses[regions[0] + " | I | Asym | G_0"];
            int ifSymParID = _paramManager.Dic["Prob sym | G_0"];
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
            {
                for (regionID = 0; regionID < regions.Count; regionID++)
                {
                    Class_Splitting ifSym = Get_IfSym(
                        id: classID,
                        region: regions[regionID],
                        r: r,
                        ifSymParID: ifSymParID + (int)r,
                        classIDIfSym: classIDIfSymp++,
                        classIDIfAsym: classIDIfAsymp++
                        );
                    _classes.Add(ifSym);
                    _dicClasses[ifSym.Name] = classID++;
                }
            }

            // if seeking retreatment after resistance or failure
            // examples "If retreat A | A --> I | Sym | G_0"
            //          "If retreat F | A --> I | Sym | G_A"
            int parIDProbRetreatIfSym = _paramManager.Dic["Prob retreatment | Sym"];
            int parIDProbRetreatIfAsym = _paramManager.Dic["Prob retreatment | Asym"];
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs)))   // A1, B1, B2
                // assume that failure after B2 will always seek retreatment 
                if (drug == Drugs.A1 || drug == Drugs.B1)
                {
                    infProfile = 0;
                    foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                        foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        {
                            for (regionID = 0; regionID < regions.Count; regionID++)
                            {
                                Class_Splitting ifRetreat = Get_IfRetreat(
                                    id: classID,
                                    region: regions[regionID],
                                    r: r,
                                    s: s,
                                    drug: drug,
                                    infProfile: infProfile,
                                    parIDProbRetreat: (s == SymStates.Sym) ? parIDProbRetreatIfSym : parIDProbRetreatIfAsym);
                                _classes.Add(ifRetreat);
                                _dicClasses[ifRetreat.Name] = classID++;

                            }
                            ++infProfile;
                        }
                }

            // if symptomatic after the emergence of resistance
            // example: "If sym | A | A --> I | Asym | G_0"
            //          true    -> "If retreat A | A --> I | Sym | G_0"
            //          false   -> "I | Asym | G_A"         
            int parIDProbSym = _paramManager.Dic["Prob sym | G_0"];
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs))) // A1, B1, or B2
                foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                    for (regionID = 0; regionID < regions.Count; regionID++)
                    {
                        string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                        // if developed resistance
                        if (resistOrFail != "F")
                        {
                            Class_Splitting ifSymp = Get_IfSymAfterR(
                                id: classID,
                                region: regions[regionID],
                                resistOrFail: resistOrFail,
                                r: r,
                                drug: drug);
                            _classes.Add(ifSymp);
                            _dicClasses[ifSymp.Name] = classID++;
                        }
                    }

            // treatment outcomes (resistance)    
            // example: "If A | A --> I | Sym | G_0"
            //          true: "If retreat A | A --> I | Sym | G_0"
            //          false: "Success A1"
            // example: "If A | A --> I | Asym | G_0"
            //          true: "If sym | A | A --> I | Asym | G_0"
            //          false: "Success A1"
            int classIDSuccess = _dicClasses[regions[0] + " | Success with " + Drugs.A1.ToString()];
            int parIDProbResistA = _paramManager.Dic["Prob resistance | Drug A"];
            int parIDProbResistB = parIDProbResistA + 1;
            foreach (Drugs drug in Enum.GetValues(typeof(Drugs))) // A1, B1, B2
                foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
                    foreach (ResistStates r in Enum.GetValues(typeof(ResistStates)))
                        for (regionID = 0; regionID < regions.Count; regionID++)
                        {
                            // considering only resistance outcome
                            string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
                            if (resistOrFail == "F")
                                continue;

                            Class_Splitting ifResist = Get_ifR(
                                id: classID,
                                region: regions[regionID],
                                resistOrFail: resistOrFail,
                                r: r,
                                s: s,
                                drug: drug,
                                parIDProbResist: (drug == Drugs.A1) ? parIDProbResistA : parIDProbResistB,
                                classIDSuccess: classIDSuccess + (int)drug);
                            _classes.Add(ifResist);
                            _dicClasses[ifResist.Name] = classID++;
                        }
        }

        protected void AddGonoSumStats(List<string> regions)
        {
            int id = 0;
            int regionID = 0;
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
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.PopSize] = id - 1;

            // gonorrhea prevalence
            _epiHist.SumTrajs.Add(
                new SumClassesTrajectory(
                    ID: id++,
                    name: "Prevalence",
                    strType: "Prevalence",
                    sumFormula: _specialStatInfo.Prev[0],
                    displayInSimOutput: true,
                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                    );
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Prev] = id - 1;

            // prevalence of symptomatic gonorrhea 
            foreach (SymStates s in Enum.GetValues(typeof(SymStates)))
            {
                if (s == SymStates.Sym)
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Prevalence | " + s.ToString(),
                            strType: "Prevalence",
                            sumFormula: _specialStatInfo.PrevSym[0][(int)s],
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
                            sumFormula: _specialStatInfo.PrevResist[0][(int)r],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );

            // received first-line treatment (= number of cases)
            SumClassesTrajectory t1st = new SumClassesTrajectory(
                ID: id++,
                name: "Received 1st Tx",
                strType: "Incidence",
                sumFormula: _specialStatInfo.Treated[0],
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
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.FirstTx] = id - 1;

            // received first-line treatment and symptomatic 
            _epiHist.SumTrajs.Add(new SumClassesTrajectory(
                ID: id++,
                name: "Received 1st Tx & Symptomatic",
                strType: "Incidence",
                sumFormula: _specialStatInfo.TreatedAndSym[0],
                displayInSimOutput: true,
                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                );

            // received first-line treatment by resistance status
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
                if (r != ResistStates.G_0)
                {
                    _epiHist.SumTrajs.Add(
                        new SumClassesTrajectory(
                            ID: id++,
                            name: "Received 1st Tx & Resistant to " + r.ToString(),
                            strType: "Incidence",
                            sumFormula: _specialStatInfo.TreatedResist[0][(int)r],
                            displayInSimOutput: true,
                            warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                            nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                            );

                    // received first-line treatment by resistance status and region
                    if (regions.Count > 1)
                    {
                        _specialStatInfo.SumStatIDsTxResist[(int)r] = id;
                        for (regionID = 0; regionID < regions.Count; regionID++)
                        {
                            _epiHist.SumTrajs.Add(
                                new SumClassesTrajectory(
                                    ID: id++,
                                    name: "Received 1st Tx & Resistant to " + r.ToString() + " | " + regions[regionID],
                                    strType: "Incidence",
                                    sumFormula: _specialStatInfo.TreatedResist[1 + regionID][(int)r],
                                    displayInSimOutput: true,
                                    warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                                    nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval)
                                    );
                        }
                    }

                }           

            // sucessful treatment
            string success1st =
                _specialStatInfo.TreatedA1B1B2[(int)Drugs.A1] +
                _specialStatInfo.TreatedA1B1B2[(int)Drugs.B1] +
                _specialStatInfo.TreatedM1M2[(int)Ms.M1];
            string successAorB =
                _specialStatInfo.TreatedA1B1B2[(int)Drugs.A1] +
                _specialStatInfo.TreatedA1B1B2[(int)Drugs.B1] +
                _specialStatInfo.TreatedA1B1B2[(int)Drugs.B2];
            string successM =
                _specialStatInfo.TreatedM1M2[(int)Ms.M1] +
                _specialStatInfo.TreatedM1M2[(int)Ms.M2];
            string successAll = successAorB + successM;

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
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.SuccessAOrB] = id - 1;

            // sucessfully treated with M
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

            // sucessfully treated 
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
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.SuccessAOrBOrM] = id - 1;

            // update times series of summation statistics
            UpdateSumStatTimeSeries();
        }

        protected void AddGonoRatioStatistics(List<string> regions)
        {
            int id = _epiHist.SumTrajs.Count();
            int idPopSize = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.PopSize];
            int idPrevalence = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.Prev];
            int idFirstTx = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.FirstTx];
            int idSuccessAOrB = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.SuccessAOrB];
            int idSuccessAOrBOrM = _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.SuccessAOrBOrM];
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
                    ifCheckWithinFeasibleRange: true,
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
            _specialStatInfo.SpecialStatIDs[(int)GonoSpecialStatIDs.PercFirstTxAndResist] = id;
            foreach (ResistStates r in Enum.GetValues(typeof(ResistStates))) // G_0, G_A, G_B, G_AB
            {
                if (r != ResistStates.G_0)
                {
                    RatioTrajectory firstTx = new RatioTrajectory(
                        id: id,
                        name: "% received 1st Tx & resistant to " + r.ToString(),
                        strType: "Incidence/Incidence",
                        ratioFormula: (idFirstTx + 1 + (int)r) + "/" + idFirstTx,   // 1 here is for symptomatics
                        displayInSimOutput: true,
                        warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                        nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);
                    if (_modelSets.ModelUse == EnumModelUse.Calibration && r != ResistStates.G_AB)
                        firstTx.CalibInfo = new SpecialStatCalibrInfo(
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

                    // TODO: check this
                    // % received 1st Tx and resistant to A, B, or AB (incidence) by region
                    if (regions.Count > 1)
                    {
                        _specialStatInfo.RatioStatIDsTxResist[(int)r] = id;
                        int firstID = _specialStatInfo.SumStatIDsTxResist[(int)r];
                        for (int regionID = 0; regionID < regions.Count; regionID++)
                        {
                            RatioTrajectory traj = new RatioTrajectory(
                                id: id,
                                name: "% received 1st Tx & resistant to " + r.ToString() + " | " + regions[regionID],
                                strType: "Incidence/Incidence",
                                ratioFormula: (firstID + regionID) + "/" + firstID,
                                displayInSimOutput: true,
                                warmUpSimIndex: _modelSets.WarmUpPeriodSimTIndex,
                                nDeltaTInAPeriod: _modelSets.NumOfDeltaT_inSimOutputInterval);

                            _epiHist.SurveyedIncidenceTrajs.Add(
                                new SurveyedIncidenceTrajectory(
                                    id: id,
                                    name: "% received 1st Tx & resistant to " + r.ToString(),
                                    displayInSimOutput: true,
                                    firstObsMarksStartOfEpidemic: false,
                                    sumClassesTrajectory: null,
                                    sumEventTrajectory: null,
                                    ratioTrajectory: traj,
                                    nDeltaTsObsPeriod: _modelSets.NumOfDeltaT_inObservationPeriod,
                                    nDeltaTsDelayed: 0,
                                    noise_nOfDemoninatorSampled: nIsolateTested,
                                    ratioNoiseN: 1 / regions.Count)
                                    );
                            _epiHist.RatioTrajs.Add(traj);
                            id++;
                        }
                    }
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


        private Class_Normal Get_S(int id, string region, int parInitialSizeID)
        {
            Class_Normal S = new Class_Normal(id, region + " | S");
            S.SetupInitialAndStoppingConditions(
                initialMembersPar: _paramManager.Parameters[parInitialSizeID]);
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

        private Class_Normal Get_I_W_U(int id, string region, int infProfileID,
            Comparts c, ResistStates r, int parIDSize, int infectivityParID)
        {
            Class_Normal C = new Class_Normal(
                ID: id,
                name: region + " | " + c.ToString() + " | " + _infProfiles[infProfileID]);
            C.SetupInitialAndStoppingConditions(
                initialMembersPar: _paramManager.Parameters[parIDSize],
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

        private Class_Splitting Get_IfSym(int id, string region, ResistStates r,
            int ifSymParID, int classIDIfSym, int classIDIfAsym)
        {
            Class_Splitting ifSym = new Class_Splitting(id, region + " | If Sym | " + r.ToString());
            ifSym.SetUp(
                parOfProbSucess: _paramManager.Parameters[ifSymParID],
                destinationClassIDIfSuccess: classIDIfSym,
                destinationClassIDIfFailure: classIDIfAsym);
            SetupClassStatsAndTimeSeries(thisClass: ifSym);
            return ifSym;
        }

        private Class_Splitting Get_IfRetreat(int id, string region,
            ResistStates r, SymStates s, Drugs drug, int infProfile, int parIDProbRetreat)
        {
            string resistOrFail = GetResistOrFail(resistStat: r, drug: drug);
            string className = region + " | If retreat " + resistOrFail + " | " + drug.ToString() + " --> I | " + _infProfiles[infProfile];

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
                parOfProbSucess: _paramManager.Parameters[parIDProbRetreat],
                destinationClassIDIfSuccess: _dicClasses[classIfSeekTreatment],
                destinationClassIDIfFailure: _dicClasses[classIfNotSeekTreatment]
                );
            SetupClassStatsAndTimeSeries(thisClass: ifRetreat);
            return ifRetreat;
        }

        private Class_Splitting Get_IfSymAfterR(int id, string region,
            string resistOrFail, ResistStates r, Drugs drug)
        {
            string className = region + " | If sym | " + resistOrFail + " | " + drug.ToString() + " --> I | Asym | " + r.ToString();
            string classIfSym = "", classIfAsym = "";

            // assuming that failure after B2 will receive M2
            if (drug == Drugs.A1 || drug == Drugs.B1)
                classIfSym = region + " | If retreat " + resistOrFail + " | " + drug.ToString() + " --> I | Sym | " + r.ToString();
            else
                classIfSym = region + " | Success with M2";
            classIfAsym = region + " | I | Asym | G_" + resistOrFail;

            Class_Splitting ifSymp = new Class_Splitting(id, className);
            ifSymp.SetUp(
                parOfProbSucess: GetParam("Prob sym | G_" + resistOrFail),
                destinationClassIDIfSuccess: _dicClasses[classIfSym],
                destinationClassIDIfFailure: _dicClasses[classIfAsym]
                );
            SetupClassStatsAndTimeSeries(thisClass: ifSymp);
            return ifSymp;
        }

        private Class_Splitting Get_ifR(int id, string region, string resistOrFail,
            ResistStates r, SymStates s, Drugs drug, int parIDProbResist, int classIDSuccess)
        {
            string strInfProfile = "I | " + s.ToString() + " | " + r.ToString();  // "I | Sym | G_0"                
            string treatmentProfile = resistOrFail + " | " + drug.ToString() + " --> " + strInfProfile; // "A | A --> I | Sym | G_0"
            string className = region + " | If " + treatmentProfile; // "If A | A --> I | Sym | G_0"
            string classIfResist = "";

            // find the destination classes
            if (drug == Drugs.A1 || drug == Drugs.B1)
            {
                // if already symptomatic 
                if (s == SymStates.Sym)
                    classIfResist = region + " | If retreat " + treatmentProfile;
                else // if not symtomatic
                    classIfResist = region + " | If sym | " + treatmentProfile;
            }
            else // if already received B2
            {
                classIfResist = region + " | U | " + s.ToString() + " | G_" + resistOrFail;
            }

            // make the splitting class
            Class_Splitting ifResist = new Class_Splitting(id, className);
            ifResist.SetUp(
                parOfProbSucess: _paramManager.Parameters[parIDProbResist],
                destinationClassIDIfSuccess: _dicClasses[classIfResist],
                destinationClassIDIfFailure: classIDSuccess);
            SetupClassStatsAndTimeSeries(thisClass: ifResist, showIncidence: true);
            return ifResist;
        }
    }
    
}