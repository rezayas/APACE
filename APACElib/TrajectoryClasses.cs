﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimulationLib;
using ComputationLib;
using RandomVariateLib;

namespace APACElib
{
    public abstract class TimeSeries
    {
        public List<double?> Recordings { get; set; } = new List<double?>();
        protected int _nRecordingsInThisPeriod = 0;
        protected int _nRecodingsInEachPeriod;

        public TimeSeries(int nRecodingsInEachPeriod)
        {
            _nRecodingsInEachPeriod = nRecodingsInEachPeriod;
        }

        public abstract void Record(double? value);

        /// <returns> the last period recording </returns>
        public double? GetLastRecording()
        {
            if (Recordings.Count == 0)
                return null;
            else
                return Recordings.Last();
        }
        /// <returns> the last period observations when there is a delay </returns>
        public double? GetLastRecording(int nPeriodDelay)
        {
            if (Recordings.Count < nPeriodDelay)
                return null;
            else
                return Recordings[Recordings.Count - nPeriodDelay - 1];
        }

        public abstract void Reset();
    }

    public class IncidenceTimeSeries: TimeSeries
    {
        // Assumption: The first observation is null that represent the observation during the obseration period right before time 0. 
        public IncidenceTimeSeries(int nOfRecodingsInEachPeriod):base(nOfRecodingsInEachPeriod)
        {
        }

        public override void Record(double? value)
        {
            // keep adding the entered values to the last observation until enought recordings are gathered,
            // then add a new element to the list of recordings

            // find if a new element should be added to the list
            if (_nRecordingsInThisPeriod % _nRecodingsInEachPeriod == 0)
            {
                Recordings.Add(value);
                _nRecordingsInThisPeriod = 1;
            }
            else
            {
                // increment the observation in the current period
                Recordings[Recordings.Count-1] += value;
                ++_nRecordingsInThisPeriod;
            }
        }

        public override void Reset()
        {
            Recordings.Clear();
            Recordings.Add(null); // first observation is always null (see above)
            _nRecordingsInThisPeriod = 0;
        }
    }
     
    public class PrevalenceTimeSeries : TimeSeries
    {
        public PrevalenceTimeSeries(int nRecodingsInEachObsPeriod) : base(nRecodingsInEachObsPeriod)
        {
        }

        public override void Record(double? value)
        {
            // find if a new element should be added to the list
            if (_nRecordingsInThisPeriod % _nRecodingsInEachPeriod == 0)
            {
                Recordings.Add(value);
                _nRecordingsInThisPeriod = 1;
            }
            else
                ++_nRecordingsInThisPeriod;
        }

        public override void Reset()
        {
            Recordings.Clear();
            _nRecordingsInThisPeriod = 0;
        }
    }

    // to store data points from one epidemic output along with the cost and health outcomes
    public class OneDimTrajectory
    {
        protected int _warmUpSimIndex;

        // statistics 
        public int ID { get; set; }
        public string Name { get; set; }

        public bool CollectAccumIncidenceStats { get; set; } = false;
        public bool CollectPrevalenceStats { get; set; } = false;

        public int NumOfNewMembersOverPastPeriod { get; set; }
        public int Prevalence { get; set; }
        public int AccumulatedIncidence { get; set; }
        public int AccumulatedIncidenceAfterWarmUp { get; set; }
        
        // health and cost outcomes
        public DeltaTCostHealth DeltaCostHealthCollector { get; set; }        

        // time-series
        public IncidenceTimeSeries IncidenceTimeSeries { get; set; }
        public PrevalenceTimeSeries PrevalenceTimeSeries { get; set; }
        public PrevalenceTimeSeries AccumIncidenceTimeSeries { get; set; }
        // average incidence prevalence
        public ObsBasedStat AveragePrevalenceStat { get; set; }

        public OneDimTrajectory(int id, string name, int warmUpSimIndex)
        {
            ID = id;
            Name = name;
            _warmUpSimIndex = warmUpSimIndex;
        }

        public void SetupStatisticsCollectors(bool accumIncidence, bool prevalence)
        {
            CollectAccumIncidenceStats = accumIncidence;
            CollectPrevalenceStats = prevalence;
            if (CollectPrevalenceStats)
                AveragePrevalenceStat = new ObsBasedStat("Average prevalence");
        }

        public void AddTimeSeries(bool collectIncidence, bool collectPrevalence, bool collectAccumIncidence, int nDeltaTInAPeriod)
        {
            if (collectIncidence)
                IncidenceTimeSeries = new IncidenceTimeSeries(nDeltaTInAPeriod);
            if (collectPrevalence)
                PrevalenceTimeSeries = new PrevalenceTimeSeries(nDeltaTInAPeriod);
            if (collectAccumIncidence)
                AccumIncidenceTimeSeries = new PrevalenceTimeSeries(nDeltaTInAPeriod);
        }

        public void Add(int value)
        {
            NumOfNewMembersOverPastPeriod += value;
            Prevalence += value;                       
        }

