﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExcelInteractorLib;
using ComputationLib;

namespace APACElib
{
    public class ExcelInterface : ExcelInteractor
    {
        // Reading settings from Excel  
        #region enums        
        public enum enumWhatToDo : int
        {
            Simulate = 1,
            Calibrate = 2,
            OptimizeTheDynamicPolicy = 3,
            Optimize = 4,
            RunExperiments = 5,
            SimulateDecisionMakingDuringEpidemics = 6,
        }        
        public enum EnumParamsColumns : int
        {
            ID = 1,
            Name = 2,
            UpdateAtEachTimeStep = 3,
            Distribution = 4,
            Par1 = 5,
            Par2 = 6,
            Par3 = 7,
            Par4 = 8,
            IncludedInCalibration = 9,
        }
        public enum enumClassColumns : int
        {
            ID = 1,
            Name = 2,
            ClassType = 3,

            InitialMembers = 5,
            EmptyToEradicate = 6,

            SusceptibilityParID = 8,
            InfectivityParID = 9,
            RowInContactMatrix = 10,

            ParIDForProbOfSuccess = 12,            
            ResourceIDToCheckAvailability = 13,
            DestinationClassIDIfSuccess = 14,
            DestinationClassIDIfFailure = 15,

            IfCollectOutcomes = 17,
            ParID_DALYPerNewMember = 18,
            ParID_CostPerNewMember = 19,
            ParID_DisableWeightPerUnitOfTime = 20,
            ParID_CostPerUnitOfTime = 21,
            ResourceUnitsConsumedPerArrival = 22,

            CollectAccumIncidenceStats = 24,
            CollectPrevalenceStats = 25,

            ShowIncidence = 27,
            ShowPrevalence = 28,
            ShowAccumIncidence = 29,
        }
        public enum enumEventColumns : int
        {
            ID = 1,
            Name = 2,
            EventType = 3,

            IDOfActiviatingIntervention = 5,
            IDOfGeneratingPathogen = 6,
            IDOfRateParameter = 7,
            IDOfDestinationClass = 8,
        }
        public enum enumInterventionColumns : int
        {
            ID = 1,
            Name = 2,
            Type = 3, 
            MutuallyExclusiveGroup = 4, 
            AffectingContactPattern = 5,

            TimeBecomesAvailable = 7,
            TimeBecomesUnavailableTo = 8,
            DelayParID = 9,
            ResourceID = 10,            

            FixedCost = 12,
            CostPerUnitOfTime = 13,
            PenaltyOfSwitchingFromOnToOff = 14,       
            InterestRate = 15,

            OnOffSwitchSetting =17,                        

            PreDeterminedEmployment_SwitchValue = 19,

            PeriodicEmployment_Periodicity = 21,
            PeriodicEmployment_Length = 22, 

            ThresholdBased_ConditionIDToTurnOn = 24,
            ThresholdBased_ConditionIDToTurnOff = 25,

            IntervalBasedOptimizationSettings_AvailableUpToTime = 27,
            IntervalBasedOptimizationSettings_MinNumOfDecisionPeriodsToUse = 28,

            SelectOnOffStatusAsFeature = 30,
            PreviousObservationPeriodToObserveValue = 31,
            UseNumOfDecisionPeriodEmployedAsFeature = 32,
            RemainsOnOnceSwitchedOn = 33
        }
        public enum enumResourceColumns : int
        {
            ID = 1,
            Name = 2,
            PricePerUnit = 3,
            ReplenishmentType = 4,

            FirstTimeAvailable_parID = 6,
            ReplenishmentQuantity_parID = 7,
            ReplenishmentInterval_parID = 8,

            ShowAvailableUnits = 10,
            SelectAsFeature = 11,
        }
        public enum enumResourceRuleColumns : int
        {
            ID = 1,
            Name = 2,
            ResourceRuleType = 3,
            ResourceID = 4,
            UnitsConsumedPerArrival = 5,
            UnitsConsumedPerUnitOfTime = 6,

            ClassIDIfSatisfied = 8,
            ClassIDIfNotSatisfied = 9,

            EventIDIfSatisfied = 11,
            EventIDIfNotSatisfied = 12,
        }
        public enum enumSpecialStatisticsColumns : int
        {
            ID = 1,
            Name = 2,

            DefinedOn = 4,
            Type = 5,
            Formula = 6,

            IfDisplay = 8,
            IfCollectOutcomes = 9,
            ParID_DALYPerNewMember = 10,
            ParID_CostPerNewMember = 11,

            SurveillanceDataAvailable = 13,
            NumOfDeltaTsDelayed = 14,
            NoiseDenominator = 15,
            FirstObservationMarksTheStartOfTheSpread = 16,  
            
            IfIncludedInCalibration = 18,
            MeasureOfFit = 19,
            Likelihood = 20,
            LikelihoodParam = 21,
            Weight_FourierCosine = 22,
            Weight_FourierEuclidean = 23,
            Weight_FourierAverage = 24,
            Weight_FourierStDev = 25,
            Weight_FourierMin = 26,
            Weight_FourierMax = 27,

            IfCheckWithinFeasibleRange = 28,
            FeasibleRange_minimum = 29,
            FeasibleRange_maximum = 30,
            FeasibleRange_hitMinimumThreshold = 31,

            NewMember_FeatureType = 33,
            NewMember_NumOfPastObsPeriodsToStore = 34   ,
        }        
        public enum enumFeaturesColumns : int
        {
            ID = 1,
            Name = 2, 
            FeatureObservation = 3,

            SpecialStatID = 5,
            SpecialStatFeatureType = 6, 
            Par = 7, 

            InterventionID = 9,
            InterventionFeatureType = 10
        }
        public enum enumConditionsColumns : int
        {
            ID = 1,
            Name = 2,
            DefinedOn = 3,

            FeatureIDs = 5,
            FeatureSigns = 6, 
            FeatureThresholds = 7,
            FeatureConclusion = 8, 

            Conditions = 10,
            ConditionsConclusions = 11

        }
        public enum enumSimulationStatisticsColumns : int
        {
            Name = 1,
            Mean = 2,
            StDev = 3,
            StError = 4,
        }
        public enum EnumSimStatsRows : int
        {
            TotalDALY = 1,
            TotalCost = 2,
            AnnualCost = 3,
            NHB = 4,
            NMB = 5,
            NumOfSwitches = 6,
            LengthOfEpidemic = 7,
        }       
        public enum enumObservationsColumns : int
        {
            ObservationPeriod = 2,
            InterventionID = 3,
            FirstObservedOutcome = 4,
        }
        public enum EnumCalibrationColumns : int
        {
            SimulationItr = 2,
            RandomSeed = 3,
            GoodnessOfFit_overal = 4,
        }
        // ADP parameter sensitivity analysis
        public enum enumSimulationOutcomesOffsets : int
        {
            E_ObjectiveFunction = 0,
            stDev_ObjectiveFunction = 1,
            E_health = 2,
            stDev_health = 3,
            E_cost = 4,
            stDev_cost = 5,
        }
        public enum enumStaticPolicyOptimizationSimulationIterationsOffsets : int
        {
            wtpForHealth = 0,
            objectiveFunction = 1,
            health = 2,
            cost = 3,
            annualCost = 4,
        }
        public enum enumADPWTPVariableType : int
        {
            Discrete = 0,
            Continuous = 1,
        }
        public enum enumADPParameterDesignOffsets : int
        {
            wtpForHealth = 0,
            harmonic_a = 1,
            epsilonGreedy_beta = 2,
        }
        public enum enumADPSASimulationIterationsOffsets : int
        {
            wtpForHealth = 0,
            harmonic_a = 1,
            epsilonGreedy_beta = 2,
            objectiveFunction = 3,
            health = 4,
            cost = 5,            
            annualCost = 6,
        }
        public enum enumSimulatingRealTimeDecisionMakingOffsets : int
        {
            Iteration = 0,
            RNDSeeds = 1,
            WTPForHEalth = 2,
            ObjFunctionStaticPolicy = 3,
            ObjFunctionDynamicPolicy = 4,
        }
        public enum enumPoliciesToSimulateInRealTime : int
        {
            Static = 1,
            Dynamic = 2,
            StaticAndDynamic = 3,
        }
        public enum enumWhenToCalibrate : int
        {
            OnceFirstTimeDecisionsShouldBeMade = 1,
            OnEveryTimeDecisionsShouldBeMade = 2,
            WhenModelIsUntunedWhenDecisionsShouldBeMade = 3,
        }
        #endregion

        private bool _makeSimulationOutputHeader = true;

        // Instantiation
        // connect to the excel file
        public void ConnectToExcelInterface()
        {
            // open an existing excel file and make it invisible
            base.OpenAnExistingExcelFile(false);
        }

        #region methods to get model settings in tables 
        // get table of sheets
        public Array GetTableOfParameters()
        {
            return GetTableOfCells("Parameters", "randomParameterBase", (int)EnumParamsColumns.IncludedInCalibration);
        }
        public Array GetTableOfPathogens()
        {
            return GetTableOfCells("Pathogens", "pathogenBase", 2);
        }
        public Array GetTableOfClasses()
        {
            return GetTableOfCells("Classes", "classesBase", (int)enumClassColumns.ShowAccumIncidence);
        }
        public Array GetTableOfInterventions()
        {
            return GetTableOfCells("Interventions", "interventionsBase", (int)enumInterventionColumns.RemainsOnOnceSwitchedOn);
        }
        public Array GetTableOfResources()
        {
            return GetTableOfCells("Resources", "resourceBase", (int)enumResourceColumns.SelectAsFeature);
        }
        public Array GetTableOfEvents()
        {
            return GetTableOfCells("Events", "eventsBase", (int)enumEventColumns.IDOfDestinationClass);
        }
        public Array GetTableOfObservedHistory(int numOfCalibrationTargets)
        {
            return GetTableOfCells("Epidemic History", "epidemicHistoryBase", 2+ 2*numOfCalibrationTargets);
        }
        //public Array GetTableOfResourceRules()
        //{
        //    return GetTableOfCells("Resource Rules", "resourceRuleBase", (int)enumSpecialStatisticsColumns.ShowAccumulatedNewMembers);
        //}
        public Array GetTableOfSummationStatistics()
        {
            return GetTableOfCells("Special Statistics", "baseSummationStatistics", (int)enumSpecialStatisticsColumns.NewMember_NumOfPastObsPeriodsToStore);
        }
        public Array GetTableOfRatioStatistics()
        {
            return GetTableOfCells("Special Statistics", "baseRatioStatistics", (int)enumSpecialStatisticsColumns.NewMember_NumOfPastObsPeriodsToStore);
        }        
        public int[,] GetConnectionsMatrix()
        {
            base.ActivateSheet("Connections");
            return SupportFunctions.ConvertMatrixToInt(base.ReadMatrixFromActiveSheet(5, 3, 4));
        }
        public Array GetTableOfFeatures()
        {
            return GetTableOfCells("Features", "baseFeatures", (int)enumFeaturesColumns.InterventionFeatureType);
        }
        public Array GetTableOfConditions()
        {
            return GetTableOfCells("Conditions", "baseConditions", (int)enumConditionsColumns.ConditionsConclusions);
        }


        #endregion

        #region general and simulation settings
        public EnumMarkOfEpidemicStartTime GetMarkOfEpidemicStartTime()
        {
            EnumMarkOfEpidemicStartTime markOfEpidemicStartTime = EnumMarkOfEpidemicStartTime.TimeZero;
            switch (GetCellValue("General Settings", "markOfEpidemicStartTime").ToString())
            {
                case "Time 0":
                    markOfEpidemicStartTime = EnumMarkOfEpidemicStartTime.TimeZero;
                    break;
                case "Time of First Observation":
                    markOfEpidemicStartTime = EnumMarkOfEpidemicStartTime.TimeOfFirstObservation;
                    break;
            }
            return markOfEpidemicStartTime;
        }
        public enumWhatToDo GetWhatToDo()
        {
            enumWhatToDo whatToDo = enumWhatToDo.Simulate;
            switch (GetCellValue("Support", "whatToDo").ToString())
            {
                case "Simulate":
                    whatToDo = enumWhatToDo.Simulate;
                    break;
                case "Calibrate":
                    whatToDo = enumWhatToDo.Calibrate;
                    break;
                case "Optimize the Dynamic Policy":
                    whatToDo = enumWhatToDo.OptimizeTheDynamicPolicy;
                    break;
                case "Optimize":
                    whatToDo = enumWhatToDo.Optimize;
                    break;
                case "Run experiments":
                    whatToDo = enumWhatToDo.RunExperiments;
                    break;
                case "Simulate decision making during epidemics":
                    whatToDo = enumWhatToDo.SimulateDecisionMakingDuringEpidemics;
                    break;
            }
            return whatToDo;
        }
        public bool IsUsingParallelComputing()
        {
            return SupportFunctions.ConvertYesNoToBool(GetCellValue("General Settings", "useParallelComputing").ToString());
        }
        public int GetMaxDegreeOfParallelism()
        {
            return (int)(double)GetCellValue("General Settings", "maxDegreeOfParallelism");
        }
        public int GetFirstRNGSeed()
        {
            return (int)(double)GetCellValue("General Settings", "firstRNGSeed");
        }
        public int GetScenarioSeed()
        {
            return (int)(double)GetCellValue("General Settings", "scenarioSeed");
        }
        public double GetEpiTimeIndexToChangeSeed()
        {
            return (double)GetCellValue("General Settings", "epiTimeChangeSeed");
        }
        public int GetNumDeltaTSimulationOutputInterval()
        {
            return (int)(double)GetCellValue("General Settings", "nDeltaTSimOutputInterval");
        }
        public bool GetIfToShowSimulationTrajectories()
        {
            return SupportFunctions.ConvertYesNoToBool(GetCellValue("General Settings", "showSimTrajectories").ToString());
        }
        public int GetNumDeltaTObservationPeriod()
        {
            return (int)(double)GetCellValue("General Settings", "nDeltaTObsPeriod");
        }
        public double GetAnnualInterestRate()
        {
            return (double)GetCellValue("General Settings", "annualInterestRate");
        }
        public double GetWTPForHealth()
        {
            return (double)GetCellValue("General Settings", "WTPForHealth");
        }
                
