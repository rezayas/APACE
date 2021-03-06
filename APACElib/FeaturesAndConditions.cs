﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ComputationLib;
using RandomVariateLib;

namespace APACElib
{
    public abstract class Feature
    {
        public string Name { get; private set; }
        public int Index { get; private set; }
        public double? Value { get; protected set; }
        public double Min { get; private set; }
        public double Max { get; private set; }
        
        public Feature(string name, int index)
        {
            Name = name;
            Index = index;

            Min = double.MaxValue;
            Max = double.MinValue;
        }

        public abstract void Update(int epiTimeIndex, double deltaT);
        protected void UpdateMinMax()
        {
            if (Value > Max)
                Max = Value.GetValueOrDefault();
            if (Value < Min)
                Min = Value.GetValueOrDefault();
        }
    }

    public class Feature_EpidemicTime : Feature
    {
        public Feature_EpidemicTime(string name, int featureID) 
            : base(name, featureID)
        {            
        }

        public override void Update(int epiTimeIndex, double deltaT)
        {
            Value = epiTimeIndex;
            UpdateMinMax();
        }
    }

    public class Feature_SpecialStats: Feature
    {
        public enum EnumFeatureType
        {
            CurrentObservedValue = 0,
            Slope = 1
        }

        private EnumFeatureType _featureType;
        private SurveyedTrajectory _surveyedTraj; // pointer 
        private Parameter _multiplierPar;

        public Feature_SpecialStats(string name, int featureID, string strFeatureType, 
            SurveyedTrajectory surveyedTraj, Parameter multiplierPar) 
            : base(name, featureID)
        {
            switch (strFeatureType)
            {
                case "Current Observed Value":
                    _featureType = EnumFeatureType.CurrentObservedValue;
                    break;
                case "Slope":
                    _featureType = EnumFeatureType.Slope;
                    break;
            }
            _surveyedTraj = surveyedTraj;
            _multiplierPar = multiplierPar;
        }
        public Feature_SpecialStats(string name, int featureID, EnumFeatureType featureType, 
            SurveyedTrajectory surveyedTraj, Parameter multiplierPar)
            : base(name, featureID)
        {
            _featureType = featureType;
            _surveyedTraj = surveyedTraj;
            _multiplierPar = multiplierPar;
        }

        public override void Update(int epiTimeIndex, double deltaT)
        {
            switch (_featureType)
            {
                case EnumFeatureType.CurrentObservedValue:
                    Value = _surveyedTraj.GetLastObs() * _multiplierPar.Value;
                    break;

                case EnumFeatureType.Slope:
                    Value = _surveyedTraj.GetIncrementalChange(epiTimeIndex) * _multiplierPar.Value;
                    break;
            }

            UpdateMinMax();
        }
    }

    public class Feature_Intervention: Feature
    {
        public enum EnumFeatureType
        {
            IfEverSwitchedOff = 0,
            IfEverSwitchedOn = 1,
            SwitchStatus = 2,
            TimeSinceTurnedOn = 3,
            TimeSinceTurnedOff = 4
        }

        private Intervention _intervention; // pointer
        private EnumFeatureType _featureType;

        public Feature_Intervention(string name, int featureID, string strFeatureType, Intervention intervention): base(name, featureID)
        {
            switch (strFeatureType)
            {
                case "If Ever Switched Off":
                    _featureType = EnumFeatureType.IfEverSwitchedOff;
                    break;
                case "If Ever Switched On":
                    _featureType = EnumFeatureType.IfEverSwitchedOn;
                    break;
                case "Swich Status":
                    _featureType = EnumFeatureType.SwitchStatus;
                    break;
                case "Time Since Turned On":
                    _featureType = EnumFeatureType.TimeSinceTurnedOn;
                    break;
                case "Time Since Turned Off":
                    _featureType = EnumFeatureType.TimeSinceTurnedOff;
                    break;
                default:
                    throw new Exception("Invalid value for feature type defined on intervention.");
            }
            _intervention = intervention;
        }
        public Feature_Intervention(string name, int featureID, EnumFeatureType featureType, Intervention intervention) : base(name, featureID)
        {
            _featureType = featureType;
            _intervention = intervention;
        }

        public override void Update(int epiTimeIndex, double deltaT)
        {
            switch (_featureType)
            {
                case EnumFeatureType.IfEverSwitchedOff:
                    Value = Convert.ToDouble(_intervention.IfEverTurnedOffBefore);
                    break;
                case EnumFeatureType.IfEverSwitchedOn:
                    Value = Convert.ToDouble(_intervention.IfEverTurnedOnBefore);
                    break;
                case EnumFeatureType.SwitchStatus:
                    Value = _intervention.OnOffStatus;
                    break;
                case EnumFeatureType.TimeSinceTurnedOn:
                    Value = (epiTimeIndex - _intervention.EpiTimeIndexLastTurnedOn)*deltaT;
                    break;
                case EnumFeatureType.TimeSinceTurnedOff:
                    Value = (epiTimeIndex - _intervention.EpiTimeIndexLastTurnedOff)*deltaT;
                    break;
            }
        }
    }
    
    public abstract class Condition
    {
        public int ID { get; }
        public string Name { get; }
        public bool? Value { get; protected set; }
        public Condition(int id, string name)
        {
            ID = id;
            Name = name;
        }

        public abstract void Update(int epiTimeIndex, RNG rng);
    }

    public class Condition_AlwaysTrue : Condition
    {
        public Condition_AlwaysTrue(int id, string name) : base(id, name) { }

        public override void Update(int epiTimeIndex, RNG rng)
        {
            Value = true;
        }
    }