        public void CollectEndOfDeltaTStats(int simIndex)
        {
            // accumulated incidence
            if (CollectAccumIncidenceStats)
            {
                AccumulatedIncidence += NumOfNewMembersOverPastPeriod;
                if (simIndex >= _warmUpSimIndex)
                    AccumulatedIncidenceAfterWarmUp += NumOfNewMembersOverPastPeriod;
            }

            // average prevalence
            if (CollectPrevalenceStats)
                AveragePrevalenceStat.Record(Prevalence);

            // time series
            if (!(IncidenceTimeSeries is null) && simIndex>0)
                IncidenceTimeSeries.Record(NumOfNewMembersOverPastPeriod);
            if (!(PrevalenceTimeSeries is null))
                PrevalenceTimeSeries.Record(Prevalence);
            if (!(AccumIncidenceTimeSeries is null))
                AccumIncidenceTimeSeries.Record(AccumulatedIncidence);

            // cost and health outcomes
            if (!(DeltaCostHealthCollector is null))
                DeltaCostHealthCollector.Update(simIndex, Prevalence, NumOfNewMembersOverPastPeriod);
        }

        public void Reset()
        {
            NumOfNewMembersOverPastPeriod = 0;
            Prevalence = 0;
            AccumulatedIncidence = 0;
            AccumulatedIncidenceAfterWarmUp = 0;
            if (IncidenceTimeSeries != null)
                IncidenceTimeSeries.Reset();
            if (PrevalenceTimeSeries != null)
                PrevalenceTimeSeries.Reset();
            if (AccumIncidenceTimeSeries != null)
                AccumIncidenceTimeSeries.Reset();
            if (DeltaCostHealthCollector != null)
                DeltaCostHealthCollector.Reset();
        }
    }

    // abstract class to manage summation statistics
    public abstract class SumTrajectory : OneDimTrajectory
    {
        public enum EnumType
        {
            Incidence = 0,
            AccumulatingIncident = 1,
            Prevalence = 2,
        }
        public enum EnumDefinedOn
        {
            Classes = 0,
            Events = 1,
        }

        public Boolean DisplayInSimOutput { get; set; }
        public EnumType Type { get; set; }
        public SpecialStatCalibrInfo CalibInfo { get; set; }

        public SumTrajectory(
            int ID,
            string name,
            string strType,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTInAPeriod)
            : base(ID, name, warmUpSimIndex)
        {
            // type
            switch (strType)
            {
                case "Incidence":
                    Type = EnumType.Incidence;
                    break;
                case "Prevalence":
                    Type = EnumType.Prevalence;
                    break;
                case "Accumulating Incidence":
                    Type = EnumType.AccumulatingIncident;
                    break;
            }
            DisplayInSimOutput = displayInSimOutput;
            switch (Type)
            {
                case EnumType.Incidence:
                    SetupStatisticsCollectors(
                        accumIncidence: true,
                        prevalence: false
                        );
                    AddTimeSeries(
                        collectIncidence: true,
                        collectPrevalence: false,
                        collectAccumIncidence: false,
                        nDeltaTInAPeriod: nDeltaTInAPeriod
                        );
                    break;

                case EnumType.AccumulatingIncident:
                    SetupStatisticsCollectors(
                        accumIncidence: true,
                        prevalence: false
                        );
                    AddTimeSeries(
                        collectIncidence: false,
                        collectPrevalence: false,
                        collectAccumIncidence: false,
                        nDeltaTInAPeriod: nDeltaTInAPeriod
                        );
                    break;

                case EnumType.Prevalence:
                    SetupStatisticsCollectors(
                        accumIncidence: false,
                        prevalence: true
                        );
                    AddTimeSeries(
                        collectIncidence: false,
                        collectPrevalence: false,
                        collectAccumIncidence: false,
                        nDeltaTInAPeriod: nDeltaTInAPeriod
                        );
                    break;
            }
        }

        public abstract bool Add(int simIndex, ref List<Class> classes, ref List<Event> events);

        public double? GetLastRecording()
        {
            switch (Type)
            {
                case EnumType.Incidence:
                    return IncidenceTimeSeries.GetLastRecording();
                case EnumType.AccumulatingIncident:
                    return AccumulatedIncidenceAfterWarmUp;
                case EnumType.Prevalence:
                    return PrevalenceTimeSeries.GetLastRecording();
            }
            return null;
        }

        // convert sum formula into the array of class IDs or event IDs
        protected int[] ConvertSumFormulaToArrayOfIDs(string formula)
        {
            string[] arrClassIDs = formula.Split('+');
            return Array.ConvertAll<string, int>(arrClassIDs, Convert.ToInt32);
        }

    }

    // summation statistics defined on classes
    public class SumClassesTrajectory: SumTrajectory
    {
        public int[] ClassIDs { get; private set; }

        public SumClassesTrajectory(
            int ID,
            string name,
            string strType,
            string sumFormula,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTInAPeriod) 
            :base(ID, name, strType, displayInSimOutput, warmUpSimIndex, nDeltaTInAPeriod)
        {           
            ClassIDs = ConvertSumFormulaToArrayOfIDs(sumFormula);
        }

