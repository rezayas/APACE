﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using RandomVariateLib;
using SimulationLib;
using ComputationLib;

namespace APACE_lib
{
    public class Epidemic
    {
        // Variable Definition 
        #region Variable Definition
        int _ID;
        ModelSettings _modelSettings;
        // collections
        private List<Parameter> _parameters = new List<Parameter>();
        private List<Class> _classes = new List<Class>();
        private List<Process> _processes = new List<Process>();
        private ArrayList _resources = new ArrayList();
        private ArrayList _resourceRules = new ArrayList();
        private List<SummationStatistics> _summationStatistics = new List<SummationStatistics>();
        private List<RatioStatistics> _ratioStatistics = new List<RatioStatistics>();
        private ArrayList _features = new ArrayList();
        private int _numOfClasses;
        private int[] _pathogenIDs;
        private int _numOfPathogens;
        // simulation setting
        RNG _rng;        
        private int _rndSeedResultedInAnAcceptibleTrajectory;
        private double[] _arrSampledParameterValues;
        private enumModelUse _modelUse = enumModelUse.Simulation;
        private double _deltaT;
        private double _decisionIntervalLength;
        private long _warmUpPeriodIndex;
        private bool _ifWarmUpPeriodHasEnded;
        private double _simulationHorizonTime;
        private long _simulationHorizonTimeIndex;
        private int _epidemicConditionTimeIndex;

        // simulation output settings        
        private bool _storeEpidemicTrajectories;
        private double _simulationOutputIntervalLength;
        private double _observationPeriodLengh;
        private int _nextTimeIndexToCollectSimulationOutputData;
        private int _nextTimeIndexToCollectObservationPeriodData;
        private int _numOfDeltaTIndexInASimulationOutputInterval;
        private int _numOfDeltaTIndexInAnObservationPeriod;

        private int _numOfTimeBasedOutputsToReport;
        private int _numOfIntervalBasedOutputsToReport;
        private int _numOfMonitoredSimulationOutputs;
        
        private int _timeIndexOfTheFirstObservation;
        private bool _firstObservationObtained;
        private int[][] _pastActionCombinations;
        private double[][] _simulationTimeBasedOutputs;
        private double[][] _simulationIntervalBasedOutputs;
        private double[][] _simulationObservableOutputs;
        private double[][] _simulationResourceAvailabilityOutput;
        private double[][] _timesOfEpidemicObservationsOverPastObservationPeriods;
        private double[][] _calibrationObservation;

        // contact and transmission matrices
        private double[][,] _baseContactMatrices = null; //[pathogen ID][group i, group j]
        private int[][][,] _percentChangeInContactMatricesParIDs = null; //[intervention ID][pathogen ID][group i, group j]
        private double[][][,] _contactMatrices = null; //[intervention ID][pathogen ID][group i, group j]
        private double[][][][] _tranmissionMatrices = null; // [intervention ID][pathogen ID][group i][group j]
        private int _numOfInterventionsAffectingContactPattern = 0;
        private int[] _indecesOfInterventionsAffectingContactPattern;
        private int[] _onOffStatusOfInterventionsAffectingContactPattern;
        // simulation
        private enumMarkOfEpidemicStartTime _markOfEpidemicStartTime;
        private int _currentSimulationTimeIndex;
        private int _currentEpidemicTimeIndex;
        private bool _stoppedDueToEradication;
        //private bool _simulationResultedInAnAcceptableTrajectory;
        private long[] _populationSizeOfMixingGroups;
        private long[] _arrNumOfMembersInEachClass;
        // decision
        private int[] _currentInterventionCombinationInEffect;
        private enumDecisionRule _decisionRule;
        private int _decisionPeriodIndex;
        private int _epidemicTimeIndexToStartDecisionMaking;
        private int _initialActionCombinationBinaryCode;
        private long _nextEpidemicTimeIndexWhenAnInterventionEffectChanges;
        private int[][] _prespecifiedDecisionsOverObservationPeriods;        
        private int _nextDecisionPointIndex;
        private int _nextDecisionCycleIndex;
        private int _numOfDeltaTsInADecisionInterval;
        // dynamic policy
        private int _numOfFeatures;
        private bool _useEpidemicTimeAsFeature;
        private double[] _arrCurrentValuesOfFeatures = null;        
        // resources
        private long[] _arrAvailableResources;
        private bool _aResourceJustReplinished; 
        // outcomes                
        double[] _arrSimulationObjectiveFunction;
        private double _currentPeriodCost;
        private double _currentPeriodQALY;
        private double _totalCost;
        private double _annualCost;
        private double _totalQALY;
        private int _numOfSwitchesBtwDecisions;
        private double _discountRate;
        private double _annualInterestRate;
        private double _wtpForHealth;
        // optimization
        private POMDP_ADP _POMDP_ADP = new POMDP_ADP();
        private enumObjectiveFunction _objectiveFunction;
        private enumSimulationRNDSeedsSource _simulationRNDSeedsSource = enumSimulationRNDSeedsSource.StartFrom0;
        private int _adpSimItr; // the index of simulation runs that should be done before doing back-propagation
        private int[] _rndSeeds;
        Discrete _discreteDistOverSeeds;        
        private bool _storeADPIterationResults;
        private double[][] _arrADPIterationResults;
        // calibration
        private int _numOfCalibratoinTargets;
        private long _numOfDiscardedTrajectoriesAmongCalibrationRuns;
        private int _numOfParametersToCalibrate;        
        private double[,] _observationMatrix;
        // computation time
        private double _timeUsedToSimulateOneTrajectory;
        private double _timeUsedToFindOneDynamicPolicy;
        private double _totalSimulationTimeToFindOneDynamicPolicy;
        // parameters
        private bool _thereAreTimeDependentParameters = false;
        private bool _thereAreTimeDependentParameters_affectingSusceptibilities = false;
        private bool _thereAreTimeDependentParameters_affectingInfectivities = false;
        private bool _thereAreTimeDependentParameters_affectingTranmissionDynamics = false;
        private bool _thereAreTimeDependentParameters_affectingNaturalHistoryRates = false;
        private bool _thereAreTimeDependentParameters_affectingSplittingClasses = false;

        #endregion

        // Instantiation
        public Epidemic(int ID)
        {
            _ID = ID;
        }

        #region Properties
        public int ID
        {
            get { return _ID; }
        }
        public double DeltaT
        {
            get { return _deltaT; }
        }
        public double DecisionIntervalLength
        {
            get { return _decisionIntervalLength; }
        }
        public int CurrentEpidemicTimeIndex
        {
            get {return _currentEpidemicTimeIndex;}
        }
        public double CurrentEpidemicTime
        {
            get { return _currentEpidemicTimeIndex * _deltaT; }
        }
        public long WarmUpPeriodIndex
        {
            get { return _warmUpPeriodIndex; }
            set { _warmUpPeriodIndex = value; }
        }
        public double SimulationHorizonTime
        {
            get { return _simulationHorizonTime; }
        }
        public long SimulationHorizonTimeIndex
        {
            get { return _simulationHorizonTimeIndex; }
        }
        public double EpidemicConditionTime
        {
            get { return _epidemicConditionTimeIndex * _deltaT; }
        }       
        public int TimeIndexOfTheFirstObservation
        {
            get { return _timeIndexOfTheFirstObservation; }
        }       
        public double EpidemicTimeToStartDecisionMaking
        {
            get { return _epidemicTimeIndexToStartDecisionMaking * _deltaT; }
        }        
        public bool IfStoppedDueToEradication
        {
            get { return _stoppedDueToEradication; }
        }
        public int NumOfDeltaTsInADecisionInterval
        {
            get { return _numOfDeltaTsInADecisionInterval; }
        }
        public int NumOfInterventionsAffectingContactPattern
        {
            get { return _numOfInterventionsAffectingContactPattern; }
        }
        public enumMarkOfEpidemicStartTime MarkOfEpidemic
        {
            get { return _markOfEpidemicStartTime; }
        }
        public double ObservationPeriodLengh
        {
            get { return _observationPeriodLengh; }
        }
        public enumModelUse ModelUse
        {
            get { return _modelUse; }
            set { _modelUse = value; }
        }
        public enumDecisionRule DecisionRule
        {
            get { return _decisionRule; }
            set { _decisionRule = value; }
        }
        public int InitialActionCombinationBinaryCode
        {
            get { return _initialActionCombinationBinaryCode; }
            set { _initialActionCombinationBinaryCode = value; }
        }
        public bool StoreEpidemicTrajectories
        {
            get { return _storeEpidemicTrajectories; }
            set { _storeEpidemicTrajectories = value; }
        }
        public int NumOfTimeBasedOutputsToReport
        {
            get { return _numOfTimeBasedOutputsToReport; }
        }
        public int NumOfIntervalBasedOutputsToReport
        {
            get { return _numOfIntervalBasedOutputsToReport; }
        }
        public int NumOfResourcesToReport
        {
            get { return _resources.Count; }
        }            
        public int[][] PastActionCombinations
        {
            get { return _pastActionCombinations; }
        }
        public double[][] SimulationTimeBasedOutputs
        {
            get { return _simulationTimeBasedOutputs; }
        }
        public double[][] SimulationIntervalBasedOutputs
        {
            get { return _simulationIntervalBasedOutputs; }
        }
        public double[][] SimulationObservableOutputs
        {
            get {return _simulationObservableOutputs; }
        }
        public double[][] CalibrationObservation
        {
            get { return _calibrationObservation; }
        }
        public double[][] SimulationResourceOutpus
        {
            get { return _simulationResourceAvailabilityOutput; }
        }
        public double[][] TimesOfEpidemicObservationsOverPastObservationPeriods
        {
            get { return _timesOfEpidemicObservationsOverPastObservationPeriods; }
        }
        
        public double[,] ArrADPIterationResults
        {
            get { return SupportFunctions.ConvertFromJaggedArrayToRegularArray(_arrADPIterationResults, 5); }
        }
        public int NumOfInterventions
        {
            get { return _POMDP_ADP.NumberOfActions; }
        }        
        public int NumOfFeatures
        {
            get { return _features.Count; }
        }
        public int NumOfInterventionsControlledDynamically
        {
            get { return _POMDP_ADP.NumOfActionsControlledDynamically; }
        }
        public int NumOfMonitoredSimulationOutputs
        {
            get { return _numOfMonitoredSimulationOutputs; }
        }
        public long NumOfDiscardedTrajectoriesAmongCalibrationRuns
        {
            get { return _numOfDiscardedTrajectoriesAmongCalibrationRuns; }
        }
        public int NumOfParametersToCalibrate
        {
            get { return _numOfParametersToCalibrate; }
        }
        public int NumOfCalibratoinTargets
        { 
            get { return _numOfCalibratoinTargets; } 
        }
        public bool UseEpidemicTimeAsFeature
        {
            get { return _useEpidemicTimeAsFeature; }
        }
        public List<Class> Classes
        {
            get { return _classes; }
        }
        public ArrayList Interventions
        {
            get { return _POMDP_ADP.Actions; }
        }
        public List<SummationStatistics> SummationStatistics
        {
            get { return _summationStatistics; }
        }
        public List<RatioStatistics> RatioStatistics
        {
            get { return _ratioStatistics; }
        }
        public ArrayList Resources
        {
            get { return _resources; }
        }
        public List<Parameter> Parameters
        {
            get { return _parameters; }
        }
        public ArrayList Features
        {
            get { return _features; }
        }
        public POMDP_ADP POMDP_ADP
        {
            get { return _POMDP_ADP; }
        }
        public int RNDSeedResultedInAnAcceptibleTrajectory
        {
            get { return _rndSeedResultedInAnAcceptibleTrajectory; }
        }
        public double[] arrSampledParameterValues
        { get { return _arrSampledParameterValues; } }
        public double WTPForHealth
        {
            get {return _wtpForHealth; }
        }
        public double TotalCost
        {
            get { return _totalCost; }
        }
        public double TotalQALY
        {
            get { return _totalQALY; }
        }
        public double AnnualCost
        {
            get { return _annualCost; }
        }
        public double TotalNMB
        {
            get { return _wtpForHealth * _totalQALY - _totalCost; }
        }
        public double TotalNHB
        {
            get { return  _totalQALY - _totalCost / _wtpForHealth; }
        }
        public int NumOfSwitchesBtwDecisions
        {
            get { return _numOfSwitchesBtwDecisions; }
        }
        // simulation run time
        public double TimeUsedToSimulateOneTrajectory
        {
            get { return _timeUsedToSimulateOneTrajectory; }
        }
        public double TimeUsedToFindOneDynamicPolicy
        {
            get { return _timeUsedToFindOneDynamicPolicy; }
        }
        public double TotalSimulationTimeToFindOneDynamicPolicy
        {
            get { return _totalSimulationTimeToFindOneDynamicPolicy; }
        }
        #endregion

        // clone
        public Epidemic Clone(int ID)
        {
            Epidemic clone = new Epidemic(ID);

            //setup simulation settings
            clone.SetupSimulationSettings(_markOfEpidemicStartTime,
                _deltaT, _decisionIntervalLength, _warmUpPeriodIndex*_deltaT, _simulationHorizonTimeIndex * _deltaT,
                _epidemicConditionTimeIndex * _deltaT, _epidemicTimeIndexToStartDecisionMaking * _deltaT,
                _initialActionCombinationBinaryCode, _decisionRule, _storeEpidemicTrajectories, _simulationOutputIntervalLength, _observationPeriodLengh, 
                _annualInterestRate, _wtpForHealth);

            clone.ModelUse = _modelUse;
            clone.DecisionRule = _decisionRule;

            if (_modelUse == enumModelUse.Calibration)
            {
                clone._observationMatrix = _observationMatrix;
                clone._prespecifiedDecisionsOverObservationPeriods = _prespecifiedDecisionsOverObservationPeriods;
            }

            return clone;
        }

        // reset 
        private void Reset()
        {
            //_parameters.Clear();
            //_classes.Clear();
            //_features = new ArrayList();
            //_resources.Clear();
            //_processes.Clear();
            //_resourceRules.Clear();
            //_summationStatistics.Clear();
            //_ratioStatistics.Clear();
            //_POMDP_ADP.Clear();

            _parameters = new List<Parameter>();
            _classes = new List<Class>();
            _processes = new List<Process>();
            _resources = new ArrayList();
            _resourceRules = new ArrayList();
            _summationStatistics = new List<SummationStatistics>();
            _ratioStatistics = new List<RatioStatistics>();
            _features = new ArrayList();
            _POMDP_ADP = new POMDP_ADP();
    }
        // clean except the results
        public void CleanExceptResults()
        {
            _modelSettings = null;

            Reset();

            //_parameters = null;
            //_classes = null;
            //_processes = null;
            //_resources = null;
            //_resourceRules = null;
            //_summationStatistics = null;
            //_ratioStatistics = null;
            //_features = null;

            _arrSampledParameterValues = null;
            _baseContactMatrices = null; 
            _percentChangeInContactMatricesParIDs = null; 
            _contactMatrices = null; 
            _tranmissionMatrices = null; 
            _indecesOfInterventionsAffectingContactPattern = null;
            _onOffStatusOfInterventionsAffectingContactPattern = null;

            _currentInterventionCombinationInEffect = null;
            _prespecifiedDecisionsOverObservationPeriods = null;
             _arrCurrentValuesOfFeatures = null;
            _arrAvailableResources = null;
            _rndSeeds = null;
    }
        
        // Simulate one trajectory (parameters will be sampled)
        public void SimulateTrajectoriesUntilOneAcceptibleFound(int threadSpecificSeedNumber, int seedNumberToStopTryingFindingATrajectory, int simReplication, long timeIndexToStop)
        {
            // time keeping
            int startTime, endTime;
            startTime = Environment.TickCount;

            bool acceptableTrajectoryFound = false;
            _numOfDiscardedTrajectoriesAmongCalibrationRuns = 0;

            while (!acceptableTrajectoryFound && threadSpecificSeedNumber < seedNumberToStopTryingFindingATrajectory)
            {
                // reset for another simulation
                ResetForAnotherSimulation(threadSpecificSeedNumber);
                // simulate
                if (Simulate(simReplication, timeIndexToStop) == true)
                {
                    acceptableTrajectoryFound = true;
                    _rndSeedResultedInAnAcceptibleTrajectory = threadSpecificSeedNumber;
                }
                else
                {
                    ++threadSpecificSeedNumber;
                    // if the model is used for calibration, record the number of discarded trajectories due to violating the feasible ranges
                    if (_modelUse == enumModelUse.Calibration)
                        ++_numOfDiscardedTrajectoriesAmongCalibrationRuns;
                }
            }

            // simulation time
            endTime = Environment.TickCount;
            _timeUsedToSimulateOneTrajectory = (double)(endTime - startTime) / 1000;    
        }
        // Simulate one trajectory (parameters will be sampled)
        public void SimulateOneTrajectory(int threadSpecificSeedNumber, int simReplication, long timeIndexToStop)
        {
            int startTime, endTime;
            startTime = Environment.TickCount;
            _rndSeedResultedInAnAcceptibleTrajectory = -1;
            
            // reset for another simulation
            ResetForAnotherSimulation(threadSpecificSeedNumber);
            // simulate
            if (Simulate(simReplication, timeIndexToStop) == true)
                _rndSeedResultedInAnAcceptibleTrajectory = threadSpecificSeedNumber;

            // simulation time
            endTime = Environment.TickCount;
            _timeUsedToSimulateOneTrajectory = (double)(endTime - startTime) / 1000;
        }
        /// <summary>
        /// Simulate one trajectory (parameters should be assigned)
        /// </summary>
        /// <param name="threadSpecificSeedNumber"></param>
        /// <param name="parameterValues"></param>
        //public void SimulateOneTrajectory(int threadSpecificSeedNumber, int simReplication, long timeIndexToStop, double[] parameterValues)
        //{
        //    bool acceptableTrajectoryFound = false;
        //    while (!acceptableTrajectoryFound)
        //    {
        //        // reset for another simulation
        //        ResetForAnotherSimulation(threadSpecificSeedNumber, false, parameterValues);
        //        // simulate
        //        if (Simulate(simReplication, timeIndexToStop) == true)
        //        {
        //            acceptableTrajectoryFound = true;
        //            _rndSeedResultedInAnAcceptibleTrajectory = threadSpecificSeedNumber;
        //        }
        //        else
        //            ++threadSpecificSeedNumber;
        //    }
        //}
        // simulate one trajectory until the first time decisions should be made
        public void SimulateOneTrajectoryUntilTheFirstDecisionPoint(int threadSpecificSeedNumber)
        {
            // stop simulating this trajectory when it is time to make decisions
            bool acceptableTrajectoryFound = false;
            while (!acceptableTrajectoryFound)
            {
                // reset for another simulation
                ResetForAnotherSimulation(threadSpecificSeedNumber);
                // simulate until the first decision point
                if (Simulate(0, _epidemicTimeIndexToStartDecisionMaking) == true)
                {
                    acceptableTrajectoryFound = true;
                    _rndSeedResultedInAnAcceptibleTrajectory = threadSpecificSeedNumber;
                }
                else
                    ++threadSpecificSeedNumber;
            }
        }
        // continue simulating current trajectory 
        public void ContinueSimulatingThisTrajectory(int additionalDeltaTs)
        {
            // find the stop time
            long timeIndexToStopSimulation = _currentSimulationTimeIndex + additionalDeltaTs;
            // continue the simulation
            Simulate(0, timeIndexToStopSimulation);
        }
        // simulate N trajectories - this sub is exclusively for the ADP algorithm
        private void SimulateNTrajectories(int numOfSimulationIterations, int [] rndSeeds)
        {
            _arrSimulationObjectiveFunction = new double[numOfSimulationIterations];
            for (_adpSimItr = 0; _adpSimItr < numOfSimulationIterations; ++_adpSimItr)
            {
                // simulate one epidemic trajectory
                SimulateTrajectoriesUntilOneAcceptibleFound(rndSeeds[_adpSimItr], rndSeeds[_adpSimItr] + 1, _adpSimItr, _simulationHorizonTimeIndex);
                
                // store outcomes of this epidemic 
                _arrSimulationObjectiveFunction[_adpSimItr] = AccumulatedReward();
            }
        }
        private void SimulateNTrajectories(int numOfSimulationIterations, ref int seedNumber)
        {
            _arrSimulationObjectiveFunction = new double[numOfSimulationIterations];
            for (_adpSimItr = 0; _adpSimItr < numOfSimulationIterations; ++_adpSimItr)
            {
                // simulate one epidemic trajectory
                SimulateTrajectoriesUntilOneAcceptibleFound(seedNumber, seedNumber + 1, _adpSimItr, _simulationHorizonTimeIndex);
                seedNumber = _rndSeedResultedInAnAcceptibleTrajectory + 1;
                // store outcomes of this epidemic 
                _arrSimulationObjectiveFunction[_adpSimItr] = AccumulatedReward();
            }
        }