    public class Condition_AlwaysFalse : Condition
    {
        public Condition_AlwaysFalse(int id, string name) : base(id, name) { }

        public override void Update(int epiTimeIndex, RNG rng)
        {
            Value = false;
        }
    }

    public class Condition_OnFeatures : Condition
    {
        private List<Feature> _features = new List<Feature>();
        private List<Parameter> _thresholdParams = new List<Parameter>();

        private double[] _thresholdValues;
        private bool _ifTresholdSetOutside = false;
        private EnumSign[] _signs = new EnumSign[0];        
        private EnumAndOr _andOr = EnumAndOr.And;

        public Condition_OnFeatures(
            int id,
            string name,
            List<Feature> allFeatures,
            List<Parameter> allParameters,
            string strFeatureIDs,
            string strThresholdParIDs,
            string strSigns,            
            string strConclusions): base(id, name)
        {            

            int[] featureIDs = SupportProcedures.ConvertStringToIntArray(strFeatureIDs);
            int[] thresholdParamIDs = SupportProcedures.ConvertStringToIntArray(strThresholdParIDs);

            for (int i = 0; i < featureIDs.Length; i++)
                _features.Add(allFeatures[featureIDs[i]]);
            for (int i = 0; i < thresholdParamIDs.Length; i++)
                _thresholdParams.Add(allParameters[thresholdParamIDs[i]]);

            _signs = SupportProcedures.ConvertToEnumSigns(strSigns);
            _thresholdValues = new double[_thresholdParams.Count()];

            if (strConclusions == "And")
                _andOr = EnumAndOr.And;
            else
                _andOr = EnumAndOr.Or;
        }

        public Condition_OnFeatures(
            int id,
            string name,
            List<Feature> features,
            List<Parameter> thresholdParams,
            EnumSign[] signs,
            EnumAndOr conclusion) : base(id, name)
        {
            _features = features;
            _thresholdParams = thresholdParams;
            _signs = signs;
            _thresholdValues = new double[_thresholdParams.Count()];
            _andOr = conclusion;
        }

        public void UpdateThresholds(double[] values)
        {
            _thresholdValues = (double[])values.Clone();
            _ifTresholdSetOutside = true;
        }

        public override void Update(int epiTimeIndex, RNG rng)
        {
            bool? result = null;

            // update threshold values 
            if (_ifTresholdSetOutside == false)
            {
                for (int i = 0; i < _thresholdParams.Count(); i++)
                    _thresholdValues[i] = _thresholdParams[i].Value; // .Sample(epiTimeIndex, rng);
            }

            switch (_andOr)
            {
                case EnumAndOr.And:
                    {
                        for (int i = 0; i < _features.Count; i++)
                        {
                            if (_features[i].Value.HasValue)
                            {
                                result = true;  // all features hit thresholds
                                break;
                            }
                        }
                        for (int i = 0; i < _features.Count; i++)
                        {
                            // if one does not 
                            if (_features[i].Value.HasValue &&
                                !SupportProcedures.ValueOfComparison(
                                    _features[i].Value.Value, _signs[i], _thresholdValues[i]))                                
                            {
                                result = false;
                                break;
                            }
                        }                        
                    }
                    break;
                case EnumAndOr.Or:
                    {
                        for (int i = 0; i < _features.Count; i++)
                        {
                            if (_features[i].Value.HasValue)
                            {
                                result = false;  // no feature hits its threshold
                                break;
                            }
                        }
                        for (int i = 0; i < _features.Count; i++)
                        {
                            // if one is within
                            if (_features[i].Value.HasValue && 
                                SupportProcedures.ValueOfComparison(
                                    _features[i].Value.Value, _signs[i], _thresholdValues[i]))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                    break;
            }

            Value = result;
        }
    }

    public class Condition_OnConditions: Condition
    {
        private List<Condition> _conditions = new List<Condition>();
        private EnumAndOr _andOr = EnumAndOr.And;

        public Condition_OnConditions(
            int id,
            string name,
            List<Condition> allConditions,
            string strConditions,
            string strConclusions): base(id, name)
        {
            int[] conditionIDs = SupportProcedures.ConvertStringToIntArray(strConditions);
            for (int i = 0; i < conditionIDs.Length; i++)
                _conditions.Add(allConditions[conditionIDs[i]]);

            if (strConclusions == "And")
                _andOr = EnumAndOr.And;
            else
                _andOr = EnumAndOr.Or;
        }
        public Condition_OnConditions(
            int id,
            string name, 
            List<Condition> conditions,
            EnumAndOr conclusion) : base(id, name)
        {
            _conditions = conditions;
            _andOr = conclusion;
        }

        public override void Update(int epiTimeIndex, RNG rng)
        {
            bool? result = null; 

            switch (_andOr)
            {
                case EnumAndOr.And:
                    {
                        for (int i = 0; i < _conditions.Count; i++)
                        {
                            if (_conditions[i].Value.HasValue)
                            {
                                result = true;  // all conditions are satisifed
                                break;
                            }
                        }
                        for (int i = 0; i < _conditions.Count(); i++)
                        {
                            // if one conditions is not satisfied
                            if (_conditions[i].Value.HasValue && _conditions[i].Value == false)
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                    break;
                case EnumAndOr.Or:
                    {
                        for (int i = 0; i < _conditions.Count; i++)
                        {
                            if (_conditions[i].Value.HasValue)
                            {
                                result = false;  // no conditions is satisifed
                                break;
                            }
                        }                        
                        for (int i = 0; i < _conditions.Count(); i++)
                        {
                            // if one is satisifed
                            if (_conditions[i].Value.HasValue && _conditions[i].Value == true)
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                    break;
            }
            Value = result;
        }
    }
}