        public override bool Add(int simIndex, ref List<Class> classes, ref List<Event> events)
        {
            bool ifFeasiableRangesViolated = false;

            switch (Type)
            {
                case EnumType.AccumulatingIncident:
                    {
                        NumOfNewMembersOverPastPeriod = 0;
                        for (int i = 0; i < ClassIDs.Length; ++i)
                            NumOfNewMembersOverPastPeriod += classes[ClassIDs[i]].ClassStat.NumOfNewMembersOverPastPeriod;
                        CollectEndOfDeltaTStats(simIndex);

                        if (!(CalibInfo is null) && CalibInfo.IfCheckWithinFeasibleRange)
                        {
                            if (AccumulatedIncidenceAfterWarmUp < CalibInfo.LowFeasibleRange 
                                || AccumulatedIncidenceAfterWarmUp > CalibInfo.UpFeasibleRange)
                                ifFeasiableRangesViolated = true;
                        }
                    }
                    break;
                case EnumType.Incidence:
                    {
                        if (simIndex > 0)
                        {
                            NumOfNewMembersOverPastPeriod = 0;
                            for (int i = 0; i < ClassIDs.Length; ++i)
                                NumOfNewMembersOverPastPeriod += classes[ClassIDs[i]].ClassStat.NumOfNewMembersOverPastPeriod;
                            CollectEndOfDeltaTStats(simIndex);
                        }
                         
                        if (!(CalibInfo is null) && CalibInfo.IfCheckWithinFeasibleRange)
                        {
                            if (IncidenceTimeSeries.GetLastRecording() < CalibInfo.LowFeasibleRange
                                || IncidenceTimeSeries.GetLastRecording() > CalibInfo.UpFeasibleRange)
                                ifFeasiableRangesViolated = true;
                        }
                    }
                    break;
                case EnumType.Prevalence:
                    {
                        Prevalence = 0;
                        for (int i = 0; i < ClassIDs.Length; ++i)
                            Prevalence += classes[ClassIDs[i]].ClassStat.Prevalence;
                        CollectEndOfDeltaTStats(simIndex);

                        if (!(CalibInfo is null) && CalibInfo.IfCheckWithinFeasibleRange)
                        {
                            if (Prevalence < CalibInfo.LowFeasibleRange
                                || Prevalence > CalibInfo.UpFeasibleRange)
                                ifFeasiableRangesViolated = true;
                        }
                    }
                    break;
            }

            return ifFeasiableRangesViolated;
        }        
    }

    // summation statistics defined on events
    public class SumEventTrajectory: SumTrajectory
    {
        int[] _arrEventIDs;

        public SumEventTrajectory(
            int ID,
            string name,
            string strType,
            string sumFormula,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTInAPeriod) 
            :base(ID, name, strType, displayInSimOutput, warmUpSimIndex, nDeltaTInAPeriod)
        {
            _arrEventIDs = ConvertSumFormulaToArrayOfIDs(sumFormula);
        }

        public override bool Add(int simIndex, ref List<Class> classes, ref List<Event> events)
        {
            bool ifFeasiableRangesViolated = false; 

            if (simIndex == 0)
                return false;

            NumOfNewMembersOverPastPeriod = 0;
            for (int i = 0; i < _arrEventIDs.Length; ++i)
                NumOfNewMembersOverPastPeriod += events[_arrEventIDs[i]].MembersOutOverPastDeltaT;

            CollectEndOfDeltaTStats(simIndex);

            // check for feasibility
            if (!(CalibInfo is null) && CalibInfo.IfCheckWithinFeasibleRange)
            {
                if (IncidenceTimeSeries.GetLastRecording() < CalibInfo.LowFeasibleRange ||
                    IncidenceTimeSeries.GetLastRecording() > CalibInfo.LowFeasibleRange)
                    ifFeasiableRangesViolated = true;
            }

            return ifFeasiableRangesViolated;
        }
    }    

    // ratio statistics 
    public class RatioTrajectory
    {
        public enum EnumType
        {
            IncidenceOverIncidence = 0,
            AccumulatedIncidenceOverAccumulatedIncidence = 1,
            PrevalenceOverPrevalence = 2,
            IncidenceOverPrevalence = 3,
        }
        public SpecialStatCalibrInfo CalibInfo { get; set; }

        public int ID { get; }
        public string Name { get; }
        public int NominatorSpecialStatID { get; }
        public int DenominatorSpecialStatID { get; }
        public Boolean DisplayInSimOutput { get; }
        public PrevalenceTimeSeries TimeSeries { get; set; }    // treating all ratio statistics as prevalence
        public ObsBasedStat AveragePrevalenceStat { get; set; }
        public double? Ratio { get; private set; } = null;
        public int Denom { get; private set; }  // denominator value
        public EnumType Type { get; set; }
        
        int _warmUpSimIndex;

        public RatioTrajectory(
            int id,
            string name,
            string strType,
            string ratioFormula,
            bool displayInSimOutput,
            int warmUpSimIndex,
            int nDeltaTInAPeriod)            
        {
            ID = id;

            Name = name;
            // find the type
            switch (strType)
            {
                case "Incidence/Incidence":
                    Type = EnumType.IncidenceOverIncidence;
                    break;
                case "Accumulated Incidence/Accumulated Incidence":
                    Type = EnumType.AccumulatedIncidenceOverAccumulatedIncidence;
                    break;
                case "Prevalence/Prevalence":
                    Type = EnumType.PrevalenceOverPrevalence;
                    break;
                case "Incidence/Prevalence":
                    Type = EnumType.IncidenceOverPrevalence;
                    break;
            }

            DisplayInSimOutput = displayInSimOutput;
            int[] arrRatio = ConvertRatioFormulaToArrayOfClassIDs(ratioFormula);
            NominatorSpecialStatID = arrRatio[0];
            DenominatorSpecialStatID = arrRatio[1];
            _warmUpSimIndex = warmUpSimIndex;

            TimeSeries = new PrevalenceTimeSeries(nDeltaTInAPeriod);
            
            if (Type == EnumType.PrevalenceOverPrevalence)
                AveragePrevalenceStat = new ObsBasedStat("Average prevalence");
        }