        // set up POMDP-ADP
        public void SetUpADPAlgorithm(enumObjectiveFunction objectiveFunction,
            int numOfADPIterations, int numOfSimRunsToBackPropogate, 
            double wtpForHealth, double harmonicRule_a, double epsilonGreedy_beta, double epsilonGreedy_delta,
            bool storeADPIterationResults)
        {
            _decisionRule = enumDecisionRule.SpecifiedByPolicy;
            _modelUse = enumModelUse.Optimization;         

            // objective function
            _objectiveFunction = objectiveFunction;
            if (_objectiveFunction == enumObjectiveFunction.MaximizeNHB)
                _wtpForHealth = Math.Max(wtpForHealth, SupportProcedures.minimumWTPforHealth);

            // ADP
            _adpSimItr = 0;
            _POMDP_ADP.SetupTraining(numOfSimRunsToBackPropogate);
            _POMDP_ADP.SetupAHarmonicStepSizeRule(harmonicRule_a);
            _POMDP_ADP.SetupAnEpsilonGreedyExplorationRule(epsilonGreedy_beta, epsilonGreedy_delta);
            _POMDP_ADP.NumberOfADPIterations = numOfADPIterations;

            _storeADPIterationResults = storeADPIterationResults;
            if (_storeADPIterationResults)
                _arrADPIterationResults = new double[0][]; 
        }
        // set up the ADP random number source
        public void SetUpADPRandomNumberSource(int[] rndSeeds)
        {
            _simulationRNDSeedsSource = enumSimulationRNDSeedsSource.PrespecifiedSquence;
            _rndSeeds = (int[])rndSeeds.Clone();            
        }
        public void SetUpADPRandomNumberSource(int[] rndSeeds, double[] rndSeedsGoodnessOfFit)
        {
            _simulationRNDSeedsSource = enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence;
            _rndSeeds = (int[])rndSeeds.Clone();

            double[] arrProb = new double[rndSeeds.Length];
            for (int i = 0; i < rndSeeds.Length; i++)
                arrProb[i] = 1 / rndSeedsGoodnessOfFit[i];

            // form a probability function over rnd seeds
            double sumInv = 1/arrProb.Sum();
            for (int i = 0; i < rndSeeds.Length; i++)
                arrProb[i] = arrProb[i] * sumInv;

            // define the sampling object
            _discreteDistOverSeeds = new Discrete("Discrete distribution over RND seeds", arrProb);            
        }
        // find the approximately optimal dynamic policy
        public void FindOptimalDynamicPolicy()
        {
            // sample rnd seeds if trajectories need to be sampled
            int[,] sampledRNDseeds = new int[_POMDP_ADP.NumberOfADPIterations, _POMDP_ADP.NumOfSimRunsToBackPropogate];
            if (_simulationRNDSeedsSource == enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence)
            {
                _rng = new RNG(0);
                for (int itr = 1; itr <= _POMDP_ADP.NumberOfADPIterations; ++itr)
                    for (int j = 1; j <= _POMDP_ADP.NumOfSimRunsToBackPropogate; ++j)
                        sampledRNDseeds[itr - 1, j - 1] = _rndSeeds[_discreteDistOverSeeds.SampleDiscrete(_rng)];
            }

            int rndSeedForThisItr = 0;
            int[] rndSeeds = new int[_POMDP_ADP.NumOfSimRunsToBackPropogate];
            int indexOfRNDSeeds = 0;
            int adpItr = 1;
            _storeEpidemicTrajectories = false;

            int optStartTime, optEndTime;
            optStartTime = Environment.TickCount;
            _totalSimulationTimeToFindOneDynamicPolicy = 0;       

            // optimize
            for (int itr = 1; itr <= _POMDP_ADP.NumberOfADPIterations; ++itr)
            {                
                // find the RND seed for this iteration
                switch (_simulationRNDSeedsSource)
                {
                    case enumSimulationRNDSeedsSource.StartFrom0:
                        break;
                    case enumSimulationRNDSeedsSource.PrespecifiedSquence:
                        {
                            for (int j = 1; j <= _POMDP_ADP.NumOfSimRunsToBackPropogate; j++)
                            {
                                if (indexOfRNDSeeds >= _rndSeeds.Length)
                                    indexOfRNDSeeds = 0;
                                rndSeeds[j-1] = _rndSeeds[indexOfRNDSeeds++];
                            }
                        }
                        break;
                    case enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence:
                        {
                            for (int j = 1; j <= _POMDP_ADP.NumOfSimRunsToBackPropogate; j++)
                                rndSeeds[j-1] = sampledRNDseeds[itr-1, j-1];
                        }
                        break;
                }

                // get ready for another ADP iteration
                GetReadyForAnotherADPIteration(itr);

                // simulate 
                int simStartTime, simEndTime;
                simStartTime = Environment.TickCount;
                switch (_simulationRNDSeedsSource)
                {
                    case enumSimulationRNDSeedsSource.StartFrom0:
                        SimulateNTrajectories(_POMDP_ADP.NumOfSimRunsToBackPropogate, ref rndSeedForThisItr);
                        break;
                    case enumSimulationRNDSeedsSource.PrespecifiedSquence:                        
                    case enumSimulationRNDSeedsSource.WeightedPrespecifiedSquence:
                        SimulateNTrajectories(_POMDP_ADP.NumOfSimRunsToBackPropogate, rndSeeds);
                        break;
                }
                
                // accumulated simulation time
                simEndTime = Environment.TickCount;
                _totalSimulationTimeToFindOneDynamicPolicy += (double)(simEndTime - simStartTime) / 1000;

                // do backpropogation
                _POMDP_ADP.DoBackpropagation(itr, _discountRate, _stoppedDueToEradication, false);

                // store ADP iterations
                if (_modelUse == enumModelUse.Optimization && _storeADPIterationResults == true)
                {
                    for (int dim = 0; dim < _POMDP_ADP.NumOfSimRunsToBackPropogate; ++dim)
                    {
                        if (_POMDP_ADP.BackPropagationResult(dim) == true)
                        {
                            double[][] thisADPIterationResult = new double[1][];
                            thisADPIterationResult[0] = new double[5];
                            
                            // iteration
                            thisADPIterationResult[0][0] = adpItr;
                            // reward
                            thisADPIterationResult[0][1] = _arrSimulationObjectiveFunction[dim];
                            // report errors
                            thisADPIterationResult[0][2] = _POMDP_ADP.ADPPredictionErrors(dim, 0); //_POMDP_ADP.PredictionErrorForTheFirstEligibleADPState(dim);
                            thisADPIterationResult[0][3] = _POMDP_ADP.ADPPredictionErrors(dim, (int)_POMDP_ADP.NumberOfValidADPStates(dim) / 2);
                            thisADPIterationResult[0][4] = _POMDP_ADP.ADPPredictionErrors(dim, _POMDP_ADP.NumberOfValidADPStates(dim) - 1); //_POMDP_ADP.PredictionErrorForTheLastEligibleADPState(dim); //
                            // concatenate 
                            _arrADPIterationResults = SupportFunctions.ConcatJaggedArray(_arrADPIterationResults, thisADPIterationResult);
                            ++adpItr;
                        }
                    }
                }
            }
            // optimization time
            optEndTime = Environment.TickCount;
            _timeUsedToFindOneDynamicPolicy = (double)(optEndTime - optStartTime) / 1000;

            // reverse back the rnd seeds
            if (_rndSeeds != null)
                _rndSeeds = _rndSeeds.Reverse().ToArray();
        }
        // get q-function coefficient estimates
        public double[] GetQFunctionCoefficientEstiamtes()
        {
            return _POMDP_ADP.GetQFunctionCoefficientEstimates();
        }
        // get dynamic policy // only one dimensional
        public void GetOptimalDynamicPolicy(ref string featureName, ref double[] headers, ref int[] optimalDecisions,
            int numOfIntervalsToDescretizeFeatures)
        {
            if (_features.Count > 1) return; // this procedure works when the feature set constrains 1 feature

            headers = new double[numOfIntervalsToDescretizeFeatures + 1];
            optimalDecisions = new int[numOfIntervalsToDescretizeFeatures + 1];

            // get the only feature
            Feature theOnlyFeature = (Feature)_features[0];

            // setup the interval length for each feature
            int featureIndex = theOnlyFeature.Index;
            double featureMin = theOnlyFeature.Min;
            double featureMax = theOnlyFeature.Max;
            double featureIntervalLength = (featureMax - featureMin) / numOfIntervalsToDescretizeFeatures;
            double featureNumOfBreakPoints = numOfIntervalsToDescretizeFeatures + 1;
            featureName = theOnlyFeature.Name;

            // reset available action combinations to the initial setting
            _POMDP_ADP.MakeAllDynamicallyControlledActionsAvailable();
            // find the dynamic policy
            double featureValue = 0;
            for (int fIndex = 0; fIndex < featureNumOfBreakPoints; ++fIndex)
            {
                // value of the feature
                featureValue = featureMin + fIndex * featureIntervalLength;
                // header
                headers[fIndex] = featureValue;
                // make an array of feature
                double[] arrFeatureValue = new double[1];
                arrFeatureValue[0] = featureValue;
                // find the optimal decision
                optimalDecisions[fIndex] = ComputationLib.SupportFunctions.ConvertToBase10FromBase2(
                        _POMDP_ADP.FindTheOptimalDynamicallyControlledActionCombination(arrFeatureValue));//, _timeIndexToStartDecisionMaking));
            }
        }
        // get dynamic policy // only two dimensional
        public void GetOptimalDynamicPolicy(ref string[] strFeatureNames, ref double[][] headers, ref int[,] optimalDecisions,
            int numOfIntervalsToDescretizeFeatures)
        {       
            int numOfFeatures = _features.Count;            

            strFeatureNames = new string[numOfFeatures];
            headers = new double[2][];
            headers[0] = new double[numOfIntervalsToDescretizeFeatures + 1];
            headers[1] = new double[numOfIntervalsToDescretizeFeatures + 1];
            optimalDecisions = new int[numOfIntervalsToDescretizeFeatures + 1, numOfIntervalsToDescretizeFeatures + 1];

            // setup the interval length for each feature
            double[] arrFeatureMin = new double[numOfFeatures];
            double[] arrFeatureMax = new double[numOfFeatures];
            double[] arrFeatureIntervalLengths = new double[numOfFeatures];
            double[] arrFeatureNumOfBreakPoints = new double[numOfFeatures];

            int featureIndex;
            foreach (Feature thisFeature in _features)
            {
                featureIndex = thisFeature.Index;
                arrFeatureMin[featureIndex] = thisFeature.Min;
                arrFeatureMax[featureIndex] = thisFeature.Max;
                arrFeatureIntervalLengths[featureIndex] = (arrFeatureMax[featureIndex] - arrFeatureMin[featureIndex]) / numOfIntervalsToDescretizeFeatures;
                arrFeatureNumOfBreakPoints[featureIndex] = numOfIntervalsToDescretizeFeatures + 1;
            }
            // feature names
            foreach (Feature thisFeature in _features)
                strFeatureNames[thisFeature.Index] = thisFeature.Name;

            // reset available action combinations to the initial setting
            _POMDP_ADP.MakeAllDynamicallyControlledActionsAvailable();

            // write the dynamic Policy
            double[] arrFeatureValues = new double[_features.Count];
            for (int f0Index = 0; f0Index < arrFeatureNumOfBreakPoints[0]; ++f0Index)
            {
                // feature 0 values
                arrFeatureValues[0] = arrFeatureMin[0] + f0Index * arrFeatureIntervalLengths[0];
                // header
                headers[0][f0Index] = arrFeatureValues[0];
                for (int f1Index = 0; f1Index < arrFeatureNumOfBreakPoints[1]; f1Index++)
                {
                    // feature 1 value
                    arrFeatureValues[1] = arrFeatureMin[1] + f1Index * arrFeatureIntervalLengths[1];
                    // header
                    headers[1][f1Index] = arrFeatureValues[1];
                    // optimal decision ID
                    optimalDecisions[f0Index, f1Index] = ComputationLib.SupportFunctions.ConvertToBase10FromBase2(
                        _POMDP_ADP.FindTheOptimalDynamicallyControlledActionCombination(arrFeatureValues));
                }
            }
        }
        // get dynamic policy //  two dimensional + 1 resource feature
        public void GetOptimalDynamicPolicy(ref string[] strFeatureNames, ref double[][] headers, ref int[][,] optimalDecisions, int numOfIntervalsToDescretizeFeatures)
        {
            int numOfFeatures = _features.Count;

            strFeatureNames = new string[numOfFeatures];
            headers = new double[3][];
            headers[0] = new double[numOfIntervalsToDescretizeFeatures + 1];
            headers[1] = new double[numOfIntervalsToDescretizeFeatures + 1];
            headers[2] = new double[numOfIntervalsToDescretizeFeatures + 1];
            optimalDecisions = new int[numOfIntervalsToDescretizeFeatures + 1][,];
            //                [numOfIntervalsToDescretizeEpidemicFeatures + 1, numOfIntervalsToDescretizeEpidemicFeatures + 1];

            // setup the interval length for each feature
            double[] arrFeatureMin = new double[numOfFeatures];
            double[] arrFeatureMax = new double[numOfFeatures];
            double[] arrFeatureIntervalLengths = new double[numOfFeatures];
            double[] arrFeatureNumOfBreakPoints = new double[numOfFeatures];

            int featureIndex;
            foreach (Feature thisFeature in _features)
            {
                featureIndex = thisFeature.Index;
                arrFeatureMin[featureIndex] = thisFeature.Min;
                arrFeatureMax[featureIndex] = thisFeature.Max;
                arrFeatureIntervalLengths[featureIndex] = (arrFeatureMax[featureIndex] - arrFeatureMin[featureIndex]) / numOfIntervalsToDescretizeFeatures;
                arrFeatureNumOfBreakPoints[featureIndex] = numOfIntervalsToDescretizeFeatures + 1;
            }
            // feature names
            foreach (Feature thisFeature in _features)
                strFeatureNames[thisFeature.Index] = thisFeature.Name;

            // reset available action combinations to the initial setting
            _POMDP_ADP.MakeAllDynamicallyControlledActionsAvailable();

            // write the dynamic Policy
            double[] arrFeatureValues = new double[_features.Count];
            for (int fResourceIndex = 0; fResourceIndex < arrFeatureNumOfBreakPoints[0]; ++fResourceIndex)
            {
                optimalDecisions[fResourceIndex] = new int[numOfIntervalsToDescretizeFeatures + 1, numOfIntervalsToDescretizeFeatures + 1];

                // feature 0 (resource) values
                arrFeatureValues[0] = arrFeatureMin[0] + fResourceIndex * arrFeatureIntervalLengths[0];
                // header
                headers[0][fResourceIndex] = arrFeatureValues[0];
                for (int f1Index = 0; f1Index < arrFeatureNumOfBreakPoints[1]; f1Index++)
                {
                    // feature 1 values
                    arrFeatureValues[1] = arrFeatureMin[1] + f1Index * arrFeatureIntervalLengths[1];
                    // header
                    headers[1][f1Index] = arrFeatureValues[1];
                    for (int f2Index = 0; f2Index < arrFeatureNumOfBreakPoints[2]; f2Index++)
                    {
                        // feature 1 value
                        arrFeatureValues[2] = arrFeatureMin[2] + f2Index * arrFeatureIntervalLengths[2];
                        // header
                        headers[2][f2Index] = arrFeatureValues[2];
                        // optimal decision ID
                        optimalDecisions[fResourceIndex][f1Index, f2Index] =
                            ComputationLib.SupportFunctions.ConvertToBase10FromBase2(_POMDP_ADP.FindTheOptimalDynamicallyControlledActionCombination(arrFeatureValues));
                    }
                }
            }
        }
        // switch off all interventions controlled by decision rule
        public void SwitchOffAllInterventionsControlledByDecisionRule()
        {
            int[] interventionCombination = new int[_POMDP_ADP.NumberOfActions];

            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
            {
                if (thisIntervention.Type == SimulationLib.SimulationAction.enumActionType.Default)
                    interventionCombination[thisIntervention.ID] = 1;
                else if (thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.Dynamic)
                    interventionCombination[thisIntervention.ID] = 0;
                else if (thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.Predetermined)
                    interventionCombination[thisIntervention.ID] = (int)thisIntervention.PredeterminedSwitchValue;
            }

            //_POMDP_ADP.DefaultActionCombination(interventionCombination);
        }

        // get the value of parameters to calibrate
        public double[] GetValuesOfParametersToCalibrate()
        {
            double[] parValues = new double[_numOfParametersToCalibrate];
            int i = 0;
            foreach (Parameter thisParameter in _parameters.Where(p => p.IncludedInCalibration == true))
                parValues[i++] =  thisParameter.Value;

            return (double[])parValues.Clone();
        }

        // get possible designs for interval-based policies
        public ArrayList GetIntervalBasedStaticPoliciesDesigns()
        {
            //TODO: update this procedure based on the latest changes to POMDP_ADP 

            ArrayList designs = new ArrayList();
            int numOfActions = _POMDP_ADP.NumberOfActions;

            // add design for when all interventions are off (if allowed)
            designs.Add(new IntervalBasedStaticPolicy(0,
                GetIntervetionCombinationCodeWithAllSpecifiedByDecisionRuleInterventionTurnedOff(), new double[numOfActions], new int[numOfActions]));

            // for each possible combination 
            int designID = 1;
            for (int decisionsCode = 0; decisionsCode < Math.Pow(2, numOfActions); decisionsCode++)
            {
                // find a combination
                int[] interventionCombination = _POMDP_ADP.DynamicallyControlledActionCombinations[decisionsCode];
                // see if this is a feasible combination
                bool feasible = true;
                foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
                {
                    int interventionID = thisIntervention.ID;
                    // if this intervention is default, it can't be turned off
                    if (thisIntervention.Type == SimulationLib.SimulationAction.enumActionType.Default)
                    {
                        if (interventionCombination[interventionID] == 0)
                            feasible = false;
                    }
                    else if (thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.Predetermined)
                    {
                        if (interventionCombination[interventionID] != _POMDP_ADP.DefaultActionCombination[interventionID])
                            feasible = false;
                    }
                    else if (thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.IntervalBased && interventionCombination[interventionID] == 0)
                        feasible = false;                    

                    if (feasible == false)
                        break;
                }
                // if not feasible break the loop
                if (feasible == true)
                {
                    // find the design for the interval-based policy (ASSUMING that only one intervention can be used by the interval-based policy)
                    int interventionID = 0;
                    double startTime = _epidemicTimeIndexToStartDecisionMaking * _deltaT;
                    int numOfDecisionIntervals = 0;
                    double lastTimeToUseThisIntervention = 0;
                    int minNumOfDecisionPeriodsToUse = 0;
                    double[] interventionStartTimes = new double[numOfActions];
                    int[] numOfDecisionPeriodsToUse = new int[numOfActions];

                    foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
                        if (thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.IntervalBased)
                        {
                            interventionID = thisIntervention.ID;
                            lastTimeToUseThisIntervention = thisIntervention.IntervalBaseEmployment_availableUntilThisTime;
                            minNumOfDecisionPeriodsToUse = thisIntervention.IntervalBaseEmployment_minNumOfDecisionPeriodsToUse;
                        }

                    // interval-based parameters
                    while (startTime < lastTimeToUseThisIntervention)
                    {
                        numOfDecisionIntervals = minNumOfDecisionPeriodsToUse;
                        while (numOfDecisionIntervals * _decisionIntervalLength <= lastTimeToUseThisIntervention - startTime)
                        {
                            // design
                            interventionStartTimes[interventionID] = startTime;
                            numOfDecisionPeriodsToUse[interventionID] = numOfDecisionIntervals;
                            // add design
                            designs.Add(new IntervalBasedStaticPolicy(designID++, decisionsCode, interventionStartTimes, numOfDecisionPeriodsToUse));

                            numOfDecisionIntervals += minNumOfDecisionPeriodsToUse;
                        }
                        startTime += minNumOfDecisionPeriodsToUse * _decisionIntervalLength;
                    }
                }
            }            
                        
            return designs;
        }
        // get intervention combination for always on/off static policies with all interventions off expect for those that are always on
        public int GetIntervetionCombinationCodeWithAllSpecifiedByDecisionRuleInterventionTurnedOff()
        {
            int[] thisCombination = (int[])_POMDP_ADP.DefaultActionCombination.Clone();
            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
            {
                if (thisIntervention.Type == SimulationLib.SimulationAction.enumActionType.Default)
                    thisCombination[thisIntervention.ID] = 1;
                if (thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.IntervalBased ||
                    thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.ThresholdBased||
                    thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.Periodic ||
                    thisIntervention.OnOffSwitchSetting == SimulationLib.SimulationAction.enumOnOffSwitchSetting.Dynamic)
                    thisCombination[thisIntervention.ID] = 0;
            }
            return SupportFunctions.ConvertToBase10FromBase2(thisCombination);
        }        