        public int GetNumberOfSimulationIterations()
        {
            return (int)(double)GetCellValue("General Settings", "numOfIterations");
        }
        public int GetNumberOfSeedsToRead()
        {
            return (int)(double)GetCellValue("General Settings", "numSeedsToRead");
        }
        public double GetTimeStep()
        {
            return (double)GetCellValue("General Settings", "timeStep");
        }
        public int GetNumDeltaTDecisionInterval()
        {
            return (int)(double)GetCellValue("General Settings", "nDeltaTDecisionInterval");
        }
        public double GetWarmUpPeriod()
        {
            return (double)GetCellValue("General Settings", "warmUpPeriod");
        }
        public double GetTimeToStop()
        {
            return (double)GetCellValue("General Settings", "timeToStop");
        }
        public int GetEpidemicStartTimeParID()
        {
            return (int)(double)GetCellValue("General Settings", "epidemicStartTimeParID");
        }
        public double GetEpidemicConditionTime()
        {
            return (double)GetCellValue("General Settings", "epidemicConditionTime");
        }
        public double GetTimeToStartDecisionMaking()
        {
            return (double)GetCellValue("General Settings", "timeToStartDecisionMaking");
        }

        #endregion
               
        
        #region dynamic policy optimization settings
        public int GetNumOfADPIterations()
        {
            return (int)(double)GetCellValue("General Settings", "numOfADPIterations");
        }
        public int GetNumOfSimulationRunsToBackPropogate()
        {
            return (int)(double)GetCellValue("General Settings", "numOfSimRunsToBackPropogate");
        }
        public int GetNumOfPrespecifiedRNDSeedsToUse()
        {
            return (int)(double)GetCellValue("General Settings", "numOfPrespecifiedRNDSeedsToUse");            
        }

        public string GetQFunctionApproximationMethod()
        {
            return GetCellValue("General Settings", "qFunctionApproximationMethod").ToString();
        }
        public string GetQFunctionApproximationModel()
        {
            return GetCellValue("General Settings", "qFunctionApproximationModel").ToString();
        }
        public bool GetIfEpidemicTimeIsUsedAsFeature()
        {
            return SupportFunctions.ConvertYesNoToBool(GetCellValue("General Settings", "useEpidemicTimeAsFeature").ToString());
        }
        public int GetPastDecisionPeriodWithDecisionAsFeature()
        {
            return (int)(double)GetCellValue("General Settings", "pastDecisionPeriodWithFeatureDecision");
        }

        public bool GetIfToUseCurrentQFunctionApproximationSettings()
        {
            return SupportFunctions.ConvertYesNoToBool(GetCellValue("General Settings", "useCurrentQFunctionApproximationSettings").ToString());
        }
        public int GetDegreeOfPolynomialQFunction()
        {
            return (int)(double)GetCellValue("General Settings", "degreeOfPolynomialQFunction");
        }
        public double GetL2RegularizationPenalty()
        {
            return (double)GetCellValue("General Settings", "L2RegularizationPenalty");
        }
        public int GetNumOfHiddenNeurons()
        {
            return (int)(double)GetCellValue("General Settings", "numOfNeurons");
        }
        public double GetHarmonicRule_a()
        {
            return (double)GetCellValue("General Settings", "harmonicRule_a");
        }
        public double GetHarmonicRule_a_min()
        {
            return (double)GetCellValue("General Settings", "harmonicRule_a_min");
        }
        public double GetHarmonicRule_a_max()
        {
            return (double)GetCellValue("General Settings", "harmonicRule_a_max");
        }
        public double GetHarmonicRule_a_step()
        {
            return (double)GetCellValue("General Settings", "harmonicRule_a_step");
        }
        public double GetEpsilonGreedy_beta()
        {
            return (double)GetCellValue("General Settings", "epsilonGreedyBeta");
        }
        public double GetEpsilonGreedy_beta_min()
        {
            return (double)GetCellValue("General Settings", "epsilonGreedyBeta_min");
        }
        public double GetEpsilonGreedy_beta_max()
        {
            return (double)GetCellValue("General Settings", "epsilonGreedyBeta_max");
        }
        public double GetEpsilonGreedy_beta_step()
        {
            return (double)GetCellValue("General Settings", "epsilonGreedyBeta_step");
        }
        public double GetEpsilonGreedy_delta()
        {
            return (double)GetCellValue("General Settings", "epsilonGreedyDelta");
        }
        public bool GetIfToShowOptimalPolicy()
        {
            return SupportFunctions.ConvertYesNoToBool(GetCellValue("General Settings", "showOptimalPolicy").ToString());
        }
        public int GetnumOfIntervalsToDescretizeFeatures()
        {
            return (int)(double)GetCellValue("General Settings", "numOfIntervalsToDescretizeFeatures");
        }
        #endregion

        #region calibration settings

        public string GetCalibrationMethod()
        {
            return GetCellValue("General Settings", "calibrationMethod").ToString();
        }

        public int GetNumOfTrajsInParallelForCalibr()
        {
            return (int)(double)GetCellValue("General Settings", "numOfParallelSimsForCalibration");
        }
        public int GetNumOfTrajsToSimForCalibr()
        {
            return (int)(double)GetCellValue("General Settings", "numOfFeasibleTrajsForCalibration");
        }

        public bool GetIfUseCurrentHistoryToCalibr()
        {
            return SupportFunctions.ConvertYesNoToBool(GetCellValue("General Settings", "useCurrentObservationsToCalibrate").ToString());
        }
        public string[] GetPrespecifiedSequenceOfInterventions()
        {
            base.ActivateSheet("Epidemic History");
            return base.ReadStringRangeFromActiveSheet(base.RowIndex("epidemicHistoryBase") + 2, (int)ExcelInterface.enumObservationsColumns.InterventionID, ExcelInteractorLib.ExcelInteractor.enumRangeDirection.DownEnd);
        }
        public double[,] GetObservationMatrix(int numOfObservableOutcomes)
        {
            base.ActivateSheet("Epidemic History");
            return base.ReadMatrixFromActiveSheet(3, 
                (int)ExcelInterface.enumObservationsColumns.FirstObservedOutcome,
                (int)ExcelInterface.enumObservationsColumns.FirstObservedOutcome + numOfObservableOutcomes - 1);
        }

        public double[,] GetMatrixOfObservationsAndWeights(int numOfInitialObsToRemove, int numOfCalibrationTargets)
        {
            base.ActivateSheet("Epidemic History");

            double[,] matrixOfObservationsAndWeigths = base.ReadMatrixFromActiveSheet(base.RowIndex("epidemicHistoryBase") + 1 + numOfInitialObsToRemove,
                (int)ExcelInterface.enumObservationsColumns.FirstObservedOutcome,
                (int)ExcelInterface.enumObservationsColumns.FirstObservedOutcome + 2 * numOfCalibrationTargets - 1);

            return matrixOfObservationsAndWeigths;
        }
        public void GetObservationAndWeightMatrices(
            int numOfTargetsWithMatrixNormAsMeasureOfFit,int numOfTargetsWithFourierCoefficientAsMeasureOfFit, int numOfInitialObsToRemove,
            ref double[,] matrixOfTargetObservations_matrixNormAsMeasureOfFit, ref double[,] matrixOfObservationWeights_matrixNormAsMeasureOfFit,
            ref double[,] matrixOfTargetObservations_fourierCoefficientsAsMeasureOfFit, ref double[,] matrixOfObservationWeights_fourierCoefficientsAsMeasureOfFit)
        {
            base.ActivateSheet("Epidemic History");

            double[,] obsAndWeightMatrices = base.ReadMatrixFromActiveSheet(base.RowIndex("epidemicHistoryBase") + 1 + numOfInitialObsToRemove,
                (int)ExcelInterface.enumObservationsColumns.FirstObservedOutcome,
                (int)ExcelInterface.enumObservationsColumns.FirstObservedOutcome + 2 * (numOfTargetsWithMatrixNormAsMeasureOfFit + numOfTargetsWithFourierCoefficientAsMeasureOfFit) - 1);

            int numOfRows = obsAndWeightMatrices.GetLength(0);
            matrixOfTargetObservations_matrixNormAsMeasureOfFit = new double[numOfRows, numOfTargetsWithMatrixNormAsMeasureOfFit];
            matrixOfObservationWeights_matrixNormAsMeasureOfFit = new double[numOfRows, numOfTargetsWithMatrixNormAsMeasureOfFit];
            matrixOfTargetObservations_fourierCoefficientsAsMeasureOfFit = new double[numOfRows, numOfTargetsWithFourierCoefficientAsMeasureOfFit];
            matrixOfObservationWeights_fourierCoefficientsAsMeasureOfFit = new double[numOfRows, numOfTargetsWithFourierCoefficientAsMeasureOfFit];

            for (int obsIndex = 0; obsIndex < numOfTargetsWithMatrixNormAsMeasureOfFit; obsIndex++)
            {
                for (int rowIndex = 0; rowIndex < numOfRows; rowIndex++)
                {
                    matrixOfTargetObservations_matrixNormAsMeasureOfFit[rowIndex, obsIndex] = obsAndWeightMatrices[rowIndex, 2 * obsIndex];
                    matrixOfObservationWeights_matrixNormAsMeasureOfFit[rowIndex, obsIndex] = obsAndWeightMatrices[rowIndex, 2 * obsIndex + 1];
                }
            }

            for (int obsIndex = numOfTargetsWithMatrixNormAsMeasureOfFit; obsIndex < numOfTargetsWithMatrixNormAsMeasureOfFit + numOfTargetsWithFourierCoefficientAsMeasureOfFit; obsIndex++)
            {
                for (int rowIndex = 0; rowIndex < numOfRows; rowIndex++)
                {
                    matrixOfTargetObservations_fourierCoefficientsAsMeasureOfFit[rowIndex, obsIndex - numOfTargetsWithMatrixNormAsMeasureOfFit] = obsAndWeightMatrices[rowIndex, 2 * obsIndex];
                    matrixOfObservationWeights_fourierCoefficientsAsMeasureOfFit[rowIndex, obsIndex - numOfTargetsWithMatrixNormAsMeasureOfFit] = obsAndWeightMatrices[rowIndex, 2 * obsIndex + 1];
                }
            }
            
        }

        #endregion

        #region experimental designs

        // get experimental design matrix
        public double[,] GetExperimentalDesignMatrix()
        {
            base.ActivateSheet("Experimental Designs");
            // read designs
            int firstRow = base.RowIndex("baseExperimentalDesigns") + 1;            
            int firstCol = base.ColIndex("baseExperimentalDesigns") + 1;
            //int lastRow = base.LastRowWithDataInThisColumn(base.ColIndex("baseExperimentalDesigns") + 1);
            int lastCol = base.ColIndex("baseExperimentalDesignsVariableValues", enumRangeDirection.RightEnd);
            //int lastCol = base.ColIndex(firstRow, firstCol, enumRangeDirection.RightEnd);
            // return the matrix
            return base.ReadMatrixFromActiveSheet(firstRow, firstCol, lastCol);
        }
        // write back a design
        public void WriteADesign(double[] design)
        {
            base.ActivateSheet("Experimental Designs");
            base.WriteToRow(design, "baseExperimentalDesignsVariableValues", 1, 1);
        }
        #endregion