        public bool Add(int simIndex, List<SumTrajectory> sumTrajectories)
        {
            Denom = -1;
            bool ifFeasiableRangesViolated = false;

            switch (Type)
            {
                case EnumType.PrevalenceOverPrevalence:
                    {
                        Denom = sumTrajectories[DenominatorSpecialStatID].Prevalence;
                        Ratio = (double)sumTrajectories[NominatorSpecialStatID].Prevalence
                            / Denom;
                    }
                    break;
                case EnumType.IncidenceOverIncidence:
                    {
                        double? denom = sumTrajectories[DenominatorSpecialStatID].GetLastRecording();
                        if (denom.HasValue)
                        {
                            Denom = (int)denom;
                            Ratio = (double)sumTrajectories[NominatorSpecialStatID].GetLastRecording()
                                / Denom;
                        }
                        else
                            Ratio = null;
                    }
                    break;
                case EnumType.AccumulatedIncidenceOverAccumulatedIncidence:
                    {
                        Denom = sumTrajectories[DenominatorSpecialStatID].AccumulatedIncidenceAfterWarmUp;
                        Ratio = (double)sumTrajectories[NominatorSpecialStatID].AccumulatedIncidenceAfterWarmUp
                            / Denom;
                    }
                    break;
                case EnumType.IncidenceOverPrevalence:
                    {
                        Denom = sumTrajectories[DenominatorSpecialStatID].Prevalence;
                        double? incidence = sumTrajectories[NominatorSpecialStatID].GetLastRecording();
                        if (incidence.HasValue)
                            Ratio = incidence.Value
                                / Denom;
                        else
                            Ratio = null;
                    }
                    break;
            }

            // record the ratio
            TimeSeries.Record(Ratio);

            // collect avergae prevalence statistics
            if (simIndex >= _warmUpSimIndex && AveragePrevalenceStat != null)
                AveragePrevalenceStat.Record(Ratio.Value);
            
            // check if within feasible range
            if (!(CalibInfo is null) && CalibInfo.IfCheckWithinFeasibleRange)
            {
                if (Ratio < CalibInfo.LowFeasibleRange || Ratio > CalibInfo.UpFeasibleRange)
                    ifFeasiableRangesViolated = true;
            }

            return ifFeasiableRangesViolated;
        }

        // convert ratio formula into the array of class IDs
        private int[] ConvertRatioFormulaToArrayOfClassIDs(string formula)
        {
            string[] arrClassIDs = formula.Split('/');
            return Array.ConvertAll<string, int>(arrClassIDs, Convert.ToInt32);
        }

        public void Reset()
        {
            TimeSeries.Reset();
            Ratio = double.NaN;
            if (Type == EnumType.PrevalenceOverPrevalence)
                AveragePrevalenceStat.Reset();
        }
    }

    // abstract class to manage trajectories of outputs that could be surveyed (observed)
    public abstract class SurveyedTrajectory
    {
        public string Name { get; private set; }
        public int ID { get; private set; }
        public bool DisplayInSimOut { get; private set; }
        protected int _nDeltaTsObsPeriod; // number of deltaT's in an observation period 
        protected int _nObsPeriodsDelay;  // number of observation periods delayed      
        protected int _noiseParValue; // value of the noise parameter
        public bool FirstObsMarksStartOfEpidemic { get; private set; }

        public SurveyedTrajectory(
            int id,
            string name,
            bool displayInSimOutput,
            bool firstObsMarksStartOfEpidemic,
            int nDeltaTsObsPeriod,
            int nDeltaTsDelayed)
        {
            ID = id;
            Name = name;
            DisplayInSimOut = displayInSimOutput;
            FirstObsMarksStartOfEpidemic = firstObsMarksStartOfEpidemic;
            _nDeltaTsObsPeriod = nDeltaTsObsPeriod;
            _nObsPeriodsDelay = nDeltaTsDelayed / nDeltaTsObsPeriod;
        }

        public abstract void Update(int epiTimeIndex, RNG rnd);
        public abstract double? GetLastObs(int epiTimeIndex);
        public abstract double? GetIncrementalChange(int epiTimeIndex);
    }

    // a surveyed incidence trajectory
    public class SurveyedIncidenceTrajectory : SurveyedTrajectory
    {
        private IncidenceTimeSeries _timeSeries;
        private SumClassesTrajectory _sumClassesTraj;
        private SumEventTrajectory _sumEventsTraj;

        public SurveyedIncidenceTrajectory(
            int id,
            string name,
            bool displayInSimOutput,
            bool firstObsMarksStartOfEpidemic,
            SumClassesTrajectory sumClassesTrajectory,
            SumEventTrajectory sumEventTrajectory,
            int nDeltaTsObsPeriod,
            int nDeltaTsDelayed) 
            : base(id, name, displayInSimOutput, firstObsMarksStartOfEpidemic, nDeltaTsObsPeriod, nDeltaTsDelayed)
        {
            _sumClassesTraj = sumClassesTrajectory;
            _sumEventsTraj = sumEventTrajectory;
            _timeSeries = new IncidenceTimeSeries(nDeltaTsObsPeriod);
        }

        public override void Update(int epiTimeIndex, RNG rnd)
        {
            if (epiTimeIndex == 0)
                return;

            double value = 0;
            if (!(_sumClassesTraj is null))
                value = _sumClassesTraj.NumOfNewMembersOverPastPeriod;
            else if (!(_sumEventsTraj is null))
                value = _sumEventsTraj.NumOfNewMembersOverPastPeriod;

            _timeSeries.Record(value);
        }
        public override double? GetLastObs(int epiTimeIndex)
        {
            if (epiTimeIndex > _nDeltaTsObsPeriod * _nObsPeriodsDelay)
                return _timeSeries.GetLastRecording(_nObsPeriodsDelay);
            return null;
        }
        public override double? GetIncrementalChange(int epiTimeIndex)
        {
            if (epiTimeIndex > _nDeltaTsObsPeriod * (_nObsPeriodsDelay+1))
                return _timeSeries.GetLastRecording(_nObsPeriodsDelay) - _timeSeries.GetLastRecording(_nObsPeriodsDelay+1);

            return null;
        }
        public void Reset()
        {
            _timeSeries.Reset();
        }
    }