        // ********* private subs to run the simulation model *************
        #region private subs to run the simulation model
        // simulate the trajectory assuming that parameter values are already assigned
        private bool Simulate(int simReplication, long timeIndexToStop)
        {
            bool toStop;
            bool acceptableTrajectory = false;
            bool ifThisIsAFeasibleCalibrationTrajectory = true;        

            // simulate the epidemic
            toStop = false;
            while (!toStop)
            {
                // reset current period statistics
                _currentPeriodCost = 0;
                _currentPeriodQALY = 0;

                // store outputs if necessary               
                StoreSelectedOutputWhileSimulating(simReplication, false, ref ifThisIsAFeasibleCalibrationTrajectory);

                // check if this is has been a feasible trajectory for calibration
                if (_modelUse == enumModelUse.Calibration && !ifThisIsAFeasibleCalibrationTrajectory)
                {
                    acceptableTrajectory = false;
                    return acceptableTrajectory;
                }

                // reset statistics if warm-up period has ended
                ResetStatisticsIfWarmUpPeriodHasEnded();

                // Update the resource status
                if (_resources.Count != 0)
                    CheckIfResourcesHaveBecomeAvailable();

                // read feature values if optimizing or using greedy decisions:
                if (_features.Count > 0 && _decisionRule == enumDecisionRule.SpecifiedByPolicy)
                    // update values of features
                    ReadValuesOfFeatures();

                // make decisions if decision is not predetermined and announce the new decisions (may not necessarily go into effect)
                MakeAndAnnounceDecisions();

                // put decisions into effect
                ImplementDecisionsThatCanGoIntoEffect(false);

                // update the effect of chance in time dependent parameter value
                UpdateTheEffectOfChangeInTimeDependentParameterValues(_currentSimulationTimeIndex * _deltaT);

                // update transmission rates
                UpdateTransmissionRates();

                // reset the number of new members to each class
                ResetClassNumberOfNewMembers();

                // send transfer class members                    
                TransferClassMembers();

                // advance time  
                _currentSimulationTimeIndex += 1;
                UpdateCurrentEpidemicTimeIndex();

                // if optimizing, update the cost of current decision period
                if (_modelUse == enumModelUse.Optimization)
                    UpdateRewardOfCurrentADPStateDecision();                

                // check if stopping rules are satisfied 
                if (_currentEpidemicTimeIndex >= timeIndexToStop || IsEradicationConditionsSatisfied() == true)
                {
                    toStop = true;
                    // is this trajectory acceptable
                    acceptableTrajectory = false;
                    if (_currentEpidemicTimeIndex >= _epidemicConditionTimeIndex)
                        acceptableTrajectory = true;
                    
                    // record end of simulation statistics
                    if (_modelUse == enumModelUse.Simulation && acceptableTrajectory == true)
                    {
                        // record simulation outcomes only if simulation time horizon has reached
                        if (_currentEpidemicTimeIndex >= _simulationHorizonTimeIndex || _stoppedDueToEradication)
                            // update annual cost
                            GatherEndOfSimulationStatistics();
                        // report simulation trajectories if necessary
                        StoreSelectedOutputWhileSimulating(simReplication, true, ref ifThisIsAFeasibleCalibrationTrajectory);
                    }
                    // store epidemic history for calibration purpose    
                    if (_modelUse == enumModelUse.Calibration && acceptableTrajectory == true)
                        // report simulation trajectories if necessary // if (_stoppedDueToEradication)
                        StoreSelectedOutputWhileSimulating(simReplication, true, ref acceptableTrajectory);

                    // reset statistics if warm-up period has ended
                    ResetStatisticsIfWarmUpPeriodHasEnded();
                }
            } // end while (!toStop)
            return acceptableTrajectory;
        }
        // Update the resource status
        private void CheckIfResourcesHaveBecomeAvailable()
        {
            _aResourceJustReplinished = false;

            foreach (Resource thisResource in _resources)
            {
                thisResource.ReplenishIfAvailable(_currentEpidemicTimeIndex * _deltaT);
                if (_arrAvailableResources[thisResource.ID] != thisResource.CurrentUnitsAvailable)
                {
                    _arrAvailableResources[thisResource.ID] = thisResource.CurrentUnitsAvailable;
                    _aResourceJustReplinished = true;
                }
            }
            // update the available resources to other classes
            if (_aResourceJustReplinished)
                UpdateResourceAvailabilityInformationForEachClass(_arrAvailableResources);
        }
        // update resources
        private void UpdateResourceAvailabilityBasedOnConsumptionOfThisClass(long[] arrResourcesConsumed)
        {
            if (arrResourcesConsumed.Length == 0 || arrResourcesConsumed.Sum() == 0)
                return;

            foreach (Resource thisResource in _resources)
            {
                thisResource.CurrentUnitsAvailable -= arrResourcesConsumed[thisResource.ID];
                _arrAvailableResources[thisResource.ID] = thisResource.CurrentUnitsAvailable;
            }

            // update the available resources to other classes
            UpdateResourceAvailabilityInformationForEachClass(_arrAvailableResources);
        }
        // update the information of resource availability for each resource-contained class 
        private void UpdateResourceAvailabilityInformationForEachClass(long[] arrAvailableResources)
        {            
            foreach (Class thisClass in _classes)
                thisClass.UpdateAvailableResources(arrAvailableResources);         
        }
        // read the feature values
        private void ReadValuesOfFeatures()
        {
            // check if it is time to record current state
            if (_currentEpidemicTimeIndex == _nextDecisionPointIndex) //|| _aResourceJustReplinished == true) //(EligibleToStoreADPStateDecision())
            {
                // update the values of features
                int i = 0;
                foreach (Feature thisFeature in _features)
                {
                    i = thisFeature.Index;

                    if (thisFeature is Feature_EpidemicTime)
                    {
                        _arrCurrentValuesOfFeatures[i] = _currentEpidemicTimeIndex * _deltaT;
                    }
                    else if (thisFeature is Feature_DefinedOnNewClassMembers)
                    {
                        _arrCurrentValuesOfFeatures[i] = Math.Max((_classes[((Feature_DefinedOnNewClassMembers)thisFeature).ClassID])
                                                                            .ReadFeatureValue((Feature_DefinedOnNewClassMembers)thisFeature), 0);
                    }
                    else if (thisFeature is Feature_DefinedOnResources)
                    {
                        _arrCurrentValuesOfFeatures[i] = (double)_arrAvailableResources[((Feature_DefinedOnResources)thisFeature).ResourceID];
                    }
                    else if (thisFeature is Feature_DefinedOnSummationStatistics)
                    {
                        int sumStatID = ((Feature_DefinedOnSummationStatistics)thisFeature).SumStatisticsID;
                        _arrCurrentValuesOfFeatures[i] = (_summationStatistics[sumStatID]).ReadFeatureValue((Feature_DefinedOnSummationStatistics)thisFeature);
                    }
                    else if (thisFeature is Feature_InterventionOnOffStatus)
                    {
                        int interventionID = ((Feature_InterventionOnOffStatus)thisFeature).InterventionID;
                        int numOfPastObservationPeriodToObserveOnOffValue = ((Feature_InterventionOnOffStatus)thisFeature).PreviousObservationPeriodToObserveOnOffValue;
                        _arrCurrentValuesOfFeatures[i] = _pastActionCombinations[numOfPastObservationPeriodToObserveOnOffValue][interventionID]; 
                    }
                    else if (thisFeature is Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed)
                    {
                        int interventionID = ((Feature_InterventionOnOffStatus)thisFeature).InterventionID;
                        _arrCurrentValuesOfFeatures[i] = ((SimulationLib.SimulationAction)_POMDP_ADP.Actions[interventionID]).NumOfDecisionPeriodsOverWhichThisInterventionWasUsed;
                    }

                    //switch (thisFeature.ToString())
                    //{
                    //    case "APACE_lib.Feature_EpidemicTime":
                    //        _arrCurrentValuesOfFeatures[i] = _currentEpidemicTimeIndex * _deltaT;
                    //        break;
                    //    case "APACE_lib.Feature_DefinedOnNewClassMembers":
                    //        _arrCurrentValuesOfFeatures[i] = Math.Max(((Class)_classes[((Feature_DefinedOnNewClassMembers)thisFeature).ClassID])
                    //                                                        .ReadFeatureValue((Feature_DefinedOnNewClassMembers)thisFeature), 0);
                    //        break;
                    //    case "APACE_lib.Feature_DefinedOnResources":
                    //        _arrCurrentValuesOfFeatures[i] = (double)_arrAvailableResources[((Feature_DefinedOnResources)thisFeature).ResourceID];
                    //        break;
                    //    case "APACE_lib.Feature_DefinedOnSummationStatistics":
                    //        {
                    //            int sumStatID = ((Feature_DefinedOnSummationStatistics)thisFeature).SumStatisticsID;
                    //            _arrCurrentValuesOfFeatures[i] = ((SummationStatistics)_summationStatistics[sumStatID]).ReadFeatureValue((Feature_DefinedOnSummationStatistics)thisFeature);
                    //        }
                    //        break;
                    //    case "APACE_lib.Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed":
                    //        {
                    //            int interventionID = ((Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed)thisFeature).InterventionID;
                    //            _arrCurrentValuesOfFeatures[i] = ((SimulationLib.SimulationAction)_POMDP_ADP.Actions[interventionID]).NumOfDecisionPeriodsOverWhichThisInterventionWasUsed;
                    //        }
                    //        break;
                    //}

                    // update the min max on this feature
                    thisFeature.UpdateMinMax(_arrCurrentValuesOfFeatures[i]);
                }
            }
        }
        // make and announce decision
        private void MakeAndAnnounceDecisions()
        {
            // is it time to make decision
            if (_currentEpidemicTimeIndex < _nextDecisionPointIndex)// && _aResourceJustReplinished == false)
                return;

            // go with the default action unless otherwise
            int[] newActionCombination = (int[])_POMDP_ADP.DefaultActionCombination.Clone();

            // find the optimal decision
            switch (_decisionRule)
            {
                case enumDecisionRule.SpecifiedByPolicy:
                    #region enumDecisionRule.SpecifiedByPolicy
                    {
                        // if no intervention is controlled dynamically
                        if (_POMDP_ADP.NumOfActionsControlledDynamically == 0)
                        {
                            // find a feasible action combination and select that as the new action combination
                            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
                            {
                                if (IfThisInterventionCanTakeThisOnOffStatus(thisIntervention, 1))
                                    newActionCombination[thisIntervention.Index] = 1;
                                else
                                    newActionCombination[thisIntervention.Index] = 0;
                                //for (int onOfStatus = 0; onOfStatus <=1 ; ++onOfStatus)
                                //{
                                //    if (IfThisInterventionCanTakeThisOnOffStatus(thisIntervention, onOfStatus))
                                //    {
                                //        newActionCombination[thisIntervention.Index] = onOfStatus;
                                //        break;
                                //    }
                                //}
                            }
                        }
                        else // there are interventions that are controlled dynamically
                        {
                            int[] indicesOfActionsControlledDynamically = _POMDP_ADP.IndicesOfActionsControlledDynamically;
                            int[] availablilityOfActionCombinationsControlledDynamically = new int[(int)Math.Pow(2, _POMDP_ADP.NumOfActionsControlledDynamically)];
                            int indexOfAnActionCombinationControlledDynamically = 0;
                            int interventionIndex;

                            // start with assuming that all action combinations controlled dynamically are feasible
                            SupportFunctions.MakeArrayEqualTo(ref availablilityOfActionCombinationsControlledDynamically, 1);

                            // go over all action combinations controlled dynamically and detect those infeasible
                            foreach (int[] thisDynamicallyControlledActionCombination in _POMDP_ADP.DynamicallyControlledActionCombinations)
                            {
                                // go over the interventions in this action combination
                                for (int i = 0; i < _POMDP_ADP.NumOfActionsControlledDynamically; i++)
                                {
                                    // get the index of this intervention which is dynamically controlled
                                    interventionIndex = _POMDP_ADP.IndicesOfActionsControlledDynamically[i];
                                    if (!IfThisInterventionCanTakeThisOnOffStatus((Intervention)_POMDP_ADP.Actions[interventionIndex], thisDynamicallyControlledActionCombination[i]))
                                    {
                                        availablilityOfActionCombinationsControlledDynamically[indexOfAnActionCombinationControlledDynamically] = 0;
                                        break;
                                    }
                                }
                                ++indexOfAnActionCombinationControlledDynamically;                                    
                            }

                            // update the feasible action combinations controlled dynamically
                            _POMDP_ADP.SpecifyAvailabilityOfDynamicallyControlledActionCombinations(availablilityOfActionCombinationsControlledDynamically);

                            // find the optimal dynamically controlled action combination 
                            int[] selectedActionCombinationDynamicallyControlled = null;
                            switch (_modelUse)
                            {
                                case enumModelUse.Simulation:
                                case enumModelUse.Calibration:
                                    {
                                       selectedActionCombinationDynamicallyControlled =  _POMDP_ADP.FindTheGreedyActionCombinationsDynamicallyControlled(_arrCurrentValuesOfFeatures);//, ref _currentPeriodCost);
                                    }
                                    break;

                                case enumModelUse.Optimization:
                                    {
                                        selectedActionCombinationDynamicallyControlled = _POMDP_ADP.FindAnEpsilongGreedyActionCombinationsDynamicallyControlled(_rng, _arrCurrentValuesOfFeatures);//, ref _currentPeriodCost);
                                    }
                                    break;
                            }

                            // find the new action combination to announce     
                            // first find the optimal on/off status of interventions dynamically control
                            for (int i = 0; i < _POMDP_ADP.NumOfActionsControlledDynamically; i++)
                            {
                                // get the index of this intervention which is dynamically controlled
                                interventionIndex = _POMDP_ADP.IndicesOfActionsControlledDynamically[i];
                                newActionCombination[interventionIndex] = selectedActionCombinationDynamicallyControlled[i];
                            }
                            // then find the on/off status of other interventions
                            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
                            {
                                if (thisIntervention.OnOffSwitchSetting != SimulationAction.enumOnOffSwitchSetting.Dynamic)
                                {
                                    for (int onOfStatus = 0; onOfStatus <= 1; ++onOfStatus)
                                    {
                                        if (IfThisInterventionCanTakeThisOnOffStatus(thisIntervention, onOfStatus))
                                        {
                                            newActionCombination[thisIntervention.Index] = onOfStatus;
                                            break;
                                        }
                                    }
                                }                                
                            }                            
                        }
                    }
                    break;
                    #endregion
                case enumDecisionRule.PredeterminedSequence:
                    #region enumDecisionRule.PredeterminedSequence:
                    {
                        //int[] nextPeriodSelectedInterventionCombination
                        //    = _prespecifiedDecisionsOverObservationPeriods[(int)_currentEpidemicTimeIndex / _numOfDeltaTIndexInAnObservationPeriod]; //[_decisionPeriodIndex];
                        //newActionCombination = ComputationLib.SupportFunctions.ConvertToBase2FromBase10(nextPeriodSelectedInterventionCombinationCode, _POMDP_ADP.NumberOfActions);
                        newActionCombination = _prespecifiedDecisionsOverObservationPeriods[(int)_currentEpidemicTimeIndex / _numOfDeltaTIndexInAnObservationPeriod];
                    }
                    break;                
                    #endregion
            }

            // announce decision
            AnnounceDecision(newActionCombination, true);
            
            // store current ADP state-decision
            if (_modelUse == enumModelUse.Optimization)
                StoreCurrentADPStateDecision();

            // if this decision is made not because it is a decision time but because a resource just became available.            
            if (_currentEpidemicTimeIndex == _nextDecisionPointIndex)
            {
                _decisionPeriodIndex += 1;
                _nextDecisionPointIndex += _numOfDeltaTsInADecisionInterval;
            }
        }
        // find available action combinations
        private int[] FindWhichDynamicallyControlledActionCombinationsAreAvailable()
        {
            int[] result = new int[_POMDP_ADP.DynamicallyControlledActionCombinations.Length];

            int i = 0;
            foreach (int[] thisActionCombination in _POMDP_ADP.DynamicallyControlledActionCombinations)
            {
                if (IsThisActionCombinationFeasible(thisActionCombination))
                    result[i++] = 1;
            }

            return result;
        }
        // check if this action combination is feasible
        private bool IfThisInterventionCanTakeThisOnOffStatus(Intervention thisIntervention, int onOffStatus)// IsThisActionCombinationFeasible(int[] actionCombination)
        {
            bool result = true;
            int index = thisIntervention.Index;

            // default intervention should always be on
            if (thisIntervention.Type == SimulationLib.SimulationAction.enumActionType.Default)
            {
                if (onOffStatus == 0)
                    return false;
            }
            else // if this intervention is not of type default
            {
                // check the availability
                if (onOffStatus == 1)
                {
                    // time
                    if (_currentEpidemicTimeIndex < thisIntervention.TimeIndexBecomeAvailable || _currentEpidemicTimeIndex >= thisIntervention.TimeIndexBecomeUnavailable)
                        return false;
                    // resources
                    if (thisIntervention.HasResourceCondition)
                        if (_arrAvailableResources[thisIntervention.IDOfTheResourceRequiredToBeAvailable] <= 0)
                            return false;
                }
                else // if (onOffStatus == 0)
                {
                    if (thisIntervention.RemainsOnOnceSwitchedOn && thisIntervention.IfThisInterventionHasBeenEmployedBefore)
                        return false;
                }

                // find the on/off switching type
                switch (thisIntervention.OnOffSwitchSetting)
                {
                    #region predetermined
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Predetermined:
                        {
                            if (thisIntervention.PredeterminedSwitchValue == SimulationLib.SimulationAction.enumSwitchValue.On &&
                                onOffStatus == 0)
                                return false;
                            if (thisIntervention.PredeterminedSwitchValue == SimulationLib.SimulationAction.enumSwitchValue.Off &&
                                onOffStatus == 1)
                                return false;
                        }
                        break;
                    #endregion

                    #region interval based
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.IntervalBased:
                        {
                            // already checked
                        }
                        break;
                    #endregion

                    #region threshold base
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.ThresholdBased:
                        {
                            int idOfSpecialStat = thisIntervention.ThresholdBasedEmployment_IDOfTheSpecialStatisticsToObserveAccumulation;
                            double threshold = thisIntervention.ThresholdBasedEmployment_thresholdToTriggerThisIntervention;
                            long numOfTimeIncidesToUse = thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention;

                            // read the value of special statistics
                            double observedEpidemiologicalMeasure = 0;
                            foreach (SummationStatistics thisSumStat in _summationStatistics)
                            {
                                if (thisSumStat.ID == idOfSpecialStat)
                                {
                                    switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
                                    {
                                        case Intervention.EnumEpidemiologicalObservation.Accumulating:
                                            observedEpidemiologicalMeasure = thisSumStat.ObservedAccumulatedNewMembers;
                                            break;
                                        case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
                                            observedEpidemiologicalMeasure = thisSumStat.NewMembersOverPastObservableObsPeriod;
                                            break;
                                    }
                                    break;
                                }
                            }
                            foreach (RatioStatistics thisRatioStat in _ratioStatistics)
                            {
                                if (thisRatioStat.ID == idOfSpecialStat)
                                {
                                    observedEpidemiologicalMeasure = thisRatioStat.CurrentValue;
                                    break;
                                }

                            }
                            // check the feasibility conditions
                            // if this intervention is on 
                            if (onOffStatus == 1)
                            {
                                // find the type of threshold
                                switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
                                {
                                    case Intervention.EnumEpidemiologicalObservation.Accumulating:
                                        #region
                                        {
                                            // compare the observation with the triggering threshold
                                            if (observedEpidemiologicalMeasure < threshold)
                                            {
                                                // check if the intervention should still remain in use 
                                                if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
                                                    return false;
                                                // check how much longer this intervention should remain in use
                                                else if (thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
                                                    return false;
                                            }
                                            else // if (observedEpidemiologicalMeasure >= threshold)
                                            {
                                                // it maybe infeasible only if this intervention has been used before
                                                if (thisIntervention.IfThisInterventionHasBeenEmployedBefore &&
                                                    thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
                                                    return false;
                                            }
                                        }
                                        #endregion
                                        break;
                                    case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
                                        #region
                                        {
                                            // compare the observation with the triggering threshold
                                            if (observedEpidemiologicalMeasure < threshold)
                                            {
                                                // check if the intervention should still remain in use 
                                                if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
                                                    return false;
                                                // check how much longer this intervention should remain in use
                                                else if (thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
                                                    return false;
                                            }
                                            else // if (observedEpidemiologicalMeasure >= threshold)
                                            {
                                                // should remain on
                                            }
                                        }
                                        #endregion
                                        break;
                                }
                            }
                            else // if this intervention is off
                            {
                                // find the type of threshold
                                switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
                                {
                                    case Intervention.EnumEpidemiologicalObservation.Accumulating:
                                        #region
                                        {
                                            // compare the observation with the triggering threshold
                                            if (observedEpidemiologicalMeasure < threshold)
                                            {
                                                // check if the intervention should still remain in use 
                                                if (thisIntervention.IfThisInterventionHasBeenEmployedBefore &&
                                                    thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention > _currentEpidemicTimeIndex)
                                                    return false;
                                            }
                                            else // if (observedEpidemiologicalMeasure >= threshold)
                                            {
                                                // it is infeasible if this intervention has been used before
                                                if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
                                                    return false;
                                            }
                                        }
                                        #endregion
                                        break;
                                    case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
                                        #region
                                        {
                                            // compare the observation with the triggering threshold
                                            if (observedEpidemiologicalMeasure < threshold)
                                            {
                                                // check if the intervention should still remain in use 
                                                if (thisIntervention.IfThisInterventionHasBeenEmployedBefore &&
                                                    thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                    thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                    thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention > _currentEpidemicTimeIndex)
                                                    return false;
                                            }
                                            else // if (observedEpidemiologicalMeasure >= threshold)
                                            {
                                                return false;
                                            }
                                        }
                                        #endregion
                                        break;
                                }
                            }
                        }
                        break;
                    #endregion

                    #region periodic
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Periodic:
                        {
                            int frequency_numOfDecisionPeriods = thisIntervention.PeriodicEmployment_Frequency_NumOfDcisionPeriods;
                            int duration_numOfDecisionPeriods = thisIntervention.PeriodicEmployment_Duration_NumOfDcisionPeriods;

                            // check the feasibility conditions
                            if (onOffStatus == 1)
                            {
                                if (_currentEpidemicTimeIndex > 0 &&
                                    _currentEpidemicTimeIndex >= thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + duration_numOfDecisionPeriods * _numOfDeltaTsInADecisionInterval)
                                    return false;
                            }
                            else // if this intervention is off
                            {
                                if (_currentEpidemicTimeIndex >= thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + frequency_numOfDecisionPeriods * _numOfDeltaTsInADecisionInterval)
                                    return false;
                                if (_currentEpidemicTimeIndex < thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + duration_numOfDecisionPeriods * _numOfDeltaTsInADecisionInterval)
                                    return false;
                            }
                        }
                        break;
                    #endregion

                    #region dynamic
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Dynamic:
                        {
                            // if it has to remain on once it's turned on
                            if (thisIntervention.RemainsOnOnceSwitchedOn == true)
                            {
                                if (onOffStatus == 0 && _POMDP_ADP.CurrentActionCombination[index] == 1)
                                    return false;
                            }
                        }
                        break;
                    #endregion
                }
            } // if (thisIntervention.Type == SimulationLib.SimulationAction.enumInterventionType.Default)

            return result;
        }
        // check if this action combination is feasible
        private bool IsThisActionCombinationFeasible(int[] actionCombination)
        {
            bool result = true;

            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
            {
                int index = thisIntervention.Index;

                // default intervention should always be on
                if (thisIntervention.Type == SimulationLib.SimulationAction.enumActionType.Default)
                {
                    if (actionCombination[index] == 0)
                        return false;
                }
                else // if this intervention is not of type default
                {
                    // check the availability
                    if (actionCombination[index] == 1)
                    {
                        // time
                        if (_currentEpidemicTimeIndex < thisIntervention.TimeIndexBecomeAvailable || _currentEpidemicTimeIndex >= thisIntervention.TimeIndexBecomeUnavailable)
                            return false;
                        // resources
                        if (thisIntervention.HasResourceCondition)
                            if (_arrAvailableResources[thisIntervention.IDOfTheResourceRequiredToBeAvailable] <= 0)
                                return false;
                    }
                    else
                    {
                        if (thisIntervention.RemainsOnOnceSwitchedOn && thisIntervention.IfThisInterventionHasBeenEmployedBefore)
                            return false;
                    }
                        

                    // find the on/off switching type
                    switch (thisIntervention.OnOffSwitchSetting)
                    {
                        #region predetermined
                        case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Predetermined:
                            {
                                if (thisIntervention.PredeterminedSwitchValue == SimulationLib.SimulationAction.enumSwitchValue.On &&
                                    actionCombination[index] == 0)
                                    return false;
                                if (thisIntervention.PredeterminedSwitchValue == SimulationLib.SimulationAction.enumSwitchValue.Off &&
                                    actionCombination[index] == 1)
                                    return false;
                            }
                            break;
                        #endregion

                        #region interval based
                        case SimulationLib.SimulationAction.enumOnOffSwitchSetting.IntervalBased:
                            {
                                // already checked
                            }
                            break;
                        #endregion

                        #region threshold base
                        case SimulationLib.SimulationAction.enumOnOffSwitchSetting.ThresholdBased:
                            {
                                int idOfSpecialStat = thisIntervention.ThresholdBasedEmployment_IDOfTheSpecialStatisticsToObserveAccumulation;
                                double threshold = thisIntervention.ThresholdBasedEmployment_thresholdToTriggerThisIntervention;
                                long numOfTimeIncidesToUse = thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention;

                                // read the value of special statistics
                                double observedEpidemiologicalMeasure = 0;
                                foreach (SummationStatistics thisSumStat in _summationStatistics)
                                {
                                    if (thisSumStat.ID == idOfSpecialStat)
                                    {
                                        switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
                                        {
                                            case Intervention.EnumEpidemiologicalObservation.Accumulating: 
                                                observedEpidemiologicalMeasure = thisSumStat.ObservedAccumulatedNewMembers;
                                                break;
                                            case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
                                                observedEpidemiologicalMeasure = thisSumStat.NewMembersOverPastObservableObsPeriod;
                                                break;
                                        }
                                        break;
                                    }
                                }
                                // check the feasibility conditions
                                // if this intervention is on 
                                if (actionCombination[index] == 1)
                                {
                                    // find the type of threshold
                                    switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
                                    {
                                        case Intervention.EnumEpidemiologicalObservation.Accumulating:
                                            #region
                                            {
                                                // compare the observation with the triggering threshold
                                                if (observedEpidemiologicalMeasure < threshold)
                                                {
                                                    // check if the intervention should still remain in use 
                                                    if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
                                                        return false;
                                                    // check how much longer this intervention should remain in use
                                                    else if (thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                        thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                        thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
                                                        return false;
                                                }
                                                else // if (observedEpidemiologicalMeasure >= threshold)
                                                {
                                                    // it maybe infeasible only if this intervention has been used before
                                                    if (thisIntervention.IfThisInterventionHasBeenEmployedBefore &&
                                                        thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                        thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                        thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
                                                        return false;
                                                }
                                            }
                                            #endregion
                                            break;
                                        case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
                                            #region
                                            {
                                                // compare the observation with the triggering threshold
                                                if (observedEpidemiologicalMeasure < threshold)
                                                {
                                                    // check if the intervention should still remain in use 
                                                    if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
                                                        return false;
                                                    // check how much longer this intervention should remain in use
                                                    else if (thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                        thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                        thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention <= _currentEpidemicTimeIndex)
                                                        return false;
                                                }
                                                else // if (observedEpidemiologicalMeasure >= threshold)
                                                {
                                                    // should remain on
                                                }
                                            }
                                            #endregion
                                            break;
                                    }
                                }
                                else // if this intervention is off
                                {
                                    // find the type of threshold
                                    switch (thisIntervention.ThresholdBasedEmployment_EpidemiologicalObservation)
                                    {
                                        case Intervention.EnumEpidemiologicalObservation.Accumulating:
                                            #region
                                            {
                                                // compare the observation with the triggering threshold
                                                if (observedEpidemiologicalMeasure < threshold)
                                                {
                                                    // check if the intervention should still remain in use 
                                                    if (thisIntervention.IfThisInterventionHasBeenEmployedBefore && 
                                                        thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                        thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                        thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention > _currentEpidemicTimeIndex)
                                                        return false;
                                                }
                                                else // if (observedEpidemiologicalMeasure >= threshold)
                                                {
                                                    // it is infeasible if this intervention has been used before
                                                    if (!thisIntervention.IfThisInterventionHasBeenEmployedBefore)
                                                        return false;
                                                }
                                            }
                                            #endregion
                                            break;
                                        case Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod:
                                            #region
                                            {
                                                // compare the observation with the triggering threshold
                                                if (observedEpidemiologicalMeasure < threshold)
                                                {
                                                    // check if the intervention should still remain in use 
                                                    if (thisIntervention.IfThisInterventionHasBeenEmployedBefore &&
                                                        thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn +
                                                        thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn +
                                                        thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention > _currentEpidemicTimeIndex)
                                                        return false;
                                                }
                                                else // if (observedEpidemiologicalMeasure >= threshold)
                                                {
                                                    return false;
                                                }
                                            }
                                            #endregion
                                            break;
                                    }
                                }
                            }
                            break;
                        #endregion

                        #region periodic
                        case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Periodic:
                            {
                                int frequency_numOfDecisionPeriods = thisIntervention.PeriodicEmployment_Frequency_NumOfDcisionPeriods;
                                int duration_numOfDecisionPeriods = thisIntervention.PeriodicEmployment_Duration_NumOfDcisionPeriods;

                                // check the feasibility conditions
                                if (actionCombination[index] == 1)
                                {
                                    if (_currentEpidemicTimeIndex > 0 &&
                                        _currentEpidemicTimeIndex >= thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + duration_numOfDecisionPeriods * _numOfDeltaTsInADecisionInterval)
                                        return false;
                                }
                                else // if this intervention is off
                                {
                                    if (_currentEpidemicTimeIndex >= thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + frequency_numOfDecisionPeriods * _numOfDeltaTsInADecisionInterval)
                                        return false;
                                    if (_currentEpidemicTimeIndex < thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn + duration_numOfDecisionPeriods * _numOfDeltaTsInADecisionInterval)
                                        return false;
                                }
                            }
                            break;
                        #endregion

                        #region dynamic
                        case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Dynamic:
                            {
                                // if it has to remain on once it's turned on
                                if (thisIntervention.RemainsOnOnceSwitchedOn == true)
                                {
                                    if (actionCombination[index] == 0 && _POMDP_ADP.CurrentActionCombination[index] == 1)
                                        return false;
                                }
                            }
                            break;
                        #endregion
                    }
                } // if (thisIntervention.Type == SimulationLib.SimulationAction.enumInterventionType.Default)
            }

            return result;
        }
        // announce the decision (may not necessarily go into effect)
        private void AnnounceDecision(int[] newInterventionCombination, bool checkIfItIsDifferentFromPast)
        {
            int[] currentInterventionCombination = (int[])_POMDP_ADP.GetCurrentActionCombination().Clone();

            //if (checkIfItIsDifferentFromPast == true && currentInterventionCombination.SequenceEqual(newInterventionCombination))
            if (checkIfItIsDifferentFromPast == true && currentInterventionCombination.SequenceEqual(newInterventionCombination))
            {
                // update the decisions just to collect necessary statistics
                _POMDP_ADP.ChangeCurrentActionCombination(newInterventionCombination);
                return;
            }
            // record the time of switch
            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
            {
                // if the intervention is turning on
                if (currentInterventionCombination[thisIntervention.Index] == 0 && newInterventionCombination[thisIntervention.Index] == 1)
                {
                    thisIntervention.IfThisInterventionHasBeenEmployedBefore = true;
                    thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOn = _currentEpidemicTimeIndex;

                    // find the time that this intervention go into effect
                    thisIntervention.EpidemicTimeIndexToGoIntoEffect = _currentEpidemicTimeIndex + thisIntervention.NumOfTimeIndeciesDelayedBeforeGoingIntoEffectOnceTurnedOn;
                    thisIntervention.EpidemicTimeIndexToBeLifted = long.MaxValue;
                }
                // if the intervention is turning off
                else if (currentInterventionCombination[thisIntervention.Index] == 1 && newInterventionCombination[thisIntervention.Index] == 0)
                {
                    thisIntervention.EpidemicTimeIndexWhenThisInterventionTurnedOff = _currentEpidemicTimeIndex;
                    // find the time that this intervention go into effect
                    thisIntervention.EpidemicTimeIndexToBeLifted = _currentEpidemicTimeIndex;
                    thisIntervention.EpidemicTimeIndexToGoIntoEffect = long.MaxValue;
                }
                else if (thisIntervention.Type != SimulationAction.enumActionType.Default)
                {
                    if (currentInterventionCombination[thisIntervention.Index] == 0)
                        thisIntervention.EpidemicTimeIndexToGoIntoEffect = long.MaxValue;
                    //if (currentInterventionCombination[thisIntervention.Index] == 1)
                        //thisIntervention.EpidemicTimeIndexToBeLifted = long.MaxValue;
                }
            }

            // update fixed and penalty costs of decisions
            _POMDP_ADP.UpdateCurrentFixedAndPenaltyCosts(newInterventionCombination);
            // update the action 
            _POMDP_ADP.ChangeCurrentActionCombination(newInterventionCombination);

            // find the next epidemic time where a decision should go into effect
            _nextEpidemicTimeIndexWhenAnInterventionEffectChanges = FindNextEpidemicTimeIndexWhenAnInterventionEffectChanges();

        }
        // make the decision effective 
        private void ImplementDecisionsThatCanGoIntoEffect(bool ifToInitializeSimulatoin)
        {
            if (_nextEpidemicTimeIndexWhenAnInterventionEffectChanges > _currentEpidemicTimeIndex && !ifToInitializeSimulatoin)
                return;

            // find the decision that is announced
            if (ifToInitializeSimulatoin)
                 _currentInterventionCombinationInEffect = (int[])_POMDP_ADP.GetCurrentActionCombination().Clone();

            int[] nextPeriodInterventionCombinationInEffect = (int[])_POMDP_ADP.GetCurrentActionCombination().Clone();

            // find the interventions with change in status
            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
            {
                if (thisIntervention.Type == SimulationAction.enumActionType.Default)
                    nextPeriodInterventionCombinationInEffect[thisIntervention.Index] = 1;
                else
                {
                    if (thisIntervention.EpidemicTimeIndexToGoIntoEffect > _currentEpidemicTimeIndex && _currentInterventionCombinationInEffect[thisIntervention.Index] == 0)
                        nextPeriodInterventionCombinationInEffect[thisIntervention.Index] = 0;

                    if (thisIntervention.EpidemicTimeIndexToBeLifted <= _currentEpidemicTimeIndex)
                        nextPeriodInterventionCombinationInEffect[thisIntervention.Index] = 0;

                    if (_currentInterventionCombinationInEffect[thisIntervention.Index] == 0 && nextPeriodInterventionCombinationInEffect[thisIntervention.Index] == 1)
                    {
                        thisIntervention.EpidemicTimeIndexToGoIntoEffect = long.MaxValue;
                        thisIntervention.EpidemicTimeIndexToBeLifted = long.MaxValue;
                        switch (thisIntervention.OnOffSwitchSetting)
                        {
                            case SimulationAction.enumOnOffSwitchSetting.Predetermined:
                                break;

                            case SimulationAction.enumOnOffSwitchSetting.ThresholdBased:
                                 thisIntervention.EpidemicTimeIndexToBeLifted = _currentEpidemicTimeIndex + thisIntervention.ThresholdBasedEmployment_numOfTimeIndicesToUseThisIntervention;
                                break;
                        }                        
                    }
                    if (_currentInterventionCombinationInEffect[thisIntervention.Index] == 1 && nextPeriodInterventionCombinationInEffect[thisIntervention.Index] == 0)
                    {
                        thisIntervention.EpidemicTimeIndexToGoIntoEffect = long.MaxValue;
                        thisIntervention.EpidemicTimeIndexToBeLifted = long.MaxValue;
                    }
                }
            }

            _currentInterventionCombinationInEffect = (int[])nextPeriodInterventionCombinationInEffect.Clone();

            // update the variable decision costs
            _POMDP_ADP.UpdateCurrentCostPerUnitOfTime(_currentInterventionCombinationInEffect);

            // update decision for each class
            foreach (Class thisClass in _classes)
                thisClass.SelectThisInterventionCombination(_currentInterventionCombinationInEffect);

            // find the next epidemic time where a decision should go into effect
            _nextEpidemicTimeIndexWhenAnInterventionEffectChanges = FindNextEpidemicTimeIndexWhenAnInterventionEffectChanges();
        }
        // find next epidemic time index when an intervention effect changes
        private long FindNextEpidemicTimeIndexWhenAnInterventionEffectChanges()
        {
            long nextEpidemicTimeIndexWhenAnInterventionEffectChanges = long.MaxValue;
            long temp;
            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
            {
                if (thisIntervention.Type != SimulationAction.enumActionType.Default ) // 
                    //(thisIntervention.EpidemicTimeIndexToGoIntoEffect < _nextEpidemicTimeIndexWhenAnInterventionEffectChanges ||
                    //thisIntervention.EpidemicTimeIndexToBeLifted < _nextEpidemicTimeIndexWhenAnInterventionEffectChanges)
                {
                    temp = Math.Min(thisIntervention.EpidemicTimeIndexToBeLifted, thisIntervention.EpidemicTimeIndexToGoIntoEffect);
                    nextEpidemicTimeIndexWhenAnInterventionEffectChanges = Math.Min(temp, nextEpidemicTimeIndexWhenAnInterventionEffectChanges);
                }
            }
            return nextEpidemicTimeIndexWhenAnInterventionEffectChanges;
        }
        // find the index of this intervention combination in transmission matrix
        private int FindIndexOfInterventionCombimbinationInTransmissionMatrix(int[] interventionCombination)
        {
            for (int i = 0; i < _numOfInterventionsAffectingContactPattern; i++)
            {
                _onOffStatusOfInterventionsAffectingContactPattern[i] = interventionCombination[_indecesOfInterventionsAffectingContactPattern[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfInterventionsAffectingContactPattern);
        }
        // find the index of this intervention combination in contact matrices
        private int FindIndexOfInterventionCombimbinationInContactMatrices(int[] interventionCombination)
        {
            for (int i = 0; i < _numOfInterventionsAffectingContactPattern; i++)
            {
                _onOffStatusOfInterventionsAffectingContactPattern[i] = interventionCombination[_indecesOfInterventionsAffectingContactPattern[i]];
            }
            return SupportFunctions.ConvertToBase10FromBase2(_onOffStatusOfInterventionsAffectingContactPattern);
        }
        // add current ADP state-decision
        private void StoreCurrentADPStateDecision()
        {
            // check if it is time to record current state
            if (!EligibleToStoreADPStateDecision())
                return;            

            // make a new state-decision
            ADP_State thisADPState = new ADP_State(_arrCurrentValuesOfFeatures, _POMDP_ADP.GetCurrentActionCombination());
            thisADPState.ValidStateToUpdateQFunction = true;

            //// check if this is eligible            
            ////thisADPState.ValidStateToUpdateQFunction = true;
            //if (_POMDP_ADP.EpsilonGreedyDecisionSelectedAmongThisManyAlternatives > 1)
            //    thisADPState.ValidStateToUpdateQFunction = true;
            //else
            //    thisADPState.ValidStateToUpdateQFunction = false;

            // store the adp state-decision
            _POMDP_ADP.AddAnADPState(_adpSimItr, thisADPState);            
        }
        // check if conditions for recording a ADP state-decision is satisfied
        private bool EligibleToStoreADPStateDecision()
        {
            bool eligible = true;

            // first check the time
            if (_currentEpidemicTimeIndex != _nextDecisionPointIndex || _modelUse != enumModelUse.Optimization)
                return false;

            return eligible;
        }
        // store selected outputs while simulating
        private void StoreSelectedOutputWhileSimulating(int simReplication, bool endOfSimulation, ref bool ifThisIsAFeasibleTrajectory)
        {
            // check if it is time to report output
            if (_currentSimulationTimeIndex < _nextTimeIndexToCollectSimulationOutputData &&
                _currentSimulationTimeIndex < _nextTimeIndexToCollectObservationPeriodData && endOfSimulation == false)
                return;

            // define the jagged array to store current observation
            int[][] thisActionCombination = new int[1][];
            double[][] thisSimulationTimeBasedOutputs = new double[1][];
            double[][] thisSimulationIntervalBasedOutputs = new double[1][];
            double[][] thisObservedOutputs = new double[1][];
            double[][] thisCalibrationObservation= new double[1][];
            double[][] thisResourceAvailability = new double[1][];
            double[][] thisTimeOfObservableOutputs = new double[1][];
            int colIndexSimulationTimeBasedOutputs = 0;
            int colIndexSimulationIntervalBasedOutputs = 0;
            int colIndexObservableOutputs = 0;
            int colIndexCalibrationObservation = 0;
            double nominatorValue = 0, denominatorValue = 0, ratio = 0;

            // first find the values of ratio statistics (using observed simulation outputs)
            // ratio statistics
            foreach (RatioStatistics thisRatioStat in _ratioStatistics)
            {
                // find the type of this ratio statistics
                switch (thisRatioStat.Type)
                {
                    case APACE_lib.RatioStatistics.enumType.IncidenceOverIncidence:
                        {
                            nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                            denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                            if (denominatorValue != 0)
                            {
                                ratio = nominatorValue / denominatorValue;
                                thisRatioStat.Record(nominatorValue, denominatorValue);
                            }
                        }
                        break;
                    case APACE_lib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence:
                        {
                            nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).ObservedAccumulatedNewMembers;
                            denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).ObservedAccumulatedNewMembers;
                            if (denominatorValue != 0)
                            {
                                ratio = nominatorValue / denominatorValue;
                                thisRatioStat.Record(nominatorValue, denominatorValue);
                            }
                        }
                        break;
                    case APACE_lib.RatioStatistics.enumType.PrevalenceOverPrevalence:
                        {
                            nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).CurrentMembers;
                            denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).CurrentMembers;
                            if (denominatorValue != 0)
                            {
                                ratio = nominatorValue / denominatorValue;
                                thisRatioStat.Record(nominatorValue, denominatorValue);
                            }
                        }
                        break;
                    case APACE_lib.RatioStatistics.enumType.IncidenceOverPrevalence:
                        {
                            nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                            denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).CurrentMembers;
                            if (denominatorValue != 0)
                            {
                                ratio = nominatorValue / denominatorValue;
                                thisRatioStat.Record(nominatorValue, denominatorValue);
                            }
                        }
                        break;
                }
            }

            // store simulation output data - time based and interval based
            #region store simulation output data
            if (_currentSimulationTimeIndex >= _nextTimeIndexToCollectSimulationOutputData)
            {
                // next time index outputs should be recorded
                _nextTimeIndexToCollectSimulationOutputData += _numOfDeltaTIndexInASimulationOutputInterval;                

                if (_storeEpidemicTrajectories == true)
                {
                    thisSimulationTimeBasedOutputs[0] = new double[_numOfTimeBasedOutputsToReport];
                    thisSimulationIntervalBasedOutputs[0] = new double[_numOfIntervalBasedOutputsToReport];
                    // the current time and interval
                    thisSimulationTimeBasedOutputs[0][colIndexSimulationTimeBasedOutputs++] = simReplication;
                    thisSimulationTimeBasedOutputs[0][colIndexSimulationTimeBasedOutputs++] = _currentSimulationTimeIndex * _deltaT;
                    thisSimulationIntervalBasedOutputs[0][colIndexSimulationIntervalBasedOutputs++] = Math.Floor(_currentSimulationTimeIndex * _deltaT / _simulationOutputIntervalLength);
                    // action combination
                    thisActionCombination[0] = (int[])_POMDP_ADP.GetCurrentActionCombination().Clone();

                    // check which statistics should be reported
                    foreach (Class thisClass in _classes)
                    {
                        if (thisClass.ShowNewMembers)
                            thisSimulationIntervalBasedOutputs[0][colIndexSimulationIntervalBasedOutputs++] = thisClass.NewMembersOverPastSimulationOutputInterval;
                        if (thisClass.ShowMembersInClass)
                            thisSimulationTimeBasedOutputs[0][colIndexSimulationTimeBasedOutputs++] = thisClass.CurrentNumberOfMembers;
                        if (thisClass.ShowAccumulatedNewMembers)
                            thisSimulationTimeBasedOutputs[0][colIndexSimulationTimeBasedOutputs++] = thisClass.AccumulatedNewMembers;
                    }
                    // summation statistics
                    foreach (SummationStatistics thisSumStat in _summationStatistics.Where(s => s.IfDisplay))
                    {
                        switch (thisSumStat.Type)
                        {
                            case APACE_lib.SummationStatistics.enumType.Incidence:
                                thisSimulationIntervalBasedOutputs[0][colIndexSimulationIntervalBasedOutputs++] = thisSumStat.NewMemberOverPastSimulationInterval;
                                break;
                            case APACE_lib.SummationStatistics.enumType.AccumulatingIncident:
                                thisSimulationTimeBasedOutputs[0][colIndexSimulationTimeBasedOutputs++] = thisSumStat.AccumulatedNewMembers;
                                break;
                            case APACE_lib.SummationStatistics.enumType.Prevalence:
                                thisSimulationTimeBasedOutputs[0][colIndexSimulationTimeBasedOutputs++] = thisSumStat.CurrentMembers;
                                break;
                        }
                    }
                    // ratio statistics
                    foreach (RatioStatistics thisRatioStat in _ratioStatistics.Where(s => s.IfDisplay))
                    {
                        // find the type of this ratio statistics
                        switch (thisRatioStat.Type)
                        {
                            case APACE_lib.RatioStatistics.enumType.IncidenceOverIncidence:
                                {
                                    nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).NewMemberOverPastSimulationInterval;
                                    denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).NewMemberOverPastSimulationInterval;
                                    if (denominatorValue == 0)
                                        ratio = -1;
                                    else
                                        ratio = nominatorValue / denominatorValue;
                                    thisSimulationIntervalBasedOutputs[0][colIndexSimulationIntervalBasedOutputs++] = ratio;
                                    //thisRatioStat.Record(nominatorValue, denominatorValue);
                                }
                                break;
                            case APACE_lib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence:
                                {
                                    nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).AccumulatedNewMembers;
                                    denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).AccumulatedNewMembers;
                                    if (denominatorValue == 0)
                                        ratio = -1;
                                    else
                                        ratio = nominatorValue / denominatorValue;
                                    thisSimulationTimeBasedOutputs[0][colIndexSimulationTimeBasedOutputs++] = ratio;
                                }
                                break;
                            case APACE_lib.RatioStatistics.enumType.PrevalenceOverPrevalence:
                                {
                                    nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).CurrentMembers;
                                    denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).CurrentMembers;
                                    if (denominatorValue == 0)
                                        ratio = -1;
                                    else
                                        ratio = nominatorValue / denominatorValue;
                                    thisSimulationTimeBasedOutputs[0][colIndexSimulationTimeBasedOutputs++] = ratio;
                                    //thisRatioStat.Record(nominatorValue, denominatorValue);
                                }
                                break;
                            case APACE_lib.RatioStatistics.enumType.IncidenceOverPrevalence:
                                {
                                    nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).NewMemberOverPastSimulationInterval;
                                    denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).CurrentMembers;
                                    if (denominatorValue == 0)
                                        ratio = -1;
                                    else
                                        ratio = nominatorValue / denominatorValue;
                                    thisSimulationIntervalBasedOutputs[0][colIndexSimulationIntervalBasedOutputs++] = ratio;
                                    //thisRatioStat.Record(nominatorValue, denominatorValue);
                                }
                                break;
                        }
                    }
                    
                    // concatenate thisRow 
                    _simulationTimeBasedOutputs = ComputationLib.SupportFunctions.ConcatJaggedArray(_simulationTimeBasedOutputs, thisSimulationTimeBasedOutputs);
                    _simulationIntervalBasedOutputs = ComputationLib.SupportFunctions.ConcatJaggedArray(_simulationIntervalBasedOutputs, thisSimulationIntervalBasedOutputs);
                    // record the action combination
                    _pastActionCombinations = ComputationLib.SupportFunctions.ConcatJaggedArray(_pastActionCombinations, thisActionCombination);
                }
            }
            #endregion

            // collect observation period and calibration data
            #region collect observation period and calibration data
            if (_currentSimulationTimeIndex >= _nextTimeIndexToCollectObservationPeriodData)
            {
                _nextTimeIndexToCollectObservationPeriodData += _numOfDeltaTIndexInAnObservationPeriod;

                // collect observation period data
                #region collect observation period data

                thisObservedOutputs[0] = new double[_numOfMonitoredSimulationOutputs];
                thisResourceAvailability[0] = new double[_resources.Count];

                // report summation statistics for which surveillance is available
                foreach (SummationStatistics thisSumStat in _summationStatistics.Where(s => s.SurveillanceDataAvailable))
                {
                    switch (thisSumStat.Type)
                    {
                        case APACE_lib.SummationStatistics.enumType.Incidence:
                            {
                                if (thisSumStat.FirstObservationMarksTheStartOfTheSpread)
                                    thisObservedOutputs[0][colIndexObservableOutputs++] = thisSumStat.NewMembersOverPastObservableObsPeriod;
                            }
                            break;
                        case APACE_lib.SummationStatistics.enumType.AccumulatingIncident:
                            {
                                thisObservedOutputs[0][colIndexObservableOutputs++] = thisSumStat.ObservedAccumulatedNewMembers;
                            }
                            break;
                        case APACE_lib.SummationStatistics.enumType.Prevalence:
                            {
                                thisObservedOutputs[0][colIndexObservableOutputs++] = thisSumStat.CurrentMembers;
                            }
                            break;
                    }
                }
                // report ratio statistics for which surveillance is available
                foreach (RatioStatistics thisRatioStat in _ratioStatistics.Where(r => r.SurveillanceDataAvailable))
                {
                    //switch (thisRatioStat.Type)
                    //{
                    //    case APACE_lib.RatioStatistics.enumType.IncidenceOverIncidence:
                    //        {
                    //            nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                    //            denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                    //        }
                    //        break;
                    //    case APACE_lib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence:
                    //        {
                    //            nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).ObservedAccumulatedNewMembers;
                    //            denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).ObservedAccumulatedNewMembers;
                    //        }
                    //        break;
                    //    case APACE_lib.RatioStatistics.enumType.PrevalenceOverPrevalence:
                    //        {
                    //            nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).CurrentMembers;
                    //            denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).CurrentMembers;
                    //        }
                    //        break;
                    //    case APACE_lib.RatioStatistics.enumType.IncidenceOverPrevalence:
                    //        {
                    //            nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                    //            denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).CurrentMembers;
                    //        }
                    //        break;
                    //}
                    //if (denominatorValue == 0)
                    //    ratio = -1;
                    //else
                    //    ratio = nominatorValue / denominatorValue;
                    //thisRatioStat.CurrentValue = ratio;
                    thisObservedOutputs[0][colIndexObservableOutputs++] = thisRatioStat.CurrentValue;
                }

                // epidemic observation
                _simulationObservableOutputs
                    = ComputationLib.SupportFunctions.ConcatJaggedArray(_simulationObservableOutputs, thisObservedOutputs);  
                #endregion

                // collect observation times
                #region collect observation times
                thisTimeOfObservableOutputs[0] = new double[1];
                // check if an observation is made
                switch (_markOfEpidemicStartTime)
                {
                    case enumMarkOfEpidemicStartTime.TimeZero:
                        {
                            thisTimeOfObservableOutputs[0][0] = Math.Floor(_currentSimulationTimeIndex * _deltaT / _observationPeriodLengh); 
                        }
                        break;
                    case enumMarkOfEpidemicStartTime.TimeOfFirstObservation:
                        {
                            if (_firstObservationObtained == false && Math.Abs(thisObservedOutputs[0].Sum()) > 0)
                            {
                                _firstObservationObtained = true;
                                _timeIndexOfTheFirstObservation = _currentSimulationTimeIndex;
                                UpdateCurrentEpidemicTimeIndex();
                            }
                            // time of epidemic observation
                            if (_firstObservationObtained)
                                thisTimeOfObservableOutputs[0][0] = Math.Floor(_currentSimulationTimeIndex * _deltaT / _observationPeriodLengh); //this.CurrentEpidemicTime;
                            else
                                thisTimeOfObservableOutputs[0][0] = -1;
                        }
                        break;
                }
                // time of observation
                _timesOfEpidemicObservationsOverPastObservationPeriods
                    = ComputationLib.SupportFunctions.ConcatJaggedArray(_timesOfEpidemicObservationsOverPastObservationPeriods, thisTimeOfObservableOutputs);
                #endregion

                // resources
                #region resources
                // available resources
                foreach (Resource thisResource in _resources)
                    if (thisResource.ShowAvailability)
                        thisResourceAvailability[0][thisResource.ID] = thisResource.CurrentUnitsAvailable;
                // resource availability
                _simulationResourceAvailabilityOutput =
                    ComputationLib.SupportFunctions.ConcatJaggedArray(_simulationResourceAvailabilityOutput, thisResourceAvailability);
                #endregion      

                // collect calibration data
                #region collect calibration data
                if (_modelUse == enumModelUse.Calibration && _currentEpidemicTimeIndex > _warmUpPeriodIndex)
                {
                    thisCalibrationObservation[0] = new double[_numOfCalibratoinTargets];
                    // go over summation statistics that are included in calibration
                    foreach (SummationStatistics thisSumStat in _summationStatistics.Where(s => s.IfIncludedInCalibration))
                    {
                        // find this summation stat type:
                        switch (thisSumStat.Type)
                        {
                            case APACE_lib.SummationStatistics.enumType.Incidence:
                                {
                                    // check if within feasible range
                                    if (thisSumStat.IfCheckWithinFeasibleRange)
                                    {
                                        if (thisSumStat.NewMembersOverPastObservableObsPeriod < thisSumStat.FeasibleRange_min ||
                                            thisSumStat.NewMembersOverPastObservableObsPeriod > thisSumStat.FeasibleRange_max)
                                        {
                                            ifThisIsAFeasibleTrajectory = false;
                                            return;
                                        }
                                    }
                                    // find the observation
                                    thisCalibrationObservation[0][colIndexCalibrationObservation++] = thisSumStat.NewMembersOverPastObservableObsPeriod;
                                }
                                break;
                            case APACE_lib.SummationStatistics.enumType.AccumulatingIncident:
                                {
                                    // check if within feasible range
                                    if (endOfSimulation && thisSumStat.IfCheckWithinFeasibleRange)
                                    {
                                        if (thisSumStat.ObservedAccumulatedNewMembers < thisSumStat.FeasibleRange_min ||
                                            thisSumStat.ObservedAccumulatedNewMembers > thisSumStat.FeasibleRange_max)
                                        {
                                            ifThisIsAFeasibleTrajectory = false;
                                            return;
                                        }
                                    }
                                    // find the observation
                                    thisCalibrationObservation[0][colIndexCalibrationObservation++] = thisSumStat.ObservedAccumulatedNewMembers;
                                }
                                break;
                            case APACE_lib.SummationStatistics.enumType.Prevalence:
                                {
                                    // check if within feasible range
                                    if (thisSumStat.IfCheckWithinFeasibleRange)
                                    {
                                        if (thisSumStat.CurrentMembers < thisSumStat.FeasibleRange_min ||
                                            thisSumStat.CurrentMembers > thisSumStat.FeasibleRange_max)
                                        {
                                            ifThisIsAFeasibleTrajectory = false;
                                            return;
                                        }
                                    }
                                    // find the observation
                                    thisCalibrationObservation[0][colIndexCalibrationObservation++] = thisSumStat.CurrentMembers;
                                }
                                break;
                        }
                    }

                    // go over ratio statistics that are included in calibration                  
                    foreach (RatioStatistics thisRatioStat in _ratioStatistics.Where(r => r.IfIncludedInCalibration))
                    {
                        // find the type of this ratio statistics
                        switch (thisRatioStat.Type)
                        {
                            case APACE_lib.RatioStatistics.enumType.IncidenceOverIncidence:
                                {
                                    nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                                    denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                                }
                                break;
                            case APACE_lib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence:
                                {
                                    nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).ObservedAccumulatedNewMembers;
                                    denominatorValue = Math.Max(1, (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).ObservedAccumulatedNewMembers);
                                }
                                break;
                            case APACE_lib.RatioStatistics.enumType.PrevalenceOverPrevalence:
                                {
                                    nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).CurrentMembers;
                                    denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).CurrentMembers;
                                }
                                break;  
                            case APACE_lib.RatioStatistics.enumType.IncidenceOverPrevalence:
                                {
                                    nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).NewMembersOverPastObservableObsPeriod;
                                    denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).CurrentMembers;
                                }
                                break;
                        }

                        // find the goodness of fit measure
                        ratio = nominatorValue / denominatorValue;

                        // check if within feasible range
                        if (thisRatioStat.IfCheckWithinFeasibleRange && !double.IsNaN(ratio))
                        {
                            if (ratio < thisRatioStat.FeasibleRange_min ||
                                ratio > thisRatioStat.FeasibleRange_max)
                            {
                                ifThisIsAFeasibleTrajectory = false;
                                return;
                            }
                        }

                        // find the observation
                        thisCalibrationObservation[0][colIndexCalibrationObservation++] = ratio;                        
                    }

                    // calibration observation
                    _calibrationObservation = 
                        ComputationLib.SupportFunctions.ConcatJaggedArray(_calibrationObservation, thisCalibrationObservation);
                }
                #endregion

                // reset new member observations gather during past observation period
                foreach (Class thisClass in _classes)
                    thisClass.ResetNewMembersOverPastObsPeriod();
                foreach (SummationStatistics thisSumStat in _summationStatistics)
                    thisSumStat.ResetNewMembersOverPastObsPeriod();                
            }
            #endregion
        }
        // set class number of new members
        private void ResetClassNumberOfNewMembers()
        {           
            foreach (Class thisClass in _classes)
                thisClass.NumberOfNewMembersOverPastDeltaT = 0;
        }
        // transfer class members        
        private void TransferClassMembers()
        {
            //// update transmission rates
            //if(_thereAreTimeDependentParameters_affectingTranmissionDynamics)
            //    UpdateTransmissionRates();

            // do the transfer on all members
            foreach (Class thisClass in _classes.Where(c => c.CurrentNumberOfMembers>0))
                thisClass.IfNeedsToBeProcessed = true;

            bool thereAreClassesToBeProcessed= true;
            while (thereAreClassesToBeProcessed)
            {
                // if members are waiting
                foreach (Class thisClass in _classes.Where(c => c.IfNeedsToBeProcessed))
                {                    
                    // calculate the number of members to be sent out from each class
                    thisClass.SendOutMembers(_deltaT, _rng);
                    // update the array of available resources
                    if (_arrAvailableResources.Length > 0)
                        UpdateResourceAvailabilityBasedOnConsumptionOfThisClass((thisClass).ArrResourcesConsumed);
                    // all departing members are processed
                    thisClass.IfNeedsToBeProcessed = false;
                }

                // receive members
                foreach (Class thisSendingOutClass in _classes.Where(c => c.IfMembersWaitingToSendOutBeforeNextDeltaT))
                {
                    for (int j = 0; j < thisSendingOutClass.ArrDestinationClasseIDs.Length; j++)
                        _classes[thisSendingOutClass.ArrDestinationClasseIDs[j]].AddNewMembers(thisSendingOutClass.ArrNumOfMembersSendingToEachDestinationClasses[j]);
                    // reset number of members sending out to each destination class
                    thisSendingOutClass.ResetNumOfMembersSendingToEachDestinationClasses();
                }

                // check if there are members waiting to be sent out
                thereAreClassesToBeProcessed = false;
                for (int i = _numOfClasses - 1; i >= 0; i--)
                    if (_classes[i].IfNeedsToBeProcessed)
                    {
                        thereAreClassesToBeProcessed = true;
                        break;
                    }
            } // end of while (membersWaitingToBeTransferred)

            foreach (Class thisClass in _classes)
                _arrNumOfMembersInEachClass[thisClass.ID] = thisClass.CurrentNumberOfMembers;

            // update and gather statistics defined for processes                         
            long[] arrNumOfNewMembersOutOfEventsOverPastDeltaT = new long[_processes.Count];
            foreach (Class thisClass in _classes)
            {
                if (_modelUse != enumModelUse.Calibration)
                {
                    thisClass.UpdateStatisticsAtTheEndOfDeltaT(_currentEpidemicTimeIndex * _deltaT, _deltaT);
                    _currentPeriodCost += thisClass.CurrentCost();
                    _currentPeriodQALY += thisClass.CurrentQALY();
                }
                // find number of members out of active processes for this class
                thisClass.ReturnAndResetNumOfMembersOutOfProcessesOverPastDeltaT(ref arrNumOfNewMembersOutOfEventsOverPastDeltaT);
            }

            foreach (SummationStatistics thisSumStat in _summationStatistics)
            {
                switch (thisSumStat.DefinedOn)
                {
                    case APACE_lib.SummationStatistics.enumDefinedOn.Classes:
                        {
                            switch (thisSumStat.Type)
                            {
                                case APACE_lib.SummationStatistics.enumType.Incidence:
                                    {
                                        thisSumStat.AddNewMembers(ref _classes, _deltaT);
                                        _currentPeriodCost += thisSumStat.CurrentCost;
                                        _currentPeriodQALY += thisSumStat.CurrentQALY;
                                    }
                                    break;
                                case APACE_lib.SummationStatistics.enumType.Prevalence:
                                    {
                                        thisSumStat.AddCurrentMembers(_currentEpidemicTimeIndex * _deltaT, _arrNumOfMembersInEachClass, _modelUse != enumModelUse.Calibration);
                                    }
                                    break;
                            }
                        }
                        break;
                    case APACE_lib.SummationStatistics.enumDefinedOn.Events:
                        {
                            switch (thisSumStat.Type)
                            {
                                case APACE_lib.SummationStatistics.enumType.Incidence:
                                    {
                                        thisSumStat.AddNewMembers(arrNumOfNewMembersOutOfEventsOverPastDeltaT, _deltaT);
                                        _currentPeriodCost += thisSumStat.CurrentCost;
                                        _currentPeriodQALY += thisSumStat.CurrentQALY;
                                    }
                                    break;
                                case APACE_lib.SummationStatistics.enumType.Prevalence:
                                    {
                                        // error
                                    }
                                    break;
                            }
                        }
                        break;
                }                
            }
            // gather outcome statistics
            if (_currentEpidemicTimeIndex >= _warmUpPeriodIndex)
            {
                // update decision costs
                _currentPeriodCost += _POMDP_ADP.CurrentCostPerUnitOfTime * _deltaT;
                _currentPeriodCost += _POMDP_ADP.CurrentFixedAnPenaltyCost;

                int numOfDiscountPeriods = Math.Max(0, (int)_currentEpidemicTimeIndex / _numOfDeltaTsInADecisionInterval);
                double coeff = Math.Pow(_discountRate, numOfDiscountPeriods);
                _totalCost += coeff * _currentPeriodCost;
                _totalQALY += coeff * _currentPeriodQALY;
            }

        }
        // reset statistics if warm-up period has ended
        private void ResetStatisticsIfWarmUpPeriodHasEnded()
        {
            if (_currentEpidemicTimeIndex >= _warmUpPeriodIndex && _ifWarmUpPeriodHasEnded == false)
            {
                _ifWarmUpPeriodHasEnded = true;
                // reset statistics
                ResetStatistics(false);
            }           
        }
        // update the cost of current ADP state-decision
        private void UpdateRewardOfCurrentADPStateDecision()
        {
            // add reward
            int numOfADPStates = _POMDP_ADP.NumberOfADPStates(_adpSimItr);
            if (numOfADPStates > 0)
                _POMDP_ADP.AddToDecisionIntervalReward(_adpSimItr, numOfADPStates - 1, CurrentDeltaTReward());
            //((ADP_State)_POMDP_ADP.ADPStates[numOfADPStates - 1]).AddToDecisionIntervalReward();
        }
        // return annual cost
        private void GatherEndOfSimulationStatistics()
        {
            if (_decisionPeriodIndex == 0)
                _annualCost = _totalCost;
            else if (_annualInterestRate == 0)
                _annualCost = (364 / _decisionIntervalLength) * _totalCost / _decisionPeriodIndex;
            else
                _annualCost = _totalCost * _annualInterestRate / (1 - Math.Pow(1 + _annualInterestRate, -_decisionPeriodIndex));

            // gather end of simulation statistics in each ratio statistics
            foreach (RatioStatistics thisRatioStat in _ratioStatistics)
            {
                switch (thisRatioStat.Type)
                {                    
                    case APACE_lib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence:
                        {
                            long nominatorValue = (_summationStatistics[thisRatioStat.NominatorSpecialStatID]).AccumulatedNewMembers;
                            long denominatorValue = (_summationStatistics[thisRatioStat.DenominatorSpecialStatID]).AccumulatedNewMembers;
                            thisRatioStat.Record(nominatorValue, denominatorValue);
                        }
                        break;
                }
            }  
        }
        // return accumulated reward
        private double AccumulatedReward()
        {
            double reward = 0;
            switch (_objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNMB:
                    reward = _wtpForHealth * _totalQALY - _totalCost;
                    break;
                case enumObjectiveFunction.MaximizeNHB:
                    reward = _totalQALY - _totalCost / _wtpForHealth;
                    break;
            }
            return reward;
        }
        // reward of this delta t period
        private double CurrentDeltaTReward()
        {
            double reward = 0;
            switch (_objectiveFunction)
            {
                case enumObjectiveFunction.MaximizeNMB:
                    reward = _wtpForHealth * _currentPeriodQALY - _currentPeriodCost;
                    break;
                case enumObjectiveFunction.MaximizeNHB:
                    reward = _currentPeriodQALY - _currentPeriodCost / _wtpForHealth;
                    break;
            }
            return reward;
        }
        // update transmission rates
        private void UpdateTransmissionRates_v1()
        {
            double rate = 0;

            // find the population size   
            _populationSizeOfMixingGroups = new long[_baseContactMatrices[0].GetLength(0)];
            foreach (Class thisClass in _classes)
            {
                _arrNumOfMembersInEachClass[thisClass.ID] = thisClass.CurrentNumberOfMembers;
                _populationSizeOfMixingGroups[thisClass.RowIndexInContactMatrix] += _arrNumOfMembersInEachClass[thisClass.ID];
            }

            long[] populationSizeOfTheMixingGroupThisClassBelongTo = new long[_numOfClasses];
            foreach (Class thisClass in _classes)
                populationSizeOfTheMixingGroupThisClassBelongTo[thisClass.ID] = _populationSizeOfMixingGroups[thisClass.RowIndexInContactMatrix];

            // find the index of current action in the transmission matrix
            int indexOfIntCombInTransmissionMatrix = FindIndexOfInterventionCombimbinationInTransmissionMatrix(_currentInterventionCombinationInEffect);

            // find the transmission rates of each pathogen for each class 
            double[][] arrTranmissionRatesByClassAndPathogen = new double[_numOfClasses][];
            foreach (Class thisClass in _classes)
            {
                if (thisClass.Type == Class.enumType.Normal && thisClass.CurrentNumberOfMembers > 0)
                {
                    int receivingClassID = thisClass.ID;
                    // build the array of transmission rates for this class 
                    arrTranmissionRatesByClassAndPathogen[receivingClassID] = new double[_numOfPathogens];
                    for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                    {
                        // calculate the transmission rate for this class and pathogen
                        rate = 0;
                        for (int i = 0; i < _arrNumOfMembersInEachClass.Length; ++i)
                            rate += _tranmissionMatrices[indexOfIntCombInTransmissionMatrix][pathogenID][receivingClassID][i]
                                * _arrNumOfMembersInEachClass[i] / populationSizeOfTheMixingGroupThisClassBelongTo[i];
                        //rate = (double)rate / _populationSize;
                        arrTranmissionRatesByClassAndPathogen[receivingClassID][pathogenID] = rate;
                    }
                }
            }
            // update the proportion of members in each class
            foreach (Class thisClass in _classes.Where(c => c.CurrentNumberOfMembers > 0))
                thisClass.UpdateTransmissionRates(arrTranmissionRatesByClassAndPathogen[thisClass.ID]);
        }


        // check if stopping condition is satisfied
        private bool IsEradicationConditionsSatisfied()
        {
            bool eradicated = true;            
            foreach (Class thisClass in _classes)
            {
                // if a class should be empty while it is not then return false
                if (thisClass.EmptyToEradicate == true && thisClass.CurrentNumberOfMembers > 0)
                {
                    eradicated = false;
                    break;
                }
            }                
            _stoppedDueToEradication = eradicated;
            return eradicated;
        }
        // reset for another simulation
        private void ResetForAnotherSimulation(int threadSpecificSeedNumber)
        {
            ResetForAnotherSimulation(threadSpecificSeedNumber, true, new double[0]);
        }        
        private void ResetForAnotherSimulation(int seed, bool sampleParameters, double[] parameterValues)
        {
            // reset the rnd object
            _rng = new RNG(seed);

            // sample from parameters
            _arrSampledParameterValues = new double[_parameters.Count];
            if (sampleParameters == true)
                SampleFromParameters(0, false);
            else
                _arrSampledParameterValues = parameterValues;

            // reset time
            _ifWarmUpPeriodHasEnded = false;
            _timeIndexOfTheFirstObservation = 0;
            _firstObservationObtained = false;
            _currentSimulationTimeIndex = 0;
            // epidemic start time
            UpdateCurrentEpidemicTimeIndex();   

            _decisionPeriodIndex = 0;
            _nextDecisionPointIndex = _epidemicTimeIndexToStartDecisionMaking;
            _nextDecisionCycleIndex = _epidemicTimeIndexToStartDecisionMaking;
            _nextTimeIndexToCollectSimulationOutputData = _currentSimulationTimeIndex; 
            _nextTimeIndexToCollectObservationPeriodData = _nextTimeIndexToCollectSimulationOutputData;

            // reset outcome
            _totalCost = 0;
            _totalQALY = 0;
            _annualCost = 0;
            _numOfSwitchesBtwDecisions = 0; 

            // update resource availability
            foreach (Resource thisResource in _resources)
            {
                switch (thisResource.ReplenishmentType)
                {
                    case Resource.enumReplenishmentType.OneTime:
                        thisResource.UpdateAvailabilityScheme(
                            _arrSampledParameterValues[thisResource.ParID_firstTimeAvailable],
                            (long)_arrSampledParameterValues[thisResource.ParID_replenishmentQuantity]);
                        break;
                    case Resource.enumReplenishmentType.Periodic:
                        thisResource.UpdateAvailabilityScheme(
                            _arrSampledParameterValues[thisResource.ParID_firstTimeAvailable],
                            (long)_arrSampledParameterValues[thisResource.ParID_replenishmentQuantity],
                            _arrSampledParameterValues[thisResource.ParID_replenishmentInterval]);
                        break;
                }
            }
            // reset resources
            _arrAvailableResources = new long[_resources.Count];
            if (_resources.Count > 0)
            {                
                foreach (Resource thisResource in _resources)
                    // get a new sample
                    thisResource.ResetForANewSimulationIteration();

                // update the available resources to other classes
                UpdateResourceAvailabilityInformationForEachClass(_arrAvailableResources);
            }

            // update intervention information
            _onOffStatusOfInterventionsAffectingContactPattern = new int[_numOfInterventionsAffectingContactPattern];
            _nextEpidemicTimeIndexWhenAnInterventionEffectChanges = long.MaxValue;
            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
                thisIntervention.UpdateDelay((int)(_arrSampledParameterValues[thisIntervention.ParIDDelayToGoIntoEffectOnceTurnedOn] / _deltaT));

            // reset the number of people in each compartment
            _arrNumOfMembersInEachClass = new long[_numOfClasses];

            // reset decisions
            _POMDP_ADP.ResetForAnotherSimulationRun();//(ref _totalCost);
            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
                thisIntervention.ResetForAnotherSimulationRun();

            // calculate contact matrices
            CalculateContactMatrices();
            // announce the initial decision 
            AnnounceDecision(_POMDP_ADP.DefaultActionCombination, false);
            // make these decisions effective
            ImplementDecisionsThatCanGoIntoEffect(true);
            // update susceptibility and infectivity of classes
            UpdateSusceptilityAndInfectivityOfClasses(true, true);

            // update rates associated with each class and their initial size
            foreach (Class thisClass in _classes)
            {
                thisClass.UpdateInitialNumberOfMembers((long)Math.Round(_arrSampledParameterValues[thisClass.InitialMemebersParID]));
                thisClass.UpdateRatesOfBirthAndEpidemicIndependentProcesses(_arrSampledParameterValues);
                thisClass.UpdateProbOfSuccess(_arrSampledParameterValues);          
            }

            // if at the beginning of simulation 
            //if (ifToInitializeSimulatoin)
            UpdateTransmissionRates();

            // reset statistics
            ResetStatistics(true);
            
            // reset features
            _arrCurrentValuesOfFeatures = new double[_features.Count];
            
            // reset the jagged array containing trajectories
            _pastActionCombinations = new int[0][];
            _simulationTimeBasedOutputs = new double[0][];
            _simulationIntervalBasedOutputs = new double[0][]; 
            _simulationObservableOutputs = new double[0][];
            _calibrationObservation = new double[0][];
            _simulationResourceAvailabilityOutput = new double[0][];
            _timesOfEpidemicObservationsOverPastObservationPeriods = new double[0][];
        }

        // reset statistics
        private void ResetStatistics(bool ifToResetForAnotherSimulationRun)
        {
            _numOfSwitchesBtwDecisions = 0;

            // reset class statistics
            foreach (Class thisClass in _classes)
                thisClass.ResetStatistics(_warmUpPeriodIndex * _deltaT, ifToResetForAnotherSimulationRun);
            // reset summation statistics
            foreach (SummationStatistics thisSumStat in _summationStatistics)
                thisSumStat.ResetStatistics(WarmUpPeriodIndex * _deltaT, ifToResetForAnotherSimulationRun);
            // reset ratio statistics
            foreach (RatioStatistics thisRatioStat in _ratioStatistics)
                thisRatioStat.ResetForAnotherSimulationRun();

            // reset summation statistics
            foreach (SummationStatistics thisSumStat in _summationStatistics)
            {
                switch (thisSumStat.Type)
                {
                    case APACE_lib.SummationStatistics.enumType.Incidence:
                        {
                            // do nothing
                        }
                        break;
                    case APACE_lib.SummationStatistics.enumType.Prevalence:
                        {
                            thisSumStat.AddCurrentMembers(_currentEpidemicTimeIndex * _deltaT, _arrNumOfMembersInEachClass, _modelUse != enumModelUse.Calibration);
                        }
                        break;
                }
            }  
        }
        // Sample this parameter
        private void SampleThisParameter(Parameter thisPar, double time)
        {
            switch (thisPar.Type)
            {
                // independent parameter
                case Parameter.EnumType.Independet:
                    {
                        _arrSampledParameterValues[thisPar.ID] = ((IndependetParameter)thisPar).Sample(_rng);
                    }
                    break;
                
                // correlated parameter
                case Parameter.EnumType.Correlated:
                    {
                        CorrelatedParameter thisCorrelatedParameter = thisPar as CorrelatedParameter;
                        int parameterIDCorrelatedTo = thisCorrelatedParameter.IDOfDepedentPar;
                        double valueOfTheParameterIDCorrelatedTo = _arrSampledParameterValues[parameterIDCorrelatedTo];
                        _arrSampledParameterValues[thisPar.ID] = thisCorrelatedParameter.Sample(valueOfTheParameterIDCorrelatedTo);
                    }
                    break;
                
                // multiplicative parameter
                case Parameter.EnumType.Multiplicative:
                    {
                        MultiplicativeParameter thisMultiplicativeParameter = thisPar as MultiplicativeParameter;
                        int firstParID = thisMultiplicativeParameter.FirstParameterID;
                        int secondParID = thisMultiplicativeParameter.SecondParameterID;
                        _arrSampledParameterValues[thisPar.ID] = thisMultiplicativeParameter.Sample(_arrSampledParameterValues[firstParID], _arrSampledParameterValues[secondParID]);
                    }
                    break;

                // linear combination parameter
                case  Parameter.EnumType.LinearCombination:
                    {
                        LinearCombination thisLinearCombinationPar = thisPar as LinearCombination;
                        int[] arrParIDs = thisLinearCombinationPar.arrParIDs;
                        double[] arrValueOfParameters = new double[arrParIDs.Length];

                        for (int i = 0; i < arrParIDs.Length; i++)
                            arrValueOfParameters[i] = _arrSampledParameterValues[arrParIDs[i]];

                        _arrSampledParameterValues[thisPar.ID] = thisLinearCombinationPar.Sample(arrValueOfParameters);
                    }
                    break;   

                // multiple combination parameter
                case Parameter.EnumType.MultipleCombination:
                    {
                        MultipleCombination thisMultipleCombinationPar = thisPar as MultipleCombination;
                        int[] arrParIDs = thisMultipleCombinationPar.arrParIDs;
                        double[] arrValueOfParameters = new double[arrParIDs.Length];
                        for (int i = 0; i < arrParIDs.Length; i++)
                            arrValueOfParameters[i] = _arrSampledParameterValues[arrParIDs[i]];

                        _arrSampledParameterValues[thisPar.ID] = thisMultipleCombinationPar.Sample(arrValueOfParameters);
                    }
                    break;
                          
                // time dependent linear parameter
                case Parameter.EnumType.TimeDependetLinear:
                    {
                        TimeDependetLinear thisTimeDepedentLinearPar = thisPar as TimeDependetLinear;
                        double intercept = _arrSampledParameterValues[thisTimeDepedentLinearPar.InterceptParID];
                        double slope = _arrSampledParameterValues[thisTimeDepedentLinearPar.SlopeParID];
                        double timeOn = thisTimeDepedentLinearPar.TimeOn;
                        double timeOff = thisTimeDepedentLinearPar.TimeOff;
                        
                        _arrSampledParameterValues[thisPar.ID] = thisTimeDepedentLinearPar.Sample(time, intercept, slope, timeOn, timeOff);
                    }
                    break;   
   
                // time dependent oscillating parameter
                case Parameter.EnumType.TimeDependetOscillating:
                    {
                        TimeDependetOscillating thisTimeDepedentOscillatingPar = thisPar as TimeDependetOscillating;
                        double a0 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a0ParID];
                        double a1 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a1ParID];
                        double a2 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a2ParID];
                        double a3 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a3ParID];
                        
                        _arrSampledParameterValues[thisPar.ID] = thisTimeDepedentOscillatingPar.Sample(time, a0, a1, a2, a3);
                    }
                    break;  
            }
        }
        // sample from parameters
        private void SampleFromParameters(double time, bool updateOnlyTimeDependent)
        {
            if (updateOnlyTimeDependent == false)
            {
                foreach (Parameter thisParameter in _parameters)
                    SampleThisParameter(thisParameter, time);
            }
            else
            {
                foreach (Parameter thisParameter in _parameters.Where(p => p.ShouldBeUpdatedByTime))
                    SampleThisParameter(thisParameter, time);
                //{ 
                    //if (thisParameter.ShouldBeUpdatedByTime)
                        
                //}
            }
            
            //// first sample independent parameters
            //bool parameterFound = false;
            //foreach (Parameter thisParameter in _parameters)
            //{
            //    parameterFound = false;

            //    // if independent parameter
            //    IndependetParameter thisIndependetParameter = thisParameter as IndependetParameter;
            //    if (thisIndependetParameter != null)
            //    {
            //        parameterFound = true;
            //        _arrSampledParameterValues[thisParameter.ID] = ((IndependetParameter)thisParameter).Sample(_rng);
            //    }

            //    if (!parameterFound)
            //    {
            //        // if correlated parameter
            //        CorrelatedParameter thisCorrelatedParameter = thisParameter as CorrelatedParameter;
            //        if (thisCorrelatedParameter != null)
            //        {
            //            parameterFound = true;
            //            int parameterIDCorrelatedTo = thisCorrelatedParameter.ParameterIDcorrelctedTo;
            //            double valueOfTheParameterIDCorrelatedTo = _arrSampledParameterValues[parameterIDCorrelatedTo];
            //            _arrSampledParameterValues[thisParameter.ID] = thisCorrelatedParameter.Sample(valueOfTheParameterIDCorrelatedTo);
            //        }

            //        if (!parameterFound)
            //        {
            //            // if multiplicative parameter
            //            MultiplicativeParameter thisMultiplicativeParameter = thisParameter as MultiplicativeParameter;
            //            if (thisMultiplicativeParameter != null)
            //            {
            //                parameterFound = true;
            //                int firstParID = thisMultiplicativeParameter.FirstParameterID;
            //                int secondParID = thisMultiplicativeParameter.SecondParameterID;
            //                _arrSampledParameterValues[thisParameter.ID] = thisMultiplicativeParameter.Sample(_arrSampledParameterValues[firstParID], _arrSampledParameterValues[secondParID]);
            //            }

            //            if (!parameterFound)
            //            {
            //                // if linear combination parameter
            //                LinearCombination thisLinearCombinationPar = thisParameter as LinearCombination;
            //                if (thisLinearCombinationPar != null)
            //                {
            //                    parameterFound = true;
            //                    int[] arrParIDs = thisLinearCombinationPar.arrParIDs;
            //                    double[] arrValueOfParameters = new double[arrParIDs.Length];

            //                    for (int i = 0; i < arrParIDs.Length; i++)
            //                        arrValueOfParameters[i] = _arrSampledParameterValues[arrParIDs[i]];

            //                    _arrSampledParameterValues[thisParameter.ID] = thisLinearCombinationPar.Sample(arrValueOfParameters);
            //                }

            //                if (!parameterFound)
            //                {
            //                    // if multiple combination parameter
            //                    MultipleCombination thisMultipleCombinationPar = thisParameter as MultipleCombination;
            //                    if (thisMultipleCombinationPar != null)
            //                    {
            //                        parameterFound = true;
            //                        int[] arrParIDs = thisMultipleCombinationPar.arrParIDs;
            //                        double[] arrValueOfParameters = new double[arrParIDs.Length];

            //                        for (int i = 0; i < arrParIDs.Length; i++)
            //                            arrValueOfParameters[i] = _arrSampledParameterValues[arrParIDs[i]];

            //                        _arrSampledParameterValues[thisParameter.ID] = thisMultipleCombinationPar.Sample(arrValueOfParameters);
            //                    }

            //                    if (!parameterFound)
            //                    {
            //                        // if time dependent linear parameter
            //                        TimeDependetLinear thisTimeDepedentLinearPar = thisParameter as TimeDependetLinear;
            //                        if (thisTimeDepedentLinearPar != null)
            //                        {
            //                            parameterFound = true;
            //                            double intercept = _arrSampledParameterValues[thisTimeDepedentLinearPar.InterceptParID];
            //                            double slope = _arrSampledParameterValues[thisTimeDepedentLinearPar.SlopeParID];
            //                            double timeOn = thisTimeDepedentLinearPar.TimeOn;
            //                            double timeOff = thisTimeDepedentLinearPar.TimeOff;

            //                            _arrSampledParameterValues[thisParameter.ID] = thisTimeDepedentLinearPar.Sample(time, intercept, slope, timeOn, timeOff);
            //                        }

            //                        if (!parameterFound)
            //                        {
            //                            // if time dependent oscillating parameter
            //                            TimeDependetOscillating thisTimeDepedentOscillatingPar = thisParameter as TimeDependetOscillating;
            //                            if (thisTimeDepedentOscillatingPar != null)
            //                            {
            //                                double a0 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a0ParID];
            //                                double a1 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a1ParID];
            //                                double a2 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a2ParID];
            //                                double a3 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a3ParID];

            //                                _arrSampledParameterValues[thisParameter.ID] = thisTimeDepedentOscillatingPar.Sample(time, a0, a1, a2, a3);
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}  
            ////// then sample dependent parameter                       
            ////UpdateDependentParameterValues();
        }
        //// update the value of parameters that are functions of other parameters
        //private void UpdateDependentParameterValues()
        //{
        //    bool parameterFound = false;
        //    foreach (Parameter thisParameter in _parameters)
        //    {
        //        parameterFound = false;
        //        CorrelatedParameter thisCorrelatedParameterPar = thisParameter as CorrelatedParameter;
        //        if (thisCorrelatedParameterPar != null)
        //        {
        //            parameterFound = true;
        //            int parameterIDCorrelatedTo = thisCorrelatedParameterPar.ParameterIDcorrelctedTo;
        //            double valueOfTheParameterIDCorrelatedTo = _arrSampledParameterValues[parameterIDCorrelatedTo];
        //            _arrSampledParameterValues[thisParameter.ID] = thisCorrelatedParameterPar.Sample(valueOfTheParameterIDCorrelatedTo);
        //        }

        //        if (!parameterFound)
        //        {
        //            MultiplicativeParameter thisMultiplicativeParameterPar = thisParameter as MultiplicativeParameter;
        //            if (thisMultiplicativeParameterPar != null)
        //            {
        //                parameterFound = true;
        //                int firstParID = thisMultiplicativeParameterPar.FirstParameterID;
        //                int secondParID = thisMultiplicativeParameterPar.SecondParameterID;
        //                _arrSampledParameterValues[thisParameter.ID] = thisMultiplicativeParameterPar.Sample(_arrSampledParameterValues[firstParID], _arrSampledParameterValues[secondParID]);
        //            }

        //            if (!parameterFound)
        //            {
        //                LinearCombination thisLinearCombinationPar = thisParameter as LinearCombination;
        //                if (thisLinearCombinationPar != null)
        //                {
        //                    parameterFound = true;
        //                    int[] arrParIDs = thisLinearCombinationPar.arrParIDs;
        //                    double[] arrValueOfParameters = new double[arrParIDs.Length];

        //                    for (int i = 0; i < arrParIDs.Length; i++)
        //                        arrValueOfParameters[i] = _arrSampledParameterValues[arrParIDs[i]];

        //                    _arrSampledParameterValues[thisParameter.ID] = thisLinearCombinationPar.Sample(arrValueOfParameters);
        //                }

        //                if (!parameterFound)
        //                {
        //                    MultipleCombination thisMultipleCombinationPar = thisParameter as MultipleCombination;
        //                    if (thisMultipleCombinationPar != null)
        //                    {
        //                        parameterFound = true;
        //                        int[] arrParIDs = thisMultipleCombinationPar.arrParIDs;
        //                        double[] arrValueOfParameters = new double[arrParIDs.Length];

        //                        for (int i = 0; i < arrParIDs.Length; i++)
        //                            arrValueOfParameters[i] = _arrSampledParameterValues[arrParIDs[i]];

        //                        _arrSampledParameterValues[thisParameter.ID] = thisMultipleCombinationPar.Sample(arrValueOfParameters);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        // update the effect of change in time dependent parameters
        private void UpdateTheEffectOfChangeInTimeDependentParameterValues(double time)
        {
            if (_thereAreTimeDependentParameters)
            {
                // sample parameters
                SampleFromParameters(time, true);
                // update time dependent parameters in the model
                UpdateTimeDepedentParametersInTheModel();
            }

            //if (_thereAreTimeDependentParameters)
            //{
            //    bool parameterFound = false;
            //    // update time dependent parameter values
            //    foreach (Parameter thisParameter in _parameters)
            //    {
            //        parameterFound = false;
            //        TimeDependetLinear thisTimeDepedentLinearPar = thisParameter as TimeDependetLinear;
            //        if (thisTimeDepedentLinearPar != null)
            //        {
            //            parameterFound = true;
            //            double intercept = _arrSampledParameterValues[thisTimeDepedentLinearPar.InterceptParID];
            //            double slope = _arrSampledParameterValues[thisTimeDepedentLinearPar.SlopeParID];
            //            double timeOn = thisTimeDepedentLinearPar.TimeOn;
            //            double timeOff = thisTimeDepedentLinearPar.TimeOff;

            //            _arrSampledParameterValues[thisParameter.ID] = thisTimeDepedentLinearPar.Sample(time, intercept, slope, timeOn, timeOff);
            //        }

            //        if (!parameterFound)
            //        {
            //            TimeDependetOscillating thisTimeDepedentOscillatingPar = thisParameter as TimeDependetOscillating;
            //            if (thisTimeDepedentOscillatingPar != null)
            //            {
            //                double a0 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a0ParID];
            //                double a1 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a1ParID];
            //                double a2 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a2ParID];
            //                double a3 = _arrSampledParameterValues[thisTimeDepedentOscillatingPar.a3ParID];

            //                _arrSampledParameterValues[thisParameter.ID] = thisTimeDepedentOscillatingPar.Sample(time, a0, a1, a2, a3);
            //            }
            //        }
            //    }
            //    // update dependent parameters
            //    UpdateDependentParameterValues();
            //    // update time dependent parameters in the model
            //    UpdateTimeDepedentParametersInTheModel();
            //}
        }
        // update time dependent parameters in the model
        private void UpdateTimeDepedentParametersInTheModel()
        {
            // update transmission dynamic matrix if necessary
            if (_thereAreTimeDependentParameters_affectingTranmissionDynamics)// || _thereAreTimeDependentParameters)
            {
                //CalculateTransmissionMatrix();
                // update transmission rates
                //UpdateTransmissionRates();
            }

            // update process rates if necessary
            if (_thereAreTimeDependentParameters_affectingNaturalHistoryRates)
            {
                // update rates associated with each class and their initial size
                foreach (Class thisClass in _classes)
                    thisClass.UpdateRatesOfBirthAndEpidemicIndependentProcesses(_arrSampledParameterValues);
            }

            // update value of splitting class parameters
            if (_thereAreTimeDependentParameters_affectingSplittingClasses)
            {
                // update the probability of success
                foreach (Class thisClass in _classes)
                    thisClass.UpdateProbOfSuccess(_arrSampledParameterValues);
            }
        }
        // calculate contract matrices
        private void CalculateContactMatrices() //[intervention ID][pathogen ID][group i, group j]
        {
            int contactMatrixSize = _baseContactMatrices[0].GetLength(0);
            int numOfCombinationsOfInterventionsAffectingContactPattern = (int)Math.Pow(2, _numOfInterventionsAffectingContactPattern);
            _contactMatrices = new double[numOfCombinationsOfInterventionsAffectingContactPattern][][,];

            // build the contact matrices
            for (int intCombIndex = 0; intCombIndex < numOfCombinationsOfInterventionsAffectingContactPattern; ++intCombIndex)
            {
                if (intCombIndex == 0)
                    _contactMatrices[intCombIndex] = _baseContactMatrices;
                else
                {
                    int[] onOfStatusOfInterventionsAffectingContactPattern = SupportFunctions.ConvertToBase2FromBase10(intCombIndex, _numOfInterventionsAffectingContactPattern);
                    for (int interventionIndex = 0; interventionIndex < _numOfInterventionsAffectingContactPattern; ++interventionIndex)
                    {
                        // initialize contact matrices
                        _contactMatrices[intCombIndex] = new double[_numOfPathogens][,];

                        if (onOfStatusOfInterventionsAffectingContactPattern[interventionIndex] == 1)
                        {
                            for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                            {
                                _contactMatrices[intCombIndex][pathogenID] = new double[contactMatrixSize, contactMatrixSize];
                                for (int i = 0; i < contactMatrixSize; ++i)
                                    for (int j = 0; j < contactMatrixSize; ++j)
                                        _contactMatrices[intCombIndex][pathogenID][i, j] = _baseContactMatrices[pathogenID][i, j] +
                                            _baseContactMatrices[pathogenID][i, j] * _arrSampledParameterValues[_percentChangeInContactMatricesParIDs[interventionIndex][pathogenID][i, j]];
                            }
                        }
                    }
                }
            }
        }
        // update susceptibility and infectivity of classes
        private void UpdateSusceptilityAndInfectivityOfClasses(bool updateSusceptibility, bool updateInfectivity)
        {
            if (updateSusceptibility && updateInfectivity)
            {
                foreach (Class thisClass in _classes)
                {
                    // susceptibility
                    if (thisClass.IsEpiDependentProcessActive)
                        thisClass.UpdateSusceptibilityParameterValues(_arrSampledParameterValues);
                    // infectivity
                    thisClass.UpdateInfectivityParameterValues(_arrSampledParameterValues);
                }
            }
            else if (updateSusceptibility)
            {
                // only susceptibility
                foreach (Class thisClass in _classes.Where(c => c.IsEpiDependentProcessActive))
                    thisClass.UpdateSusceptibilityParameterValues(_arrSampledParameterValues);
            }
            else if (updateInfectivity)
            {
                // only infectivity
                foreach (Class thisClass in _classes)
                    thisClass.UpdateInfectivityParameterValues(_arrSampledParameterValues);
            }
        }
        // update transmission rates
        private void UpdateTransmissionRates()
        {
            // update susceptibility and infectivity of classes
            UpdateSusceptilityAndInfectivityOfClasses(_thereAreTimeDependentParameters_affectingSusceptibilities, _thereAreTimeDependentParameters_affectingInfectivities);

            // find the population size of each mixing group
            _populationSizeOfMixingGroups = new long[_baseContactMatrices[0].GetLength(0)];
            foreach (Class thisClass in _classes)
            {
                //_arrNumOfMembersInEachClass[thisClass.ID] = thisClass.CurrentNumberOfMembers;
                _populationSizeOfMixingGroups[thisClass.RowIndexInContactMatrix] += thisClass.CurrentNumberOfMembers;
            }

            // find the index of current action in the contact matrices
            int indexOfIntCombInContactMatrices = FindIndexOfInterventionCombimbinationInTransmissionMatrix(_currentInterventionCombinationInEffect);

            // calculate the transmission rates for each class
            double susContactInf = 0, rate = 0, infectivity = 0;
            double[] arrTransmissionRatesByPathogen = new double[_numOfPathogens];
            foreach (Class thisRecievingClass in _classes.Where(c => c.IsEpiDependentProcessActive && c.CurrentNumberOfMembers > 0))
            {
                // calculate the transmission rate for each pathogen
                for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                {
                    rate = 0;
                    for (int j = 0; j < _numOfClasses; j++)
                    {
                        // find the infectivity of infection-causing class
                        if (_classes[j].Type == Class.enumType.Normal) 
                        {
                            infectivity = _classes[j].InfectivityValues[pathogenID];
                            if (infectivity > 0)
                            {
                                susContactInf = thisRecievingClass.SusceptibilityValues[pathogenID]
                                                * _contactMatrices[indexOfIntCombInContactMatrices][pathogenID][thisRecievingClass.RowIndexInContactMatrix, _classes[j].RowIndexInContactMatrix]
                                                * infectivity;

                                rate += susContactInf * _arrNumOfMembersInEachClass[j] / _populationSizeOfMixingGroups[_classes[j].RowIndexInContactMatrix];
                            }
                        }
                    }

                    // store the rate
                    //if (rate<0)
                    //    MessageBox.Show("Transmission rates cannot be negative (Seed: " + _rng.Seed + ").", "Error in Calculating Transmission Rates");

                    arrTransmissionRatesByPathogen[pathogenID] = rate;
                }

                // update the transition rates of this class for all pathogens
                thisRecievingClass.UpdateTransmissionRates(arrTransmissionRatesByPathogen);
            }
        }
        // calculate transmission matrix
        private void CalculateTransmissionMatrix()//(int[] nextPeriodActionCombinationInEffect)
        {
            int contactMatrixSize = _baseContactMatrices[0].GetLength(0);
            int numOfCombinationsOfInterventionsAffectingContactPattern = (int)Math.Pow(2, _numOfInterventionsAffectingContactPattern);

            double[][] arrInfectivity = new double[_numOfClasses][];            
            double[][] arrSusceptibility = new double[_numOfClasses][];
            int[] arrRowInContactMatrix = new int[_numOfClasses];
            double[][][,] contactMatrices = new double[numOfCombinationsOfInterventionsAffectingContactPattern][][,];
            _tranmissionMatrices = new double[numOfCombinationsOfInterventionsAffectingContactPattern][][][];

            // build the contact matrices
            // for all possible intervention combinations
            for (int intCombIndex = 0; intCombIndex < numOfCombinationsOfInterventionsAffectingContactPattern; ++intCombIndex)
            {
                if (intCombIndex == 0)
                    contactMatrices[intCombIndex] = _baseContactMatrices;
                else
                {
                    int[] onOfStatusOfInterventionsAffectingContactPattern = SupportFunctions.ConvertToBase2FromBase10(intCombIndex, _numOfInterventionsAffectingContactPattern);
                    for (int interventionIndex = 0; interventionIndex < _numOfInterventionsAffectingContactPattern; ++interventionIndex)
                    {
                        // initialize contact matrices
                        contactMatrices[intCombIndex] = new double[_numOfPathogens][,];

                        if (onOfStatusOfInterventionsAffectingContactPattern[interventionIndex] == 1)
                        {
                            for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                            {
                                contactMatrices[intCombIndex][pathogenID] = new double[contactMatrixSize, contactMatrixSize];
                                for (int i = 0; i < contactMatrixSize; ++i)
                                    for (int j = 0; j < contactMatrixSize; ++j)
                                        contactMatrices[intCombIndex][pathogenID][i, j] = _baseContactMatrices[pathogenID][i,j]+
                                            _baseContactMatrices[pathogenID][i, j] * _arrSampledParameterValues[_percentChangeInContactMatricesParIDs[interventionIndex][pathogenID][i, j]];
                            }
                        }
                    }
                }
            }

            // get the sample of infectivity and susceptibility of classes                    
            int classID = 0;
            foreach (Class thisClass in _classes)
            {
                // update the susceptibility and infectivity parameters based on the sampled parameter values
                //thisClass.UpdateSusceptibilityAndInfectivityParameterValues(_arrSampledParameterValues);

                arrInfectivity[classID] = thisClass.InfectivityValues;
                arrSusceptibility[classID] = thisClass.SusceptibilityValues;
                arrRowInContactMatrix[classID] = thisClass.RowIndexInContactMatrix;
                ++classID;
            }

            // populate the transmission matrix
            for (int intCombIndex = 0; intCombIndex < numOfCombinationsOfInterventionsAffectingContactPattern; ++intCombIndex)
            {
                _tranmissionMatrices[intCombIndex] = new double[_numOfPathogens][][];
                for (int pathogenID = 0; pathogenID < _numOfPathogens; pathogenID++)
                {
                    _tranmissionMatrices[intCombIndex][pathogenID] = new double[_numOfClasses][];
                    for (int i = 0; i < _numOfClasses; ++i)
                    {
                        if (arrSusceptibility[i].Count() != 0)
                        {
                            _tranmissionMatrices[intCombIndex][pathogenID][i] = new double[_numOfClasses];
                            for (int j = 0; j < _numOfClasses; ++j)
                            {
                                if (arrInfectivity[j].Count() != 0)
                                {
                                    _tranmissionMatrices[intCombIndex][pathogenID][i][j]
                                        = arrSusceptibility[i][pathogenID]
                                            * contactMatrices[intCombIndex][pathogenID][arrRowInContactMatrix[i], arrRowInContactMatrix[j]]
                                            * arrInfectivity[j][pathogenID];
                                }
                            }
                        }
                    }
                }
            }
        }
        // get ready for another ADP iteration
        private void GetReadyForAnotherADPIteration(long itr)
        {
            // update the epsilon greedy
            _POMDP_ADP.UpdateEpsilonGreedy(itr);
            // clear ADP state-decision collection      
            //_POMDP_ADP.ResetForAnotherSimulationRun(_totalCost);
        }
        // update current epidemic time
        private void UpdateCurrentEpidemicTimeIndex()
        {
            switch (_markOfEpidemicStartTime)
            {
                case enumMarkOfEpidemicStartTime.TimeZero:
                    {
                        _currentEpidemicTimeIndex = _currentSimulationTimeIndex;
                    }
                    break;

                case enumMarkOfEpidemicStartTime.TimeOfFirstObservation:
                    {
                        if (_firstObservationObtained)
                            _currentEpidemicTimeIndex = _currentSimulationTimeIndex - _timeIndexOfTheFirstObservation + _numOfDeltaTIndexInAnObservationPeriod;
                        else
                            _currentEpidemicTimeIndex = int.MinValue;
                    }
                    break;
            }            
        }
        
        #endregion

        // ********* public subs to set up the model *************
        #region public subs to set up the model
        // create the model
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parametersSheet"></param>
        /// <param name="classesSheet"></param>
        /// <param name="interventionsSheet"></param>
        /// <param name="resourcesSheet"></param>
        /// <param name="processesSheet"></param>
        /// <param name="resourceRulesSheet"></param>
        /// <param name="summationStatisticsSheet"></param>
        /// <param name="ratioStatisticsSheet"></param>
        /// <param name="modelStructureSheet"></param>
        /// <param name="baseContactMatrices">[pathogenID][class ID, class ID] </param>
        /// <param name="percentChangeInContactMatricesParIDs">[intervention ID][pathogenID][class ID, class ID]</param>
        public void BuildModel(ref ModelSettings modelSettings)
        {
            _modelSettings = modelSettings;

            // reset the epidemic
            Reset();
            // add parameters
            AddParameters(modelSettings.ParametersSheet);
            // add pathogens
            AddPathogens(modelSettings.PathogenSheet);
            // add classes
            AddClasses(modelSettings.ClassesSheet);
            // add interventions
            AddInterventions(modelSettings.InterventionSheet);
            // add resources
            AddResources(modelSettings.ResourcesSheet);
            // add processes
            AddProcesses(modelSettings.ProcessesSheet);            
            // add summation statistics
            AddSummationStatistics(modelSettings.SummationStatisticsSheet);
            // add ratio statistics
            AddRatioStatistics(modelSettings.RatioStatisticsSheet);
            // add connections
            AddConnections(modelSettings.ConnectionsMatrix);
            // setup storing simulation trajectories
            SetupStoringSimulationTrajectory();
            // update contact matrices
            UpdateContactMatrices();
        }
        ///// <summary>
        ///// ad contact matrices
        ///// </summary>
        ///// <param name="baseContactMatrices">[pathogenID][class ID, class ID] </param>
        ///// <param name="percentChangeInContactMatricesParIDs">[intervention ID][pathogenID][class ID, class ID]</param>
        //public void AddContactMatrices(ref double[][,] baseContactMatrices, ref int[][][,] percentChangeInContactMatricesParIDs)
        //{
        //    _baseContactMatrices = baseContactMatrices;
        //    _percentChangeInContactMatricesParIDs = percentChangeInContactMatricesParIDs;
        //}
        public void UpdateContactMatrices()
        {
            _baseContactMatrices = _modelSettings.GetBaseContactMatrices();
            _percentChangeInContactMatricesParIDs = _modelSettings.GetPercentChangeInContactMatricesParIDs();
        }
        // setup simulation settings
        public void SetupSimulationSettings(
            enumMarkOfEpidemicStartTime markOfEpidemicStartTime, double deltaT, double decisionIntervalLength,  double warmUpPeriod,          
            double simulationHorizonTime, double epidemicConditionTimeIndex,
            double epidemicTimeToStartDecisionMaking, int initialActionCombinationBinaryCode, enumDecisionRule decisionRule,
            bool storeEpidemicTrajectories, double simulationOutputIntervalLength, double observationPeriodLength,
            double annualInterestRate, double wtpForHealth)
        {
            _markOfEpidemicStartTime = markOfEpidemicStartTime;
            if (_markOfEpidemicStartTime == enumMarkOfEpidemicStartTime.TimeZero) _firstObservationObtained = true;

            _deltaT = deltaT;
            _decisionIntervalLength = decisionIntervalLength;
            _warmUpPeriodIndex = (int)(warmUpPeriod/_deltaT);

            _numOfDeltaTsInADecisionInterval = (int)(_decisionIntervalLength / _deltaT);
            _simulationHorizonTime = simulationHorizonTime;
            _simulationHorizonTimeIndex = (int)(simulationHorizonTime/_deltaT);
            _epidemicConditionTimeIndex = (int)(epidemicConditionTimeIndex/_deltaT);
            _epidemicTimeIndexToStartDecisionMaking = (int)(epidemicTimeToStartDecisionMaking / _deltaT);

            _initialActionCombinationBinaryCode = initialActionCombinationBinaryCode;

            _decisionRule = decisionRule;

            _storeEpidemicTrajectories = storeEpidemicTrajectories;
            _simulationOutputIntervalLength = simulationOutputIntervalLength;
            _numOfDeltaTIndexInASimulationOutputInterval = (int)(_simulationOutputIntervalLength / _deltaT);
            _observationPeriodLengh = observationPeriodLength;
            _numOfDeltaTIndexInAnObservationPeriod = (int)(_observationPeriodLengh / _deltaT);

            _annualInterestRate = annualInterestRate;
            double decisionPeriodInterestRate = annualInterestRate * _decisionIntervalLength / 364;
            _discountRate = 1 / (1 + decisionPeriodInterestRate);

            _wtpForHealth = wtpForHealth;
        }

        // setup dynamic policy related settings
        public void SetupDynamicPolicySettings
            (POMDP_ADP.enumQFunctionApproximationMethod qFunctionApproximationMethod, bool useEpidemicTimeAsFeature, int degreeOfPolynomialQFunction, double L2RegularizationPenalty)
        {
            _useEpidemicTimeAsFeature = useEpidemicTimeAsFeature;
            if (_useEpidemicTimeAsFeature)
            {
                _features.Add(new Feature_EpidemicTime("Epidemic Time", _numOfFeatures));
                ++_numOfFeatures;
            }            

            //_pastDecisionPeriodWithDecisionAsFeature = Math.Max(1, pastDecisionPeriodWithDecisionAsFeature);
            //_decisionsOverPastAndCurrentDecisionPeriods = new int[_pastDecisionPeriodWithDecisionAsFeature + 1];
            // setup Q-functions
            SetupPolynomialQFunctions(qFunctionApproximationMethod, degreeOfPolynomialQFunction);
            // add L2 regularization
            if (L2RegularizationPenalty > 0) 
                AddL2Regularization(L2RegularizationPenalty);
        }
        // setup always on/off and interval-based static policy settings
        public void SetupAlwaysOnOffAndIntervalBasedStaticPolicySettings(int[] interventionsOnOffSwitchStatus, double[] startTimes, int[] numOfDecisionPeriodsToUse)
        {
            //_POMDP_ADP.SetInitialActionCombination(interventionsOnOffSwitchStatus);

            //foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
            //{
            //    int i = thisIntervention.ID;
            //    if (thisIntervention.StaticPolicyType ==  enumStaticPolicyType.IntervalBased)
            //        thisIntervention.AddIntervalBaseEmploymentSetting(
            //            (long)(startTimes[i] / _deltaT), (long)((startTimes[i] + numOfDecisionPeriodsToUse[i] * _decisionIntervalLength) / _deltaT));
            //}
        }
        // setup threshold-based static policy settings
        public void SetupThresholdBasedStaticPolicySettings(int[] interventionIDs, int[] specialStatisticsIDs, double[] thresholds, int[] numOfDecisionPeriodsToUseInterventions)
        {
            //for (int i = 0; i < interventionIDs.Length; ++i)
            //    ((Intervention)_POMDP_ADP.Actions[interventionIDs[i]]).SetupThresholdBasedEmployment                
            //        (specialStatisticsIDs[i], thresholds[i], numOfDecisionPeriodsToUseInterventions[i] * _numOfDeltaTsInADecisionInterval);
        }
        public void SetupThresholdBasedStaticPolicySettings(int[] interventionIDs, double[] thresholds, int[] numOfDecisionPeriodsToUseInterventions)
        {
            //for (int i = 0; i < interventionIDs.Length; ++i)
            //    ((Intervention)_POMDP_ADP.Actions[interventionIDs[i]]).SetupThresholdBasedEmployment   
            //        (thresholds[i], numOfDecisionPeriodsToUseInterventions[i] * _numOfDeltaTsInADecisionInterval);
        }
        // setup Q-functions with polynomial functions
        public void SetupPolynomialQFunctions(POMDP_ADP.enumQFunctionApproximationMethod qFunctionApproximationMethod, int degreeOfPolynomialQFunction)
        {
            int numOfFeatures = _features.Count;
            _POMDP_ADP.SetUpQFunctionApproximationModel(
                qFunctionApproximationMethod, ComputationLib.POMDP_ADP.enumResponseTransformation.None, 
                numOfFeatures, degreeOfPolynomialQFunction, 2);
        }
        // add L2 regularization
        public void AddL2Regularization(double penaltyParameter)
        {
            _POMDP_ADP.AddL2Regularization(penaltyParameter);
        }
        /// <summary>
        /// update Q-function coefficients 
        /// </summary>
        /// <param name="qFunctionCoefficients"> [decisionID][coefficientIndex]</param>
        public void UpdateQFunctionCoefficients(double[][] qFunctionCoefficients)
        {
            double[] arrCoefficients = new double[qFunctionCoefficients.Length * qFunctionCoefficients[0].Length] ;

            // concatenate initial estimates
            int k= 0;
            for (int i = 0; i < qFunctionCoefficients.Length; i++)
                for (int j = 0; j < qFunctionCoefficients[i].Length; j++)
                    arrCoefficients[k++] = qFunctionCoefficients[i][j];

            _POMDP_ADP.UpdateQFunctionCoefficients(arrCoefficients);                  
        }
        public void UpdateQFunctionCoefficients(double[] qFunctionCoefficients)
        {
            _POMDP_ADP.UpdateQFunctionCoefficients(qFunctionCoefficients);
        }     
        // setup storing the simulation trajectory
        public void SetupStoringSimulationTrajectory()
        {
            // return if trajectories should not be reported
            if (_storeEpidemicTrajectories == false)
                return;

            _numOfTimeBasedOutputsToReport = 2; // 1 for simulation replication and 1 for simulation time
            _numOfIntervalBasedOutputsToReport = 1; // 1 for interval 
            _numOfMonitoredSimulationOutputs = 0;

            foreach (Class thisClass in _classes)
            {
                if (thisClass.ShowNewMembers == true)
                    ++_numOfIntervalBasedOutputsToReport;
                if (thisClass.ShowMembersInClass == true)
                    ++_numOfTimeBasedOutputsToReport;
                if (thisClass.ShowAccumulatedNewMembers == true)
                    ++_numOfTimeBasedOutputsToReport;
            }
            foreach (SummationStatistics thisSumStat in _summationStatistics.Where(s => s.IfDisplay))
            {
                switch (thisSumStat.Type)
                {
                    case APACE_lib.SummationStatistics.enumType.Incidence:
                        ++_numOfIntervalBasedOutputsToReport;
                        break;
                    case APACE_lib.SummationStatistics.enumType.AccumulatingIncident:
                    case APACE_lib.SummationStatistics.enumType.Prevalence:
                        ++_numOfTimeBasedOutputsToReport;
                        break;
                }
            }
            foreach (RatioStatistics thisRatioStat in _ratioStatistics.Where(r => r.IfDisplay))
            {
                switch (thisRatioStat.Type)
                {
                    case APACE_lib.RatioStatistics.enumType.IncidenceOverIncidence:                    
                    case APACE_lib.RatioStatistics.enumType.IncidenceOverPrevalence:
                        ++_numOfIntervalBasedOutputsToReport;
                        break;
                    case APACE_lib.RatioStatistics.enumType.PrevalenceOverPrevalence:
                    case APACE_lib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence:
                        ++_numOfTimeBasedOutputsToReport;
                        break;
                }                
            }

            // report summation statistics for which surveillance is available
            _numOfMonitoredSimulationOutputs = _summationStatistics.Where(s => s.SurveillanceDataAvailable).Count();
            // report ratio statistics for which surveillance is available
            _numOfMonitoredSimulationOutputs += _ratioStatistics.Where(r => r.SurveillanceDataAvailable).Count();
        }
        // add intervention history
        public void AddInterventionHistory(int[][] prespecifiedDecisionsOverObservationPeriods)
        {
            _prespecifiedDecisionsOverObservationPeriods = prespecifiedDecisionsOverObservationPeriods;
        }                
        // update the time to start decision making
        public void UpdateTheTimeToStartDecisionMakingAndWarmUpPeriod(double epidemicTimeToStartDecisionMaking, double warmUpPeriod)
        {
            _epidemicTimeIndexToStartDecisionMaking = (int)(epidemicTimeToStartDecisionMaking / _deltaT);
            _warmUpPeriodIndex = (int)(warmUpPeriod / _deltaT);
        }
        // get which interventions are affecting contact pattern
        public bool[] IfInterventionsAreAffectingContactPattern()
        {
            bool[] result = new bool[_POMDP_ADP.NumberOfActions];

            foreach (Intervention thisIntervention in _POMDP_ADP.Actions)
                if (thisIntervention.AffectingContactPattern)
                    result[thisIntervention.ID] = true;
            return result;
        }

        #endregion

        // private subs to create model
        #region private subs to create model
        
        // read parameters
        private void AddParameters(Array parametersSheet)
        {
            _numOfParametersToCalibrate = 0;
            int lastRowIndex = parametersSheet.GetLength(0);
            for (int rowIndex = 1; rowIndex <= lastRowIndex; ++rowIndex)
            {
                // ID and Name
                int parameterID = Convert.ToInt32(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.ID));
                string name = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Name));
                double defalutValue = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.DefalutValue));
                bool updateAtEachTimeStep = SupportFunctions.ConvertYesNoToBool(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.UpdateAtEachTimeStep).ToString());
                string distribution = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Distribution));
                enumRandomVariates enumRVG = RandomVariateLib.SupportProcedures.ConvertToEnumRVG(distribution);
                bool includedInCalibration = SupportFunctions.ConvertYesNoToBool(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.IncludedInCalibration).ToString());

                Parameter thisParameter = null;
                double par1 = 0, par2 = 0, par3 = 0, par4 = 0;

                if (enumRVG == enumRandomVariates.LinearCombination)
                {
                    string strPar1 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par1));
                    string strPar2 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par2));

                    // remove spaces and parenthesis
                    strPar1 = strPar1.Replace(" ", "");
                    strPar1 = strPar1.Replace("(", "");
                    strPar1 = strPar1.Replace(")", "");
                    strPar2 = strPar2.Replace(" ", "");
                    strPar2 = strPar2.Replace("(", "");
                    strPar2 = strPar2.Replace(")", "");
                    // convert to array
                    string[] strParIDs = strPar1.Split(',');
                    string[] strCoefficients = strPar2.Split(',');
                    // convert to numbers
                    int[] arrParIDs = Array.ConvertAll<string, int>(strParIDs, Convert.ToInt32);
                    double[] arrCoefficients = Array.ConvertAll<string, double>(strCoefficients, Convert.ToDouble);

                    thisParameter = new LinearCombination(parameterID, name, defalutValue, arrParIDs, arrCoefficients);
                }
                else if (enumRVG == enumRandomVariates.MultipleCombination)
                {
                    string strPar1 = Convert.ToString(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par1));

                    // remove spaces and parenthesis
                    strPar1 = strPar1.Replace(" ", "");
                    strPar1 = strPar1.Replace("(", "");
                    strPar1 = strPar1.Replace(")", "");
                    // convert to array
                    string[] strParIDs = strPar1.Split(',');
                    // convert to numbers
                    int[] arrParIDs = Array.ConvertAll<string, int>(strParIDs, Convert.ToInt32);

                    thisParameter = new MultipleCombination(parameterID, name, defalutValue, arrParIDs);
                }
                else
                {
                    par1 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par1));
                    par2 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par2));
                    par3 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par3));
                    par4 = Convert.ToDouble(parametersSheet.GetValue(rowIndex, (int)ExcelInterface.enumParameterColumns.Par4));
                }           

                switch (enumRVG)
                {
                    case (enumRandomVariates.LinearCombination):
                    case (enumRandomVariates.MultipleCombination):
                        // created above
                        break;
                    case (enumRandomVariates.Correlated):
                        thisParameter = new CorrelatedParameter(parameterID, name, defalutValue, (int)par1, par2, par3);
                        break;
                    case (enumRandomVariates.Multiplicative):
                        thisParameter = new MultiplicativeParameter(parameterID, name, defalutValue, (int)par1, (int)par2, (bool)(par3==1));
                        break;
                    case (enumRandomVariates.TimeDependetLinear):
                        {
                            thisParameter = new TimeDependetLinear(parameterID, name, defalutValue, (int)par1, (int)par2, par3, par4);
                            _thereAreTimeDependentParameters = true;
                        }
                        break;
                    case (enumRandomVariates.TimeDependetOscillating):
                        {
                            thisParameter = new TimeDependetOscillating(parameterID, name, defalutValue, (int)par1, (int)par2, (int)par3, (int)par4);
                            _thereAreTimeDependentParameters = true;
                        }
                        break;
                    default:
                        thisParameter = new IndependetParameter(parameterID, name, defalutValue, enumRVG, par1, par2, par3, par4);
                        break;
                }

                thisParameter.ShouldBeUpdatedByTime = updateAtEachTimeStep;
                thisParameter.IncludedInCalibration = includedInCalibration;
                
                if (includedInCalibration)
                    ++_numOfParametersToCalibrate;

                // add the parameter to the list
                _parameters.Add(thisParameter);
            }
        }
        // add pathogens
        private void AddPathogens(Array pathogenSheet)
        {
            _pathogenIDs = new int[pathogenSheet.GetLength(0)];
            for (int rowIndex = 1; rowIndex <= pathogenSheet.GetLength(0); ++rowIndex)
            {
                _pathogenIDs[rowIndex - 1] = Convert.ToInt32(pathogenSheet.GetValue(rowIndex, 1));
            }
            _numOfPathogens = _pathogenIDs.Length;
        }
        // add classes
        private void AddClasses(Array classesSheet)
        {
            for (int rowIndex = 1; rowIndex <= classesSheet.GetLength(0); ++rowIndex)
            {
                // ID and Name
                int classID = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ID));
                string name = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.Name));
                // class type
                string strClassType = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ClassType));

                // QALY loss and cost outcomes
                double QALYLoss = (double)Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.QALYLoss));
                double costPerNewMember = (double)Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CostPerNewMember));
                double healthQualityPerUnitOfTime = (double)Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.HealthQualityPerUnitOfTime));
                double costPerUnitOfTime = (double)Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CostPerUnitOfTime));
                // statistics                
                bool collectNewMembers = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CollectNewMembers)));
                bool collectMembersInClass = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.CollectMembers)));
                bool showStatisticsInSimulationResults = SupportFunctions.ConvertYesNoToBool(Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ShowInSimulationStaitistcsReport)));

                // simulation output
                string strShowNewMembers = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ShowNewMembers));
                string strShowMembersInClass = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ShowMembersInClass));
                string strShowAccumulatedNewMembers = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ShowAccumulatedNewMembers));
                bool showNewMembers = SupportFunctions.ConvertYesNoToBool(strShowNewMembers);
                bool showMembersInClass = SupportFunctions.ConvertYesNoToBool(strShowMembersInClass);
                bool showAccumulatedNewMembers = SupportFunctions.ConvertYesNoToBool(strShowAccumulatedNewMembers);

                // build and add the class
                #region Build class
                switch (strClassType)
                {
                    case "Class: Normal":
                        {
                            // initial number parameter ID
                            int initialMembersParID = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.InitialMembers));
                            // empty to eradicate
                            string strEmptyToEradicate = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.EmptyToEradicate));
                            bool emptyToEradicate = SupportFunctions.ConvertYesNoToBool(strEmptyToEradicate);

                            // susceptibility parameter ID
                            string strSusceptibilityIDs = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.SusceptibilityParID));
                            // infectivity parameter ID
                            string strInfectivityIDs = Convert.ToString(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.InfectivityParID));
                            // row in contact matrix
                            int rowInContactMatrix = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.RowInContactMatrix));

                            // build the class
                            Class_Normal thisNormalClass = new Class_Normal(classID, name);
                            // set up initial member and eradication rules
                            thisNormalClass.SetupInitialAndStoppingConditions(initialMembersParID, emptyToEradicate);
                            // set up transmission dynamics properties                            
                            thisNormalClass.SetupTransmissionDynamicsProperties(strSusceptibilityIDs, strInfectivityIDs, rowInContactMatrix);

                            // set up new members statistics
                            if (collectNewMembers == true || showNewMembers) 
                                thisNormalClass.SetupNewMembersStatistics
                                    (QALYLoss, costPerNewMember, (int)(_simulationOutputIntervalLength/_deltaT) , (int)(_observationPeriodLengh / _deltaT));                                
                            // set up members in class statistics
                            if (collectMembersInClass == true)
                                thisNormalClass.SetupMembersInClassStatistics(healthQualityPerUnitOfTime, costPerUnitOfTime);
                            // set up which statistics to show
                            thisNormalClass.ShowStatisticsInSimulationResults = showStatisticsInSimulationResults;
                            thisNormalClass.ShowNewMembers = showNewMembers;
                            thisNormalClass.ShowMembersInClass = showMembersInClass;
                            thisNormalClass.ShowAccumulatedNewMembers = showAccumulatedNewMembers;

                            // add class
                            _classes.Add(thisNormalClass);

                            // check if infectivity and susceptibility parameters are time dependent
                            if (_thereAreTimeDependentParameters)
                            {
                                for (int i = 0; i < thisNormalClass.InfectivityParIDs.Length; i++)
                                    if (_parameters[thisNormalClass.InfectivityParIDs[i]].ShouldBeUpdatedByTime)
                                    {
                                        _thereAreTimeDependentParameters_affectingTranmissionDynamics = true;
                                        _thereAreTimeDependentParameters_affectingInfectivities = true;
                                    }
                                for (int i = 0; i < thisNormalClass.SusceptibilityParIDs.Length; i++)
                                    if (_parameters[thisNormalClass.SusceptibilityParIDs[i]].ShouldBeUpdatedByTime)
                                    {
                                        _thereAreTimeDependentParameters_affectingTranmissionDynamics = true;
                                        _thereAreTimeDependentParameters_affectingSusceptibilities = true;
                                    }
                            }
                        }
                        break;
                    case "Class: Death":
                        {
                            // build the class
                            Class_Death thisDealthClass = new Class_Death(classID, name);

                            // set up new members statistics                            
                            if (collectNewMembers == true)
                                thisDealthClass.SetupNewMembersStatistics(QALYLoss, costPerNewMember, 1, (int)(_observationPeriodLengh / _deltaT)); 
                            // set up which statistics to show
                            thisDealthClass.ShowStatisticsInSimulationResults = showStatisticsInSimulationResults;
                            thisDealthClass.ShowNewMembers = showNewMembers;
                            thisDealthClass.ShowMembersInClass = showMembersInClass;
                            thisDealthClass.ShowAccumulatedNewMembers = showAccumulatedNewMembers;

                            // add class
                            _classes.Add(thisDealthClass);
                        }
                        break;
                    case "Class: Splitting":
                        {
                            // read settings
                            int parIDForProbOfSuccess = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ParIDForProbOfSuccess));
                            int destinationClassIDGivenSuccess = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfSuccess));
                            int destinationClassIDGivenFailure = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfFailure));

                            // build the class
                            Class_Splitting thisSplittingClass = new Class_Splitting(classID, name);
                            thisSplittingClass.SetUp(parIDForProbOfSuccess, destinationClassIDGivenSuccess, destinationClassIDGivenFailure);

                            // set up new members statistics
                            if (collectNewMembers == true)
                                thisSplittingClass.SetupNewMembersStatistics(QALYLoss, costPerNewMember, 1, (int)(_observationPeriodLengh / _deltaT)); ;
                            // set up which statistics to show
                            thisSplittingClass.ShowStatisticsInSimulationResults = showStatisticsInSimulationResults;
                            thisSplittingClass.ShowNewMembers = showNewMembers;
                            thisSplittingClass.ShowMembersInClass = showMembersInClass;
                            thisSplittingClass.ShowAccumulatedNewMembers = showAccumulatedNewMembers;

                            // add class
                            _classes.Add(thisSplittingClass);

                            // check if the rate parameter is time dependent
                            if (_thereAreTimeDependentParameters && _parameters[parIDForProbOfSuccess].ShouldBeUpdatedByTime)
                                _thereAreTimeDependentParameters_affectingSplittingClasses = true;
                        }
                        break;
                    case "Class: Resource Monitor":
                        {
                            // read settings
                            int resourceIDToCheckAvailability = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ResourceIDToCheckAvailability));
                            double resourceUnitsConsumedPerArrival = (double)Convert.ToDouble(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.ResourceUnitsConsumedPerArrival));
                            int destinationClassIDGivenSuccess = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfSuccess));
                            int destinationClassIDGivenFailure = Convert.ToInt32(classesSheet.GetValue(rowIndex, (int)ExcelInterface.enumClassColumns.DestinationClassIDIfFailure));                            

                            // build the class
                            Class_ResourceMonitor thisResourceMonitorClass = new Class_ResourceMonitor(classID, name);                            
                            thisResourceMonitorClass.SetUp(resourceIDToCheckAvailability, resourceUnitsConsumedPerArrival, destinationClassIDGivenSuccess, destinationClassIDGivenFailure);

                            // set up new members statistics
                            if (collectNewMembers == true)
                                thisResourceMonitorClass.SetupNewMembersStatistics(QALYLoss, costPerNewMember, 1, (int)(_observationPeriodLengh / _deltaT)); ;
                            // set up which statistics to show
                            thisResourceMonitorClass.ShowStatisticsInSimulationResults = showStatisticsInSimulationResults;
                            thisResourceMonitorClass.ShowNewMembers = showNewMembers;
                            thisResourceMonitorClass.ShowMembersInClass = showMembersInClass;
                            thisResourceMonitorClass.ShowAccumulatedNewMembers = showAccumulatedNewMembers;

                            // add class
                            _classes.Add(thisResourceMonitorClass);
                        }
                        break;
                }
                #endregion               

            }// end of for
            _numOfClasses = _classes.Count;
        }
        // add interventions
        private void AddInterventions(Array interventionsSheet)
        {
            //_useSameContactMatrixForAllDecisions = true;

            for (int rowIndex = 1; rowIndex <= interventionsSheet.GetLength(0); ++rowIndex)
            {
                Intervention thisIntervention;
                double timeBecomeAvailable = 0;
                double timeBecomeUnavailable = 0;
                int resourceID = 0;
                int delayParID = 0;
                SimulationLib.SimulationAction.enumSwitchValue switchValue = SimulationLib.SimulationAction.enumSwitchValue.Off;

                // read intervention information
                int ID = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ID));
                // name
                string name = Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.Name));
                // type
                string strInterventionType = Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.Type));
                SimulationLib.SimulationAction.enumActionType type = SimulationLib.SimulationAction.ConvertToActionType(strInterventionType);
                // mutually exclusive group
                int mutuallyExclusiveGroup = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.MutuallyExclusiveGroup));
                // if affecting contact pattern
                bool affectingContactPattern = SupportFunctions.ConvertYesNoToBool(Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.AffectingContactPattern)));
                if (type != SimulationAction.enumActionType.Default && affectingContactPattern)
                {
                    ++_numOfInterventionsAffectingContactPattern;
                    SupportFunctions.AddToEndOfArray(ref _indecesOfInterventionsAffectingContactPattern, rowIndex - 1);
                }

                // costs
                double fixedCost = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.FixedCost));
                double costPerUnitOfTime = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.CostPerUnitOfTime));
                double penaltyForSwitchingFromOnToOff = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PenaltyOfSwitchingFromOnToOff));

                // on/off switch setting
                string strOnOffSwitchSetting = Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.OnOffSwitchSetting));                
                SimulationLib.SimulationAction.enumOnOffSwitchSetting onOffSwitchSetting = SimulationLib.SimulationAction.ConvertToOnOffSwitchSettings(strOnOffSwitchSetting);

                // switch value for the pre-determined employment
                switchValue = SimulationLib.SimulationAction.ConvertToSwitchValue(
                    Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PreDeterminedEmployment_SwitchValue)));

                // if type is default
                if (type == SimulationLib.SimulationAction.enumActionType.Default)
                {
                    affectingContactPattern = true;
                    onOffSwitchSetting = SimulationLib.SimulationAction.enumOnOffSwitchSetting.Predetermined;
                    switchValue = SimulationLib.SimulationAction.enumSwitchValue.On;
                }
                else // if type is not default
                {
                    //if (affectingContactPattern == true)
                    //    _useSameContactMatrixForAllDecisions = false;

                    // availability
                    timeBecomeAvailable = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.TimeBecomesAvailable));
                    timeBecomeUnavailable = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.TimeBecomesUnavailableTo));
                    delayParID = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.DelayParID));
                    resourceID = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ResourceID));
                }              

                // create the intervention
                thisIntervention = new Intervention(rowIndex - 1, ID, name, type, affectingContactPattern);
                // set up availability
                thisIntervention.SetUpAvailability(onOffSwitchSetting, (long)(timeBecomeAvailable / _deltaT), (long)(timeBecomeUnavailable / _deltaT), delayParID);
                // set up cost
                thisIntervention.SetUpCost(fixedCost, costPerUnitOfTime, penaltyForSwitchingFromOnToOff);
                // set up resource requirement
                if (Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ResourceID)) != "")
                    thisIntervention.SetupResourceRequirement(Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ResourceID)));
                // default switch status
                thisIntervention.PredeterminedSwitchValue = switchValue;

                // find the switch setting
                switch (onOffSwitchSetting)
                {
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Predetermined:
                        {
                            thisIntervention.OnOffSwitchSetting = SimulationLib.SimulationAction.enumOnOffSwitchSetting.Predetermined;
                        }
                        break;
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Periodic:
                        {
                            int frequency = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PeriodicEmployment_Periodicity));
                            int duration = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PeriodicEmployment_Length));
                            thisIntervention.AddPeriodicEmploymentSetting(frequency, duration);
                        }
                        break;
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.ThresholdBased:
                        {
                            int IDOfSpecialStatistics = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_IDOfSpecialStatisticsToObserveAccumulation));
                            string strObservation = Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_Observation));
                            APACE_lib.Intervention.EnumEpidemiologicalObservation observation = Intervention.EnumEpidemiologicalObservation.OverPastObservationPeriod;
                            if (strObservation == "Accumulating")
                                observation = Intervention.EnumEpidemiologicalObservation.Accumulating;

                            double threshold = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_ThresholdToTriggerThisDecision));
                            int duration = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.ThresholdBased_NumOfDecisionPeriodsToUseThisDecision));
                            thisIntervention.AddThresholdBasedEmploymentSetting(IDOfSpecialStatistics, observation, threshold, (long)(duration * _numOfDeltaTsInADecisionInterval));
                        }
                        break;
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.IntervalBased:
                        {
                            double availableUntilThisTime = Convert.ToDouble(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.IntervalBasedOptimizationSettings_AvailableUpToTime));
                            int minNumOfDecisionPeriodsToUse = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.IntervalBasedOptimizationSettings_MinNumOfDecisionPeriodsToUse));
                            thisIntervention.AddIntervalBaseEmploymentSetting(availableUntilThisTime, minNumOfDecisionPeriodsToUse);
                        }
                        break;                   
                    case SimulationLib.SimulationAction.enumOnOffSwitchSetting.Dynamic:
                        {
                            bool selectOnOffStatusAsFeature = SupportFunctions.ConvertYesNoToBool(Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.SelectOnOffStatusAsFeature)));

                            int previousObservationPeriodToObserveOnOffValue=0;
                            if (selectOnOffStatusAsFeature)
                                previousObservationPeriodToObserveOnOffValue = Convert.ToInt32(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.PreviousObservationPeriodToObserveValue));
                            
                            bool useNumOfDecisionPeriodEmployedAsFeature = SupportFunctions.ConvertYesNoToBool(Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.UseNumOfDecisionPeriodEmployedAsFeature)));
                            bool remainOnOnceSwitchedOn = SupportFunctions.ConvertYesNoToBool(Convert.ToString(interventionsSheet.GetValue(rowIndex, (int)ExcelInterface.enumInterventionColumns.RemainsOnOnceSwitchedOn)));
                            thisIntervention.AddDynamicPolicySettings(remainOnOnceSwitchedOn); //selectOnOffStatusAsFeature, previousObservationPeriodToObserveOnOffValue, useNumOfDecisionPeriodEmployedAsFeature,

                            // add features related to this intervention
                            // on/off status features (note that default interventions can not have on/off status feature)
                            if (selectOnOffStatusAsFeature && thisIntervention.Type != SimulationAction.enumActionType.Default)
                                _features.Add(new Feature_InterventionOnOffStatus("On/Off status of " + thisIntervention.Name, _numOfFeatures++, thisIntervention.ID, previousObservationPeriodToObserveOnOffValue));
                            // feature on the number of decision periods over which this intervention is used
                            if (useNumOfDecisionPeriodEmployedAsFeature)
                                _features.Add(new Feature_NumOfDecisoinPeriodsOverWhichThisInterventionWasUsed("Number of decision periods " + thisIntervention.Name + " is used", _numOfFeatures++, thisIntervention.ID));
                        }
                        break;
                }                                                               

                // add the intervention
                _POMDP_ADP.AddAnAction(thisIntervention);
            }

            // gather info
            _POMDP_ADP.GatherInformationAfterAllActionsAreEntered();            

        }
        
        // add resources
        private void AddResources(Array resourcesSheet)
        {
            if (resourcesSheet == null) return;

            for (int rowIndex = 1; rowIndex <= resourcesSheet.GetLength(0); ++rowIndex)
            {
                // read resource information
                int ID = Convert.ToInt32(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ID));
                string name = Convert.ToString(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.Name));
                double pricePerUnit = Convert.ToDouble(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.PricePerUnit));
                string replenishmentType = Convert.ToString(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ReplenishmentType));
                int parID_firstTimeAvailability = Convert.ToInt32(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.FirstTimeAvailable_parID));
                int parID_replenishmentQuantity = Convert.ToInt32(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ReplenishmentQuantity_parID));

                // create the resource
                Resource thisResource = new Resource(ID, name, pricePerUnit);

                // setup the availability time and quantity
                switch (replenishmentType)
                {
                    case "One-Time":
                            thisResource.SetupAvailability(parID_firstTimeAvailability, parID_replenishmentQuantity);
                        break;
                    case "Periodic":
                        {
                            int parID_replenishmentInterval = Convert.ToInt32(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ReplenishmentInterval_parID));
                            thisResource.SetupAvailability(parID_firstTimeAvailability, parID_replenishmentQuantity, parID_replenishmentInterval);
                        }
                        break;
                }

                // if its availability should be reported
                thisResource.ShowAvailability = SupportFunctions.ConvertYesNoToBool(Convert.ToString(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.ShowAvailableUnits)));
                // add the resource
                _resources.Add(thisResource);

                // if a feature should be associated to this
                if (SupportFunctions.ConvertYesNoToBool(Convert.ToString(resourcesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceColumns.SelectAsFeature))))
                    _features.Add(new Feature_DefinedOnResources(name, _numOfFeatures++, ID));
            } 
        }
        // add processes
        private void AddProcesses(Array processesSheet)
        {
            for (int rowIndex = 1; rowIndex <= processesSheet.GetLength(0); ++rowIndex)
            {
                // general settings
                int ID = Convert.ToInt32(processesSheet.GetValue(rowIndex, (int)ExcelInterface.enumProcessColumns.ID));
                string name = Convert.ToString(processesSheet.GetValue(rowIndex, (int)ExcelInterface.enumProcessColumns.Name));
                string strProcessType = Convert.ToString(processesSheet.GetValue(rowIndex, (int)ExcelInterface.enumProcessColumns.ProcessType));
                int IDOfActivatingIntervention = Convert.ToInt32(processesSheet.GetValue(rowIndex, (int)ExcelInterface.enumProcessColumns.IDOfActiviatingIntervention));
                int IDOfDestinationClass = Convert.ToInt32(processesSheet.GetValue(rowIndex, (int)ExcelInterface.enumProcessColumns.IDOfDestinationClass));

                // build the process
                #region Build process
                switch (strProcessType)
                {
                    case "Process: Birth":
                        {
                            int IDOfRateParameter = Convert.ToInt32(processesSheet.GetValue(rowIndex, (int)ExcelInterface.enumProcessColumns.IDOfRateParameter));
                            // create the process
                            Process_Birth thisProcess_Birth = new Process_Birth(name, ID, IDOfActivatingIntervention, IDOfRateParameter, IDOfDestinationClass);
                            _processes.Add(thisProcess_Birth);
                        }
                        break;
                    case "Process: Epidemic-Independent":
                        {
                            int IDOfRateParameter = Convert.ToInt32(processesSheet.GetValue(rowIndex, (int)ExcelInterface.enumProcessColumns.IDOfRateParameter));
                            // create the process
                            Process_EpidemicIndependent thisProcess_EpidemicIndependent = new Process_EpidemicIndependent(name, ID, IDOfActivatingIntervention, IDOfRateParameter, IDOfDestinationClass);
                            _processes.Add(thisProcess_EpidemicIndependent);

                            // check if the rate parameter is time dependent
                            if (_thereAreTimeDependentParameters && _parameters[IDOfRateParameter].ShouldBeUpdatedByTime)
                                    _thereAreTimeDependentParameters_affectingNaturalHistoryRates = true;
                        }
                        break;
                    case "Process: Epidemic-Dependent":
                        {
                            int IDOfPathogenToGenerate = Convert.ToInt32(processesSheet.GetValue(rowIndex, (int)ExcelInterface.enumProcessColumns.IDOfGeneratingPathogen));
                            // create the process
                            Process_EpidemicDependent thisProcess_EpidemicDependent = new Process_EpidemicDependent(name, ID, IDOfActivatingIntervention, IDOfPathogenToGenerate, IDOfDestinationClass);
                            _processes.Add(thisProcess_EpidemicDependent);
                        }
                        break;
                }
                #endregion

            } // end of for
        }
        // add resource rules
        private void AddResourceRules(Array resourceRulesSheet)
        {
            if (resourceRulesSheet == null) return;

            // return if there is no resource
            if (_resources.Count == 0) return;

            for (int rowIndex = 1; rowIndex <= resourceRulesSheet.GetLength(0); ++rowIndex)
            {
                // read resource rule information
                int ID = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ID));
                string name = Convert.ToString(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.Name));
                int associatedResourceID = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ResourceID));
                string resourceRuleType = Convert.ToString(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ResourceRuleType));
                int consumptionPerArrival = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.UnitsConsumedPerArrival));
                int consumptionPerUnitOfTime = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.UnitsConsumedPerUnitOfTime));

                // create the resource
                ResourceRule thisResourceRule = new ResourceRule(ID, name, associatedResourceID, consumptionPerArrival, consumptionPerUnitOfTime);

                // setup the unavailability rule
                switch (resourceRuleType)
                {
                    case "Rule: Send to Another Class":
                        {
                            int classIDIfSatisfied = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ClassIDIfSatisfied));
                            int classIDIfNotSatisfied = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ClassIDIfNotSatisfied));
                            thisResourceRule.SetupUnavailabilityRuleSendToAnotherClass(classIDIfSatisfied, classIDIfNotSatisfied);
                        }
                        break;
                    case "Rule: Send to Another Process":
                        {
                            int processIDIfSatisfied = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ProcessIDIfSatisfied));
                            int processIDIfNotSatisfied = Convert.ToInt32(resourceRulesSheet.GetValue(rowIndex, (int)ExcelInterface.enumResourceRuleColumns.ProcessIDIfNotSatisfied));
                            thisResourceRule.SetupUnavailabilityRuleSendToAnotherClass(processIDIfSatisfied, processIDIfNotSatisfied);
                        }
                        break;
                }

                // add the resource
                _resourceRules.Add(thisResourceRule);
            }
        }
        // add summation statistics
        private void AddSummationStatistics(Array summationStatisticsSheet)
        {
            if (summationStatisticsSheet == null) return;
            for (int rowIndex = 1; rowIndex <= summationStatisticsSheet.GetLength(0); ++rowIndex)
            {
                // ID and Name
                int statID = Convert.ToInt32(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.ID));
                string name = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Name));
                string strDefinedOn = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.DefinedOn));
                string strType = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Type));
                string sumFormula = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Formula));

                // defined on 
                APACE_lib.SummationStatistics.enumDefinedOn definedOn = APACE_lib.SummationStatistics.enumDefinedOn.Classes;
                if (strDefinedOn == "Events") definedOn = APACE_lib.SummationStatistics.enumDefinedOn.Events;

                // type
                APACE_lib.SummationStatistics.enumType type = APACE_lib.SummationStatistics.enumType.Incidence;
                if (strType == "Prevalence") type = APACE_lib.SummationStatistics.enumType.Prevalence;
                else if (strType == "Accumulating Incidence") type = APACE_lib.SummationStatistics.enumType.AccumulatingIncident;

                // if display
                bool ifDispay = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfDisplay).ToString());

                // QALY loss and cost outcomes
                double QALYLossPerNewMember = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.QALYLoss));
                double costPerNewMember = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.CostPerNewMember));
                // real-time monitoring
                bool surveillanceDataAvailable = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.SurveillanceDataAvailable).ToString());
                int numOfObservationPeriodsDelayBeforeObservating = Convert.ToInt32(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.NumOfObservationPeriodsDelayBeforeObservating));
                bool firstObservationMarksTheStartOfTheSpread = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FirstObservationMarksTheStartOfTheSpread).ToString());

                // calibration
                bool ifIncludedInCalibration = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfIncludedInCalibration).ToString());
                
                // goodness of fit measure
                // default values
                SimulationLib.CalibrationTarget.enumGoodnessOfFitMeasure goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries;
                double[] fourierWeights = new double[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.SIZE];
                bool ifCheckWithinFeasibleRange = false;
                double feasibleMin = 0, feasibleMax = 0;
                
                // check if included in calibration 
                double overalWeight = 0;
                if (ifIncludedInCalibration)
                {
                    // measure of fit
                    string strGoodnessOfFitMeasure = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.MeasureOfFit));
                    if (strGoodnessOfFitMeasure == "Fourier") goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.Fourier;

                    // overall weight
                    overalWeight = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_OveralFit));
                    // Fourier weights
                    if (goodnessOfFitMeasure == CalibrationTarget.enumGoodnessOfFitMeasure.Fourier)
                    {
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Cosine]
                        = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierCosine));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Norm2]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierEuclidean));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Average]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierAverage));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.StDev]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierStDev));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Min]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMin));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Max]
                            = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMax));
                    }
                    // if to check if within a feasible range
                    ifCheckWithinFeasibleRange = SupportFunctions.ConvertYesNoToBool(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfCheckWithinFeasibleRange).ToString());
                    if (ifCheckWithinFeasibleRange)
                    {
                        feasibleMin = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_minimum));
                        feasibleMax = Convert.ToDouble(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_maximum));
                    }
                }

                // feature selection
                // TODO: check if features are defined on enough number of obs periods!
                string strNewMemberFeature = Convert.ToString(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.NewMember_FeatureType));
                enumFeatureCombinations newMemberFeature = SupportProcedures.ConvertToFeatureCombination(strNewMemberFeature);
                //int numOfPastObsPeriodsToStore = Convert.ToInt32(summationStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.NewMember_NumOfPastObsPeriodsToStore));
                //numOfPastObsPeriodsToStore = Math.Max(numOfPastObsPeriodsToStore, 1);

                // build and add the summation statistics
                SummationStatistics thisSummationStatistics = new
                    SummationStatistics(statID, name, definedOn, type, sumFormula, QALYLossPerNewMember, costPerNewMember, surveillanceDataAvailable, firstObservationMarksTheStartOfTheSpread,
                                       (int)(_simulationOutputIntervalLength / _observationPeriodLengh), (int)(_observationPeriodLengh / _deltaT), numOfObservationPeriodsDelayBeforeObservating);
                // if display
                thisSummationStatistics.IfDisplay = ifDispay;

                // set up calibration
                thisSummationStatistics.IfIncludedInCalibration = ifIncludedInCalibration;
                if (ifIncludedInCalibration)
                {                    
                    ++_numOfCalibratoinTargets;
                    thisSummationStatistics.GoodnessOfFitMeasure = goodnessOfFitMeasure;
                    thisSummationStatistics.Weight_overalFit = overalWeight;

                    if (goodnessOfFitMeasure == CalibrationTarget.enumGoodnessOfFitMeasure.Fourier)
                        thisSummationStatistics.Weight_fourierSimilarities = (double[])fourierWeights.Clone();

                    if (ifCheckWithinFeasibleRange)
                    {
                        thisSummationStatistics.IfCheckWithinFeasibleRange = ifCheckWithinFeasibleRange;
                        thisSummationStatistics.FeasibleRange_min = feasibleMin;
                        thisSummationStatistics.FeasibleRange_max = feasibleMax;
                    }
                }

                // add the summation statistics
                _summationStatistics.Add(thisSummationStatistics);

                // setup feature for new members
                switch (newMemberFeature)
                {
                    case enumFeatureCombinations.IncidenceOnly:
                        {
                            // incidence only
                            _features.Add(new Feature_DefinedOnSummationStatistics(name + ": Incidence", _numOfFeatures, Feature.enumFeatureType.Incidence, statID));
                            ++_numOfFeatures;
                        }
                        break;
                    case enumFeatureCombinations.PredictionOnly:
                        {
                            // prediction only
                            // NOTE: THIS SHOULD BE UPDATED
                            //_features.Add(new Feature_DefinedOnSummationStatistics(name + ": Prediction", _numOfFeatures, Feature.enumFeatureType.Prediction, statID, numOfPastObsPeriodsToStore));
                            ++_numOfFeatures;
                        }
                        break;
                    case enumFeatureCombinations.AccumulatingIncidenceOnly:
                        {
                            // accumulating incidence only
                            _features.Add(new Feature_DefinedOnSummationStatistics(name + ": Accumulating incidence", _numOfFeatures, Feature.enumFeatureType.AccumulatingIncidence, statID));
                            ++_numOfFeatures;
                        }
                        break;
                    case enumFeatureCombinations.AccumulatingIncidenceANDPrediction:
                        {
                            // accumulating incidence and prediction
                            _features.Add(new Feature_DefinedOnSummationStatistics(name + ": Accumulating incidence", _numOfFeatures, Feature.enumFeatureType.AccumulatingIncidence, statID));
                            ++_numOfFeatures;
                            // NOTE: THIS SHOULD BE UPDATED
                            //_features.Add(new Feature_DefinedOnSummationStatistics(name + ": Prediction", _numOfFeatures, Feature.enumFeatureType.Prediction, statID, numOfPastObsPeriodsToStore));
                            ++_numOfFeatures;
                        }
                        break;
                    case enumFeatureCombinations.IncidenceANDAccumulatingIncidence:
                        {
                            // total count and accumulating incidence
                            _features.Add(new Feature_DefinedOnSummationStatistics(name + ": Incidence", _numOfFeatures, Feature.enumFeatureType.Incidence, statID));
                            ++_numOfFeatures;
                            _features.Add(new Feature_DefinedOnSummationStatistics(name + ": Accumulating incidence", _numOfFeatures, Feature.enumFeatureType.AccumulatingIncidence, statID));
                            ++_numOfFeatures;
                        }
                        break;
                }
            }
        }
        // add ratio statistics
        private void AddRatioStatistics(Array ratioStatisticsSheet)
        {
            if (ratioStatisticsSheet == null) return;

            for (int rowIndex = 1; rowIndex <= ratioStatisticsSheet.GetLength(0); ++rowIndex)
            {
                // ID and Name
                int statID = Convert.ToInt32(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.ID));
                string name = Convert.ToString(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Name));
                string strType = Convert.ToString(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Type));
                string ratioFormula = Convert.ToString(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Formula));

                // if display
                bool ifDispay = SupportFunctions.ConvertYesNoToBool(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfDisplay).ToString());

                bool ifSurveillanceDataAvailable = SupportFunctions.ConvertYesNoToBool(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.SurveillanceDataAvailable).ToString());
                
                // calibration
                bool ifIncludedInCalibration = SupportFunctions.ConvertYesNoToBool(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfIncludedInCalibration).ToString());

                // default values
                SimulationLib.CalibrationTarget.enumGoodnessOfFitMeasure goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries;
                double[] fourierWeights = new double[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.SIZE];
                bool ifCheckWithinFeasibleRange = false;
                double feasibleMin = 0, feasibleMax = 0;

                // check if included in calibration 
                double overalWeight = 0;
                if (ifIncludedInCalibration)
                {
                    // measure of fit
                    string strGoodnessOfFitMeasure = Convert.ToString(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.MeasureOfFit));
                    switch (strGoodnessOfFitMeasure)
                    {
                        case "Sum Sqr Err (time-series)":
                            goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_timeSeries;
                            break;
                        case "Sum Sqr Err (average time-series)":
                            goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.SumSqurError_average;
                            break;
                        case "Fourier":
                            goodnessOfFitMeasure = CalibrationTarget.enumGoodnessOfFitMeasure.Fourier;
                            break;
                    }
                    // overall weight
                    overalWeight = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_OveralFit));
                    // Fourier weights
                    if (goodnessOfFitMeasure == CalibrationTarget.enumGoodnessOfFitMeasure.Fourier)
                    {
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Cosine]
                        = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierCosine));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Norm2]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierEuclidean));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Average]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierAverage));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.StDev]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierStDev));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Min]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMin));
                        fourierWeights[(int)SimulationLib.CalibrationTarget.enumFourierSimilarityMeasures.Max]
                            = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.Weight_FourierMax));
                    }
                    // if to check if within a feasible range
                    ifCheckWithinFeasibleRange = SupportFunctions.ConvertYesNoToBool(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.IfCheckWithinFeasibleRange).ToString());
                    if (ifCheckWithinFeasibleRange)
                    {
                        feasibleMin = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_minimum));
                        feasibleMax = Convert.ToDouble(ratioStatisticsSheet.GetValue(rowIndex, (int)ExcelInterface.enumSpecialStatisticsColumns.FeasibleRange_maximum));
                    }
                }

                // build a ratio stat
                RatioStatistics thisRatioStatistics = null;
                switch (strType)
                {
                    case "Incidence/Incidence":
                        thisRatioStatistics = new RatioStatistics(statID, name, APACE_lib.RatioStatistics.enumType.IncidenceOverIncidence, ratioFormula, ifSurveillanceDataAvailable);
                        break;
                    case "Accumulated Incidence/Accumulated Incidence":
                        thisRatioStatistics = new RatioStatistics(statID, name, APACE_lib.RatioStatistics.enumType.AccumulatedIncidenceOverAccumulatedIncidence, ratioFormula, ifSurveillanceDataAvailable);
                        break;
                    case "Prevalence/Prevalence":
                        thisRatioStatistics = new RatioStatistics(statID, name, APACE_lib.RatioStatistics.enumType.PrevalenceOverPrevalence, ratioFormula, ifSurveillanceDataAvailable);
                        break;
                    case "Incidence/Prevalence":
                        thisRatioStatistics = new RatioStatistics(statID, name, APACE_lib.RatioStatistics.enumType.IncidenceOverPrevalence, ratioFormula, ifSurveillanceDataAvailable);
                        break;
                }
                // if display
                thisRatioStatistics.IfDisplay = ifDispay;

                // set up calibration
                thisRatioStatistics.IfIncludedInCalibration = ifIncludedInCalibration;
                if (ifIncludedInCalibration)
                {
                    ++_numOfCalibratoinTargets;
                    thisRatioStatistics.GoodnessOfFitMeasure = goodnessOfFitMeasure;
                    thisRatioStatistics.Weight_overalFit = overalWeight;

                    if (goodnessOfFitMeasure == CalibrationTarget.enumGoodnessOfFitMeasure.Fourier)
                        thisRatioStatistics.Weight_fourierSimilarities = (double[])fourierWeights.Clone();

                    if (ifCheckWithinFeasibleRange)
                    {
                        thisRatioStatistics.IfCheckWithinFeasibleRange = ifCheckWithinFeasibleRange;
                        thisRatioStatistics.FeasibleRange_min = feasibleMin;
                        thisRatioStatistics.FeasibleRange_max = feasibleMax;
                    }
                }

                // add the summation statistics
                _ratioStatistics.Add(thisRatioStatistics);
            }
        }
        // add connections
        private void AddConnections(int[,] connectionsMatrix)
        {
            int i = 0;
            int classID, processID;
            while (i < connectionsMatrix.GetLength(0))
            {
                classID = connectionsMatrix[i, 0];
                processID = connectionsMatrix[i, 1];
                ((Class_Normal)_classes[classID]).AddAProcess((Process)_processes[processID].Clone());
                
                ++i;

                //foreach (Class thisClass in _classes)
                //{
                //    if (thisClass.ID == connectionsMatrix[i, 0])
                //    {
                //        foreach (Process thisProcess in _processes)
                //        {
                //            if (thisProcess.ID == connectionsMatrix[i, 1])
                //            {
                //                ((Class_Normal)thisClass).AddAProcess((Process)thisProcess.Clone());
                //                ++i;
                //                break;
                //            }
                //        }
                //        break;
                //    }
                //}
            }
        }        
        #endregion

    }
}