        // get decision rule
        public EnumEpiDecisions GetDecisionRule()
        {
            EnumEpiDecisions thisDecisionRule = EnumEpiDecisions.SpecifiedByPolicy;
            switch (GetCellValue("General Settings", "decisionRule").ToString())
            {
                case "Specified by Policy":
                    thisDecisionRule = EnumEpiDecisions.SpecifiedByPolicy;
                    break;
                case "Predetermined Sequence":
                    thisDecisionRule = EnumEpiDecisions.PredeterminedSequence;
                    break;               
            }
            return thisDecisionRule;
        }
        // get random seeds
        public EnumSimRNDSeedsSource GetSimulationRNDSeedsSource()
        {
            EnumSimRNDSeedsSource simulationRNDSeedsSource = EnumSimRNDSeedsSource.StartFrom0;
            switch (GetCellValue("General Settings", "sourceOfRNGSeeds").ToString())
            {
                case "Start from 0":
                    simulationRNDSeedsSource = EnumSimRNDSeedsSource.StartFrom0;
                    break;
                case "Prespecified Sequence":
                    simulationRNDSeedsSource = EnumSimRNDSeedsSource.Prespecified;
                    break;
                case "Random Unweighted":
                    simulationRNDSeedsSource = EnumSimRNDSeedsSource.RandomUnweighted;
                    break;
                case "Random Weighted":
                    simulationRNDSeedsSource = EnumSimRNDSeedsSource.RandomWeighted;
                    break;
            }
            return simulationRNDSeedsSource;
        }
        // get objective function
        public EnumObjectiveFunction GetObjectiveFunction()
        {
            EnumObjectiveFunction thisObjectiveFunction = EnumObjectiveFunction.MaximizeNHB;
            string strObjectiveFunction = GetCellValue("General Settings", "objectiveFunction").ToString();

            switch (strObjectiveFunction)
            {
                case "Net Monetary Benefit":
                    thisObjectiveFunction = EnumObjectiveFunction.MaximizeNMB;
                    break;
                case "Net Health Benefit":
                    thisObjectiveFunction = EnumObjectiveFunction.MaximizeNHB;
                    break;
            }
            return thisObjectiveFunction;
        }
        // get base contact matrix and matrix of percentage changes for other interventions
        public void GetBaseAndPercentageChangeContactMatrix
            (int numOfInterventionsAffectingContactPattern, int numOfPathogens,
            ref double[][,] baseContactMatrix, ref int[][][,] percentChangeInContactMatricesParIDs)
        {
            int sizeOfContactMatrix = 0;
            baseContactMatrix = new double[numOfPathogens][,];
            percentChangeInContactMatricesParIDs = new int[numOfInterventionsAffectingContactPattern][][,];

            base.ActivateSheet("Base Contact Matrices");
            int rowIndex = base.RowIndex("baseContactMatrix");
            int colIndex = base.ColIndex("baseContactMatrix");
            int rowOffset = 0;
            // for each interventions
            for (int intID = 0; intID <= numOfInterventionsAffectingContactPattern; intID++)
            {
                if (intID > 0)
                    percentChangeInContactMatricesParIDs[intID - 1] = new int[numOfPathogens][,];
                if (intID == 1)
                {
                    base.ActivateSheet("Additive Contact Matrices");
                    rowIndex = base.RowIndex("baseAdditiveContactMatrix");
                    colIndex = base.ColIndex("baseAdditiveContactMatrix");
                    rowOffset = 0;
                }

                // read this matrix                
                // for each pathogen
                for (int pathogenID = 0; pathogenID < numOfPathogens; pathogenID++)
                {
                    rowIndex += rowOffset;
                    // check if this is the default intervention                        
                    if (intID == 0)
                    {
                        #region default intervention
                        // read the matrix
                        baseContactMatrix[pathogenID] = GetTableEntities("Base Contact Matrices", rowIndex, colIndex); //(double[,])base.ReadMatrixFromActiveSheet(rowIndex + 1, colIndex + 1)
                        // increment the row index
                        sizeOfContactMatrix = baseContactMatrix[pathogenID].GetLength(0);
                        rowIndex += sizeOfContactMatrix + 1;
                        #endregion
                    }
                    else
                    {
                        #region non-default intervention
                        // read the matrix
                        double[,] thisPercentChangeInContactMatricesParIDs = GetTableEntities("Additive Contact Matrices", rowIndex, colIndex);
                        // resize the % change matrix
                        percentChangeInContactMatricesParIDs[intID - 1][pathogenID] = new int[sizeOfContactMatrix, sizeOfContactMatrix];
                        // convert                         
                        for (int i = 0; i < sizeOfContactMatrix; ++i)
                            for (int j = 0; j < sizeOfContactMatrix; ++j)
                                percentChangeInContactMatricesParIDs[intID - 1][pathogenID][i, j] = (int)thisPercentChangeInContactMatricesParIDs[i, j];

                        rowIndex += sizeOfContactMatrix + 1;
                        #endregion
                    }
                    // find the row index of next matrix  
                    #region find next matrix
                    bool found = false;
                    while (!found && rowOffset < 100)
                    {
                        if (base.ReadCellFromActiveSheet(rowIndex + rowOffset, colIndex).ToString() != "")
                        {
                            found = true;
                            --rowOffset;
                        }
                        else
                            ++rowOffset;
                    }
                    #endregion

                } // next pathogenID
            } // next interventionID
        }
        //public void GetBaseAndPercentageChangeContactMatrix
        //    (int numOfInterventions, int numOfPathogens, 
        //    ref double[][,] baseContactMatrix, ref int[][][,] percentChangeInContactMatricesParIDs)
        //{
        //    base.ActivateSheet("Contact Matrices");

        //    int rowIndex = base.RowIndex("baseContactMatrix");
        //    int colIndex = base.ColIndex("baseContactMatrix");
        //    int sizeOfContactMatrix = 0;
        //    baseContactMatrix = new double[numOfPathogens][,];
        //    percentChangeInContactMatricesParIDs = new int[numOfInterventions - 1][][,];

        //    int rowOffset = 0;
        //    // for each interventions
        //    for (int interventionID = 0; interventionID < numOfInterventions; interventionID++)
        //    {                
        //        if (interventionID >0)
        //            percentChangeInContactMatricesParIDs[interventionID - 1] = new int[numOfPathogens][,];

        //        // read this matrix                
        //        // for each pathogen
        //        for (int pathogenID = 0; pathogenID < numOfPathogens; pathogenID++)
        //        {
        //            rowIndex += rowOffset;
        //            // check if this is the default intervention                        
        //            if (interventionID == 0)
        //            {
        //                #region default intervention
        //                // read the matrix
        //                baseContactMatrix[pathogenID] = GetTableEntities("Contact Matrices", rowIndex, colIndex); //(double[,])base.ReadMatrixFromActiveSheet(rowIndex + 1, colIndex + 1)
        //                // increment the row index
        //                sizeOfContactMatrix = baseContactMatrix[pathogenID].GetLength(0);
        //                rowIndex += sizeOfContactMatrix + 1;                        
        //                #endregion
        //            }                        
        //            else
        //            {
        //                #region non-default intervention
        //                // read the matrix
        //                double[,] thisPercentChangeInContactMatricesParIDs = GetTableEntities("Contact Matrices", rowIndex, colIndex);
        //                // resize the % change matrix
        //                percentChangeInContactMatricesParIDs[interventionID - 1][pathogenID] = new int[sizeOfContactMatrix, sizeOfContactMatrix];
        //                // convert                         
        //                for (int i = 0; i < sizeOfContactMatrix; ++i)
        //                    for (int j = 0; j < sizeOfContactMatrix; ++j)
        //                        percentChangeInContactMatricesParIDs[interventionID - 1][pathogenID][i, j] = (int)thisPercentChangeInContactMatricesParIDs[i, j];
               
        //                rowIndex += sizeOfContactMatrix + 1;
        //                #endregion
        //            }
        //            // find the row index of next matrix  
        //            #region find next matrix
        //            bool found = false;
        //            while (!found)
        //            {
        //                if (rowOffset >= 100)
        //                    return;
        //                else
        //                {
        //                    if (base.ReadCellFromActiveSheet(rowIndex + rowOffset, colIndex).ToString() != "")
        //                    {
        //                        found = true;
        //                        --rowOffset;
        //                    }
        //                    else
        //                        ++rowOffset;
        //                }
        //            }
        //            #endregion

        //        } // next pathogenID
        //    } // next interventionID
        //}
        // get RND seeds
        public int[] GetRNDSeeds(int n)
        {
            ActivateSheet("Calibration");
            return SupportFunctions.ConvertArrayToInt(
                //base.ReadRangeFromActiveSheet(3, (int)ExcelInterface.enumCalibrationColumns.RandomSeed, 2 + n)
                ReadRangeFromActiveSheet(
                    3, 
                    (int)EnumCalibrationColumns.RandomSeed,
                    2+n) //LastRowWithDataInThisColumn((int)EnumCalibrationColumns.RandomSeed)
                );
     
            //return SupportFunctions.ConvertArrayToInt(base.ReadRangeFromActiveSheet(3, (int)ExcelInterface.enumCalibrationColumns.RandomSeed, ExcelInteractorLib.ExcelInteractor.enumRangeDirection.DownEnd));
        }
        // get goodness of fit for RND seeds
        public double[] GetGoodnessOfFitForRNDSeeds(int n)
        {
            base.ActivateSheet("Calibration");
            return base.ReadRangeFromActiveSheet(3, (int)ExcelInterface.EnumCalibrationColumns.GoodnessOfFit_overal, 2 + n);
            //return base.ReadRangeFromActiveSheet(3, (int)ExcelInterface.enumCalibrationColumns.GoodnessOfFit_overal, ExcelInteractorLib.ExcelInteractor.enumRangeDirection.DownEnd);
        }
        // get Q-function coefficients estimates
        public double[] GetQFunctionCoefficients(int numOfFeatures)
        {
            return GetQFunctionCoefficients(numOfFeatures, 0);
        }
        // get Q-function coefficients initial values
        public double[] GetQFunctionCoefficientsInitialValues(int numOfFeatures)
        {
            return GetQFunctionCoefficients(numOfFeatures, 1);
        }
        // get Q-function coefficients (estimates or initial value)
        private double[] GetQFunctionCoefficients(int numOfFeatures, int enter0ToReadEstimates_2ToReadInitialValues)
        {            
            // activate feature sheet
            base.ActivateSheet("Q-Functions");
            int rowIndex = numOfFeatures + 8, colIndex = numOfFeatures + 5;

            return base.ReadRangeFromActiveSheet(rowIndex + 2, colIndex + enter0ToReadEstimates_2ToReadInitialValues, ExcelInteractor.enumRangeDirection.DownEnd);
        }
        // get Q-function coefficients (estimates or initial value
        private double[][] GetQFunctionCoefficients(int numOfDecisions, int numOfFeatures, int enter0ToReadEstimates_2ToReadInitialValues)
        {
            double[][] qFunctionCoefficients = new double[0][];
            
            // activate feature sheet
            base.ActivateSheet("Q-Functions");
            int rowIndex = numOfFeatures + 8, colIndex = numOfFeatures + 5;
            
            double[] arrCoefficients;
            double[][] coefficients = new double[1][];

            // read eastimates for each decision            
            for (int decisionIndex = 0; decisionIndex < numOfDecisions; ++decisionIndex)
            {
                // read coefficient
                arrCoefficients = base.ReadRangeFromActiveSheet(rowIndex + 2, colIndex + enter0ToReadEstimates_2ToReadInitialValues, ExcelInteractor.enumRangeDirection.DownEnd);
                coefficients[0] = new double[arrCoefficients.Length];
                for (int i = 0; i < arrCoefficients.Length; i++)
                    coefficients[0][i] = arrCoefficients[i];
                // concatinate arrays
                qFunctionCoefficients = SupportFunctions.ConcatJaggedArray(qFunctionCoefficients, coefficients);                    

                // increment rowIndex
                rowIndex += arrCoefficients.Length + 5;
            }
            return qFunctionCoefficients;
        }

        // report results subs
        #region Report results subs