    // a surveryed prevalence trajectory (ratio statistics are considered a prevalence measure)
    public class SurveyedPrevalenceTrajectory : SurveyedTrajectory
    {
        private PrevalenceTimeSeries _timeSeries;
        private SumClassesTrajectory _sumClassesTraj;   // reference to the summation statistics
        private RatioTrajectory _ratioTraj;             // reference to the ratio statistics
        private double _noise_percOfDemoninatorSampled; 

        public SurveyedPrevalenceTrajectory(
            int id,
            string name,
            bool displayInSimOutput,
            bool firstObsMarksStartOfEpidemic,
            SumClassesTrajectory sumClassesTrajectory,
            RatioTrajectory ratioTrajectory,
            int nDeltaTsObsPeriod,
            int nDeltaTsDelayed, 
            double noise_percOfDemoninatorSampled)
            : base(id, name, displayInSimOutput, firstObsMarksStartOfEpidemic, nDeltaTsObsPeriod, nDeltaTsDelayed)
        {
            _sumClassesTraj = sumClassesTrajectory;
            _ratioTraj = ratioTrajectory;
            _timeSeries = new PrevalenceTimeSeries(nDeltaTsObsPeriod);
            _noise_percOfDemoninatorSampled = noise_percOfDemoninatorSampled;
        }

        public override void Update(int epiTimeIndex, RNG rnd)
        {
            double? obsValue = null;
            if (!(_sumClassesTraj is null))
                obsValue = _sumClassesTraj.Prevalence;
            else if (!(_ratioTraj is null))
            {
                // check if there is noise (less than 100% of the denominator is sampled in reality)
                if (_noise_percOfDemoninatorSampled < 0.9999999 && _ratioTraj.Ratio.HasValue && !(_ratioTraj.Ratio is double.NaN))
                {
                    double mean = _ratioTraj.Ratio.Value;
                    if (mean > 0)
                    {
                        double stDev = Math.Sqrt(mean * (1 - mean));
                        Normal noiseModel = new Normal("Noise model", 0,
                            stDev / Math.Sqrt(_noise_percOfDemoninatorSampled * _ratioTraj.Denom));
                        double noise = noiseModel.SampleContinuous(rnd);
                        obsValue = Math.Min(Math.Max(mean + noise, 0), 1);
                    }
                    else
                        obsValue = 0;
                }
                else
                    obsValue = _ratioTraj.Ratio;
            }

            _timeSeries.Record(obsValue);
        }
        public override double? GetLastObs(int epiTimeIndex)
        {
            if (epiTimeIndex >= _nDeltaTsObsPeriod * _nObsPeriodsDelay)
                return _timeSeries.GetLastRecording(_nObsPeriodsDelay);
            return null;
        }
        public override double? GetIncrementalChange(int epiTimeIndex)
        {
            if (epiTimeIndex >= _nDeltaTsObsPeriod * (_nObsPeriodsDelay+1))
                return _timeSeries.GetLastRecording(_nObsPeriodsDelay) - _timeSeries.GetLastRecording(_nObsPeriodsDelay+1);
            return null;
        }
        public void Reset()
        {
            _timeSeries.Reset();
        }
    }

    // abstract class to manage what the simulation outputs to the Excel file
    public abstract class ExcelOutputTrajs
    {
        protected int _simRepIndex;
        protected double _deltaT;
        protected int _nextTimeIndexToStore;
        protected DecisionMaker _decisionMaker;

        public List<string> PrevalenceOutputsHeader { get; protected set; } = new List<string>();
        public List<string> IncidenceOutputsHeader { get; protected set; } = new List<string>();
        public int NumOfPrevalenceOutputsToReport { get; protected set; }
        public int NumOfIncidenceOutputsToReport { get; protected set; }

        public int[][] SimRepIndeces { get; protected set; }
        public double[][] SimIncidenceOutputs { get; protected set; }
        public double[][] SimPrevalenceOutputs { get; protected set; }
        public int[][] InterventionCombinations { get; protected set; }

        public ExcelOutputTrajs(
            int simRepIndex,
            double deltaT,
            ref DecisionMaker decisionMaker,
            bool findHeader = false)
        {
            _simRepIndex = simRepIndex;
            _deltaT = deltaT;
            _decisionMaker = decisionMaker;
            _nextTimeIndexToStore = 0;
        }

        public void Reset()
        {
            _nextTimeIndexToStore = 0;
            SimRepIndeces = new int[0][];
            InterventionCombinations = new int[0][];
            SimPrevalenceOutputs = new double[0][];
            SimIncidenceOutputs = new double[0][];
        }

        // store selected outputs while simulating
        public void Record(int timeIndex, bool endOfSim)
        {
            // check if it is time to store output
            if (timeIndex < _nextTimeIndexToStore && !endOfSim)
                return;

            // define the jagged array to store current observation
            int[][] thisSimRepIndeces = new int[1][];
            thisSimRepIndeces[0] = new int[1];
            double[][] thisIncidenceOutputs = new double[1][];
            double[][] thisPrevalenceOutputs = new double[1][];
            int[][] thisActionCombination = new int[1][];

            // simulation replication index
            thisSimRepIndeces[0][0] = _simRepIndex;
            // action combination
            thisActionCombination[0] = (int[])_decisionMaker.CurrentDecision.Clone();

            // fill in the rest
            FillIn(timeIndex, ref thisIncidenceOutputs, ref thisPrevalenceOutputs, ref thisActionCombination);

            // concatenate this row 
            SimRepIndeces = SupportFunctions.ConcatJaggedArray(SimRepIndeces, thisSimRepIndeces);
            SimIncidenceOutputs = SupportFunctions.ConcatJaggedArray(SimIncidenceOutputs, thisIncidenceOutputs);
            SimPrevalenceOutputs = SupportFunctions.ConcatJaggedArray(SimPrevalenceOutputs, thisPrevalenceOutputs);
            InterventionCombinations = SupportFunctions.ConcatJaggedArray(InterventionCombinations, thisActionCombination);
        }

        protected abstract void FillIn(int timeIndex, ref double[][] thisIncidenceOutputs, ref double[][] thisPrevalenceOutputs, ref int[][] thisActionCombination);
    }

    // simulation output trajectories to be written in the Excel file
    public class ExcelOutputTrajs_Sim : ExcelOutputTrajs
    {
        private int _nDeltaTInSimOutputInterval;
        private List<Class> _classes;
        private List<SumTrajectory> _sumTrajectories;
        private List<RatioTrajectory> _ratioTrajectories;

        public ExcelOutputTrajs_Sim(
            int simReplication, 
            double deltaT,
            int nDeltaTInSimOutputInterval,
            ref DecisionMaker decisionMaker,
            ref List<Class> classes,
            ref List<SumTrajectory> sumTrajectories,
            ref List<RatioTrajectory> ratioTrajectories, 
            bool findHeader = false) : base(simReplication, deltaT, ref decisionMaker, findHeader)
        {
            _nDeltaTInSimOutputInterval = nDeltaTInSimOutputInterval;
            
            _classes = classes;
            _sumTrajectories = sumTrajectories;
            _ratioTrajectories = ratioTrajectories;

            FindNumOfOutputsAndHeaders(findHeader);
        }

        protected override void FillIn(int timeIndex, ref double[][] thisIncidenceOutputs, ref double[][] thisPrevalenceOutputs, ref int[][] thisActionCombination)
        {
            int colIndexPrevalenceOutputs = 0;
            int colIndexIncidenceOutputs = 0;
            thisPrevalenceOutputs[0] = new double[NumOfPrevalenceOutputsToReport];
            thisIncidenceOutputs[0] = new double[NumOfIncidenceOutputsToReport];

            // store the current time and the current interval            
            thisIncidenceOutputs[0][colIndexIncidenceOutputs++]
                = Math.Floor((double)(timeIndex - 1) / _nDeltaTInSimOutputInterval) + 1;
            thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = timeIndex * _deltaT;

            // classes
            foreach (Class thisClass in _classes)
            {
                if (thisClass.ShowIncidence)
                    thisIncidenceOutputs[0][colIndexIncidenceOutputs++] = 
                        thisClass.ClassStat.IncidenceTimeSeries.GetLastRecording().GetValueOrDefault(-1);
                if (thisClass.ShowPrevalence)
                    thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = thisClass.ClassStat.Prevalence;
                if (thisClass.ShowAccumIncidence)
                    thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = thisClass.ClassStat.AccumulatedIncidence;
            }

            // summation statistics
            foreach (SumTrajectory thisSumTraj in _sumTrajectories.Where(s => s.DisplayInSimOutput))
            {
                switch (thisSumTraj.Type)
                {
                    case SumTrajectory.EnumType.Incidence:
                            thisIncidenceOutputs[0][colIndexIncidenceOutputs++] = 
                                thisSumTraj.IncidenceTimeSeries.GetLastRecording().GetValueOrDefault(-1);
                        break;
                    case SumTrajectory.EnumType.AccumulatingIncident:
                        thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = thisSumTraj.AccumulatedIncidence;
                        break;
                    case SumTrajectory.EnumType.Prevalence:
                            thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] =
                                SupportProcedures.ReplaceNaNWith(thisSumTraj.Prevalence, -1);
                        break;
                }
            }
            // ratio statistics
            foreach (RatioTrajectory thisRatioTraj in _ratioTrajectories.Where(s => s.DisplayInSimOutput))
                thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = 
                    SupportProcedures.ReplaceNaNWith(thisRatioTraj.TimeSeries.GetLastRecording(), -1);  // could get nan if denominator is 0 or null for simIndex = 0

            // find next time index to store trajectories
            _nextTimeIndexToStore += _nDeltaTInSimOutputInterval;
        }