        // setup simulation output sheet
        public void SetupSimulationOutputSheet(
            string[] simIncidenceHeader,
            string[] simPrevalenceHeader,
            string[] obsIncidenceHeader,
            string[] obsPrevalenceHeader,
            string[] resouceOutputs)
        {
            ActivateSheet("Simulation Output");
            int rowIndex1 = 2;
            int colIndex1 = 1;
            int rowIndex2 = Math.Max(LastRowWithDataInThisColumn(1), 3);
            int colIndex2 = ColIndex("simulationOutput", enumRangeDirection.RightEnd) + 1; // LastColWithDataInThisRow(2); // ;

            //clear content and format
            if (_makeSimulationOutputHeader == true)
            {
                ClearContent(rowIndex1, colIndex1, rowIndex2, colIndex2);
                ClearBorders(rowIndex1, colIndex1, rowIndex2, colIndex2);
                _makeSimulationOutputHeader = false;
            }
            else
            {
                ClearContent(rowIndex1 + 1, colIndex1, rowIndex2, colIndex2);
                //base.ClearBorders(rowIndex1 + 1, colIndex1, rowIndex2, colIndex2);                
                return;
            }

            // report and format the heading
            int offSimRep           = 0;
            int offSimIncidence     = offSimRep + 1;
            int OffSimIntrvn = offSimIncidence + simIncidenceHeader.Length;
            int offSimPrevalence    = OffSimIntrvn + 1;
           

            int offObsRep           = offSimPrevalence + simPrevalenceHeader.Length;
            int offObsIncidence     = offObsRep + 1;
            int offObsIntrvn = offObsIncidence + obsIncidenceHeader.Length;
            int offObsPrevalence    = offObsIntrvn +1;
           

            // replication 
            WriteToCell("Replication", "simulationOutput",          0, offSimRep);
            // incidence
            WriteToRow(simIncidenceHeader, "simulationOutput",      0, offSimIncidence);
            AddABorder("simulationOutput",                          0, 0, enumRangeDirection.RightEnd, enumBorder.Right);
            // action code
            WriteToCell("Code of Next Action", "simulationOutput", 0, OffSimIntrvn);
            // prevalence 
            WriteToRow(simPrevalenceHeader, "simulationOutput",     0, offSimPrevalence);
            AddABorder("simulationOutput",                          0, 0, enumRangeDirection.RightEnd, enumBorder.Right);
            
            AddABorder("simulationOutput", 0, 0, enumRangeDirection.RightEnd, enumBorder.Right);

            // replication 
            WriteToCell("Replication", "simulationOutput", 0, offObsRep);
            // incidence
            WriteToRow(obsIncidenceHeader, "simulationOutput", 0, offObsIncidence);
            AddABorder("simulationOutput", 0, 0, enumRangeDirection.RightEnd, enumBorder.Right);
            // action code
            WriteToCell("Obs: Code of Next Action", "simulationOutput", 0, offObsIntrvn);
            AddABorder("simulationOutput", 0, 0, enumRangeDirection.RightEnd, enumBorder.Right);
            // prevalence 
            WriteToRow(obsPrevalenceHeader, "simulationOutput", 0, offObsPrevalence);
            AddABorder("simulationOutput", 0, 0, enumRangeDirection.RightEnd, enumBorder.Right);           

            // final formating
            AddABorder("simulationOutput", 0, 0, enumRangeDirection.RightEnd, enumBorder.Bottom);
            Align("simulationOutput", 0, 0, enumRangeDirection.RightEnd, enumAlignment.Center);
            WrapText("simulationOutput", 0, 0, enumRangeDirection.RightEnd);

            //colIndex2 = simIncidenceHeader.Length + obsIncidenceHeader.Length + simPrevalenceHeader.Length;
            //AddABorder(rowIndex1, colIndex1, rowIndex1, colIndex2, enumBorder.Bottom);
            //Align(rowIndex1, colIndex1, rowIndex1, colIndex2, enumAlignment.Center);
            //WrapText(rowIndex1, colIndex1, rowIndex1, colIndex2);
        }
        // report trajectories
        public void ReportEpidemicTrajectories(
            int[,]      simRepIndeces,
            double[,]   simIncidenceOutputs,
            double[,]   simPrevalenceOutputs, 
            string[]    simIntrvnCombinationCodes,
            int[,]      obsRepIndeces,
            double[,]   obsIncidenceOutputs,
            double[,]   obsPrevalenceOutputs,
            string[]    obsIntrvnCombinationCodes)
            //double[,] arrSimulationObservableOutputs, 
            //double[,] arrSimulationResourceAvailabilityOutput)
        {
            ActivateSheet("Simulation Output");
            // find the final row
            int finalRow = base.LastRowWithDataInThisColumn(1);

            int colOfSimRepIndeces          = 1;
            int colOfSimIncidenceOutputs    = colOfSimRepIndeces + 1;
            int colOfSimIntrvnCombination = colOfSimIncidenceOutputs + simIncidenceOutputs.GetLength(1);
            int colOfSimPrevalenceOutputs   = colOfSimIntrvnCombination + 1;
           

            WriteToMatrix(simRepIndeces,                finalRow + 1, colOfSimRepIndeces);
            WriteToMatrix(simIncidenceOutputs,          finalRow + 1, colOfSimIncidenceOutputs);
            WriteToColumn(simIntrvnCombinationCodes, finalRow + 1, colOfSimIntrvnCombination);
            WriteToMatrix(simPrevalenceOutputs,         finalRow + 1, colOfSimPrevalenceOutputs);
           

            int colOfObsRepIndeces          = colOfSimPrevalenceOutputs + simPrevalenceOutputs.GetLength(1);
            int colOfObsIncidenceOutputs    = colOfObsRepIndeces + obsRepIndeces.GetLength(1);
            int colOfObsIntrvnCombination = colOfObsIncidenceOutputs + obsIncidenceOutputs.GetLength(1);
            int colOfObsPrevalenceOutputs   = colOfObsIntrvnCombination + 1;
            

            WriteToMatrix(obsRepIndeces,                finalRow + 1, colOfObsRepIndeces);
            WriteToMatrix(obsIncidenceOutputs,          finalRow + 1, colOfObsIncidenceOutputs);
            WriteToColumn(obsIntrvnCombinationCodes, finalRow + 1, colOfObsIntrvnCombination);
            WriteToMatrix(obsPrevalenceOutputs,         finalRow + 1, colOfObsPrevalenceOutputs);
            
            //base.AddABorder(finalRow + 1, colOfSimIncidenceOutputs + simIncidenceOutputs.GetLength(1), enumRangeDirection.DownEnd, enumBorder.Left);

        }
        // report simulation statistics
        public void ReportSimulationStatistics(
            string[] strSummaryStatistics, double[,] summaryStatistics,
            string[] strClassAndSumStatistics, double[,] classAndSumStatistics,
            string[] strRatioStatistics, double[,] ratioStatistics,
            string[] strInterventionUtilizationStatistics, double[,] interventionUtilizationStatistics,
            string[] strComputationStatistics, double[,] computationStatistics,
            string[] strSimulationIterationStatistics, double[,] simulationIterationStatistics
            )
        {
            base.ActivateSheet("Simulation Statistics");
            // make header
            int rowIndex = 1;
            int colIndex = 1;

            //clear content and format
            //base.ClearContent(rowIndex, colIndex, (int)base.LastRowWithData(), (int)enumSimulationStatisticsColumns.StError + 2);
            //base.ClearFormat(rowIndex, colIndex, (int)base.LastRowWithData(), (int)enumSimulationStatisticsColumns.StError + 2);
            base.ClearContent(rowIndex, colIndex, (int)base.LastRowWithData(), (int)base.LastColumnWithData());
            base.ClearFormat(rowIndex, colIndex, (int)base.LastRowWithData(), (int)base.LastColumnWithData());

            // create headers
            ++rowIndex;
            base.WriteToCell("Statistics", rowIndex, (int)enumSimulationStatisticsColumns.Name + 1);
            base.WriteToCell("Mean", rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1);
            base.WriteToCell("StDev", rowIndex, (int)enumSimulationStatisticsColumns.StDev +  1);
            base.WriteToCell("StError", rowIndex, (int)enumSimulationStatisticsColumns.StError + 1);
            base.MakeBold(rowIndex, (int)enumSimulationStatisticsColumns.Name + 1, ExcelInteractor.enumRangeDirection.RightEnd);
            base.Align(rowIndex, (int)enumSimulationStatisticsColumns.Name + 1, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.AddABorder(rowIndex, (int)enumSimulationStatisticsColumns.Name + 1, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
            
            // report summary statistics
            ++rowIndex;
            base.WriteToColumn(strSummaryStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Name + 1);
            base.WriteToMatrix(summaryStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1);
            base.Align(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1, 
                rowIndex + strSummaryStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1, 
                enumAlignment.Center);
            base.FormatNumber(
                rowIndex + (int)EnumSimStatsRows.TotalDALY - 1, (int)enumSimulationStatisticsColumns.Mean, ExcelInteractor.enumRangeDirection.RightEnd, 
                "#,##0");
            base.FormatNumber(
                rowIndex + (int)EnumSimStatsRows.TotalCost - 1, (int)enumSimulationStatisticsColumns.Mean, ExcelInteractor.enumRangeDirection.RightEnd, 
                "$#,##0");
            base.FormatNumber(
                rowIndex + (int)EnumSimStatsRows.AnnualCost - 1, (int)enumSimulationStatisticsColumns.Mean, ExcelInteractor.enumRangeDirection.RightEnd, 
                "$#,##0");
            base.FormatNumber(
                rowIndex + (int)EnumSimStatsRows.NHB - 1, (int)enumSimulationStatisticsColumns.Mean, ExcelInteractor.enumRangeDirection.RightEnd, 
                "#,##0");
            base.FormatNumber(
                rowIndex + (int)EnumSimStatsRows.NMB - 1, (int)enumSimulationStatisticsColumns.Mean, ExcelInteractor.enumRangeDirection.RightEnd, 
                "$#,##0");
            base.FormatNumber(
                rowIndex + (int)EnumSimStatsRows.NumOfSwitches - 1, (int)enumSimulationStatisticsColumns.Mean, ExcelInteractor.enumRangeDirection.RightEnd, 
                "#,##0.0");
            base.FormatNumber(
                rowIndex + (int)EnumSimStatsRows.LengthOfEpidemic - 1, (int)enumSimulationStatisticsColumns.Mean, ExcelInteractor.enumRangeDirection.RightEnd,
                "#,##0.00");

            // report class and summation statistics
            rowIndex = base.LastRowWithDataInThisColumn((int)enumSimulationStatisticsColumns.Name + 1) + 2;
            base.WriteToColumn(strClassAndSumStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Name + 1);
            base.WriteToMatrix(classAndSumStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1);
            base.Align(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
                rowIndex + strClassAndSumStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
                enumAlignment.Center);
            base.FormatNumber(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
                rowIndex + strClassAndSumStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
               "#,##0");

            // report ratio statistics
            rowIndex = base.LastRowWithDataInThisColumn((int)enumSimulationStatisticsColumns.Name + 1) + 2;
            base.WriteToColumn(strRatioStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Name + 1);
            base.WriteToMatrix(ratioStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1);
            base.Align(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
                rowIndex + strRatioStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
                enumAlignment.Center);
            base.FormatNumber(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
                rowIndex + strRatioStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
                "0.00%");

            // report utilization of intervention statistics
            rowIndex = base.LastRowWithDataInThisColumn((int)enumSimulationStatisticsColumns.Name + 1) + 2;
            base.WriteToColumn(strInterventionUtilizationStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Name + 1);
            base.WriteToMatrix(interventionUtilizationStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1);
            base.Align(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
                rowIndex + strInterventionUtilizationStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
                enumAlignment.Center);
            base.FormatNumber(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
                rowIndex + strInterventionUtilizationStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
                "#,##0.0");

            // report computational statistics
            rowIndex = base.LastRowWithDataInThisColumn((int)enumSimulationStatisticsColumns.Name + 1) + 2;
            base.WriteToColumn(strComputationStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Name + 1);
            base.WriteToMatrix(computationStatistics, rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1);
            base.Align(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
                rowIndex + strComputationStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
                enumAlignment.Center);
            base.FormatNumber(
               rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
               rowIndex + strComputationStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
               "#,##0.00");

            // report simulation iteration statistics
            rowIndex = base.LastRowWithDataInThisColumn((int)enumSimulationStatisticsColumns.Name + 1) + 2;
            base.WriteToRow(strSimulationIterationStatistics, rowIndex, 2);
            //base.WriteToCell("RNG Seed", rowIndex, 2);
            //base.WriteToCell("Health Measure", rowIndex, 3);
            //base.WriteToCell("Total Cost", rowIndex, 4);
            //base.WriteToCell("Annual Cost", rowIndex, 5);
            base.Align(rowIndex, 2, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.AddABorder(rowIndex, 2, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
            base.MakeBold(rowIndex, 2, ExcelInteractor.enumRangeDirection.RightEnd);
            base.WrapText(rowIndex, 2, ExcelInteractor.enumRangeDirection.RightEnd);

            base.WriteToMatrix(simulationIterationStatistics, ++rowIndex, 2);
            base.Align(
               rowIndex, (int)enumSimulationStatisticsColumns.Mean,
               rowIndex + simulationIterationStatistics.Length, (int)enumSimulationStatisticsColumns.StError + 1,
               enumAlignment.Center);
            base.FormatNumber(rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1, enumRangeDirection.DownEnd, "#,##0.0");
            base.FormatNumber(rowIndex, (int)enumSimulationStatisticsColumns.StDev + 1, enumRangeDirection.DownEnd, "$#,##0");
            base.FormatNumber(rowIndex, (int)enumSimulationStatisticsColumns.StError + 1, enumRangeDirection.DownEnd, "$#,##0");
            //base.FormatNumber()
                    
            // clean the last column
            int thisCol = (int)enumSimulationStatisticsColumns.StError + 2;
            base.ClearContent(3, thisCol, rowIndex - 2, thisCol); //base.LastRowWithDataInThisColumn(thisCol)

        }
        // report sampled parameter values
        public void ReportSampledParameterValues(string[] namesOfParameters, int[] simItrs, int[] RNDSeeds, double[,] matrixOfParameterValues)
        {
            // select "Calibration" sheet
            base.ActivateSheet("Sampled Parameters");

            // clear worksheet
            int rowIndex = base.LastRowWithData();
            int colIndex = base.ColIndex(2, 2, enumRangeDirection.RightEnd);
            base.ClearAll(1, 1, rowIndex, colIndex);

            // setup the header definitions
            rowIndex = 2;
            colIndex = 2;
            base.WriteToCell("Simulation Replication", rowIndex, colIndex);
            base.WriteToCell("Random Seed", rowIndex, colIndex + 1);
            
            base.WriteToRow(namesOfParameters, rowIndex, colIndex + 2);

            // formate the header
            base.Align(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.MakeBold(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd);
            base.WrapText(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd);
            base.AddABorder(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);

            // write results
            ++rowIndex;
            base.WriteToColumn(SupportFunctions.ConvertArrayToDouble(simItrs), rowIndex, colIndex + 0);
            base.WriteToColumn(SupportFunctions.ConvertArrayToDouble(RNDSeeds), rowIndex, colIndex + 1);
            base.WriteToMatrix(matrixOfParameterValues, rowIndex, colIndex + 2);
            base.AlignAMatrix(rowIndex, colIndex, ExcelInteractor.enumAlignment.Center);

            // clearn the last column
            int thisCol = base.ColIndex(2, 2, enumRangeDirection.RightEnd)  + 1;
            base.ClearContent(3, thisCol, base.LastRowWithDataInThisColumn(thisCol), thisCol);
        }
        // set up observation worksheet
        public void SetupEpidemicHistoryWorksheet(string[,] strObsHeader, int[] observationPeriodIndeces)
        {
            // clear worksheet
            base.ActivateSheet("Epidemic History");
            int rowIndex = base.LastRowWithData();
            int colIndex = base.ColIndex("epidemicHistoryBase", enumRangeDirection.RightEnd);
            base.ClearAll(1, 1, rowIndex, colIndex);

            // setup the header definitions
            rowIndex = base.RowIndex("epidemicHistoryBase");
            base.WriteToCell("Observation Period", rowIndex, (int)enumObservationsColumns.ObservationPeriod);
            base.WriteToCell("Action Code", rowIndex, (int)enumObservationsColumns.InterventionID);
            colIndex = (int)enumObservationsColumns.FirstObservedOutcome;
            base.WriteToMatrix(strObsHeader, "epidemicHistoryBase", -1, 2);

            int lastHeaderColIndex = colIndex + strObsHeader.GetLength(1) - 1;
            int thisCol = lastHeaderColIndex + 1;
            base.ClearContent(rowIndex - 1, thisCol, base.LastRowWithDataInThisColumn(thisCol), thisCol);
            
            // formating
            colIndex = (int)enumObservationsColumns.ObservationPeriod;
            base.Align(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.MakeBold(rowIndex - 1, colIndex + 2, rowIndex - 1, lastHeaderColIndex);
            base.AddABorder(rowIndex - 1, colIndex + 2, rowIndex - 1, lastHeaderColIndex, ExcelInteractor.enumBorder.Bottom);
            //base.MakeBold(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd);
            base.WrapText(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd);
            base.AddABorder(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);

            // write observation period indices
            base.WriteToColumn(observationPeriodIndeces, "epidemicHistoryBase", 1, 0);

            // make the workbook visible            
            base.Visible = true;

            // have user to enter feature specifications
            MessageBox.Show("Enter the observations into the sheet 'Epidemic History' and then click the OK button.",
                "Observations", MessageBoxButtons.OK);
        }

        // report calibration results
        public void ReportCalibrationResults(
            double calibrationTimeInMinute, 
            int numOfTrajectoriesDiscarded, 
            int[] simItrs, 
            int[] rndSeeds,
            double[] probs)
        {
            // select "Calibration" sheet
            ActivateSheet("Calibration");

            // clear worksheet
            int rowIndex = LastRowWithData();
            int colIndex = ColIndex(2, 2, enumRangeDirection.RightEnd);
            ClearAll(1, 1, rowIndex, colIndex);

            // computation time
            WriteToCell("Computation time (minutes): ", 1, 2);
            WriteToCell(calibrationTimeInMinute, 1, 3);
            FormatNumber(1, 3, "#,##0.0");

            // report the number of trajectories discarded
            WriteToCell(numOfTrajectoriesDiscarded, 1, 5);
            WriteToCell("Total trajectories discarded due to violating feasibility ranges or early eradication.", 1, 6);

            // setup the header definitions
            rowIndex = 2;
            WriteToCell("Simulation Iteration", rowIndex, (int)EnumCalibrationColumns.SimulationItr);
            WriteToCell("Random Seed", rowIndex, (int)EnumCalibrationColumns.RandomSeed);
            WriteToCell("Goodness of Fit", rowIndex, (int)EnumCalibrationColumns.GoodnessOfFit_overal);

            // formate the header
            colIndex = (int)EnumCalibrationColumns.SimulationItr;
            Align(rowIndex, colIndex, enumRangeDirection.RightEnd, enumAlignment.Center);
            MakeBold(rowIndex, colIndex, enumRangeDirection.RightEnd);
            WrapText(rowIndex, colIndex, enumRangeDirection.RightEnd);
            AddABorder(rowIndex, colIndex, enumRangeDirection.RightEnd, enumBorder.Bottom);

            if (!(simItrs is null))
            {
                // write results
                ++rowIndex;
                // simulation iterations
                WriteToColumn(SupportFunctions.ConvertArrayToDouble(simItrs), rowIndex, (int)EnumCalibrationColumns.SimulationItr);
                // rnd seeds
                WriteToColumn(SupportFunctions.ConvertArrayToDouble(rndSeeds), rowIndex, (int)EnumCalibrationColumns.RandomSeed);
                // goodness of fit
                WriteToColumn(probs, rowIndex, (int)EnumCalibrationColumns.GoodnessOfFit_overal);
                // align
                AlignAMatrix(rowIndex - 1, (int)EnumCalibrationColumns.SimulationItr, enumAlignment.Center);
            }
        }

        // report calibration results
        public void OldReportCalibrationResults(double calibrationTimeInMinute, int numOfTrajectoriesDiscarded, string[] namesOfCalibrationTargets, 
            //string[] namesOfParameters,
            string[] namesOfSimOutsWithNonZeroWeights, 
            int[] calibrationItrs, int[] calibrationRNDSeeds, 
            //double[,] goodnessOfFitValues, double[,] matrixOfParameterValues, 
            double[, ] matrixOfSelectedSimObs)
        {
            // select "Calibration" sheet
            base.ActivateSheet("Calibration");

            // clear worksheet
            int rowIndex = base.LastRowWithData();
            int colIndex = base.ColIndex(2, 2, enumRangeDirection.RightEnd);
            base.ClearAll(1, 1, rowIndex, colIndex);

            // computation time
            base.WriteToCell("Computation time (minutes): ", 1, 2);
            base.WriteToCell(calibrationTimeInMinute, 1, 3);
            base.FormatNumber(1, 3, "#,##0.0");

            // report the number of trajectories discarded
            base.WriteToCell(numOfTrajectoriesDiscarded, 1, 5);
            base.WriteToCell("Total trajectories discarded due to violating feasibility ranges or early eradication.", 1, 6);
            
            // setup the header definitions
            rowIndex = 2;
            base.WriteToCell("Simulation Iteration", rowIndex, (int)EnumCalibrationColumns.SimulationItr);
            base.WriteToCell("Random Seed", rowIndex, (int)EnumCalibrationColumns.RandomSeed);
            base.WriteToCell("Goodness of Fit", rowIndex, (int)EnumCalibrationColumns.GoodnessOfFit_overal);
            // goodness of fit names
            //for (int j = 0; j < namesOfCalibrationTargets.Length; j++)
            //    base.WriteToCell("Goodness of Fit | " + namesOfCalibrationTargets[j], rowIndex, (int)enumCalibrationColumns.GoodnessOfFit_overal + 1 + j);
            // names of parameters
            colIndex = (int)EnumCalibrationColumns.GoodnessOfFit_overal + 1; //+ namesOfCalibrationTargets.Length;
            //base.WriteToRow(namesOfParameters, rowIndex, colIndex);

            // names of observations with nonzero weights
            //colIndex = (int)enumCalibrationColumns.GoodnessOfFit_overal + 1 + namesOfCalibrationTargets.Length + namesOfParameters.Length;
            base.WriteToRow(namesOfSimOutsWithNonZeroWeights, rowIndex, colIndex);
            
            // formate the header
            colIndex = (int)EnumCalibrationColumns.SimulationItr;
            base.Align(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.MakeBold(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd);
            base.WrapText(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd);
            base.AddABorder(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);

            if (calibrationItrs != null)
            {
                // write results
                ++rowIndex;
                // simulation iterations
                base.WriteToColumn(SupportFunctions.ConvertArrayToDouble(calibrationItrs), rowIndex, (int)EnumCalibrationColumns.SimulationItr);
                // rnd seeds
                base.WriteToColumn(SupportFunctions.ConvertArrayToDouble(calibrationRNDSeeds), rowIndex, (int)EnumCalibrationColumns.RandomSeed);
                // goodness of fit
                //base.WriteToMatrix(goodnessOfFitValues, rowIndex, (int)enumCalibrationColumns.GoodnessOfFit_overal);
                // parameter values
                colIndex = 5;// + (int)enumCalibrationColumns.GoodnessOfFit_overal;
                             //base.WriteToMatrix(matrixOfParameterValues, rowIndex, colIndex);
                             // simulation observations
                             //colIndex = colIndex + matrixOfParameterValues.GetLength(1);
                base.WriteToMatrix(matrixOfSelectedSimObs, rowIndex, colIndex);

                // aligm
                base.AlignAMatrix(rowIndex - 1, (int)EnumCalibrationColumns.SimulationItr, ExcelInteractor.enumAlignment.Center);
            }
        }
        // set up q-function worksheet
        public void SetUpQFunctionWorksheet(string[] strDecisionNames, string[] strFeatureNames, string[] strFeatureAbbreviatedNames, int[,] polynomialTerms)
        {
            int rowIndex = 2; 
            int colIndex = 2;

            // activate feature sheet
            base.ActivateSheet("Q-Functions");

            // clear worksheet
            int rowIndex1 = 1;
            int colIndex1 = 1;
            int rowIndex2 = base.LastRowWithData();
            int colIndex2 = base.LastColumnWithData();            
            base.ClearAll(rowIndex1, colIndex1, rowIndex2, colIndex2);

            // setup feature definitions
            base.WriteToCell("Features", rowIndex, colIndex);
            base.MakeBold(rowIndex, colIndex);
            rowIndex += 2;
            base.WriteToColumn(strFeatureAbbreviatedNames, rowIndex, colIndex + 1);
            base.WriteToColumn(strFeatureNames, rowIndex, colIndex + 2);
            rowIndex += strFeatureNames.Length + 2;

            // setup polynomial function for each decision
            // add header                
            base.WriteToCell("Polynomial Approximation Function:", rowIndex, colIndex);
            base.MakeBold(rowIndex, colIndex);

            rowIndex += 2;
            // create the polynomial feature table
            CreateOnePolynomialFeatureTable(ref rowIndex, colIndex + 1, strDecisionNames.Length, strFeatureAbbreviatedNames, polynomialTerms);
            

            //// setup polynomial function for each decision
            //for (int decisionID = 0; decisionID < strDecisionNames.Length; decisionID++)
            //{
            //    // add header                
            //    base.WriteToCell("Polynomial Approximation Function For Decision "
            //        + decisionID + " : " + strDecisionNames[decisionID], rowIndex, colIndex);
            //    base.MakeBold(rowIndex, colIndex);
            //    rowIndex += 2;                                
            //    // create the polynomial feature table
            //    CreateOnePolynomialFeatureTable(ref rowIndex, colIndex + 1,strFeatureAbbreviatedNames, polynomialTerms);
            //    // increment rowIndex
            //    rowIndex += 1;
            //}
            
            // make the workbook visible
            base.ActivateSheet("Q-Functions");
            base.Visible = true;

            // have user to enter feature specifications
            MessageBox.Show("Modify, if necessary, the initial values of polynomical terms in approximation functions on sheet 'Q-Functions' and then click the OK button.",
                "Features Setup", MessageBoxButtons.OK);
        }
       
        // set up optimization worksheet
        public void SetUpOptimizationOutput(int numberOfIterations)
        {
            base.ActivateSheet("ADP Iterations");
            int rowIndex1 = 1;
            int colIndex1 = 1;
            int rowIndex2 = base.LastRowWithDataInThisColumn(colIndex1);
            int colIndex2 = base.ColIndex(rowIndex1, colIndex1, ExcelInteractor.enumRangeDirection.RightEnd);
            //clear content and format            
            base.ClearContent(rowIndex1, colIndex1, rowIndex2, colIndex2);
            base.ClearFormat(rowIndex1, colIndex1, rowIndex1, colIndex2);
            // create headers
            base.WriteToCell("Iteration", rowIndex1, colIndex1);
            base.WriteToCell("Expected Discounted Total NMB", rowIndex1, colIndex1 + 1);
            base.WriteToCell("First Period Prediction Error", rowIndex1, colIndex1 + 2);
            base.WriteToCell("Middle Period Prediction Error", rowIndex1, colIndex1 + 3);
            base.WriteToCell("Last Period Prediction Error", rowIndex1, colIndex1 + 4);
            base.AddABorder(rowIndex1, colIndex1, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
            base.Align(rowIndex1, colIndex1, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.WrapText(rowIndex1, colIndex1, ExcelInteractor.enumRangeDirection.RightEnd);
            rowIndex1 += 1;
            // update chart
            base.UpdateScatterChartValues("chartNMBOpitmization", ExcelInteractor.enumAxis.x, rowIndex1, colIndex1, rowIndex1 + numberOfIterations , colIndex1);
            base.UpdateScatterChartValues("chartNMBOpitmization", ExcelInteractor.enumAxis.y, rowIndex1, colIndex1 + 1, rowIndex1 + numberOfIterations, colIndex1 + 1);
        }
        /// <summary>
        /// report Q-functions coefficient estimates
        /// </summary>
        /// <param name="collectionOfEstimates"> contains the coefficients (double[]) of each decision's Q-function</param>
        /// <param name="numOfFeatures"></param>
        public void ReportQFunctionsCoefficientEstimates(double[] estimates, int numOfFeatures)
        {
            int rowIndex = numOfFeatures + 10, colIndex = numOfFeatures + 5;
            // activate feature sheet
            base.ActivateSheet("Q-Functions");

            base.WriteToColumn(estimates, rowIndex, colIndex);
            base.FormatNumber(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.DownEnd, "#,##0.00");
            base.Align(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.DownEnd, ExcelInteractor.enumAlignment.Center);

            //// report Q-function parameter estiamtes for each decision
            //foreach (double[] thisCoefficients in collectionOfEstimates)
            //{                
            //    base.WriteToColumn(thisCoefficients, rowIndex, colIndex);
            //    base.FormatNumber(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.DownEnd, "#,##0.00");
            //    base.Align(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.DownEnd, ExcelInteractor.enumAlignment.Center);
            //    // increment rowIndex
            //    rowIndex += thisCoefficients.Length + 5;
            //}
        }
        // report optimization iterations
        public void ReportADPIterations(double[,] adpIterations, double harmonicStep_a, double epsilonGreedy_beta)
        {
            base.ActivateSheet("ADP Iterations");
            // write the result
            base.WriteToMatrix(adpIterations, 2, 1);
            // format
            base.AlignAMatrix(2, 1, enumAlignment.Center);

            // write learning and exploration parameters
            base.WriteToCell("Harmonic step size - a", 2, 10);
            base.WriteToCell(harmonicStep_a, 2, 11);
            base.WriteToCell("Epsilon-greedy exploration - beta", 3, 10);
            base.WriteToCell(epsilonGreedy_beta, 3, 11);
        }
        // report dynamic policy
        public void Report1DimOptimalPolicy(string featureName, double[] headers, int[] optimalDecisions)
        {
            int rowIndex = 3;
            int colIndex = 2;

            base.ActivateSheet("Dynamic Policy");
            // clear
            base.ClearAll(rowIndex, colIndex, base.LastRowWithData(), base.LastColumnWithData());

            // write feature names
            base.WriteToCell(featureName, rowIndex, colIndex);
            base.WriteToCell("Action", rowIndex, colIndex + 1);
            base.AddABorder(rowIndex, colIndex, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);

            // write headers
            base.WriteToColumn(headers, rowIndex + 1, colIndex);
            // write optimal decision
            base.WriteToColumn(Array.ConvertAll<int, double>(optimalDecisions, Convert.ToDouble), rowIndex + 1, colIndex + 1);
            // format
            base.Align(rowIndex + 1, colIndex + 1, rowIndex + 1 + headers.Length, colIndex + 1, ExcelInteractor.enumAlignment.Center);
            base.FormatNumber(rowIndex + 1, colIndex, rowIndex + 1 + headers.Length, colIndex + 1, "#,##0");
        }
        // report dynamic policy
        public void Report2DimOptimalPolicy(string[] strFeatureNames, double[][] headers, int[,] optimalDecisions)
        {
            int rowIndex = 1;
            int colIndex = 1;

            base.ActivateSheet("Dynamic Policy");
            // clear
            base.ClearAll(rowIndex, colIndex, base.LastRowWithData(), base.LastColumnWithData());
            
            // write feature names
            base.WriteToCell(strFeatureNames[0], rowIndex + 2, colIndex); // F0
            base.WriteToCell(strFeatureNames[1], rowIndex, colIndex + 2); // F1
            // write headers
            base.WriteToColumn(headers[0], rowIndex + 2, colIndex + 1);
            base.WriteToRow(headers[1], rowIndex + 1, colIndex + 2);
            // write optimal decision
            base.WriteToMatrix(optimalDecisions, rowIndex + 2, colIndex + 2);
            // format
            base.Align(rowIndex + 1, colIndex + 1, rowIndex + 1 + headers[0].Length, colIndex + 1 + headers[1].Length, ExcelInteractor.enumAlignment.Center);
            base.AddABorder(rowIndex + 2, colIndex + 1, ExcelInteractor.enumRangeDirection.DownEnd, ExcelInteractor.enumBorder.Right);
            base.AddABorder(rowIndex + 1, colIndex + 2, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
            base.FormatNumber(rowIndex + 2, colIndex + 1, ExcelInteractor.enumRangeDirection.DownEnd, "#,##0");
            base.FormatNumber(rowIndex + 1, colIndex + 2, ExcelInteractor.enumRangeDirection.RightEnd, "#,##0");
        }
        // report dynamic policy
        public void Report3DimOptimalPolicy(string[] strFeatureNames, double[][] headers, int[][,] optimalDecisions)
        {
            int rowIndex = 1;
            int colIndex = 1;

            base.ActivateSheet("Dynamic Policy");
            // clear
            base.ClearAll(rowIndex, colIndex, base.LastRowWithData(), base.LastColumnWithData());

            // for each resource value
            for (int resourceFeatureIndex = 0; resourceFeatureIndex < optimalDecisions.Length; resourceFeatureIndex++)
            {
                // write resouce feature names
                rowIndex += 3;
                base.WriteToCell(strFeatureNames[0], rowIndex, colIndex);
                // resource value 
                base.WriteToCell(headers[0][resourceFeatureIndex], rowIndex, colIndex + 1);

                ++rowIndex;
                // write feature names
                base.WriteToCell(strFeatureNames[1], rowIndex + 2, colIndex); // F0
                base.WriteToCell(strFeatureNames[2], rowIndex, colIndex + 2); // F1
                // write headers
                base.WriteToColumn(headers[1], rowIndex + 2, colIndex + 1);
                base.WriteToRow(headers[2], rowIndex + 1, colIndex + 2);
                // write optimal decision
                base.WriteToMatrix(optimalDecisions[resourceFeatureIndex], rowIndex + 2, colIndex + 2);
                // format
                base.Align(rowIndex + 1, colIndex + 1, rowIndex + 1 + headers[0].Length, colIndex + 1 + headers[1].Length, ExcelInteractor.enumAlignment.Center);
                base.AddABorder(rowIndex + 2, colIndex + 1, ExcelInteractor.enumRangeDirection.DownEnd, ExcelInteractor.enumBorder.Right);
                base.AddABorder(rowIndex + 1, colIndex + 2, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
                base.FormatNumber(rowIndex + 2, colIndex + 1, ExcelInteractor.enumRangeDirection.DownEnd, "#,##0");
                base.FormatNumber(rowIndex + 1, colIndex + 2, ExcelInteractor.enumRangeDirection.RightEnd, "#,##0");

                // increment the row index
                rowIndex += headers[1].Length;

            }
        }
        // report the results of dynamic policy optimization
        public void ReportADPResultsForAllEpidemic(double[,] adpParameterDesigns, double[,] simulationOutcomes, double[,] simulationIterations, EnumObjectiveFunction objectiveFunction)
        {
            // clear sheet        
            int lastCol;            
            base.ActivateSheet("Dynamic Policy Optimizaion");
            lastCol = base.ColIndex("baseADPParameterDesigns", enumRangeDirection.RightEnd);
            base.ClearAll(base.RowIndex("baseADPParameterDesigns"), base.ColIndex("baseADPParameterDesigns"),
                base.LastRowWithDataInThisColumn(base.ColIndex("baseADPParameterDesigns")), lastCol);
            lastCol = base.ColIndex("baseADPSASimulationIterations", enumRangeDirection.RightEnd);
            base.ClearAll(base.RowIndex("baseADPSASimulationIterations"), base.ColIndex("baseADPSASimulationIterations"),
                base.LastRowWithDataInThisColumn(base.ColIndex("baseADPSASimulationIterations")), lastCol);

            // make headers
            #region make headers
            base.WriteToCell("WTP for Health","baseADPParameterDesigns", 0, (int)enumADPParameterDesignOffsets.wtpForHealth);
            base.WriteToCell("Harmonic Step - a", "baseADPParameterDesigns", 0, (int)enumADPParameterDesignOffsets.harmonic_a);
            base.WriteToCell("ε-Greedy - β", "baseADPParameterDesigns", 0, (int)enumADPParameterDesignOffsets.epsilonGreedy_beta);
            base.Align("baseADPParameterDesigns", 0, 0, enumRangeDirection.RightEnd, enumAlignment.Center);
            base.AddABorder("baseADPParameterDesigns", 0, 0, enumRangeDirection.RightEnd, enumBorder.Bottom);
            base.WrapText("baseADPParameterDesigns", 0, 0, enumRangeDirection.RightEnd);
            
            base.WriteToCell("WTP for Health", "baseADPSASimulationIterations", 0, (int)enumADPSASimulationIterationsOffsets.wtpForHealth);
            base.WriteToCell("Harmonic Step - a", "baseADPSASimulationIterations", 0, (int)enumADPSASimulationIterationsOffsets.harmonic_a);
            base.WriteToCell("ε-Greedy - β", "baseADPSASimulationIterations", 0, (int)enumADPSASimulationIterationsOffsets.epsilonGreedy_beta);
            base.WriteToCell("Objective Function", "baseADPSASimulationIterations", 0, (int)enumADPSASimulationIterationsOffsets.objectiveFunction);
            base.WriteToCell("Health", "baseADPSASimulationIterations", 0, (int)enumADPSASimulationIterationsOffsets.health);
            base.WriteToCell("Cost", "baseADPSASimulationIterations", 0, (int)enumADPSASimulationIterationsOffsets.cost);
            base.WriteToCell("Annual Cost", "baseADPSASimulationIterations", 0, (int)enumADPSASimulationIterationsOffsets.annualCost);
            base.Align("baseADPSASimulationIterations", 0, 0, enumRangeDirection.RightEnd, enumAlignment.Center);
            base.AddABorder("baseADPSASimulationIterations", 0, 0, enumRangeDirection.RightEnd, enumBorder.Bottom);
            base.WrapText("baseADPSASimulationIterations", 0, 0, enumRangeDirection.RightEnd);
            #endregion

            // write the results
            base.WriteToMatrix(adpParameterDesigns, "baseADPParameterDesigns", 1, 0);
            base.WriteToMatrix(simulationIterations, "baseADPSASimulationIterations", 1, 0);

            // formattings
            #region
            // align
            base.AlignAMatrix(base.RowIndex("baseADPParameterDesigns") + 1, base.ColIndex("baseADPParameterDesigns"), enumAlignment.Center);
            base.AlignAMatrix(base.RowIndex("baseADPSASimulationIterations") + 1, base.ColIndex("baseADPSASimulationIterations"), enumAlignment.Center);

            // format simulation iterations            
            base.FormatNumber("baseADPParameterDesigns", 1, (int)enumADPParameterDesignOffsets.wtpForHealth, enumRangeDirection.DownEnd, "$#,000");
            base.FormatNumber("baseADPSASimulationIterations", 1, (int)enumADPSASimulationIterationsOffsets.wtpForHealth, enumRangeDirection.DownEnd, "$#,000");
            if (objectiveFunction == EnumObjectiveFunction.MaximizeNMB)
                base.FormatNumber("baseADPSASimulationIterations", 1, (int)enumADPSASimulationIterationsOffsets.objectiveFunction, enumRangeDirection.DownEnd, "$#,000");
            else
                base.FormatNumber("baseADPSASimulationIterations", 1, (int)enumADPSASimulationIterationsOffsets.objectiveFunction, enumRangeDirection.DownEnd, "#,000");
            base.FormatNumber("baseADPSASimulationIterations", 1, (int)enumADPSASimulationIterationsOffsets.health, enumRangeDirection.DownEnd, "#,000");
            base.FormatNumber("baseADPSASimulationIterations", 1, (int)enumADPSASimulationIterationsOffsets.cost, enumRangeDirection.DownEnd, "$#,000");
            base.FormatNumber("baseADPSASimulationIterations", 1, (int)enumADPSASimulationIterationsOffsets.annualCost, enumRangeDirection.DownEnd, "$#,000");
            #endregion

            // finally report simulation outcomes
            ReportSummaryOutcomes("Dynamic Policy Optimizaion", "baseADPSASimulationOutcomes", simulationOutcomes, objectiveFunction);
        }
        // report ADP computation time
        public void ReportADPComputationTimes(string[] strMeasures, double[,] statistics)
        {
            base.ActivateSheet("Computation Time");
            // make header
            int rowIndex = 1;
            int colIndex = 1;

            //clear content and format
            base.ClearContent(rowIndex, colIndex, (int)base.LastRowWithData(), (int)enumSimulationStatisticsColumns.StError + 2);
            base.ClearFormat(rowIndex, colIndex, (int)base.LastRowWithData(), (int)enumSimulationStatisticsColumns.StError + 2);

            // create headers
            ++rowIndex;
            base.WriteToCell("Statistics", rowIndex, (int)enumSimulationStatisticsColumns.Name + 1);
            base.WriteToCell("Mean", rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1);
            base.WriteToCell("StDev", rowIndex, (int)enumSimulationStatisticsColumns.StDev + 1);
            base.WriteToCell("StError", rowIndex, (int)enumSimulationStatisticsColumns.StError + 1);
            base.MakeBold(rowIndex, (int)enumSimulationStatisticsColumns.Name + 1, ExcelInteractor.enumRangeDirection.RightEnd);
            base.Align(rowIndex, (int)enumSimulationStatisticsColumns.Name + 1, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.AddABorder(rowIndex, (int)enumSimulationStatisticsColumns.Name + 1, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);

            ++rowIndex;
            base.WriteToColumn(strMeasures, rowIndex, (int)enumSimulationStatisticsColumns.Name + 1);
            base.WriteToMatrix(statistics, rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1);

            base.Align(
                rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
                rowIndex + strMeasures.Length, (int)enumSimulationStatisticsColumns.StError + 1,
                enumAlignment.Center);
            base.FormatNumber(
               rowIndex, (int)enumSimulationStatisticsColumns.Mean + 1,
               rowIndex + strMeasures.Length, (int)enumSimulationStatisticsColumns.StError + 1,
               "#,##0.00");
        }

        // report the results of optimization
        public void ReportOptimization(string[,] results)
        {
            // clear sheet        
            ActivateSheet("Optimization");
            int lastCol = base.ColIndex("baseOptimizationResults", enumRangeDirection.RightEnd);
            ClearAll(
                RowIndex("baseOptimizationResults")+1, 
                ColIndex("baseOptimizationResults"),
                LastRowWithDataInThisColumn(base.ColIndex("baseOptimizationResults")), 
                lastCol
                );

            //WriteToCell(policyParams, "baseOptimizationResults", -2, 1);
            WriteToMatrix(results, "baseOptimizationResults", 1, 0);
            AlignAMatrix(base.RowIndex("baseOptimizationResults"), base.ColIndex("baseOptimizationResults"), enumAlignment.Center);
        }


        // report the results of static policy optimization
        public void ReportStaticPolicyOptimization(double[] WTPs, string[,] staticPolicies, 
            double[,] simulationOutcomes, double[,] simulationIterations, EnumObjectiveFunction objectiveFunction)
        {   
            int lastCol;
            // clear sheet        
            base.ActivateSheet("Static Policy Optimization");
            lastCol = base.ColIndex("baseStaticPolicyParameterDesigns", enumRangeDirection.RightEnd);
            base.ClearAll(base.RowIndex("baseStaticPolicyParameterDesigns"), base.ColIndex("baseStaticPolicyParameterDesigns"),
                base.LastRowWithDataInThisColumn(base.ColIndex("baseStaticPolicyParameterDesigns")), lastCol);            
            lastCol = base.ColIndex("baseStaticPolicySimulationIterations", enumRangeDirection.RightEnd);
            base.ClearAll(base.RowIndex("baseStaticPolicySimulationIterations"), base.ColIndex("baseStaticPolicySimulationIterations"),
                base.LastRowWithDataInThisColumn(base.ColIndex("baseStaticPolicySimulationIterations")), lastCol);

            // make headers
            #region make headers
            base.WriteToCell("WTP for Health", "baseStaticPolicyParameterDesigns", 0, (int)0);
            base.WriteToCell("Interventions", "baseStaticPolicyParameterDesigns", 0, (int)1);
            base.WriteToCell("Optimal Start Time", "baseStaticPolicyParameterDesigns", 0, (int)2);                    
            //base.WriteToCell("Optimal Threshold", "baseStaticPolicyParameterDesigns", 0, (int)2);

            base.WriteToCell("Optimal Duration (Number of Decision Periods)", "baseStaticPolicyParameterDesigns", 0, (int)3);
            base.Align("baseStaticPolicyParameterDesigns", 0, 0, enumRangeDirection.RightEnd, enumAlignment.Center);
            base.AddABorder("baseStaticPolicyParameterDesigns", 0, 0, enumRangeDirection.RightEnd, enumBorder.Bottom);
            base.WrapText("baseStaticPolicyParameterDesigns", 0, 0, enumRangeDirection.RightEnd);

            base.WriteToCell("WTP for Health", "baseStaticPolicySimulationIterations", 0, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.wtpForHealth);
            base.WriteToCell("Objective Function", "baseStaticPolicySimulationIterations", 0, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.objectiveFunction);
            base.WriteToCell("Health", "baseStaticPolicySimulationIterations", 0, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.health);
            base.WriteToCell("Cost", "baseStaticPolicySimulationIterations", 0, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.cost);
            base.WriteToCell("Annual Cost", "baseStaticPolicySimulationIterations", 0, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.annualCost);
            base.Align("baseStaticPolicySimulationIterations", 0, 0, enumRangeDirection.RightEnd, enumAlignment.Center);
            base.AddABorder("baseStaticPolicySimulationIterations", 0, 0, enumRangeDirection.RightEnd, enumBorder.Bottom);
            base.WrapText("baseStaticPolicySimulationIterations", 0, 0, enumRangeDirection.RightEnd);
            #endregion

            // write the results
            base.WriteToColumn(WTPs, "baseStaticPolicyParameterDesigns", 1, 0);
            base.WriteToMatrix(staticPolicies, "baseStaticPolicyParameterDesigns", 1,  1);            
            base.WriteToMatrix(simulationIterations, "baseStaticPolicySimulationIterations", 1, 0);

            // formattings
            #region
            // align
            base.AlignAMatrix(base.RowIndex("baseStaticPolicyParameterDesigns") + 1, base.ColIndex("baseStaticPolicyParameterDesigns"), enumAlignment.Center);            
            base.AlignAMatrix(base.RowIndex("baseStaticPolicySimulationIterations") + 1, base.ColIndex("baseStaticPolicySimulationIterations"), enumAlignment.Center);

            // format simulation iterations
            base.FormatNumber("baseStaticPolicyParameterDesigns", 1, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.wtpForHealth, enumRangeDirection.DownEnd, "$#,000");
            base.FormatNumber("baseStaticPolicySimulationIterations", 1, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.wtpForHealth, enumRangeDirection.DownEnd, "$#,000");
            if (objectiveFunction == EnumObjectiveFunction.MaximizeNMB)
                base.FormatNumber("baseStaticPolicySimulationIterations", 1, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.objectiveFunction, enumRangeDirection.DownEnd, "$#,000");
            else
                base.FormatNumber("baseStaticPolicySimulationIterations", 1, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.objectiveFunction, enumRangeDirection.DownEnd, "#,000");
            base.FormatNumber("baseStaticPolicySimulationIterations", 1, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.health, enumRangeDirection.DownEnd, "#,000");
            base.FormatNumber("baseStaticPolicySimulationIterations", 1, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.cost, enumRangeDirection.DownEnd, "$#,000");
            base.FormatNumber("baseStaticPolicySimulationIterations", 1, (int)enumStaticPolicyOptimizationSimulationIterationsOffsets.annualCost, enumRangeDirection.DownEnd, "$#,000");
            #endregion

            // finally report simulation outcomes
            ReportSummaryOutcomes("Static Policy Optimization", "baseStaticPolicySimulationOutcomes", simulationOutcomes, objectiveFunction);
        }
        // report simulation outcomes for adp or static optimization
        public void ReportSummaryOutcomes(string sheetName, string baseCellname, double[,] simulationOutcomes, EnumObjectiveFunction objectiveFunction)
        {
            int firstCol, lastCol;
            // clear sheet 
            ActivateSheet(sheetName);
            firstCol = ColIndex(baseCellname);
            lastCol = ColIndex(baseCellname, enumRangeDirection.RightEnd);
            ClearAll(RowIndex(baseCellname), firstCol, LastRowWithDataInThisColumn(firstCol), lastCol);

            // make headers
            WriteToCell("E[Obj]", baseCellname, 0, (int)enumSimulationOutcomesOffsets.E_ObjectiveFunction);
            WriteToCell("StDev[Obj]", baseCellname, 0, (int)enumSimulationOutcomesOffsets.stDev_ObjectiveFunction);
            WriteToCell("E[Health]", baseCellname, 0, (int)enumSimulationOutcomesOffsets.E_health);
            WriteToCell("StDev[Health]", baseCellname, 0, (int)enumSimulationOutcomesOffsets.stDev_health);
            WriteToCell("E[Cost]", baseCellname, 0, (int)enumSimulationOutcomesOffsets.E_cost);
            WriteToCell("StDev[Cost]", baseCellname, 0, (int)enumSimulationOutcomesOffsets.stDev_cost);
            Align(baseCellname, 0, 0, enumRangeDirection.RightEnd, enumAlignment.Center);
            AddABorder(baseCellname, 0, 0, enumRangeDirection.RightEnd, enumBorder.Bottom);
            WrapText(baseCellname, 0, 0, enumRangeDirection.RightEnd);            

            // write the data
            WriteToMatrix(simulationOutcomes, baseCellname, 1, 0);
            int thisCol = ColIndex(baseCellname, enumRangeDirection.RightEnd) + 1;
            ClearContent(RowIndex(baseCellname), thisCol, LastRowWithDataInThisColumn(thisCol), thisCol);

            // formattings
            AlignAMatrix(RowIndex(baseCellname) + 1, ColIndex(baseCellname), enumAlignment.Center);
            if (objectiveFunction == EnumObjectiveFunction.MaximizeNMB)
            {
                FormatNumber(baseCellname, 1, (int)enumSimulationOutcomesOffsets.E_ObjectiveFunction, enumRangeDirection.DownEnd, "$#,000");
                FormatNumber(baseCellname, 1, (int)enumSimulationOutcomesOffsets.stDev_ObjectiveFunction, enumRangeDirection.DownEnd, "$#,000");
            }
            else
            {
                FormatNumber(baseCellname, 1, (int)enumSimulationOutcomesOffsets.E_ObjectiveFunction, enumRangeDirection.DownEnd, "#,000");
                FormatNumber(baseCellname, 1, (int)enumSimulationOutcomesOffsets.stDev_ObjectiveFunction, enumRangeDirection.DownEnd, "#,000");
            }
            FormatNumber(baseCellname, 1, (int)enumSimulationOutcomesOffsets.E_health, enumRangeDirection.DownEnd, "#,000");
            FormatNumber(baseCellname, 1, (int)enumSimulationOutcomesOffsets.stDev_health, enumRangeDirection.DownEnd, "#,000");
            FormatNumber(baseCellname, 1, (int)enumSimulationOutcomesOffsets.E_cost, enumRangeDirection.DownEnd, "$#,000");
            FormatNumber(baseCellname, 1, (int)enumSimulationOutcomesOffsets.stDev_cost, enumRangeDirection.DownEnd, "$#,000");
        }
        // report experimental design simulation outcomes
        public void ReportSAReplicationOutcomes
            (string[] strVarNames,             // variable names
            string[] strSimItrOutcomeLabels,   // lables of simulation outcomes
            string[] strScenarios,             // names of scenarios for all replications
            double[,] varAndObjFuncValues,  // values of variables and objective function
            double[,] simItrOutcomes,       // values of simulation outcomes accross all replications
            EnumObjectiveFunction objectiveFunction)    // objective function 
        {
            // clear all
            int firstRow = RowIndex("baseExperimetalDesignSimOutcomes");
            int firstCol = ColIndex("baseExperimetalDesignSimOutcomes");
            ClearAll(firstRow, firstCol, LastRowWithDataInThisColumn(ColIndex("baseExperimetalDesignSimOutcomes")), LastColumnWithData());

            // create headers
            int numOfVars = strVarNames.Length;
            WriteToCell("Scenario", "baseExperimetalDesignSimOutcomes");
            WriteToRow(strVarNames, "baseExperimetalDesignSimOutcomes", 0, 1);
            WriteToCell("Objective Function", "baseExperimetalDesignSimOutcomes", 0, numOfVars + 1);
            WriteToRow(strSimItrOutcomeLabels, "baseExperimetalDesignSimOutcomes", 0, numOfVars + 2);
            // formate headers
            Align("baseExperimetalDesignSimOutcomes", 0, 0, enumRangeDirection.RightEnd, enumAlignment.Center);
            AddABorder("baseExperimetalDesignSimOutcomes", 0, 0, enumRangeDirection.RightEnd, enumBorder.Bottom);
            WrapText("baseExperimetalDesignSimOutcomes", 0, 0, enumRangeDirection.RightEnd);

            // write the results
            WriteToColumn(strScenarios, "baseExperimetalDesignSimOutcomes", 1, 0); 
            WriteToMatrix(varAndObjFuncValues, "baseExperimetalDesignSimOutcomes", 1, 1);
            WriteToMatrix(simItrOutcomes, "baseExperimetalDesignSimOutcomes", 1, numOfVars + 2);

            //int thisCol = ColIndex("baseExperimetalDesignSimOutcomes") + numOfVars + 1 + strSimItrOutcomeLabels.Length;
            //ClearContent(RowIndex("baseExperimetalDesignSimOutcomes"), thisCol , LastRowWithDataInThisColumn(thisCol), thisCol);

            // align
            AlignAMatrix(RowIndex("baseExperimetalDesignSimOutcomes") + 1, ColIndex("baseExperimetalDesignSimOutcomes")+1, enumAlignment.Center);            

            // format simulation iterations
            if (objectiveFunction == EnumObjectiveFunction.MaximizeNMB)
                FormatNumber("baseExperimetalDesignSimOutcomes", 1, numOfVars + 1, enumRangeDirection.DownEnd, "$#,000");
            else
                FormatNumber("baseExperimetalDesignSimOutcomes", 1, numOfVars + 1, enumRangeDirection.DownEnd, "#,000");
            FormatNumber("baseExperimetalDesignSimOutcomes", 1, numOfVars + 3, enumRangeDirection.DownEnd, "#,000");
            FormatNumber("baseExperimetalDesignSimOutcomes", 1, numOfVars + 4, enumRangeDirection.DownEnd, "$#,000");
            FormatNumber("baseExperimetalDesignSimOutcomes", 1, numOfVars + 5, enumRangeDirection.DownEnd, "$#,000");
        }
        // report real-time decision making results
        public void ReportEvaluatingRealTimeDecisionMaking(int[] hypotheticalEpidemicsIterations, int[] hypotheticalEpidemicsRNDSeeds, double[] wtpForHealthValuesHypotheticalEpidemics,
            double[] objFunctionHypotheticalEpidemics_staticPolicies, double[] objFunctionHypotheticalEpidemics_dynamicPolicies, string[] strComputationMeasures, double[,] computationStatistics,
            enumPoliciesToSimulateInRealTime policiesToSimulateInRealTime, EnumObjectiveFunction objectiveFunction)
        {
            #region headers
            // set up the sheet
            base.ActivateSheet("Simulating Decision Making");
            int firstRow = base.RowIndex("baseSimulatingDecisionMaking");
            int firstCol = base.ColIndex("baseSimulatingDecisionMaking");
            int lastRow = base.LastRowWithData();
            int lastCol = base.LastColumnWithData();
            base.ClearAll(firstRow, firstCol, lastRow, lastCol);
            // headers
            string[] headers = new string[5] {"Hypothetical Epidemic Iterations", "RND Seed", "WTP for Health", 
                "Objective Function - Static Policy", "Objective Function - Dynamic Policy"};
            base.WriteToRow(headers, firstRow, firstCol);
            // format
            base.Align(firstRow, firstCol, enumRangeDirection.RightEnd, enumAlignment.Center);
            base.MakeBold(firstRow, firstCol, enumRangeDirection.RightEnd);
            base.WrapText(firstRow, firstCol, enumRangeDirection.RightEnd);
            base.AddABorder(firstRow, firstCol, enumRangeDirection.RightEnd, enumBorder.Bottom);
            #endregion

            // write results
            #region performance comparison
            base.WriteToColumn(hypotheticalEpidemicsIterations, firstRow + 1, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.Iteration);
            base.WriteToColumn(hypotheticalEpidemicsRNDSeeds, firstRow + 1, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.RNDSeeds);
            base.WriteToColumn(wtpForHealthValuesHypotheticalEpidemics, firstRow + 1, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.WTPForHEalth);
            switch (policiesToSimulateInRealTime)
            {
                case enumPoliciesToSimulateInRealTime.Static:
                    base.WriteToColumn(objFunctionHypotheticalEpidemics_staticPolicies, firstRow + 1, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.ObjFunctionStaticPolicy);
                    break;
                case enumPoliciesToSimulateInRealTime.Dynamic:
                    base.WriteToColumn(objFunctionHypotheticalEpidemics_dynamicPolicies, firstRow + 1, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.ObjFunctionDynamicPolicy);
                    break;
                case enumPoliciesToSimulateInRealTime.StaticAndDynamic:
                    {
                        base.WriteToColumn(objFunctionHypotheticalEpidemics_staticPolicies, firstRow + 1, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.ObjFunctionStaticPolicy);
                        base.WriteToColumn(objFunctionHypotheticalEpidemics_dynamicPolicies, firstRow + 1, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.ObjFunctionDynamicPolicy);
                    }
                    break;
            }

            // format results
            base.AlignAMatrix(firstRow, firstCol, enumAlignment.Center);
            switch (objectiveFunction)
            {
                case EnumObjectiveFunction.MaximizeNHB:
                    {
                        base.FormatNumber(firstRow, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.ObjFunctionStaticPolicy, enumRangeDirection.DownEnd, "#,000");
                        base.FormatNumber(firstRow, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.ObjFunctionDynamicPolicy, enumRangeDirection.DownEnd, "#,000");
                    }
                    break;
                case EnumObjectiveFunction.MaximizeNMB:
                    {
                        base.FormatNumber(firstRow, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.ObjFunctionStaticPolicy, enumRangeDirection.DownEnd, "$#,000");
                        base.FormatNumber(firstRow, firstCol + (int)enumSimulatingRealTimeDecisionMakingOffsets.ObjFunctionDynamicPolicy, enumRangeDirection.DownEnd, "$#,000");
                    }
                    break;
            }
            #endregion

            // report computation statistics
            #region computation statistics
            firstRow = base.RowIndex("realTimeSimComputationStats");
            firstCol = base.ColIndex("realTimeSimComputationStats");
            // headers
            headers = new string[4] {"Computation Statistics", "Mean", "StDev", "StError"};
            base.WriteToRow(headers, firstRow, firstCol);
            // format
            base.Align(firstRow, firstCol, enumRangeDirection.RightEnd, enumAlignment.Center);
            base.MakeBold(firstRow, firstCol, enumRangeDirection.RightEnd);
            base.WrapText(firstRow, firstCol, enumRangeDirection.RightEnd);
            base.AddABorder(firstRow, firstCol, enumRangeDirection.RightEnd, enumBorder.Bottom);

            // write results
            base.WriteToColumn(strComputationMeasures, firstRow + 1, firstCol);
            base.WriteToMatrix(computationStatistics, firstRow + 1, firstCol + 1);

            base.Align(
                firstRow, firstCol + 1,
                firstRow + strComputationMeasures.Length, firstCol + 3,
                enumAlignment.Center);
            base.FormatNumber(
               firstRow, firstCol + 1,
               firstRow + strComputationMeasures.Length, firstCol + 3,
               "#,##0.00");
            #endregion
        }
        // report temp
        public void ReportStaticPolicyDesignsAndOutputs(string[] header, double[,] designs, double[,] healthAndCost)
        {
            base.ActivateSheet("Static Policy Designs");

            // headers
            // clear sheet        
            int firstRow = base.RowIndex("baseStaticPolicyDesigns");
            int firstCol = base.ColIndex("baseStaticPolicyDesigns");
            int lastCol = base.ColIndex("baseStaticPolicyDesigns", enumRangeDirection.RightEnd) + 1;
            base.ClearAll(firstRow, firstCol, base.LastRowWithDataInThisColumn(base.ColIndex("baseStaticPolicyDesigns")), lastCol);            

            // make headers
            #region make headers
            base.WriteToRow(header, firstRow, firstCol);
            base.Align("baseStaticPolicyDesigns", 0, 0, enumRangeDirection.RightEnd, enumAlignment.Center);
            base.AddABorder("baseStaticPolicyDesigns", 0, 0, enumRangeDirection.RightEnd, enumBorder.Bottom);
            base.WrapText("baseStaticPolicyDesigns", 0, 0, enumRangeDirection.RightEnd);
            #endregion

            // write the data
            base.WriteToMatrix(designs, firstRow + 1, firstCol);
            base.WriteToMatrix(healthAndCost, firstRow + 1, firstCol + designs.GetLength(1));

            // formattings
            #region
            // align
            base.AlignAMatrix(firstRow + 1, firstCol, enumAlignment.Center);

            // format simulation iterations
            base.FormatNumber(firstRow + 1, firstCol + 4, enumRangeDirection.DownEnd, "#,000");
            base.FormatNumber(firstRow + 1, firstCol + 5, enumRangeDirection.DownEnd, "$#,000");            
            
            #endregion
        }
        #endregion

        // Private Subs 
        #region Private subs
        // get cell value
        public object GetCellValue(string sheetName, string cellName)//, enumCellValueType cellValueType)
        {
            base.ActivateSheet(sheetName);
            return base.ReadCellFromActiveSheet(cellName);            
        }
        // get a matrix
        private double[,] GetTableEntities(string sheetName, int baseRowIndex, int baseColIndex)
        {
            base.ActivateSheet(sheetName);
            int lastRowIndex = base.RowIndex(baseRowIndex, baseColIndex + 1, ExcelInteractor.enumRangeDirection.DownEnd);
            int lastColIndex = base.ColIndex(baseRowIndex + 1, baseColIndex, ExcelInteractor.enumRangeDirection.RightEnd);
            // read the base contact matrix            
            return (double[,])base.ReadMatrixFromActiveSheet(baseRowIndex + 1, baseColIndex + 1, lastRowIndex, lastColIndex);            
        }
        // get the array of cells
        private Array GetTableOfCells(string sheetName, string baseCellName, int lastColumnIndex)
        {
            // select parameter definition sheet
            base.ActivateSheet(sheetName);

            // check if there is data on the first cell
            if (Convert.ToString(base.ReadCellFromActiveSheet(baseCellName, 1, 0)) == "")
                return (Array) (new double[0,0]);

            // read the entire parameter matrix            
            int firstRowIndex = base.RowIndex(baseCellName) + 1;
            int firstColIndex = base.ColIndex(baseCellName);
            int lastRowIndex = base.RowIndex(baseCellName, ExcelInteractor.enumRangeDirection.DownEnd); //base.LastRowWithDataInThisColumn(firstColIndex);
            int lastColIndex = firstColIndex + lastColumnIndex - 1;
            Array matrix = (Array)base.ReadObjectMatrixFromActiveSheet(firstRowIndex, firstColIndex, lastRowIndex, lastColIndex);

            return matrix;
        }
        private Array GetTableOfCells(string sheetName, string baseCellName)
        {
            // select parameter definition sheet
            base.ActivateSheet(sheetName);

            // check if there is data on the first cell
            if (Convert.ToString(base.ReadCellFromActiveSheet(baseCellName, 1, 0)) == "")
                return null;

            // read the entire parameter matrix            
            int firstRowIndex = base.RowIndex(baseCellName);
            int firstColIndex = base.ColIndex(baseCellName);
            int lastRowIndex = base.LastRowWithData(); //.LastRowWithDataInThisColumn(firstColIndex);
            int lastColIndex = base.LastColumnWithData();
            Array matrix = (Array)base.ReadObjectMatrixFromActiveSheet(firstRowIndex, firstColIndex, lastRowIndex, lastColIndex);

            return matrix;
        }      
        // crete one polynomial feature table
        private void CreateOnePolynomialFeatureTable(ref int rowIndex, int colIndex, string[] strFeatureAbbriviatedNames, int[,] polynomialTerms)
        {
            int numOfFeatures = strFeatureAbbriviatedNames.Length;

            // add table for polynomial degrees and estimates
            base.WriteToCell("Polynomial Degrees", rowIndex, colIndex);
            base.WriteToCell("Coefficients", rowIndex, colIndex + numOfFeatures + 2);
            base.WriteToCell("Estimates", rowIndex + 1, colIndex + numOfFeatures + 2);
            base.WriteToCell("Initial Values", rowIndex + 1, colIndex + numOfFeatures + 2 + 1);
            base.Align(rowIndex + 1, colIndex + numOfFeatures + 2, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.AddABorder(rowIndex + 1, colIndex + numOfFeatures + 2, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
            ++ rowIndex;

            // feature abbrivated names
            base.WriteToRow(strFeatureAbbriviatedNames, rowIndex, colIndex + 1);
            base.Align(rowIndex, colIndex + 1, enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.AddABorder(rowIndex, colIndex + 1, enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
            ++ rowIndex;

            // write polynomial terms
            base.WriteToMatrix(polynomialTerms, rowIndex, colIndex + 1);
            int numOfPolynomialTerms = polynomialTerms.GetLength(0);
            // clear #N/A 
            base.ClearAll(rowIndex, colIndex + 1 + numOfFeatures, ExcelInteractor.enumRangeDirection.DownEnd);
            base.Align(rowIndex, colIndex + 1, rowIndex + numOfPolynomialTerms, colIndex + 1 + numOfFeatures, ExcelInteractor.enumAlignment.Center);

            // write initial coefficients 
            double[] initialCoefficients = new double[numOfPolynomialTerms];
            SupportFunctions.MakeArrayEqualTo(ref initialCoefficients, 0);
            base.WriteToColumn(initialCoefficients, rowIndex, colIndex + 1 + numOfFeatures + 2);
            base.Align(rowIndex, colIndex + 1 + numOfFeatures + 2, ExcelInteractor.enumRangeDirection.DownEnd, ExcelInteractor.enumAlignment.Center);

            rowIndex += numOfPolynomialTerms;
        }
        // crete one polynomial feature table
        private void CreateOnePolynomialFeatureTable(ref int rowIndex, int colIndex, int numOfActions, string[] strFeatureAbbriviatedNames, int[,] polynomialTerms)
        {
            int numOfFeatures = strFeatureAbbriviatedNames.Length;

            // add table for polynomial degrees and estimates
            base.WriteToCell("Polynomial Degrees", rowIndex, colIndex);
            base.WriteToCell("Coefficients", rowIndex, colIndex + numOfFeatures + 2);
            base.WriteToCell("Estimates", rowIndex + 1, colIndex + numOfFeatures + 2);
            base.WriteToCell("Initial Values", rowIndex + 1, colIndex + numOfFeatures + 2 + 1);
            base.Align(rowIndex + 1, colIndex + numOfFeatures + 2, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.AddABorder(rowIndex + 1, colIndex + numOfFeatures + 2, ExcelInteractor.enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
            ++rowIndex;

            // feature abbrivated names
            base.WriteToRow(strFeatureAbbriviatedNames, rowIndex, colIndex + 1);
            base.Align(rowIndex, colIndex + 1, enumRangeDirection.RightEnd, ExcelInteractor.enumAlignment.Center);
            base.AddABorder(rowIndex, colIndex + 1, enumRangeDirection.RightEnd, ExcelInteractor.enumBorder.Bottom);
            ++rowIndex;

            for (int actionID = 0; actionID < numOfActions; actionID++)
            {
                // write polynomial terms
                base.WriteToMatrix(polynomialTerms, rowIndex, colIndex + 1);
                int numOfPolynomialTerms = polynomialTerms.GetLength(0);
                // clear #N/A 
                base.ClearAll(rowIndex, colIndex + 1 + numOfFeatures, ExcelInteractor.enumRangeDirection.DownEnd);
                base.Align(rowIndex, colIndex + 1, rowIndex + numOfPolynomialTerms, colIndex + 1 + numOfFeatures, ExcelInteractor.enumAlignment.Center);

                // write initial coefficients 
                double[] initialCoefficients = new double[numOfPolynomialTerms];
                SupportFunctions.MakeArrayEqualTo(ref initialCoefficients, 0);
                base.WriteToColumn(initialCoefficients, rowIndex, colIndex + 1 + numOfFeatures + 2);

                base.Align(rowIndex, colIndex + 1 + numOfFeatures + 2, ExcelInteractor.enumRangeDirection.DownEnd, ExcelInteractor.enumAlignment.Center);
                base.AddABorder(rowIndex + numOfPolynomialTerms - 1, colIndex + 1, rowIndex + numOfPolynomialTerms - 1, colIndex + numOfFeatures, enumBorder.Bottom);
                base.AddABorder(rowIndex + numOfPolynomialTerms - 1, colIndex + 1 + numOfFeatures + 1, rowIndex + numOfPolynomialTerms - 1, colIndex + 1 + numOfFeatures + 2, enumBorder.Bottom);

                rowIndex += numOfPolynomialTerms;
            }
        }
        #endregion

    }
}