        // get header
        private void FindNumOfOutputsAndHeaders(bool storeHeaders)
        {
            // create headers
            NumOfIncidenceOutputsToReport = 1;
            if (storeHeaders) IncidenceOutputsHeader.Add("Simulation Period");
            NumOfPrevalenceOutputsToReport = 1;
            if (storeHeaders) PrevalenceOutputsHeader.Add("Simulation Time");

            // class headers
            foreach (Class thisClass in _classes)
            {
                if (thisClass.ShowIncidence)
                {
                    if (storeHeaders) IncidenceOutputsHeader.Add("To: " + thisClass.Name);
                    ++NumOfIncidenceOutputsToReport;
                }
                if (thisClass.ShowPrevalence)
                {
                    if (storeHeaders) PrevalenceOutputsHeader.Add("In: " + thisClass.Name);
                    ++NumOfPrevalenceOutputsToReport;
                }
                if (thisClass.ShowAccumIncidence)
                {
                    if (storeHeaders) PrevalenceOutputsHeader.Add("Sum To: " + thisClass.Name);
                    ++NumOfPrevalenceOutputsToReport;
                }
            }
            // summation statistics header
            foreach (SumTrajectory thisSumTraj in _sumTrajectories.Where(s => s.DisplayInSimOutput))
            {
                switch (thisSumTraj.Type)
                {
                    case SumTrajectory.EnumType.Incidence:
                        {
                            if (storeHeaders) IncidenceOutputsHeader.Add(thisSumTraj.Name);
                            ++NumOfIncidenceOutputsToReport;
                        }
                        break;
                    case SumTrajectory.EnumType.AccumulatingIncident:
                    case SumTrajectory.EnumType.Prevalence:
                        {
                            if (storeHeaders) PrevalenceOutputsHeader.Add(thisSumTraj.Name);
                            ++NumOfPrevalenceOutputsToReport;
                        }
                        break;
                }
            }
            // ratio statistics
            foreach (RatioTrajectory thisRatioTaj in _ratioTrajectories.Where(s => s.DisplayInSimOutput))
            {
                if (storeHeaders) PrevalenceOutputsHeader.Add(thisRatioTaj.Name);
                ++NumOfPrevalenceOutputsToReport;
            }
        }
    }

    // surveyed output trajectories to be written in the Excel file
    public class ExcelOutputTrajs_Surveyed : ExcelOutputTrajs
    {
        private int _nDeltaTInObsInterval;
        List<SurveyedIncidenceTrajectory> _surveyIncidenceTrajs;
        List<SurveyedPrevalenceTrajectory> _surveyPrevalenceTrajs;

        public ExcelOutputTrajs_Surveyed(
            int simReplication,
            double deltaT,
            int nDeltaTInObsInterval,
            ref DecisionMaker decisionMaker,
            ref List<SurveyedIncidenceTrajectory> surveyedIncidenceTrajectories,
            ref List<SurveyedPrevalenceTrajectory> surveyedPrevalenceTrajectories,
            bool findHeader = false) : base(simReplication, deltaT, ref decisionMaker, findHeader)
        {
            _nDeltaTInObsInterval = nDeltaTInObsInterval;
            _surveyIncidenceTrajs = surveyedIncidenceTrajectories;
            _surveyPrevalenceTrajs = surveyedPrevalenceTrajectories;

            FindNumOfOutputsAndHeaders(findHeader);
        }

        protected override void FillIn(int epiTimeIndex, ref double[][] thisIncidenceOutputs, ref double[][] thisPrevalenceOutputs, ref int[][] thisActionCombination)
        {

            // return if epidemic has not started yet
            if (epiTimeIndex < 0)
                return;

            int colIndexPrevalenceOutputs = 0;
            int colIndexIncidenceOutputs = 0;
            thisPrevalenceOutputs[0] = new double[NumOfPrevalenceOutputsToReport];
            thisIncidenceOutputs[0] = new double[NumOfIncidenceOutputsToReport];

            // store the current time and the current interval            
            thisIncidenceOutputs[0][colIndexIncidenceOutputs++]
                = Math.Floor((double)(epiTimeIndex - 1) / _nDeltaTInObsInterval) + 1;
            thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = epiTimeIndex * _deltaT;

            foreach (SurveyedIncidenceTrajectory incdTraj in _surveyIncidenceTrajs.Where(i => i.DisplayInSimOut))
                thisIncidenceOutputs[0][colIndexIncidenceOutputs++] = incdTraj.GetLastObs(epiTimeIndex).GetValueOrDefault(-1);

            foreach (SurveyedPrevalenceTrajectory prevTraj in _surveyPrevalenceTrajs.Where(i => i.DisplayInSimOut))
                thisPrevalenceOutputs[0][colIndexPrevalenceOutputs++] = 
                    SupportProcedures.ReplaceNaNWith(prevTraj.GetLastObs(epiTimeIndex), -1);

            // find next time index to store trajectories
            _nextTimeIndexToStore += _nDeltaTInObsInterval;
        }

        private void FindNumOfOutputsAndHeaders(bool storeHeaders)
        {
            // create headers
            NumOfIncidenceOutputsToReport = 1;
            if (storeHeaders) IncidenceOutputsHeader.Add("Observation Period");
            NumOfPrevalenceOutputsToReport = 1;
            if (storeHeaders) PrevalenceOutputsHeader.Add("Epidemic Time");

            foreach (SurveyedIncidenceTrajectory incdTraj in _surveyIncidenceTrajs.Where(i => i.DisplayInSimOut))
            {
                if (storeHeaders) IncidenceOutputsHeader.Add("Obs: " + incdTraj.Name);
                ++NumOfIncidenceOutputsToReport;
            }
            foreach (SurveyedPrevalenceTrajectory prevTraj in _surveyPrevalenceTrajs.Where(i => i.DisplayInSimOut))
            {
                if (storeHeaders) PrevalenceOutputsHeader.Add("Obs: " + prevTraj.Name);
                ++NumOfPrevalenceOutputsToReport;
            }
        }
    }

    // stores the epidemic history 
    public class EpidemicHistory
    {
        private List<Class> _classes; // pointer to classes
        private List<Event> _events;  // pointer to events

        // summation and ratio trajectories
        public List<SumTrajectory> _sumTrajs = new List<SumTrajectory>();
        public List<RatioTrajectory> _ratioTraj = new List<RatioTrajectory>();
        public List<SumTrajectory> SumTrajs { get => _sumTrajs; set => _sumTrajs = value; }
        public List<RatioTrajectory> RatioTrajs { get => _ratioTraj; set => _ratioTraj = value; }
        
        // surveyed trajectories
        public List<SurveyedIncidenceTrajectory> _survIncidenceTrajs = new List<SurveyedIncidenceTrajectory>();
        public List<SurveyedPrevalenceTrajectory> _survPrevalenceTrajs = new List<SurveyedPrevalenceTrajectory>();
        public List<SurveyedIncidenceTrajectory> SurveyedIncidenceTrajs { get => _survIncidenceTrajs; set => _survIncidenceTrajs = value; }
        public List<SurveyedPrevalenceTrajectory> SurveyedPrevalenceTrajs { get => _survPrevalenceTrajs; set => _survPrevalenceTrajs = value; }
        
        // all trajectories prepared for simulation output 
        public ExcelOutputTrajs_Sim SimOutputTrajs { get; private set; }
        public ExcelOutputTrajs_Surveyed SurveyedOutputTrajs { get; private set; }

        // features and conditions
        public List<Feature> Features { set; get; } = new List<Feature>();
        public List<Condition> Conditions { set; get; } = new List<Condition>();   

        public EpidemicHistory(List<Class> classes, List<Event> events)
        {
            _classes = classes;
            _events = events;
        }

        public void SetupSimOutputTrajs(
            int ID,
            double deltaT,
            int nDeltaTinSimOutputInterval,
            int nDeltaTInObsInterval,
            ref DecisionMaker decisionMaker,
            ref List<Class> classes,
            bool extractOutputHeaders)
        {
            SimOutputTrajs = new ExcelOutputTrajs_Sim(
               ID,
               deltaT,
               nDeltaTinSimOutputInterval,
               ref decisionMaker,
               ref classes,
               ref _sumTrajs,
               ref _ratioTraj,
               extractOutputHeaders);
            SurveyedOutputTrajs = new ExcelOutputTrajs_Surveyed(
                ID,
                deltaT,
                nDeltaTInObsInterval,
                ref decisionMaker,
                ref _survIncidenceTrajs,
                ref _survPrevalenceTrajs,
                extractOutputHeaders);
        }

        public void AddASpecialStatisticsFeature(string name, int featureID, int specialStatID, string strFeatureType, double par)
        {
            bool ifFound = false;
            SurveyedTrajectory survTraj = null;
            foreach (SurveyedIncidenceTrajectory t in _survIncidenceTrajs)
            {
                if (t.ID == specialStatID)
                {
                    ifFound = true;
                    survTraj = t;
                    break;
                }
            }
            if (ifFound == false)
            {
                foreach (SurveyedPrevalenceTrajectory t in _survPrevalenceTrajs)
                {
                    if (t.ID == specialStatID)
                    {
                        ifFound = true;
                        survTraj = t;
                        break;
                    }
                }
            }

            Features.Add(new Feature_SpecialStats(name, featureID, strFeatureType, survTraj, par));
        }

        public bool Update(int simTimeIndex, int epiTimeIndex, bool endOfSim, RNG rnd)
        {
            bool ifFeasibleRangesViolated = false; // for calibration

            // update class statistics                      
            foreach (Class thisClass in _classes)
                thisClass.ClassStat.CollectEndOfDeltaTStats(simTimeIndex);
            // update summation statistics
            foreach (SumTrajectory thisSumTaj in SumTrajs)
                if (thisSumTaj.Add(simTimeIndex, ref _classes, ref _events))
                     ifFeasibleRangesViolated = true;
            // update ratio statistics
            foreach (RatioTrajectory ratioTraj in RatioTrajs)
                if (ratioTraj.Add(simTimeIndex, _sumTrajs))
                    ifFeasibleRangesViolated = true;
            // update surveyed incidence            
            foreach (SurveyedIncidenceTrajectory survIncdTraj in SurveyedIncidenceTrajs)
                survIncdTraj.Update(epiTimeIndex, rnd);
            // update surveyed prevalence 
            foreach (SurveyedPrevalenceTrajectory survPrevTraj in SurveyedPrevalenceTrajs)
                survPrevTraj.Update(epiTimeIndex, rnd);
            // update features
            foreach (Feature f in Features)
                f.Update(epiTimeIndex);

            return ifFeasibleRangesViolated;
        }

        public void Record(int simTimeIndex, int epiTimeIndex, bool endOfSim)
        {
            SimOutputTrajs.Record(simTimeIndex, endOfSim);
            SurveyedOutputTrajs.Record(epiTimeIndex, endOfSim);
        }

        public void Reset()
        {
            foreach (SumTrajectory thisSumTaj in SumTrajs)
                thisSumTaj.Reset();
            foreach (RatioTrajectory ratioTraj in RatioTrajs)
                ratioTraj.Reset();
            foreach (SurveyedIncidenceTrajectory survIncdTraj in SurveyedIncidenceTrajs)
                survIncdTraj.Reset();
            foreach (SurveyedPrevalenceTrajectory survPrevTraj in SurveyedPrevalenceTrajs)
                survPrevTraj.Reset();

            SimOutputTrajs.Reset();
            SurveyedOutputTrajs.Reset();            
        }

        public void Clean()
        {
            // to fee up memory
            SumTrajs = new List<SumTrajectory>();
            RatioTrajs = new List<RatioTrajectory>();
        }
    }

}
